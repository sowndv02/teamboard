namespace Teamboard.Shared.Http;

public static class CorrelationHeaders
{
    /// <summary>Tên header theo <c>api-endpoints.md</c> mục 6.</summary>
    public const string Name = "X-Correlation-Id";

    /// <summary>Khoá trong <c>HttpContext.Items</c> chứa correlation id đã kiểm.</summary>
    public const string ItemKey = "Teamboard.CorrelationId";
}
