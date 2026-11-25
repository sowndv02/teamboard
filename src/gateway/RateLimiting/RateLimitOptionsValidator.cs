using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Gateway.RateLimiting;

/// <summary>Applies data annotations to nested read/write groups because the root options type has no attributes on those properties.</summary>
public sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        var failures = new List<string>();
        CollectValidationFailures(options.Read, failures);
        CollectValidationFailures(options.Write, failures);
        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    private static void CollectValidationFailures(RateLimitGroupOptions group, List<string> failures)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(group, new ValidationContext(group), results, validateAllProperties: true))
        {
            failures.AddRange(results.Select(static result => result.ErrorMessage ?? "Invalid rate limit configuration."));
        }
    }
}
