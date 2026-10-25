using System.Xml.Linq;
using BoardService.Tests.Support;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class RepositoryHygieneSteps
{
    private readonly List<string> _violations = [];

    [When("I read the package declarations of the .NET projects")]
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

    [When("I read the compose files of the repo")]
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

    [Then("every package has an exact version, with no ranges or wildcards")]
    public void ThenPinned() => Assert.True(_violations.Count == 0, "Unpinned packages: " + string.Join("; ", _violations));

    [Then("every password, key or token value only references an environment variable")]
    public void ThenNoSecrets() => Assert.True(_violations.Count == 0, "Hard-coded secret at: " + string.Join(", ", _violations));

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
}
