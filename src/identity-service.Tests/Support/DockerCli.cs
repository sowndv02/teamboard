using System.Diagnostics;
using Xunit;

namespace IdentityService.Tests.Support;

/// <summary>Calls `docker compose` via Process for @docker tests (copied from gateway.Tests; extracting a shared project is tracked debt, see plan TB-010).</summary>
internal sealed class DockerCli
{
    public const string ProjectName = "tb021-tests";

    private readonly string _repoRoot = RepoRoot.Find();
    private bool _composeStarted;
    private string[] _composeFiles = [];
    private string? _envFile;

    /// <summary>Skips the scenario (not red) when the machine has no Docker daemon.</summary>
    public static void RequireDocker()
    {
        var result = Run(RepoRoot.Find(), TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        if (result.ExitCode != 0)
        {
            throw new SkipException("Docker daemon is not available; skipping docker tests.");
        }
    }

    public void UseFiles(params string[] composeFiles) => _composeFiles = composeFiles;

    /// <summary>The main compose file requires DB password variables; tests use the example file (sample values, not real passwords).</summary>
    public void UseEnvFile(string envFile) => _envFile = envFile;

    public CommandResult ComposeBuild(params string[] services) =>
        Compose(TimeSpan.FromMinutes(10), ["build", .. services]);

    /// <summary>Runs only the given services (no `--build`: the image is built in a separate step to measure startup time).</summary>
    public CommandResult ComposeUp(params string[] services)
    {
        _composeStarted = true;
        return Compose(TimeSpan.FromMinutes(5), ["up", "-d", .. services]);
    }

    /// <summary>Runs a command in the container of a running service (no TTY).</summary>
    public CommandResult ComposeExec(string service, params string[] command) =>
        Compose(TimeSpan.FromSeconds(30), ["exec", "-T", service, .. command]);

    public CommandResult Compose(TimeSpan timeout, params string[] args)
    {
        var arguments = new List<string> { "compose", "-p", ProjectName };
        if (_envFile is not null)
        {
            arguments.Add("--env-file");
            arguments.Add(_envFile);
        }

        foreach (var file in _composeFiles)
        {
            arguments.Add("-f");
            arguments.Add(file);
        }

        arguments.AddRange(args);
        return Run(_repoRoot, timeout, arguments.ToArray());
    }

    /// <summary>Cleans up containers and volumes of the test's own compose project (-v is safe since the project name is not shared); a leftover DB volume skews the password and data on the next run.</summary>
    public void ComposeDown()
    {
        if (_composeStarted)
        {
            Compose(TimeSpan.FromMinutes(2), "down", "-v", "--remove-orphans");
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
