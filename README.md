# dotnet-cli-template

`dotnet new` templates for my C# command-line tools and scripts.

| template | short name | produces |
|---|---|---|
| Spectacl3 CLI | `s3-cli` | A NativeAOT CLI repo: XenoAtom.CommandLine, pretty/plain/json output, TUnit tests, a `build.cs` loop, CI. |
| Spectacl3 Script | `s3-script` | A single-file `dotnet run name.cs` script with usage check, step output and exit codes. |

## Install

```sh
dotnet new install ./path/to/dotnet-cli-template/templates/cli --force
dotnet new install ./path/to/dotnet-cli-template/templates/script --force
```

Or pack once and install the package (needs the .NET 10 SDK):

```sh
dotnet build.cs pack
dotnet new install artifacts/nupkg/Spectacl3.Templates.0.1.0.nupkg --force
```

## s3-cli

```sh
dotnet new s3-cli -n Acme.Tools                      # e2e + release, no mcp
dotnet new s3-cli -n Widget --mcp true               # add a stdio MCP server
dotnet new s3-cli -n Widget --e2e false --release false
```

The name becomes the namespace, project and folder names. The exe and command name is the last segment, lowercased (`Acme.Tools` gives `tools`).

| flag | default | adds |
|---|---|---|
| `--e2e` | `true` | `tests/<Name>.E2E`, which drives the published native exe |
| `--mcp` | `false` | `<name> mcp`, a stdio MCP server on `ModelContextProtocol.Core` |
| `--release` | `true` | release-please config plus the release and publish workflows |

Always included: `stats <dir>` and `doctor` example commands, TUnit unit tests, `build.cs` (`restore build test publish ci`), CI on windows, ubuntu and macos, `AGENTS.md`, `docs/decisions.md` and `docs/specs/cli.md`. Creating the project restores it, which writes the lock files. Commit them.

Inside the new project:

```sh
dotnet build.cs ci          # restore, build, publish native, unit, e2e
artifacts/publish/widget stats .
```

NativeAOT on Windows needs the VS C++ build tools. `build.cs` adds `vswhere.exe` to PATH for its child processes.

## s3-script

```sh
dotnet new s3-script -n deploy      # writes deploy.cs
dotnet run deploy.cs -- .
dotnet publish deploy.cs              # NativeAOT exe
```

## Develop the templates

```sh
dotnet build.cs test
```

Packs the templates, installs them into an isolated hive under `artifacts/`, generates three `s3-cli` combinations plus a script, runs `dotnet build.cs ci` inside each generated project, then runs and publishes the script. It takes several minutes because each combination publishes a native exe.

Template tokens in the cli sources: `Starter` is the project name, `Prefix` the class prefix (last name segment), `starter` the lowercase exe name and `STARTER` the env var prefix.
