using Microsoft.Extensions.Options;

namespace IdentityService.RateLimiting;

public sealed class IdentityRateLimitOptionsValidator : IValidateOptions<IdentityRateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityRateLimitOptions options)
    {
        if (options.PermitLimit < 1)
        {
            return ValidateOptionsResult.Fail("Identity:RateLimiting:PermitLimit must be at least 1.");
        }

        if (options.WindowSeconds < 1)
        {
            return ValidateOptionsResult.Fail("Identity:RateLimiting:WindowSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}