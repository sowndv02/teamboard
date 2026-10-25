using System.Diagnostics;
using Xunit;

namespace IdentityService.Tests.Support;

/// <summary>Gọi `docker compose` qua Process cho test nhóm @docker (bản sao từ gateway.Tests, ghi nợ tách project dùng chung; xem plan TB-010).</summary>
internal sealed class DockerCli
{
    public const string ProjectName = "tb021-tests";

    private readonly string _repoRoot = RepoRoot.Find();
    private bool _composeStarted;
    private string[] _composeFiles = [];
    private string? _envFile;

    /// <summary>Bỏ qua scenario (không phải đỏ) khi máy không có Docker daemon.</summary>
    public static void RequireDocker()
    {
        var result = Run(RepoRoot.Find(), TimeSpan.FromSeconds(30), "info", "--format", "{{.ServerVersion}}");
        if (result.ExitCode != 0)
        {
            throw new SkipException("Docker daemon không khả dụng; bỏ qua test nhóm docker.");
        }
    }

    public void UseFiles(params string[] composeFiles) => _composeFiles = composeFiles;

    /// <summary>Compose chính đòi biến mật khẩu DB bắt buộc; test dùng file mẫu (giá trị mẫu, không phải mật khẩu thật).</summary>
    public void UseEnvFile(string envFile) => _envFile = envFile;

    public CommandResult ComposeBuild(params string[] services) =>
        Compose(TimeSpan.FromMinutes(10), ["build", .. services]);

    /// <summary>Chạy chỉ các service được chỉ định (không `--build`: image đã build ở bước riêng để đo thời gian khởi động).</summary>
    public CommandResult ComposeUp(params string[] services)
    {
        _composeStarted = true;
        return Compose(TimeSpan.FromMinutes(5), ["up", "-d", .. services]);
    }

    /// <summary>Chạy lệnh trong container của một service đang chạy (không cấp TTY).</summary>
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

    /// <summary>Dọn container và volume của dự án compose riêng của test (-v an toàn vì tên dự án không dùng chung); volume DB còn sót làm lệch mật khẩu và dữ liệu ở lần chạy sau.</summary>
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
