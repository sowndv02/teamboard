using System.Diagnostics;

namespace Gateway.Tests.Support;

internal static class HttpWait
{
    /// <summary>Chờ có điều kiện (không ngủ cố định): thử lại tới khi có phản hồi HTTP hoặc hết hạn.</summary>
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

    /// <summary>Chờ tới khi nhận đúng mã trạng thái mong đợi (thử lại khi mã khác hoặc chưa kết nối) hoặc hết hạn; trả null nếu hết hạn.</summary>
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
                // Dịch vụ chưa sẵn sàng: thử lại.
            }
            catch (TaskCanceledException)
            {
                // Hết thời gian một lần gọi: thử lại.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }

    /// <summary>Chờ tới khi nội dung phản hồi thoả điều kiện hoặc hết hạn.</summary>
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
                // Dịch vụ chưa sẵn sàng: thử lại.
            }
            catch (TaskCanceledException)
            {
                // Hết thời gian một lần gọi: thử lại.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }
}
