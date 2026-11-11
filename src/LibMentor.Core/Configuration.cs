namespace LibMentor.Core;

/// <summary>Configures a versioned documentation build.</summary>
public sealed record Configuration
{
    /// <summary>Gets the configuration schema version.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Gets the library name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the library description.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the documentation version.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the public repository URL.</summary>
    public required string Repository { get; init; }

    /// <summary>Gets the source revision or branch.</summary>
    public string Revision { get; init; } = "main";

    /// <summary>Gets the public URL of the generated directory.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Gets the output directory relative to the configuration.</summary>
    public string Output { get; init; } = ".libmentor";

    /// <summary>Gets Markdown and HTML input patterns.</summary>
    public string[] Documents { get; init; } = ["README.md", "docs/**/*.md"];

    /// <summary>Gets C# source patterns for declaration extraction.</summary>
    public string[] Sources { get; init; } = [];

    /// <summary>Gets compiler XML documentation patterns.</summary>
    public string[] XmlDocumentation { get; init; } = [];

    /// <summary>Gets TypeDoc JSON input patterns.</summary>
    public string[] TypeDoc { get; init; } = [];

    /// <summary>Gets excluded input patterns.</summary>
    public string[] Exclude { get; init; } = [];

    /// <summary>Gets example projects to compile during validation.</summary>
    public string[] Examples { get; init; } = [];

    /// <summary>Gets consumer guidance included in agent skills.</summary>
    public string[] Guidance { get; init; } = [];

    /// <summary>Gets package identities associated with this version.</summary>
    public string[] Packages { get; init; } = [];
}
