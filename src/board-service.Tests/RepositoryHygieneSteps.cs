using System.Xml.Linq;
using BoardService.Tests.Support;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class RepositoryHygieneSteps
{
    private readonly List<string> _violations = [];

    [When("tôi đọc khai báo gói của các dự án .NET")]
    public void WhenReadPackages()
    {
        var src = Path.Combine(RepoRoot.Find(), "src");
        foreach (var project in Directory.EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path)))
        {
            foreach (var reference in XDocument.Load(project).Descendants("PackageReference"))
            {
                var version = (string?)reference.Attribute("Version") ?? (string?)reference.Attribute("VersionOverride");
                if (!RepositoryPolicies.IsPinnedVersion(version))
                {
                    _violations.Add($"{Path.GetRelativePath(src, project)}: {(string?)reference.Attribute("Include")} = '{version}'");
                }
            }
        }
    }

    [When("tôi đọc các tệp compose của repo")]
    public void WhenReadCompose()
    {
        var src = Path.Combine(RepoRoot.Find(), "src");
        foreach (var file in Directory.EnumerateFiles(src, "docker-compose*.yml"))
        {
            foreach (var line in File.ReadAllLines(file).Select((text, index) => (text, index)))
            {
                if (RepositoryPolicies.IsHardcodedSecret(line.text))
                {
                    _violations.Add($"{Path.GetFileName(file)}:{line.index + 1}");
                }
            }
        }
    }

    [Then("mọi gói có phiên bản chính xác, không dùng dải hay dấu sao")]
    public void ThenPinned() => Assert.True(_violations.Count == 0, "Gói chưa pin: " + string.Join("; ", _violations));

    [Then("mọi giá trị mật khẩu, khoá hay token chỉ tham chiếu biến môi trường")]
    public void ThenNoSecrets() => Assert.True(_violations.Count == 0, "Có bí mật viết cứng tại: " + string.Join(", ", _violations));

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
}
