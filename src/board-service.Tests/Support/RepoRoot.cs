namespace BoardService.Tests.Support;

internal static class RepoRoot
{
    /// <summary>Gốc repo: thư mục cha của thư mục chứa src/teamboard.slnx, tìm bằng cách đi ngược từ thư mục chạy test.</summary>
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

        throw new InvalidOperationException("Không tìm thấy gốc repo (src/teamboard.slnx).");
    }
}
