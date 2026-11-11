using LibMentor.Core;

namespace LibMentor.Application;

/// <summary>Builds validated documentation through replaceable adapters.</summary>
public sealed class BuildDocumentation(IKnowledgeExtractor extractor, IKnowledgeValidator validator,
    IEnumerable<IKnowledgeExporter> exporters)
{
    /// <summary>Extracts, validates and exports repository knowledge.</summary>
    public async Task<BuildResult> ExecuteAsync(Configuration configuration, string root,
        Knowledge? baseline = null, CancellationToken cancellationToken = default)
    {
        var knowledge = await extractor.ExtractAsync(configuration, root, cancellationToken);
        var diagnostics = (await validator.ValidateAsync(knowledge, configuration, root, cancellationToken)).ToList();
        if (baseline is not null)
            diagnostics.AddRange(Compatibility.Compare(baseline, knowledge));
        if (diagnostics.Count > 0)
            throw new ValidationException(diagnostics);

        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var exporter in exporters)
        foreach (var file in exporter.Export(knowledge, configuration))
            if (!files.TryAdd(file.Key, file.Value.Replace("\r\n", "\n", StringComparison.Ordinal)))
                throw new InvalidOperationException($"Exporters produced the same path: {file.Key}");
        return new(knowledge, files);
    }
}

/// <summary>Reports documentation validation failures.</summary>
public sealed class ValidationException(IReadOnlyList<Diagnostic> diagnostics)
    : Exception(string.Join(Environment.NewLine, diagnostics.Select(d => $"{d.Code}: {d.Path}: {d.Message}")))
{
    /// <summary>Gets the validation failures.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;
}
