using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LibMentor.Core;

namespace LibMentor.Infrastructure;

/// <summary>Reads and validates repository configuration.</summary>
public static partial class ConfigurationFile
{
    /// <summary>Gets the shared JSON format.</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>Loads a configuration without allowing unknown settings.</summary>
    public static Configuration Load(string path)
    {
        var configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("Configuration is empty.");
        Validate(configuration, Path.GetDirectoryName(Path.GetFullPath(path))!);
        return configuration;
    }

    /// <summary>Validates configuration values and output containment.</summary>
    public static void Validate(Configuration configuration, string root)
    {
        if (configuration.SchemaVersion != 1)
            throw new InvalidDataException("Only schemaVersion 1 is supported.");
        if (string.IsNullOrWhiteSpace(configuration.Name) || string.IsNullOrWhiteSpace(configuration.Description))
            throw new InvalidDataException("Name and description are required.");
        if (!VersionPattern().IsMatch(configuration.Version))
            throw new InvalidDataException("Version must be a semantic version such as 1.2.3 or 1.2.3-preview.1.");
        if (!Uri.TryCreate(configuration.Repository, UriKind.Absolute, out var repository) || repository.Scheme != "https")
            throw new InvalidDataException("Repository must be an absolute HTTPS URL.");
        if (configuration.BaseUrl is not null &&
            (!Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out var baseUrl) || baseUrl.Scheme != "https"))
            throw new InvalidDataException("BaseUrl must be an absolute HTTPS URL.");
        if (string.IsNullOrWhiteSpace(configuration.Revision))
            throw new InvalidDataException("Revision is required.");
        if (configuration.Documents is null || configuration.Sources is null || configuration.Exclude is null ||
            configuration.XmlDocumentation is null || configuration.TypeDoc is null || configuration.Examples is null ||
            configuration.Guidance is null || configuration.Packages is null)
            throw new InvalidDataException("Configuration arrays cannot be null.");
        foreach (var pattern in configuration.Documents.Concat(configuration.Sources).Concat(configuration.XmlDocumentation)
                     .Concat(configuration.TypeDoc).Concat(configuration.Exclude).Concat(configuration.Examples))
            SafePaths.ValidateRelative(pattern);
        SafePaths.ValidateRelative(configuration.Output);
        var output = SafePaths.Resolve(root, configuration.Output);
        if (output.Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) ||
            configuration.Output.Split('/', '\\').Any(p => p is ".git" or ".github" or "src" or "docs"))
            throw new InvalidDataException("Output must be a dedicated generated directory.");
    }

    [GeneratedRegex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
