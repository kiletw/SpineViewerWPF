using System.Diagnostics;
using System.Text;
using SpineViewerWPF.Application;

// TASK-078: automatic Runtime detection reads the export version first and
// only tries adapters for that major.minor line.
static class RuntimeDetectionSmoke
{
    public static void Run(AssetService service, string temporaryRoot)
    {
        // Every committed JSON fixture reports a version on its own line.
        var fixtures = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures");
        foreach (var directory in Directory.EnumerateDirectories(fixtures, "v*-minimal"))
        {
            var name = Path.GetFileName(directory);
            var expected = $"{name[1]}.{name[2]}";
            var version = SkeletonVersionProbe.Read(Path.Combine(directory, "minimal.json"));
            Check(version is not null && SkeletonVersionProbe.Line(version) == expected,
                $"{name}: probe read '{version ?? "null"}', expected a {expected} version.");
        }

        Check(SkeletonVersionProbe.ReadJson("""{"skeleton":{"hash":"x","spine" : "3.8.99","x":0}}"""u8) == "3.8.99",
            "JSON probe missed the spine version.");
        Check(SkeletonVersionProbe.ReadJson("""{"skeleton":{"hash":"x"},"bones":[]}"""u8) is null,
            "JSON probe invented a version.");
        Check(SkeletonVersionProbe.ReadJson("""{"skeleton":{"spine":"not a version"}}"""u8) is null,
            "JSON probe accepted a malformed version.");

        // Binary headers: 2.x/3.x (hash string, version string), 4.x (8-byte hash, version string).
        Check(SkeletonVersionProbe.ReadBinary(Legacy("3.8.99")) == "3.8.99", "3.x binary header was not read.");
        Check(SkeletonVersionProbe.ReadBinary(Legacy("2.1.25", hash: null)) == "2.1.25", "Null-hash binary header was not read.");
        Check(SkeletonVersionProbe.ReadBinary(Modern("4.2.43")) == "4.2.43", "4.x binary header was not read.");
        Check(SkeletonVersionProbe.ReadBinary(Modern("4.3.39-beta")) == "4.3.39-beta", "4.x suffixed version was not read.");
        Check(SkeletonVersionProbe.ReadBinary(Encoding.ASCII.GetBytes("garbage bytes that are not a header")) is null,
            "Binary probe accepted garbage.");
        Check(SkeletonVersionProbe.ReadBinary(ReadOnlySpan<byte>.Empty) is null, "Binary probe accepted an empty file.");
        Check(SkeletonVersionProbe.Line("3.2.xx") == "3.2" && SkeletonVersionProbe.Line("4.1") == "4.1"
              && SkeletonVersionProbe.Line("x.y") is null, "Version line parsing failed.");

        // A 3.8 binary with a valid header and a damaged body fails fast with the
        // 3.8 adapter's error instead of running older binary parsers.
        var root = Path.Combine(temporaryRoot, "runtime-detection");
        Directory.CreateDirectory(root);
        var source = Path.Combine(fixtures, "v38_95-minimal");
        File.Copy(Path.Combine(source, "minimal.atlas"), Path.Combine(root, "damaged.atlas"));
        File.Copy(Path.Combine(source, "minimal.ppm"), Path.Combine(root, "minimal.ppm"));
        var damaged = Path.Combine(root, "damaged.skel");
        var body = new byte[4096];
        new Random(78).NextBytes(body);
        File.WriteAllBytes(damaged, [.. Legacy("3.8.99"), .. body]);
        var watch = Stopwatch.StartNew();
        var attempt = Task.Run(() => service.Inspect(damaged, null, null));
        Check(((IAsyncResult)attempt).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(30)),
            "Automatic detection did not finish on a damaged 3.8 binary.");
        Check(attempt.IsFaulted, "A damaged 3.8 binary was accepted.");
        var message = attempt.Exception!.InnerException!.Message;
        string? forced = null;
        try { service.Inspect(damaged, null, "3.8.95"); }
        catch (Exception exception) { forced = exception.Message; }
        Check(message == forced, $"Automatic detection did not stop at the 3.8 adapter: '{message}' vs '{forced}'.");
        Console.WriteLine($"TASK-078: runtime detection passed ({watch.Elapsed.TotalSeconds:0.00}s for the damaged binary).");
    }

    private static byte[] Legacy(string version, string? hash = "HASHHASHHASHHASHHASHHASHHAS") =>
        [.. String(hash), .. String(version)];

    private static byte[] Modern(string version) =>
        [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, .. String(version)];

    // Spine binary string: varint (length + 1, 0 = null) then UTF-8 bytes.
    private static byte[] String(string? value)
    {
        if (value is null) return [0];
        var bytes = Encoding.UTF8.GetBytes(value);
        return [(byte)(bytes.Length + 1), .. bytes];
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
