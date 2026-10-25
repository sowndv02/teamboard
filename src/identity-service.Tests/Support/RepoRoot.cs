namespace IdentityService.Tests.Support;

internal static class RepoRoot
{
    /// <summary>Repo root: parent of the directory containing src/teamboard.slnx, found by walking up from the test run directory.</summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "teamboard.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repo root (src/teamboard.slnx) not found.");
    }
}
