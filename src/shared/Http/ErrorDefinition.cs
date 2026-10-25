namespace Teamboard.Shared.Http;

/// <summary>A stable API error code with its HTTP status and default message (English, no internal details leaked).</summary>
public sealed record ErrorDefinition(string Code, int Status, string Message);
