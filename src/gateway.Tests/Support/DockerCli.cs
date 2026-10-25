using System.Diagnostics;
using Xunit;

namespace Gateway.Tests.Support;

/// <summary>Runs `docker compose` through Process for @docker tests (copied from board-service.Tests; splitting out a shared project is tracked as debt, see plan TB-010).</summary>
internal sealed class DockerCli
{
    private readonly string _projectName;
    private readonly string _repoRoot = RepoRoot.Find();

    private bool _composeStarted;
    private string[] _composeFiles = [];

    public DockerCli(string projectName = "tb010-tests") => _projectName = projectName;

    /// <summary>Skip the scenario (not fail it) when the machine has no Docker daemon.</summary>
    public static void RequireDocker()
    {
        var result = Run(RepoRoot.Find(), TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        if (result.ExitCode != 0)
        {
            throw new SkipException("Docker daemon is not available; skipping docker-group tests.");
        }
    }

    public void UseFiles(params string[] composeFiles) => _composeFiles = composeFiles;

    public CommandResult ComposeBuild(params string[] services) =>
        Compose(TimeSpan.FromMinutes(10), ["build", .. services]);

    /// <summary>Runs only the given services (no `--build`: the image is built in a separate step so startup time can be measured).</summary>
    public CommandResult ComposeUp(params string[] services)
    {
        _composeStarted = true;
        return Compose(TimeSpan.FromMinutes(5), ["up", "-d", .. services]);
    }

    public CommandResult Compose(TimeSpan timeout, params string[] args)
    {
        // The main compose file requires variables for identity-service/postgres (TB-021); the example file is enough for compose to parse, and those services do not run here.
        var arguments = new List<string> { "compose", "-p", _projectName, "--env-file", "src/.env.example" };
        foreach (var file in _composeFiles)
        {
            arguments.Add("-f");
            arguments.Add(file);
        }

        arguments.AddRange(args);
        return Run(_repoRoot, timeout, arguments.ToArray());
    }

    /// <summary>Clean up the test containers; no -v so volumes are not deleted.</summary>
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
