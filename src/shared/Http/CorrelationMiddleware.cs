using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Teamboard.Shared.Http;

/// <summary>
/// Gắn một correlation id đã kiểm cho mỗi request: giữ id hợp lệ của client, thay mọi giá trị khác bằng GUID mới.
/// Không bao giờ tin hay ghi lại giá trị thô của client (chống log injection).
/// </summary>
internal sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = ReadValidId(context) ?? Guid.NewGuid().ToString();

        context.Items[CorrelationHeaders.ItemKey] = id;
        // Ghi đè header request để YARP chuyển đúng id đã kiểm xuống upstream.
        context.Request.Headers[CorrelationHeaders.Name] = id;
        // OnStarting: UseExceptionHandler gọi Response.Clear() nên header đặt trực tiếp sẽ mất; callback này chạy sau khi upstream đã chép header.
        context.Response.OnStarting(static state =>
        {
            var (response, value) = ((HttpResponse, string))state;
            response.Headers[CorrelationHeaders.Name] = value;
            return Task.CompletedTask;
        }, (context.Response, id));

        Activity.Current?.SetTag("correlationId", id);
        using (logger.BeginScope(new Dictionary<string, object?> { ["correlationId"] = id }))
        {
            await next(context);
        }
    }

    // Duyệt từng ký tự thay vì regex: `$` của .NET khớp trước "\n" cuối chuỗi nên regex sẽ nhận nhầm "abc\n".
    private static string? ReadValidId(HttpContext context)
    {
        var values = context.Request.Headers[CorrelationHeaders.Name];
        if (values.Count != 1)
        {
            return null;
        }

        var value = values[0];
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return null;
        }

        foreach (var c in value)
        {
            var allowed = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-';
            if (!allowed)
            {
                return null;
            }
        }

        return value;
    }
}
