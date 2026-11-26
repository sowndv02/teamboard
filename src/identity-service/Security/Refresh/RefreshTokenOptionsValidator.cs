using Microsoft.Extensions.Options;

namespace IdentityService.Security.Refresh;

public sealed class RefreshTokenOptionsValidator : IValidateOptions<RefreshTokenOptions>
{
    public ValidateOptionsResult Validate(string? name, RefreshTokenOptions options)
    {
        if (options.LifetimeSeconds < 60)
        {
            return ValidateOptionsResult.Fail("Identity:RefreshToken:LifetimeSeconds must be at least 60.");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultClientId))
        {
            return ValidateOptionsResult.Fail("Identity:RefreshToken:DefaultClientId is required.");
        }

        return ValidateOptionsResult.Success;
    }
}