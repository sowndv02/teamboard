namespace Teamboard.Shared.Auth;

/// <summary>Validates access tokens issued by identity-service (stub until TB-033 coding-start).</summary>
public sealed class TeamboardJwtTokenValidator
{
    public Task<TeamboardJwtValidationResult> ValidateAccessTokenAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        _ = bearerToken;
        _ = cancellationToken;
        return Task.FromResult(new TeamboardJwtValidationResult(false, "not_implemented"));
    }
}