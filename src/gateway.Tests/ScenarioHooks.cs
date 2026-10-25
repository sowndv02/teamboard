using Reqnroll;

namespace Gateway.Tests;

[Binding]
public sealed class ScenarioHooks(GatewayContext context)
{
    // Reqnroll does not call DisposeAsync on injected objects: without this hook every scenario leaks its gateway,
    // fake upstream (a hanging one never ends) and MeterListener into the next scenario.
    [AfterScenario(Order = 1000)]
    public async Task DisposeContext() => await context.DisposeAsync();
}
