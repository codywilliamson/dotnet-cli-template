# Starter agent guide

Starter is a NativeAOT .NET 10 command-line tool: one native binary, fast startup, output that reads well in a terminal and stays clean when piped or read by an agent. Read [docs/specs/cli.md](docs/specs/cli.md) before changing behavior and [docs/decisions.md](docs/decisions.md) before reopening a settled choice.

## Status

Scaffolded from `cody-cli`. The `stats` and `doctor` commands are examples of the two output shapes (result and steps). Replace them with real features.

## Layout

- `src/Starter/Cli/` has the command wiring and the single error boundary. `CliEnvironment.cs` is the only file that touches `Console` and `Environment`.
- `src/Starter/Features/<Command>/` is one vertical slice per command: `sealed class XCommand(RunContext run)` with `RunAsync(XOptions, CancellationToken)`.
- `src/Starter/Shared/` holds only what two or more features use: output, processes, json.
<!--#if (mcp)-->
- `src/Starter/Mcp/` is the stdio MCP server. See [docs/specs/cli.md](docs/specs/cli.md#mcp).
<!--#endif-->
- `tests/Starter.Tests/` is TUnit: pure logic plus real-tool tests in temp dirs.
<!--#if (e2e)-->
- `tests/Starter.E2E/` drives the published native exe and enforces the CLI spec.
<!--#endif-->

## Agents are a first-class user

- Agent mode (`--agent`, or `CLAUDECODE` / `CODEX_*`) prints plain output, never prompts, and puts one summary line on stderr. Anything that would prompt exits 2 with a hint naming `--yes`.
- Every data command takes `--json`: one object per line, snake_case, sorted, relative paths. Errors are JSON on stderr in that mode.
- Hints are literal next commands (`try: starter stats .`).
- `skills/starter/SKILL.md` is the agent skill, embedded in the exe and printed by `starter skill`. Update it in the same change as any command, flag or JSON shape. An e2e test fails when a command in `--help` is missing from it.
- Spec: [docs/specs/cli.md](docs/specs/cli.md).

## Rules that came from spikes

- Never start a live widget (spinner, progress) when stdout is redirected. Frames leak into the output. Only `PrettyReporter` does live work, and it is only built on a terminal.
- Plain mode emits zero ESC bytes. The e2e suite checks it.
- Results go to stdout. Progress, warnings, errors and the agent footer go to stderr.
- Features never read `Console`, `Environment` or `DateTime.Now`. Take them from `CliEnvironment`.
- `XenoAtom.CommandLine` rejects root positionals once subcommands exist. Keep positionals on the commands.
- The parser reports bad args as exit 1. `PrefixApp` maps that to 2, so only commands decide a real exit 1.
- All JSON goes through a source-generated `JsonSerializerContext` of records.
- Any new dependency must publish under NativeAOT with zero IL2xxx/IL3xxx warnings. Check before adding it.
- Lock files are on, except for the exe project: `PublishAot` adds a host-RID ILCompiler pack, so its lock file would differ per OS. Refresh the others after an SDK bump.

## Principles

- Every task has a runnable pass/fail check: a test, a build exit code, or the native exe. "Looks done" is not done.
- Evidence over assertion. Show the command and its output, and label guesses as guesses.
- KISS, SRP, YAGNI. Small files, one job each, sealed classes, primary constructors.
- One obvious way to do each thing. Follow the existing pattern.
- New decisions go in [docs/decisions.md](docs/decisions.md) with the evidence that settled them.
- Conventional commits, lowercase, one concern per commit.

## The loop

`dotnet build.cs <target>` runs every step: `restore`, `build`, `test`,
<!--#if (e2e)-->
`e2e`,
<!--#endif-->
`publish`, `ci`. A change is done when `dotnet build.cs ci` passes against the native exe, not just the JIT build. On Windows, NativeAOT linking needs the VS C++ build tools, and `build.cs` puts `vswhere.exe` on PATH for its child processes.
