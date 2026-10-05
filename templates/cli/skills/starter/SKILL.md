---
name: starter
description: Use when you need to count the files under a directory, find its largest files, or check that dotnet and git are installed, using the starter command-line tool.
---

# starter

A small native CLI. Output is built for agents: plain text with no escape codes, one JSON object with `--json`, and a one-line summary on stderr.

## Commands

- `starter stats <dir>` counts the files under a directory, adds up their size and lists the largest ones.
- `starter doctor` checks that `dotnet` and `git` are installed. Exit 1 means a check failed and the error says how to fix it.
- `starter help <command>` is the long help with examples first.
- `starter skill` prints this file.
<!--#if (mcp)-->
- `starter mcp` runs a stdio MCP server with one tool, `stats`, which takes `dir` and returns the same object as `stats --json`.
<!--#endif-->

## Flags

- `--json` (stats) prints one JSON object on stdout. Errors go to stderr as JSON.
- `--limit <n>` (stats) lists the largest `n` files, default 20, `0` for all.
- `--agent` forces agent output. `CLAUDECODE` and `CODEX_*` env vars switch it on by itself.
- `--plain` forces plain output, `--verbose` streams child output to stderr.

## JSON shapes

`starter stats src --json`:

```json
{"dir":"src","files":312,"bytes":58123,"largest":[{"path":"a/big.cs","bytes":9120}],"truncated":true}
```

Paths are relative to `dir` and sorted by size, then path. `truncated` is true when more files exist than `--limit` showed.

An expected failure prints this to stderr:

```json
{"error":"directory 'nope' does not exist","hint":"try: starter stats .","exit_code":2}
```

## Exit codes

| code | meaning |
|---|---|
| 0 | success |
| 1 | a command ran and failed |
| 2 | usage error, the hint names the fix |
| 3 | cancelled |
| 4 | declined a confirmation |

The tool never prompts when stdin is redirected or in agent mode. A command that needs confirmation exits 2 and the hint names `--yes`.

## Examples

```sh
starter stats .
starter stats src --json
starter stats . --limit 0 --agent
starter doctor
```
