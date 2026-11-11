using System.Text;
using System.Text.Json;
using LibMentor.Core;

namespace LibMentor.Infrastructure;

/// <summary>Writes and checks generated files with an ownership manifest.</summary>
public static class OutputDirectory
{
    private const string ManifestName = "manifest.json";

    /// <summary>Returns generated paths whose contents differ from disk.</summary>
    public static IReadOnlyList<string> Differences(string root, Configuration configuration, IReadOnlyDictionary<string, string> files)
    {
        var output = SafePaths.Resolve(root, configuration.Output);
        var expected = WithManifest(files);
        var differences = expected.Where(f => !File.Exists(SafePaths.Resolve(output, f.Key)) ||
            File.ReadAllText(SafePaths.Resolve(output, f.Key)) != f.Value).Select(f => f.Key).ToList();
        differences.AddRange(ReadOwned(output).Except(expected.Keys, StringComparer.Ordinal));
        return differences;
    }

    /// <summary>Writes generated files and removes only previously owned stale files.</summary>
    public static void Write(string root, Configuration configuration, IReadOnlyDictionary<string, string> files)
    {
        ConfigurationFile.Validate(configuration, root);
        var output = SafePaths.Resolve(root, configuration.Output);
        var expected = WithManifest(files);
        var owned = ReadOwned(output);
        foreach (var path in expected.Keys.Concat(owned))
            SafePaths.Resolve(output, path);
        foreach (var file in expected)
        {
            var path = SafePaths.Resolve(output, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, file.Value, new UTF8Encoding(false));
                File.Move(temporary, path, overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        foreach (var stale in owned.Except(expected.Keys, StringComparer.Ordinal))
            File.Delete(SafePaths.Resolve(output, stale));
    }

    private static SortedDictionary<string, string> WithManifest(IReadOnlyDictionary<string, string> files)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            SafePaths.ValidateRelative(file.Key);
            result.Add(file.Key, file.Value);
        }
        var hashes = result.ToDictionary(f => f.Key, f => RepositoryExtractor.Hash(f.Value), StringComparer.Ordinal);
        result.Add(ManifestName, JsonSerializer.Serialize(new { schemaVersion = 1, files = hashes }, ConfigurationFile.Json) + "\n");
        return result;
    }

    private static IReadOnlyList<string> ReadOwned(string output)
    {
        var manifest = SafePaths.Resolve(output, ManifestName);
        if (!File.Exists(manifest))
            return [];
        using var json = JsonDocument.Parse(File.ReadAllText(manifest));
        if (json.RootElement.GetProperty("schemaVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported output manifest schema.");
        return json.RootElement.GetProperty("files").EnumerateObject().Select(p => p.Name).ToArray();
    }
}
