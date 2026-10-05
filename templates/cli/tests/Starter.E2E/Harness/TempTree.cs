namespace Starter.E2E.Harness;

// two files, 12 bytes in total, one of them nested
public sealed class TempTree : IDisposable
{
    public const int FILES = 2;
    public const int BYTES = 12;

    TempTree(string root) => Root = root;

    public string Root { get; }

    public static TempTree Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "starter-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        File.WriteAllText(Path.Combine(root, "a.txt"), "12345");
        File.WriteAllText(Path.Combine(root, "sub", "b.txt"), "1234567");
        return new TempTree(root);
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
