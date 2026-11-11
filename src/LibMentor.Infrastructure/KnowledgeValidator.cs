using System.Diagnostics;
using LibMentor.Application;
using LibMentor.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LibMentor.Infrastructure;

/// <summary>Validates local links, API references and example builds.</summary>
public sealed class KnowledgeValidator : IKnowledgeValidator
{
    /// <summary>Validates original documents and compiles configured examples.</summary>
    public async Task<IReadOnlyList<Diagnostic>> ValidateAsync(Knowledge knowledge, Configuration configuration,
        string root, CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();
        if (knowledge.Documents.Count == 0 && knowledge.Api.Count == 0)
            diagnostics.Add(new("LM100", "No documents or API declarations matched the configured inputs."));
        var ids = knowledge.Api.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var document in knowledge.Documents.Where(d => d.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var source = await File.ReadAllTextAsync(SafePaths.Resolve(root, document.Path), cancellationToken);
            foreach (var link in Markdown.Parse(source).Descendants<LinkInline>())
            {
                var url = link.Url;
                if (string.IsNullOrEmpty(url) || url.StartsWith('#') || url.StartsWith('/') ||
                    Uri.TryCreate(url, UriKind.Absolute, out _))
                    continue;
                var local = Uri.UnescapeDataString(url.Split('#', '?')[0]);
                if (string.IsNullOrEmpty(local))
                    continue;
                var target = Path.GetFullPath(Path.Combine(root, Path.GetDirectoryName(document.Path)!, local));
                var relative = Path.GetRelativePath(root, target);
                try
                {
                    target = SafePaths.Resolve(root, relative);
                    if (!File.Exists(target) && !Directory.Exists(target) && !File.Exists(target + ".md") && !File.Exists(target + ".html"))
                        diagnostics.Add(new("LM101", $"Missing local link target: {url}", document.Path));
                }
                catch (InvalidDataException exception)
                {
                    diagnostics.Add(new("LM101", exception.Message, document.Path));
                }
            }
            foreach (var inline in Markdown.Parse(source).Descendants<AutolinkInline>().Where(i => i.Url.StartsWith("xref:", StringComparison.Ordinal)))
                if (!ids.Contains(inline.Url[5..]))
                    diagnostics.Add(new("LM102", $"Unknown API symbol: {inline.Url[5..]}", document.Path));
            foreach (var link in Markdown.Parse(source).Descendants<LinkInline>().Where(l => l.Url?.StartsWith("xref:", StringComparison.Ordinal) == true))
                if (!ids.Contains(link.Url![5..]))
                    diagnostics.Add(new("LM102", $"Unknown API symbol: {link.Url[5..]}", document.Path));
        }
        foreach (var project in configuration.Examples)
        {
            var path = SafePaths.Resolve(root, project);
            if (!File.Exists(path) || Path.GetExtension(path) is not (".csproj" or ".fsproj"))
            {
                diagnostics.Add(new("LM103", "Example must reference an existing .NET project.", project));
                continue;
            }
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var argument in new[] { "build", path, "--configuration", "Release", "--nologo", "--verbosity", "quiet", "-m:1", "-p:UseSharedCompilation=false" })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
                diagnostics.Add(new("LM103", "Example build exceeded two minutes.", project));
                continue;
            }
            var output = await stdout + await stderr;
            if (process.ExitCode != 0)
                diagnostics.Add(new("LM103", $"Example build failed: {output}", project));
        }
        return diagnostics;
    }
}
