using System.Diagnostics;
using Xunit;

namespace BoardService.Tests.Support;

/// <summary>Calls `docker compose` via Process for @docker tests (no extra dependency, see plan TB-001).</summary>
internal sealed class DockerCli
{
    public const string ProjectName = "tb001-tests";

    private readonly string _repoRoot = RepoRoot.Find();
    private bool _composeStarted;
    private string[] _composeFiles = [];

    /// <summary>Skips the scenario (not red) when the machine has no Docker daemon.</summary>
    public static void RequireDocker()
    {
        var result = Run(RepoRoot.Find(), TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        if (result.ExitCode != 0)
        {
            throw new SkipException("Docker daemon is not available; skipping docker tests.");
        }
    }

    /// <summary>Runs only the given services, so the test does not pull in other services from the same compose file (see plan TB-010).</summary>
    public CommandResult ComposeUp(string[] composeFiles, params string[] services)
    {
        _composeFiles = composeFiles;
        _composeStarted = true;
        return Compose(TimeSpan.FromMinutes(10), ["up", "--build", "-d", .. services]);
    }

    public CommandResult Compose(TimeSpan timeout, params string[] args)
    {
        // The main compose has required variables for identity-service/postgres (TB-021); the example file is enough for compose to parse, and those services do not run here.
        var arguments = new List<string> { "compose", "-p", ProjectName, "--env-file", "src/.env.example" };
        foreach (var file in _composeFiles)
        {
            arguments.Add("-f");
            arguments.Add(file);
        }

        arguments.AddRange(args);
        return Run(_repoRoot, timeout, arguments.ToArray());
    }

    /// <summary>Cleans up test containers; no -v so volumes are not deleted.</summary>
    public void ComposeDown()
    {
        if (_composeStarted)
        {
            Compose(TimeSpan.FromMinutes(2), "down", "--remove-orphans");
            _composeStarted = false;
        }
    }

    private static CommandResult Run(string workingDirectory, TimeSpan timeout, params string[] arguments)
    {
        var info = new ProcessStartInfo("docker")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeout))
            {
                process.Kill(entireProcessTree: true);
                return new CommandResult(-1, string.Empty, $"Timed out after {timeout}.");
            }

            return new CommandResult(process.ExitCode, output.Result, error.Result);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return new CommandResult(-1, string.Empty, exception.Message);
        }
    }
}

internal sealed record CommandResult(int ExitCode, string Output, string Error);
