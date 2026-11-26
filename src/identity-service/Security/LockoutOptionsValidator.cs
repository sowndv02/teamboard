using Microsoft.Extensions.Options;

namespace IdentityService.Security;

public sealed class LockoutOptionsValidator : IValidateOptions<LockoutOptions>
{
    public ValidateOptionsResult Validate(string? name, LockoutOptions options)
    {
        if (options.MaxFailures < 1)
        {
            return ValidateOptionsResult.Fail("Identity:Lockout:MaxFailures must be at least 1.");
        }

        if (options.LockoutDurationSeconds < 1)
        {
            return ValidateOptionsResult.Fail("Identity:Lockout:LockoutDurationSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}