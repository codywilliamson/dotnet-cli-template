using System.ComponentModel;

namespace Starter.Mcp;

// the single source for the advertised schema and for validating arguments
sealed record StatsArgs
{
    [Description("Directory to scan, absolute or relative to the server's working directory.")]
    public required string Dir { get; init; }
}
