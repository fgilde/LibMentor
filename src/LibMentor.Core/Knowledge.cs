namespace LibMentor.Core;

/// <summary>Stores portable documentation with source provenance.</summary>
public sealed record Knowledge(int SchemaVersion, string Name, string Description, string Version,
    string Repository, string Revision, IReadOnlyList<string> Packages,
    IReadOnlyList<Document> Documents, IReadOnlyList<ApiEntry> Api);

/// <summary>Stores a source document and its normalized content.</summary>
public sealed record Document(string Path, string Title, string Content, string Sha256);

/// <summary>Stores a declared API and its extraction evidence.</summary>
public sealed record ApiEntry(string Id, string Signature, string Summary, string Path, int Line, string Evidence);

/// <summary>Stores a validation diagnostic.</summary>
public sealed record Diagnostic(string Code, string Message, string? Path = null);

/// <summary>Stores generated files and input knowledge.</summary>
public sealed record BuildResult(Knowledge Knowledge, IReadOnlyDictionary<string, string> Files);
