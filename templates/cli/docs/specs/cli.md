# CLI spec

Reference for every command, flag and output mode. Behavior described here is the contract the e2e suite tests. When behavior and spec disagree, fix one of them in the same change.

## Commands

| command | does |
|---|---|
| `starter stats <dir>` | Count the files under a directory, add up their size and list the largest. |
| `starter doctor` | Check that `dotnet` and `git` are installed, with a fix for each failure. |
<!--#if (mcp)-->
| `starter mcp` | Run the stdio MCP server. |
<!--#endif-->
| `starter skill` | Print the agent skill (`skills/starter/SKILL.md`, embedded in the exe) to stdout. |
| `starter help [command]` | Long help: examples first, then the command's flags. |

## Flags

| flag | commands | meaning |
|---|---|---|
| `--limit <n>` | `stats` | List the largest `n` files. Default 20, `0` for all. Anything else is a usage error. |
| `--json` | data commands (`stats`) | One JSON object on stdout. Errors go to stderr as JSON. |
| `--agent` | all | Force agent mode, even without an agent env var. |
| `--plain` | all | Plain output even on a terminal. |
| `--verbose` | all | Show child process output on stderr and turn live widgets off. |
| `--help` | all | One screen: usage, flags, examples and the exit codes. |
| `--version` | root | Print the version. |

## Output modes

| mode | when | shape |
|---|---|---|
| json | `--json` | One JSON object per line, snake_case keys, sorted, relative paths. |
| plain | `--plain`, `CI` set, or stdout redirected | No escape bytes, stable line prefixes, no live widgets. |
| agent | `--agent`, or an agent env var: `CLAUDECODE`, `CODEX_SANDBOX` | Plain output, never prompts, plus one summary line on stderr. |
| pretty | everything else (a terminal) | Color, spinner per step, clickable paths (OSC 8), dim metadata. `NO_COLOR` drops color. |

The first match wins, in this order: `--json`, `--plain`, agent flag or env var, `CI` or redirected stdout, pretty. Only pretty mode may start live widgets.

Results go to stdout. Progress, warnings, errors and the agent footer go to stderr.

### Plain and agent output

`stats` prints `dir <path>`, `files <n>`, `bytes <n>`, then one `file <bytes> <relative path>` line per listed file, largest first. Steps print `> name`, then `ok name (1.2s)  detail`, `- name (skipped: why)` or `FAILED name (0.4s)`, and a final `done:` or `failed:` line.

Agent mode adds one footer line on stderr, always last: `stats: 1234 files, 56.7 MB, 12ms`. When a list was cut short, stderr also says how to get the rest: `showing 20 of 312 files, pass --limit 0 for all`.

### JSON shapes

`stats --json` prints one object. Paths are relative to `dir`, which is the argument as given. `largest` is sorted by `bytes` descending, then `path`. `truncated` is true when more files exist than `--limit` listed.

```json
{"dir":"src","files":312,"bytes":58123,"largest":[{"path":"a/big.cs","bytes":9120}],"truncated":true}
```

In json mode an expected failure prints this to stderr instead of text. `hint` is omitted when there is none.

```json
{"error":"directory 'nope' does not exist","hint":"try: starter stats .","exit_code":2}
```

Errors from the argument parser itself (a missing positional, an unknown flag) are text on stderr, still exit 2.

### Errors

Text mode prints `starter: message` and an indented `hint:` line to stderr. The hint is a literal next command when one exists, like `try: starter stats .`.

### Prompts

Nothing blocks on input. A command that needs confirmation (`RunContext.Confirm`) asks only on a terminal. With redirected stdin or in agent mode it exits 2 with the hint `try: starter <command> --yes`. Declining on a terminal exits 4. The example commands do not need confirmation, so no e2e test covers this path end to end, and `RunContextTests` covers it as a unit.

## Exit codes

| code | meaning |
|---|---|
| 0 | Success. |
| 1 | A command ran and failed, for example a `doctor` check. |
| 2 | Usage error: bad arguments, unknown command, a directory that does not exist, a missing `--yes`. |
| 3 | Cancelled with ctrl+c. |
| 4 | Declined a confirmation. |

## Agent skill

`skills/starter/SKILL.md` teaches an agent when to use the tool, the commands, the JSON shapes and the exit codes. `starter skill` prints it, so `starter skill > ~/.claude/skills/starter/SKILL.md` installs it. An e2e test fails when a command listed in `--help` is missing from the skill.
<!--#if (mcp)-->

## MCP

`starter mcp` serves MCP over stdio with hand-written handlers on `ModelContextProtocol.Core`. It exits 0 when stdin closes.

| tool | arguments | returns |
|---|---|---|
| `stats` | `dir` (required) | The same object as `stats --json`, as structured content, plus a text line. |

Unknown tools and bad arguments are JSON-RPC error `-32602` (invalid params). The input schema is generated from `StatsArgs`, so it cannot drift from the validation.
<!--#endif-->
