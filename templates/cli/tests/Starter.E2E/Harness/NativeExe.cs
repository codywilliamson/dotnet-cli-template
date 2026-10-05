using System.Diagnostics;

namespace Starter.E2E.Harness;

// set STARTER_EXE to reuse an existing build instead of publishing
public static class NativeExe
{
    public static string Path { get; private set; } = "";

    [Before(TestSession)]
    public static async Task Publish()
    {
        if (Environment.GetEnvironmentVariable("STARTER_EXE") is { Length: > 0 } existing)
        {
            Path = System.IO.Path.GetFullPath(existing);
            return;
        }

        var repo = RepoRoot();
        var output = System.IO.Path.Combine(repo, "artifacts", "e2e");
        var psi = new ProcessStartInfo("dotnet", ["publish", System.IO.Path.Combine(repo, "src", "Starter"), "-c", "Release", "-o", output])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // aot link needs vswhere.exe on PATH
        var vsInstaller = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");
        if (OperatingSystem.IsWindows() && Directory.Exists(vsInstaller))
        {
            psi.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH") + System.IO.Path.PathSeparator + vsInstaller;
        }

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"native publish failed ({process.ExitCode}):\n{await stdout}\n{await stderr}");
        }

        Path = System.IO.Path.Combine(output, OperatingSystem.IsWindows() ? "starter.exe" : "starter");
    }

    static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "Starter.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("cannot find Starter.slnx above " + AppContext.BaseDirectory);
    }
}
