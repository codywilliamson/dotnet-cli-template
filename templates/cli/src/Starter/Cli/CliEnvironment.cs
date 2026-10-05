using System.Reflection;

namespace Starter.Cli;

// the only file that touches Console and Environment, built once in Program
public sealed record CliEnvironment(
    string CurrentDirectory,
    bool StdoutRedirected,
    bool StdinRedirected,
    Func<string, string?> GetVariable,
    TimeProvider Clock,
    string Version)
{
    public static CliEnvironment FromProcess() => new(
        Environment.CurrentDirectory,
        Console.IsOutputRedirected,
        Console.IsInputRedirected,
        Environment.GetEnvironmentVariable,
        TimeProvider.System,
        typeof(CliEnvironment).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
}
