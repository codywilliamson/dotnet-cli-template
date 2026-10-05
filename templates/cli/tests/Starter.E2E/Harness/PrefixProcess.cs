using System.Diagnostics;
using System.Text;

namespace Starter.E2E.Harness;

public sealed record CliRun(int ExitCode, string Stdout, byte[] StdoutBytes, string Stderr)
{
    public string[] Lines => Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public static class PrefixProcess
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    static readonly string[] AgentEnvVars = ["CLAUDECODE", "CODEX_SANDBOX", "CODEX_SANDBOX_NETWORK_DISABLED"];

    public static ProcessStartInfo StartInfo(string[] args)
    {
        var psi = new ProcessStartInfo(NativeExe.Path, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // the suite may itself run under an agent, which would change the output mode
        foreach (var name in AgentEnvVars)
        {
            psi.Environment.Remove(name);
        }
        return psi;
    }

    public static async Task<CliRun> RunAsync(params string[] args)
    {
        using var process = Process.Start(StartInfo(args))!;
        using var stdout = new MemoryStream();
        var copy = process.StandardOutput.BaseStream.CopyToAsync(stdout);
        var stderr = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(Timeout);
        await process.WaitForExitAsync(cts.Token);
        await copy;
        var bytes = stdout.ToArray();
        return new CliRun(process.ExitCode, Encoding.UTF8.GetString(bytes), bytes, await stderr);
    }
}
