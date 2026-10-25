namespace Teamboard.Shared.Http;

public static class CorrelationHeaders
{
    /// <summary>Header names per <c>api-endpoints.md</c> section 6.</summary>
    public const string Name = "X-Correlation-Id";

    /// <summary>Key in <c>HttpContext.Items</c> holding the validated correlation id.</summary>
    public const string ItemKey = "Teamboard.CorrelationId";
}
