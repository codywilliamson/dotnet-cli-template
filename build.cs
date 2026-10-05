#:property RestorePackagesWithLockFile=false

using System.Diagnostics;

Console.OutputEncoding = new System.Text.UTF8Encoding(false);
return Build.Run(args);

static class Build
{
    const int EXIT_USAGE = 2;
    const string TEMPLATE_PACKAGE = "Cody.Templates";

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

    static Step PackStep() => new("pack", () => Dotnet(Root, "pack", "Cody.Templates.csproj", "-c", "Release", "-o", Nupkgs));

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
            new("install", () => Dotnet(Root, ["new", "install", Directory.GetFiles(Nupkgs, "Cody.Templates.*.nupkg")[0], "--force", .. HiveFlag()])),
        ];

        foreach (var combo in Combos)
        {
            var dir = Path.Combine(Work, combo.Label);
            steps.Add(new($"cody-cli {combo.Label} ({combo.Name})", () =>
                Dotnet(Work, ["new", "cody-cli", "-n", combo.Name, "-o", dir, .. combo.Flags, .. HiveFlag()])));
            steps.Add(new($"ci in {combo.Label}", () => Dotnet(dir, "build.cs", "ci")));
        }

        var script = Path.Combine(Work, "script");
        steps.Add(new("cody-script new", () => Dotnet(Work, ["new", "cody-script", "-n", "sample", "-o", script, .. HiveFlag()])));
        steps.Add(new("cody-script run", () => Dotnet(script, "run", "sample.cs", "--", ".")));
        steps.Add(new("cody-script publish", () => Dotnet(script, "publish", "sample.cs")));
        steps.Add(new("uninstall", () => Dotnet(Root, ["new", "uninstall", TEMPLATE_PACKAGE, .. HiveFlag()])));
        return [.. steps];
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
