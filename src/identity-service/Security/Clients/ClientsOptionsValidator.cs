using Microsoft.Extensions.Options;

namespace IdentityService.Security.Clients;

public sealed class ClientsOptionsValidator : IValidateOptions<ClientsOptions>
{
    public ValidateOptionsResult Validate(string? name, ClientsOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        foreach (var seed in options.Seed)
        {
            if (string.IsNullOrWhiteSpace(seed.ClientId))
            {
                return ValidateOptionsResult.Fail("Identity:Clients:Seed entries require ClientId.");
            }

            if (seed.ClientId.Length > 64)
            {
                return ValidateOptionsResult.Fail("Identity:Clients:Seed ClientId must be at most 64 characters.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}