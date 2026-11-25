using System.Diagnostics.Metrics;
using System.Net;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

/// <summary>State shared between the step classes of one scenario (Reqnroll injects it per scenario).</summary>
public sealed class GatewayContext : IAsyncDisposable
{
    private const string UpstreamAddressKey = "ReverseProxy:Clusters:board-service:Destinations:d1:Address";
    private const string AbortedInstrumentName = "gateway_upstream_aborted";

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private MeterListener? _meterListener;
    private long _abortedCount;
    private readonly Dictionary<string, long> _counterTotals = new(StringComparer.Ordinal);
    private long _requestDurationSamples;
    private long _overheadDurationSamples;

    internal UpstreamStub? UpstreamOrNull { get; set; }

    internal UpstreamStub Upstream => UpstreamOrNull ?? throw new InvalidOperationException("No fake upstream yet.");

    public HttpResponseMessage? Response { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string Method { get; private set; } = string.Empty;

    public string Path { get; private set; } = string.Empty;

    /// <summary>The X-Correlation-Id value the test sent; null means the header was not sent.</summary>
    public string? SentCorrelationId { get; set; }

    /// <summary>Configuration overrides applied when the gateway starts (timeouts, Retry-After...).</summary>
    public Dictionary<string, string?> Settings { get; } = new();

    /// <summary>When true the gateway runs with a cluster that has no destination at all.</summary>
    internal bool WithoutDestinations { get; set; }

    internal CapturingLoggerProvider Logs { get; } = new();

    /// <summary>When set, a middleware in front of the whole gateway pipeline adds this header to every response (stands in for CORS, slice 6).</summary>
    internal (string Name, string Value)? OuterResponseHeader { get; set; }

    /// <summary>Error raised while reading the response body (for example when the gateway cuts the connection after the headers).</summary>
    public Exception? BodyReadError { get; private set; }

    /// <summary>Sum of the measurements of the gateway_upstream_aborted counter seen since the gateway started.</summary>
    public long AbortedCount => Interlocked.Read(ref _abortedCount);

    /// <summary>Sum of measurements for a named counter instrument on the gateway meter.</summary>
    public long CounterTotal(string instrumentName) =>
        _counterTotals.TryGetValue(instrumentName, out var total) ? total : 0;

    /// <summary>Number of histogram samples recorded for gateway_request_duration_seconds.</summary>
    public long RequestDurationSamples => Interlocked.Read(ref _requestDurationSamples);

    /// <summary>Number of histogram samples recorded for gateway_overhead_duration_seconds.</summary>
    public long OverheadDurationSamples => Interlocked.Read(ref _overheadDurationSamples);

    /// <summary>When true the gateway listens on a real Kestrel socket: TestServer has no request body size feature (plan TB-025 spike).</summary>
    internal bool UseRealKestrel { get; set; }

    internal void StartGateway(bool forceUnhandledError = false)
    {
        StartMeterListener();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // The upstream address comes from configuration (NFR-004), not hardcoded in the gateway.
            if (!WithoutDestinations)
            {
                builder.UseSetting(UpstreamAddressKey, Upstream.Address);
            }

            foreach (var (key, value) in Settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureLogging(logging => logging.AddProvider(Logs));
            // Tests choose the client address per request with the X-Test-Remote-Ip header; TestServer has no real socket address.
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, TestRemoteIpStartupFilter>());
            if (OuterResponseHeader is { } outer)
            {
                builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new OuterHeaderStartupFilter(outer.Name, outer.Value)));
            }

