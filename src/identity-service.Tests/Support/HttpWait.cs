using System.Diagnostics;

namespace IdentityService.Tests.Support;

internal static class HttpWait
{
    /// <summary>Conditional wait (no fixed sleep): retries until an HTTP response arrives or time runs out.</summary>
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

    /// <summary>Waits until the expected status code arrives (retries on another code or no connection) or time runs out; returns null on timeout.</summary>
    public static async Task<int?> GetStatusUntilAsync(string url, int expected, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout)
        {
            try
            {
                using var response = await client.GetAsync(url);
                if ((int)response.StatusCode == expected)
                {
                    return expected;
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

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }

    /// <summary>Waits until the response body satisfies the condition or time runs out.</summary>
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
