namespace IdentityService.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Enabled only locally and in compose; in real environments migrations run as a separate step (backend-engineering.md section 7).</summary>
    public bool MigrateOnStartup { get; init; }
}
