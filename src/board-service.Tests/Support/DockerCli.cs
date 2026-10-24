using System.Diagnostics;
using Xunit;

namespace BoardService.Tests.Support;

/// <summary>Gọi `docker compose` qua Process cho test nhóm @docker (không thêm phụ thuộc, xem plan TB-001).</summary>
internal sealed class DockerCli
{
    public const string ProjectName = "tb001-tests";

    private readonly string _repoRoot = RepoRoot.Find();
    private bool _composeStarted;
    private string[] _composeFiles = [];

    /// <summary>Bỏ qua scenario (không phải đỏ) khi máy không có Docker daemon.</summary>
    public static void RequireDocker()
    {
        var result = Run(RepoRoot.Find(), TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        if (result.ExitCode != 0)
        {
            throw new SkipException("Docker daemon không khả dụng; bỏ qua test nhóm docker.");
        }
    }

    /// <summary>Chạy chỉ các service được chỉ định, để test không kéo theo service khác trong cùng file compose (xem plan TB-010).</summary>
    public CommandResult ComposeUp(string[] composeFiles, params string[] services)
    {
        _composeFiles = composeFiles;
        _composeStarted = true;
        return Compose(TimeSpan.FromMinutes(10), ["up", "--build", "-d", .. services]);
    }

    public CommandResult Compose(TimeSpan timeout, params string[] args)
    {
        var arguments = new List<string> { "compose", "-p", ProjectName };
        foreach (var file in _composeFiles)
        {
            arguments.Add("-f");
            arguments.Add(file);
        }

        arguments.AddRange(args);
        return Run(_repoRoot, timeout, arguments.ToArray());
    }

    /// <summary>Dọn container của test; không dùng -v để không xoá volume.</summary>
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
                return new CommandResult(-1, string.Empty, $"Quá thời gian chờ {timeout}.");
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
