namespace Starter.Tests;

sealed class TempDir : IDisposable
{
    TempDir(string path) => Path = path;

    public string Path { get; }

    public static TempDir Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "starter-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDir(path);
    }

    public string Write(string relativePath, string content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
