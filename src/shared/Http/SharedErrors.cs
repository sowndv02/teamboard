namespace Teamboard.Shared.Http;

/// <summary>Error codes shared by every service. Service-specific codes live in that service's own table, using the same <see cref="ErrorDefinition"/> type.</summary>
public static class SharedErrors
{
    public static readonly ErrorDefinition ValidationFailed = new("VALIDATION_FAILED", 400, "One or more fields are invalid.");

    public static readonly ErrorDefinition NotFound = new("NOT_FOUND", 404, "The requested resource was not found.");

    public static readonly ErrorDefinition Conflict = new("CONFLICT", 409, "The request conflicts with the current state of the resource.");

    public static readonly ErrorDefinition InternalError = new("INTERNAL_ERROR", 500, "An unexpected error occurred.");

    public static IReadOnlyList<ErrorDefinition> All { get; } = [ValidationFailed, NotFound, Conflict, InternalError];
}
