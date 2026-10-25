using System.Diagnostics;

namespace Gateway.Tests.Support;

internal static class WaitFor
{
    /// <summary>Conditional wait (no fixed sleep): polls until the condition holds or the timeout expires.</summary>
    public static async Task<bool> UntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        return condition();
    }
}
