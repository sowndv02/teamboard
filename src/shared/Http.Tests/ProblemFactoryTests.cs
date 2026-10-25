using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Teamboard.Shared.Http.Tests;

public sealed class ProblemFactoryTests
{
    private static readonly ErrorDefinition RouteNotFound = new("ROUTE_NOT_FOUND", 404, "No route matches the request.");

    [Fact]
    public void Create_KnownError_HasAllProblemFields()
    {
        using var source = new ActivitySource("Teamboard.Shared.Http.Tests");
        using var listener = ListenToAll();
        using var activity = source.StartActivity("request")!;

        var problem = ProblemFactory.Create(RouteNotFound, new DefaultHttpContext());

        Assert.Equal(404, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.StartsWith("https://teamboard/errors/", problem.Type);
        Assert.Equal("No route matches the request.", problem.Detail);
        Assert.Equal("ROUTE_NOT_FOUND", problem.Extensions["code"]);
        Assert.Equal(activity.Id, problem.Extensions["traceId"]);
    }

    [Fact]
    public void Create_WithActivity_CorrelationIdEqualsTraceId()
    {
        using var source = new ActivitySource("Teamboard.Shared.Http.Tests");
        using var listener = ListenToAll();
        using var activity = source.StartActivity("request")!;

        var problem = ProblemFactory.Create(RouteNotFound, new DefaultHttpContext());

        Assert.Equal(problem.Extensions["traceId"], problem.Extensions["correlationId"]);
    }

    [Fact]
    public void Create_NoActivity_FallsBackToTraceIdentifier()
    {
        Assert.Null(Activity.Current);
        var context = new DefaultHttpContext { TraceIdentifier = "0HN-test-trace" };

        var problem = ProblemFactory.Create(RouteNotFound, context);

        Assert.Equal("0HN-test-trace", problem.Extensions["traceId"]);
        Assert.Equal("0HN-test-trace", problem.Extensions["correlationId"]);
    }

    [Fact]
    [Trait("Scenario", "S04")]
    public void Create_ContextHasCorrelationId_UsesItAndKeepsTraceId()
    {
        using var source = new ActivitySource("Teamboard.Shared.Http.Tests");
        using var listener = ListenToAll();
        using var activity = source.StartActivity("request")!;
        var context = new DefaultHttpContext();
        context.Items[CorrelationHeaders.ItemKey] = "client-id-1";

        var problem = ProblemFactory.Create(RouteNotFound, context);

        Assert.Equal("client-id-1", problem.Extensions["correlationId"]);
        Assert.Equal(activity.Id, problem.Extensions["traceId"]);
    }

    [Fact]
    public void Create_DetailOverride_UsesProvidedDetail()
    {
        var problem = ProblemFactory.Create(RouteNotFound, new DefaultHttpContext(), "Custom detail.");

        Assert.Equal("Custom detail.", problem.Detail);
    }

    private static ActivityListener ListenToAll()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
