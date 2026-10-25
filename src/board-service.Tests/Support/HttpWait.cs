using System.Diagnostics;

namespace BoardService.Tests.Support;

internal static class HttpWait
{
    /// <summary>Conditional wait (no fixed sleep): retries until an HTTP response arrives or the deadline expires.</summary>
    public static async Task<int?> GetStatusAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout)
        {
            try
            {
                using var response = await client.GetAsync(url);
                return (int)response.StatusCode;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            catch (TaskCanceledException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        return null;
    }

    /// <summary>Waits until the response body satisfies the predicate or the deadline expires.</summary>
    public static async Task<string?> GetBodyUntilAsync(string url, Func<string, bool> condition, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout)
        {
            try
            {
                var body = await client.GetStringAsync(url);
                if (condition(body))
                {
                    return body;
                }
            }
            catch (HttpRequestException)
            {
                // Service not ready yet: retry.
            }
            catch (TaskCanceledException)
            {
                // Single call timed out: retry.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }
}
