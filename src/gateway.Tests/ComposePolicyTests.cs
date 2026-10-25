using Gateway.Tests.Support;
using Xunit;

namespace Gateway.Tests;

/// <summary>
/// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true removes the trusted-proxy limits of the ForwardedHeaders middleware, so any client could fake
/// its address and escape the rate limit (design D5). Many container images set it by default, hence the explicit check.
/// </summary>
public sealed class ComposePolicyTests
{
    private const string ForbiddenVariable = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";

    [Fact]
    public void DeploymentFiles_DoNotEnableForwardedHeadersForEveryProxy()
    {
        var src = Path.Combine(RepoRoot.Find(), "src");
        var files = new[] { "docker-compose*.y*ml", "compose*.y*ml", ".env*", "Dockerfile*" }
            .SelectMany(pattern => Directory.EnumerateFiles(src, pattern, SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .Distinct()
            .ToList();

        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.DoesNotContain(ForbiddenVariable, WithoutComments(file), StringComparison.OrdinalIgnoreCase));
    }

    // A comment that merely names the variable (for example to warn against it) must not fail the policy, whether it is a whole line
    // or follows a value ("KEY=value # note").
    private static string WithoutComments(string file) =>
        string.Join('\n', File.ReadLines(file)
            .Select(line => line.TrimStart().StartsWith('#') ? string.Empty : StripTrailingComment(line)));

    private static string StripTrailingComment(string line)
    {
        var index = line.IndexOf(" #", StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    private static bool IsBuildOutput(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
    }
}
