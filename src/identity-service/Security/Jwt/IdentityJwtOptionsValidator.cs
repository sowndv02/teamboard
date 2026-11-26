using Microsoft.Extensions.Options;

namespace IdentityService.Security.Jwt;

public sealed class IdentityJwtOptionsValidator : IValidateOptions<IdentityJwtOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityJwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Identity:Jwt:Issuer must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Identity:Jwt:Audience must not be empty.");
        }

        if (options.AccessTokenLifetimeSeconds < 1)
        {
            return ValidateOptionsResult.Fail("Identity:Jwt:AccessTokenLifetimeSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}