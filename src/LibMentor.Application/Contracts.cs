using LibMentor.Core;

namespace LibMentor.Application;

/// <summary>Extracts knowledge from configured repository inputs.</summary>
public interface IKnowledgeExtractor
{
    /// <summary>Reads source documents and API declarations.</summary>
    Task<Knowledge> ExtractAsync(Configuration configuration, string root, CancellationToken cancellationToken);
}

/// <summary>Validates source knowledge and configured examples.</summary>
public interface IKnowledgeValidator
{
    /// <summary>Returns diagnostics that prevent publication.</summary>
    Task<IReadOnlyList<Diagnostic>> ValidateAsync(Knowledge knowledge, Configuration configuration,
        string root, CancellationToken cancellationToken);
}

/// <summary>Exports knowledge into portable text files.</summary>
public interface IKnowledgeExporter
{
    /// <summary>Renders files relative to the output directory.</summary>
    IReadOnlyDictionary<string, string> Export(Knowledge knowledge, Configuration configuration);
}
