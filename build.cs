#:property RestorePackagesWithLockFile=false

using System.Diagnostics;
using System.Text.Json;

Console.OutputEncoding = new System.Text.UTF8Encoding(false);
return Build.Run(args);

static class Build
{
    const int EXIT_USAGE = 2;
    const string TEMPLATE_PACKAGE = "Spectacl3.Templates";

    static readonly string Root = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
    static readonly string Artifacts = Path.Combine(Root, "artifacts");
    static readonly string Nupkgs = Path.Combine(Artifacts, "nupkg");
    static readonly string Work = Path.Combine(Artifacts, "template-tests");
    static readonly string Hive = Path.Combine(Work, "hive");

    sealed record Combo(string Label, string Name, string[] Flags);

    static readonly Combo[] Combos =
    [
        new("defaults", "Acme.Tools", []),
        new("minimal", "Widget", ["--e2e", "false", "--mcp", "false", "--release", "false"]),
        new("full", "Widget", ["--mcp", "true"]),
    ];

    sealed record Step(string Name, Func<int> Run);

    public static int Run(string[] args)
    {
        Step[]? steps = args switch
        {
            ["pack"] => [PackStep()],
            ["test"] => [PackStep(), .. TestSteps()],
            ["ci"] => [PackStep(), .. TestSteps()],
            _ => null,
        };
        if (steps is null)
        {
            Console.Error.WriteLine("usage: dotnet build.cs <pack|test|ci>");
            return EXIT_USAGE;
        }

        return RunAll(steps);
    }

    static Step PackStep() => new("pack", () => Dotnet(Root, "pack", "Spectacl3.Templates.csproj", "-c", "Release", "-o", Nupkgs));

    static Step[] TestSteps()
    {
        List<Step> steps =
        [
            new("clean", () =>
            {
                if (Directory.Exists(Work))
                {
                    Directory.Delete(Work, recursive: true);
                }
                Directory.CreateDirectory(Work);
                return 0;
            }),
            new("install", () => Dotnet(Root, ["new", "install", Directory.GetFiles(Nupkgs, "Spectacl3.Templates.*.nupkg")[0], "--force", .. HiveFlag()])),
        ];

        foreach (var combo in Combos)
        {
            var dir = Path.Combine(Work, combo.Label);
            steps.Add(new($"s3-cli {combo.Label} ({combo.Name})", () =>
                Dotnet(Work, ["new", "s3-cli", "-n", combo.Name, "-o", dir, .. combo.Flags, .. HiveFlag()])));
            steps.Add(new($"ci in {combo.Label}", () => Dotnet(dir, "build.cs", "ci")));
        }

        var script = Path.Combine(Work, "script");
        steps.Add(new("s3-script new", () => Dotnet(Work, ["new", "s3-script", "-n", "sample", "-o", script, .. HiveFlag()])));
        steps.Add(new("s3-script run", () => Dotnet(script, "run", "sample.cs", "--", ".")));
        steps.Add(new("s3-script agent output", () => CheckScript(script, agentEnv: true, "sample.cs", "--", ".")));
        steps.Add(new("s3-script --json output", () => CheckScript(script, agentEnv: false, "sample.cs", "--", ".", "--json")));
        steps.Add(new("s3-script usage exits 2", () => CheckScriptUsage(script)));
        steps.Add(new("s3-script publish", () => Dotnet(script, "publish", "sample.cs")));
        steps.Add(new("uninstall", () => Dotnet(Root, ["new", "uninstall", TEMPLATE_PACKAGE, .. HiveFlag()])));
        return [.. steps];
    }

    // run the script like an agent would and check what it printed
    static int CheckScript(string script, bool agentEnv, params string[] args)
    {
        var (exit, stdout, stderr) = Capture(script, agentEnv, ["run", .. args]);
        var json = args.Contains("--json");
        var problems = new List<string>();
        if (exit != 0)
        {
            problems.Add($"exit {exit}");
        }
        if (stdout.Contains((char)0x1b))
        {
            problems.Add("stdout has an ESC byte");
        }
        if (agentEnv && !stderr.Contains("sample: ", StringComparison.Ordinal))
        {
            problems.Add("no agent footer on stderr");
        }
        if (json)
        {
            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    _ = doc.RootElement.GetProperty("step");
                }
                catch (Exception e) when (e is JsonException or KeyNotFoundException)
                {
                    problems.Add($"bad json line: {line}");
                }
            }
        }

        Console.WriteLine(stdout.TrimEnd());
        Console.WriteLine($"stderr: {stderr.Trim()}");
        problems.ForEach(p => Console.WriteLine($"problem: {p}"));
        return problems.Count;
    }

    static int CheckScriptUsage(string script)
    {
        var (exit, _, stderr) = Capture(script, agentEnv: false, ["run", "sample.cs"]);
        Console.WriteLine($"exit {exit}: {stderr.Trim()}");
        return exit == 2 && stderr.Contains("usage:") && stderr.Contains("example:") ? 0 : 1;
    }

    static (int Exit, string Stdout, string Stderr) Capture(string workingDirectory, bool agentEnv, string[] args)
    {
        var info = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.Environment.Remove("CLAUDECODE");
        if (agentEnv)
        {
            info.Environment["CLAUDECODE"] = "1";
        }
        using var process = Process.Start(info)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout, stderr.Result);
    }

    // an isolated hive keeps test installs out of the real template list
    static string[] HiveFlag() => ["--debug:custom-hive", Hive];

    static int RunAll(Step[] steps)
    {
        var results = new List<(string Name, string Outcome, TimeSpan Elapsed)>();
        var exit = 0;
        for (var i = 0; i < steps.Length; i++)
        {
            if (exit != 0)
            {
                results.Add((steps[i].Name, "skipped", TimeSpan.Zero));
                continue;
            }

            Console.WriteLine();
            Console.WriteLine($"[{i + 1}/{steps.Length}] {steps[i].Name} ───");
            var clock = Stopwatch.StartNew();
            exit = steps[i].Run();
            var outcome = exit == 0 ? "pass" : "fail";
            Console.WriteLine($"{(exit == 0 ? "✓" : "✗")} {steps[i].Name} {clock.Elapsed.TotalSeconds:0.0}s");
            results.Add((steps[i].Name, outcome, clock.Elapsed));
        }

        Console.WriteLine();
        foreach (var (name, outcome, elapsed) in results)
        {
            Console.WriteLine($"{name,-40}{outcome,-9}{elapsed.TotalSeconds,6:0.0}s");
        }
        return exit;
    }

    static int Dotnet(string workingDirectory, params string[] args)
    {
        var info = new ProcessStartInfo("dotnet", args) { WorkingDirectory = workingDirectory };
        AddVsInstallerToPath(info);
        Console.WriteLine($"$ dotnet {string.Join(' ', args)}");
        using var process = Process.Start(info)!;
        process.WaitForExit();
        return process.ExitCode;
    }

    // NativeAOT linking on windows needs vswhere.exe
    static void AddVsInstallerToPath(ProcessStartInfo info)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var installer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");
        var path = info.Environment["PATH"] ?? "";
        if (Directory.Exists(installer) && !path.Contains(installer, StringComparison.OrdinalIgnoreCase))
        {
            info.Environment["PATH"] = $"{path}{Path.PathSeparator}{installer}";
        }
    }
}
