namespace Teamboard.Shared.Auth;

public sealed record TeamboardJwtValidationResult(bool IsValid, string? FailureReason);