using LibMentor.Core;

namespace LibMentor.Application;

/// <summary>Detects removed or changed API declarations.</summary>
public static class Compatibility
{
    /// <summary>Compares API evidence from the same library.</summary>
    public static IReadOnlyList<Diagnostic> Compare(Knowledge baseline, Knowledge current)
    {
        if (baseline.SchemaVersion != current.SchemaVersion || baseline.Name != current.Name)
            return [new("LM201", "Baseline schema and library name must match.")];

        var currentApi = current.Api.Select(a => (a.Id, a.Signature, a.Evidence)).ToHashSet();
        return baseline.Api.Where(a => !currentApi.Contains((a.Id, a.Signature, a.Evidence)))
            .Select(a => new Diagnostic("LM202", $"API removed, changed or extracted differently: {a.Signature}", a.Path))
            .ToArray();
    }
}
