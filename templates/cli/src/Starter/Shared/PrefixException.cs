namespace Starter.Shared;

// an expected failure with a message the user can act on, never a stack trace
public sealed class PrefixException(string message, string? hint = null, ExitCode exitCode = ExitCode.Failed) : Exception(message)
{
    public string? Hint { get; } = hint;

    public ExitCode ExitCode { get; } = exitCode;

    public static PrefixException Usage(string message, string? hint = null) => new(message, hint, ExitCode.Usage);
}
