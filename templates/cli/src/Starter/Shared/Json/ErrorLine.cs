namespace Starter.Shared.Json;

// what json mode prints to stderr for an expected failure
public sealed record ErrorLine(string Error, string? Hint, int ExitCode);
