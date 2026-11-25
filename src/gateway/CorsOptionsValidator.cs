using Microsoft.Extensions.Options;

namespace Gateway;

/// <summary>Stub for TB-026 test-init: accepts every value, so the validation tests fail for the right reason.</summary>
public sealed class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options) => ValidateOptionsResult.Success;
}
