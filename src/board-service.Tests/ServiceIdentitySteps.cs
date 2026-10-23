using System.Reflection;
using System.Xml.Linq;
using BoardService.Tests.Support;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class ServiceIdentitySteps
{
    // Ghép từ hai nửa để chính tệp test này không chứa tên cũ và không tự bắt lỗi của mình.
    private static readonly string[] LegacyNames = ["teamboard" + "-api", "teamboard" + "_api"];

    // Thư mục lịch sử bất biến: issue, plan, workflow, lesson, ADR (theo docs-update-specs.md).
    private static readonly string[] HistoricalDirectories =
    [
        Path.Combine("document", "issues"),
        Path.Combine("document", "plans"),
        Path.Combine("document", "workflow"),
        Path.Combine("document", "lessons"),
        Path.Combine("document", "architecture", "adr"),
    ];

    private static readonly string[] IgnoredDirectoryNames = ["bin", "obj", ".vs", ".git", "node_modules"];

    private static readonly string[] ScannedExtensions =
    [
        ".cs", ".csproj", ".slnx", ".dcproj", ".yml", ".yaml", ".json", ".md", ".http", ".props", ".feature",
    ];

    private readonly DockerCli _docker = new();
    private Assembly? _assembly;
    private List<string> _violations = [];
    private CommandResult? _composeResult;

    [Given("Docker khả dụng")]
    public void GivenDocker() => DockerCli.RequireDocker();

    [When("tôi kiểm tra assembly của service")]
    public void WhenInspectAssembly() => _assembly = typeof(Program).Assembly;

    [When("tôi quét mã nguồn, cấu hình và tài liệu hiện hành")]
    public void WhenScan()
    {
        var root = RepoRoot.Find();
        var targets = new List<string>();
        foreach (var directory in new[] { "src", "config", "document" })
        {
            targets.AddRange(Enumerate(Path.Combine(root, directory), root));
        }

        foreach (var file in new[] { "PROJECT.md", "COMMANDS.md" })
        {
            targets.Add(Path.Combine(root, file));
        }

        _violations = targets
            .Where(File.Exists)
            .Where(path => LegacyNames.Any(name => File.ReadAllText(path).Contains(name, StringComparison.OrdinalIgnoreCase)))
            .Select(path => Path.GetRelativePath(root, path))
            .Order()
            .ToList();
    }

    [When("tôi chạy compose chính với tên dự án riêng")]
    public void WhenComposeUp()
    {
        _composeResult = _docker.ComposeUp(Path.Combine("src", "docker-compose.yml"));
        Assert.True(_composeResult.ExitCode == 0, $"docker compose up lỗi: {_composeResult.Error}");
    }

    [Then("tên assembly là {string}")]
    public void ThenAssemblyName(string expected) => Assert.Equal(expected, _assembly!.GetName().Name);

    [Then("namespace gốc của dự án service là {string}")]
    public void ThenRootNamespace(string expected)
    {
        const string projectPath = "src/board-service/board-service.csproj";
        var path = Path.Combine(RepoRoot.Find(), projectPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Không có dự án {projectPath}.");
        var rootNamespace = XDocument.Load(path).Descendants("RootNamespace").Select(e => e.Value).FirstOrDefault();
        Assert.Equal(expected, rootNamespace);
    }

    [Then("không tệp nào chứa tên cũ của service")]
    public void ThenNoLegacyName() =>
        Assert.True(_violations.Count == 0, "Còn tên cũ trong: " + string.Join(", ", _violations));

    [Then("danh sách service đang chạy gồm {string}")]
    public void ThenServiceRunning(string service)
    {
        var result = _docker.Compose(TimeSpan.FromMinutes(1), "ps", "--services", "--status", "running");
        var services = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Contains(service, services);
    }

    [Then("cổng host 6060 phản hồi một mã HTTP")]
    public async Task ThenPortResponds()
    {
        var status = await HttpWait.GetStatusAsync("http://127.0.0.1:6060/", TimeSpan.FromSeconds(60));
        Assert.NotNull(status);
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();

    private static IEnumerable<string> Enumerate(string directory, string root)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(file);
            if (name == "Dockerfile" || ScannedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            var relative = Path.GetRelativePath(root, child);
            if (IgnoredDirectoryNames.Contains(Path.GetFileName(child))
                || HistoricalDirectories.Any(h => relative.Equals(h, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var file in Enumerate(child, root))
            {
                yield return file;
            }
        }
    }
}
