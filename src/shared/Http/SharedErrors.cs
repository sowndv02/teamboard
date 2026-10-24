namespace Teamboard.Shared.Http;

/// <summary>Bảng mã lỗi dùng chung cho mọi service. Mã riêng của service nằm ở bảng của service đó, cùng kiểu <see cref="ErrorDefinition"/>.</summary>
public static class SharedErrors
{
    public static readonly ErrorDefinition ValidationFailed = new("VALIDATION_FAILED", 400, "One or more fields are invalid.");

    public static readonly ErrorDefinition NotFound = new("NOT_FOUND", 404, "The requested resource was not found.");

    public static readonly ErrorDefinition Conflict = new("CONFLICT", 409, "The request conflicts with the current state of the resource.");

    public static readonly ErrorDefinition InternalError = new("INTERNAL_ERROR", 500, "An unexpected error occurred.");

    public static IReadOnlyList<ErrorDefinition> All { get; } = [ValidationFailed, NotFound, Conflict, InternalError];
}
