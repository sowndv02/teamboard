using Gateway.Tests.Support;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gateway.Tests;

/// <summary>
/// A client that gives up (plan P-02): the gateway writes no response, logs at Information and does not count it as an upstream failure.
/// Host-level test without a feature file because the behavior is internal (the client is gone), not something a caller observes.
/// </summary>
public sealed class ClientCancellationTests
{
    [Fact]
    public async Task ClientGivesUpWhileUpstreamHangs_IsLoggedAsInformationAndNotCounted()
    {
        await using var context = new GatewayContext();
        context.UpstreamOrNull = await UpstreamStub.StartAsync(behavior: UpstreamBehavior.HangBeforeHeaders);
        context.StartGateway();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.SendAsync("GET", "/api/v1/boards", cancellationToken: cancellation.Token));

        var logged = await WaitFor.UntilAsync(
            () => context.Logs.Entries.Any(e => e.Level == LogLevel.Information && e.Category.StartsWith("Gateway.", StringComparison.Ordinal) && e.Scopes.Contains("correlationId=")),
            TimeSpan.FromSeconds(5));
        Assert.True(logged, "The gateway did not log the client cancellation at Information in a correlation scope.");
        Assert.DoesNotContain(context.Logs.Entries, e => e.Level >= LogLevel.Warning && e.Category.StartsWith("Gateway.", StringComparison.Ordinal));
        Assert.Equal(0, context.AbortedCount);
    }
}
