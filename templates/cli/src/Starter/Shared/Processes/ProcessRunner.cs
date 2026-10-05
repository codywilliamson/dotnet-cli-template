using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Starter.Shared.Processes;

public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Succeeded => ExitCode == 0;

    public string Tail(int maxChars = 1500)
    {
        var text = (string.IsNullOrWhiteSpace(Stderr) ? Stdout : Stderr).Trim();
        return text.Length <= maxChars ? text : "…" + text[^maxChars..];
    }
}

// argv only, never a shell, so paths and names can't be word-split or injected into
public static class ProcessRunner
{
    public const int NOT_FOUND_EXIT_CODE = 127;

    public static async Task<ProcessResult> RunAsync(
        string file,
        IEnumerable<string> args,
        CancellationToken ct,
        string? stdin = null,
        string? workingDirectory = null,
        Action<string>? onLine = null)
    {
        var info = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin is not null,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = info };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => Collect(stdout, e.Data, onLine);
        process.ErrorDataReceived += (_, e) => Collect(stderr, e.Data, onLine);

        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            return new ProcessResult(NOT_FOUND_EXIT_CODE, "", $"could not start '{file}': {e.Message}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (stdin is not null)
        {
            // utf8 without bom, bash chokes on a bom
            await using var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
            await writer.WriteAsync(stdin);
        }

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    static void Collect(StringBuilder buffer, string? line, Action<string>? onLine)
    {
        if (line is null)
        {
            return;
        }

        lock (buffer)
        {
            buffer.AppendLine(line);
        }
        onLine?.Invoke(line);
    }
}
