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
        var files = Directory.EnumerateFiles(src, "docker-compose*.yml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(src, ".env*", SearchOption.TopDirectoryOnly))
            .Concat(Directory.EnumerateFiles(src, "Dockerfile", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.DoesNotContain(ForbiddenVariable, File.ReadAllText(file), StringComparison.OrdinalIgnoreCase));
    }
}
