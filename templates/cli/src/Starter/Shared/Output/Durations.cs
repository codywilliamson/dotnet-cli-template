namespace Starter.Shared.Output;

public static class Durations
{
    public static string Format(TimeSpan elapsed) => elapsed.TotalSeconds switch
    {
        < 1 => $"{elapsed.TotalMilliseconds:0}ms",
        < 60 => $"{elapsed.TotalSeconds:0.0}s",
        _ => $"{(int)elapsed.TotalMinutes}m{elapsed.Seconds:00}s",
    };

    public static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes / (1024.0 * 1024):0.0} MB",
    };

    public static string Age(TimeSpan age) => age.TotalMinutes switch
    {
        < 1 => "just now",
        < 60 => $"{(int)age.TotalMinutes}m ago",
        < 60 * 48 => $"{(int)age.TotalHours}h ago",
        _ => $"{(int)age.TotalDays}d ago",
    };
}
