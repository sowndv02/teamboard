using Microsoft.Extensions.Options;

namespace IdentityService.Security.Passwords;

/// <summary>Guards PBKDF2 iteration count and password length bounds at startup.</summary>
public sealed class PasswordOptionsValidator : IValidateOptions<PasswordOptions>
{
    private const int MinimumIterations = 100_000;

    public ValidateOptionsResult Validate(string? name, PasswordOptions options)
    {
        if (options.Iterations < MinimumIterations)
        {
            return ValidateOptionsResult.Fail($"Password:Iterations must be at least {MinimumIterations}.");
        }

        if (options.MinLength < 12)
        {
            return ValidateOptionsResult.Fail("Password:MinLength must be at least 12.");
        }

        if (options.MaxLength < options.MinLength)
        {
            return ValidateOptionsResult.Fail("Password:MaxLength must be greater than or equal to MinLength.");
        }

        return ValidateOptionsResult.Success;
    }
}