namespace Starter.Cli;

// copy-pasteable examples for agents, kept apart from the wiring
static class HelpText
{
    public static readonly string[] RootExamples =
    [
        "Examples:",
        "  starter stats .",
        "  starter stats src --json",
        "  starter stats . --limit 0 --agent",
        "  starter skill > ~/.claude/skills/starter/SKILL.md",
        "",
        "Exit codes: 0 ok, 1 failed, 2 usage, 3 cancelled, 4 declined",
        "More: starter help <command>",
    ];

    static readonly Dictionary<string, string[]> CommandExamples = new()
    {
        ["stats"] =
        [
            "starter stats .                 largest 20 files, human output",
            "starter stats src --json        one json object, relative paths, sorted",
            "starter stats . --limit 0       list every file",
        ],
        ["doctor"] = ["starter doctor                   check dotnet and git, exit 1 on a problem"],
        ["skill"] = ["starter skill > ~/.claude/skills/starter/SKILL.md"],
        ["help"] = ["starter help stats"],
    };

    public static IEnumerable<string> For(string command) =>
        CommandExamples.TryGetValue(command, out var lines) ? lines : [];
}
