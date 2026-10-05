using Starter.Shared;

namespace Starter.Cli;

public sealed record SkillOptions;

// prints the agent skill embedded from skills/starter/SKILL.md
public sealed class SkillCommand
{
    const string RESOURCE_NAME = "SKILL.md";

    public Task<ExitCode> RunAsync(SkillOptions options, CancellationToken ct)
    {
        using var stream = typeof(SkillCommand).Assembly.GetManifestResourceStream(RESOURCE_NAME)
            ?? throw new PrefixException("the skill is not embedded in this build");
        using var reader = new StreamReader(stream);
        Console.Out.Write(reader.ReadToEnd());
        return Task.FromResult(ExitCode.Success);
    }
}
