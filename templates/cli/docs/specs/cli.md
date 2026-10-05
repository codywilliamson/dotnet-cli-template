# CLI spec

Reference for every command, flag and output mode. Behavior described here is the contract the e2e suite tests. When behavior and spec disagree, fix one of them in the same change.

## Commands

| command | does |
|---|---|
| `starter stats <dir>` | Count the files under a directory and add up their size. |
| `starter doctor` | Check that `dotnet` and `git` are installed, with a fix for each failure. |
<!--#if (mcp)-->
| `starter mcp` | Run the stdio MCP server. |
<!--#endif-->

## Flags

| flag | commands | meaning |
|---|---|---|
| `--json` | `stats` | One JSON object on stdout. |
| `--plain` | all | Plain output even on a terminal. |
| `--verbose` | all | Show child process output on stderr and turn live widgets off. |
| `--help` | all | One dense screen: usage, flags and examples. |
| `--version` | root | Print the version. |

## Output modes

| mode | when | shape |
|---|---|---|
| json | `--json` | One JSON object per line, snake_case keys. |
| plain | `--plain`, `CI` set, or stdout redirected | No escape bytes, stable line prefixes, no live widgets. |
| agent | an agent env var is set: `CLAUDECODE`, `CODEX_SANDBOX` | Plain, plus a one-line summary footer for result commands. |
| pretty | everything else (a terminal) | Color, spinner per step, clickable paths (OSC 8), dim metadata. `NO_COLOR` drops color. |

The first match wins, in this order: `--json`, `--plain`, agent env var, `CI` or redirected stdout, pretty. Only pretty mode may start live widgets.

Plain lines: `stats` prints `dir <path>`, `files <n>` and `bytes <n>`. Steps print `> name`, then `ok name (1.2s)  detail`, `- name (skipped: why)` or `FAILED name (0.4s)`, and a final `done:` or `failed:` line.

Results go to stdout. Progress, warnings and errors go to stderr as `starter: message` plus an indented `hint:` line.

## Exit codes

| code | meaning |
|---|---|
| 0 | Success. |
| 1 | A command ran and failed, for example a `doctor` check. |
| 2 | Usage error: bad arguments, unknown command, a directory that does not exist. |
| 3 | Cancelled with ctrl+c. |
<!--#if (mcp)-->

## MCP

`starter mcp` serves MCP over stdio with hand-written handlers on `ModelContextProtocol.Core`. It exits 0 when stdin closes.

| tool | arguments | returns |
|---|---|---|
| `stats` | `dir` (required) | `{dir, files, bytes}` as structured content, plus a text line. |

Unknown tools and bad arguments are JSON-RPC error `-32602` (invalid params). The input schema is generated from `StatsArgs`, so it cannot drift from the validation.
<!--#endif-->
