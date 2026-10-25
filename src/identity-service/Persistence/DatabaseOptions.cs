namespace IdentityService.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Chỉ bật ở local và compose; ở môi trường thật migration chạy bằng bước riêng (backend-engineering.md mục 7).</summary>
    public bool MigrateOnStartup { get; init; }
}