            builder.ConfigureTestServices(services =>
            {
                if (forceUnhandledError)
                {
                    // Force an unexpected error in the forwarding layer to exercise the ExceptionHandler (plan A-03).
                    services.AddSingleton<IHttpForwarder, ThrowingForwarder>();
                }

                if (WithoutDestinations)
                {
                    services.RemoveAll<IProxyConfigProvider>();
                    services.AddSingleton<IProxyConfigProvider>(new InMemoryConfigProvider(
                        [
                            new RouteConfig
                            {
                                RouteId = "boards-read",
                                ClusterId = "board-service",
                                Match = new RouteMatch { Path = "/api/v1/boards/{**catch-all}", Methods = ["GET", "HEAD"] },
                            },
                        ],
                        [
                            new ClusterConfig
                            {
                                ClusterId = "board-service",
                                LoadBalancingPolicy = "PowerOfTwoChoices",
                                Destinations = new Dictionary<string, DestinationConfig>(),
                            },
                        ]));
                }
            });
        });
        if (UseRealKestrel)
        {
            _factory.UseKestrel(0);
            _factory.StartServer();
        }

        _client = _factory.CreateClient();
        // A safety net far above every timeout used in the tests, so a missing gateway timeout fails instead of hanging.
        _client.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task SendAsync(string method, string path, Action<HttpRequestMessage>? configure = null, CancellationToken cancellationToken = default)
    {
        (Method, Path) = (method, path);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        configure?.Invoke(request);
        Response = await _client!.SendAsync(request, cancellationToken);
        Body = await Response.Content.ReadAsStringAsync();
    }

    /// <summary>Returns as soon as the headers arrive, then reads the body and keeps the read error (if any) instead of throwing.</summary>
    public async Task SendAndReadBodyAsync(string method, string path)
    {
        (Method, Path) = (method, path);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        Response = await _client!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        try
        {
            Body = await Response.Content.ReadAsStringAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
        {
            BodyReadError = exception;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _meterListener?.Dispose();
        Response?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        if (UpstreamOrNull is not null)
        {
            await UpstreamOrNull.DisposeAsync();
        }
    }

    private void StartMeterListener()
    {
        // Listens by instrument name only, so the test does not depend on the meter name or on the counter's numeric type.
        _counterTotals.Clear();
        Interlocked.Exchange(ref _requestDurationSamples, 0);
        Interlocked.Exchange(ref _overheadDurationSamples, 0);
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (IsObservedInstrument(instrument.Name))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) => RecordMeasurement(instrument, measurement));
        _meterListener.SetMeasurementEventCallback<int>((instrument, measurement, _, _) => RecordMeasurement(instrument, measurement));
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) => RecordHistogramSample(instrument.Name, measurement));
        _meterListener.Start();
    }

    private static bool IsObservedInstrument(string name) => name switch
    {
        AbortedInstrumentName => true,
        "gateway_http_429" or "gateway_http_502" or "gateway_http_503" or "gateway_http_504" => true,
        "gateway_request_duration_seconds" or "gateway_overhead_duration_seconds" => true,
        _ => false,
    };

    private void RecordMeasurement(Instrument instrument, long measurement)
    {
        if (instrument.Name == AbortedInstrumentName)
        {
            Interlocked.Add(ref _abortedCount, measurement);
            return;
        }

        lock (_counterTotals)
        {
            _counterTotals[instrument.Name] = _counterTotals.GetValueOrDefault(instrument.Name) + measurement;
        }
    }

    private void RecordHistogramSample(string instrumentName, double _)
    {
        if (instrumentName == "gateway_request_duration_seconds")
        {
            Interlocked.Increment(ref _requestDurationSamples);
        }
        else if (instrumentName == "gateway_overhead_duration_seconds")
        {
            Interlocked.Increment(ref _overheadDurationSamples);
        }
    }

    private sealed class ThrowingForwarder : IHttpForwarder
    {
        public ValueTask<ForwarderError> SendAsync(HttpContext context, string destinationPrefix, HttpMessageInvoker httpClient, ForwarderRequestConfig requestConfig, HttpTransformer transformer) =>
            throw new InvalidOperationException("secret-forwarder-failure");

        public ValueTask<ForwarderError> SendAsync(HttpContext context, string destinationPrefix, HttpMessageInvoker httpClient, ForwarderRequestConfig requestConfig, HttpTransformer transformer, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("secret-forwarder-failure");
    }
}

/// <summary>Adds a header with a plain assignment before the rest of the pipeline runs, the way a CORS middleware does.</summary>
internal sealed class OuterHeaderStartupFilter(string name, string value) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Response.Headers[name] = value;
            return nextMiddleware(context);
        });
        next(app);
    };
}
