namespace IdentityService.Security.Clients;

public sealed class ClientSecretOptions
{
    public const string SectionName = "Identity:Clients:SecretHash";

    public int Iterations { get; init; } = 600_000;
}