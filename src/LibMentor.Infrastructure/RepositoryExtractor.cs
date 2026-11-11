using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AngleSharp.Html.Parser;
using LibMentor.Application;
using LibMentor.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LibMentor.Infrastructure;

/// <summary>Extracts Markdown, HTML, C#, compiler XML and TypeDoc inputs.</summary>
public sealed class RepositoryExtractor : IKnowledgeExtractor
{
    /// <summary>Extracts deterministic knowledge with source locations.</summary>
    public async Task<Knowledge> ExtractAsync(Configuration configuration, string root, CancellationToken cancellationToken)
    {
        var documents = new List<Document>();
        foreach (var path in InputFiles.Match(root, configuration.Documents, configuration))
        {
            var raw = await File.ReadAllTextAsync(SafePaths.Resolve(root, path), cancellationToken);
            var content = path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                ? HtmlToText(raw) : RewriteLinks(raw, path, configuration, root);
            var title = Markdown.Parse(content).OfType<HeadingBlock>().FirstOrDefault()?.Inline?.ToString();
            documents.Add(new(path, string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(path) : title,
                content.Replace("\r\n", "\n", StringComparison.Ordinal), Hash(raw.Replace("\r\n", "\n", StringComparison.Ordinal))));
        }

        var api = new List<ApiEntry>();
        foreach (var path in InputFiles.Match(root, configuration.Sources, configuration))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = await File.ReadAllTextAsync(SafePaths.Resolve(root, path), cancellationToken);
            api.AddRange(CSharpExtractor.Extract(source, path, cancellationToken));
        }
        foreach (var path in InputFiles.Match(root, configuration.XmlDocumentation, configuration, includeBuildOutput: true))
        {
            var xml = XDocument.Load(SafePaths.Resolve(root, path));
            api.AddRange(xml.Descendants("member").Select(m => new ApiEntry(
                (string?)m.Attribute("name") ?? throw new InvalidDataException($"Missing XML member name: {path}"),
                (string)m.Attribute("name")!, Normalize(m.Element("summary")?.Value ?? ""), path, 1, "compiler-xml")));
        }
        foreach (var path in InputFiles.Match(root, configuration.TypeDoc, configuration))
        {
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(SafePaths.Resolve(root, path), cancellationToken));
            ExtractTypeDoc(json.RootElement, path, "", api);
        }

        return new(1, configuration.Name, configuration.Description, configuration.Version,
            configuration.Repository.TrimEnd('/'), configuration.Revision, configuration.Packages.Order(StringComparer.Ordinal).ToArray(),
            documents, api.Distinct().OrderBy(a => a.Id, StringComparer.Ordinal).ThenBy(a => a.Path, StringComparer.Ordinal).ToArray());
    }

    internal static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string HtmlToText(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        foreach (var element in document.QuerySelectorAll("script,style,nav,footer"))
            element.Remove();
        var builder = new StringBuilder();
        foreach (var element in document.QuerySelectorAll("h1,h2,h3,h4,p,li,pre"))
        {
            if (element.ParentElement?.Closest("pre,li") is not null)
                continue;
            var prefix = element.LocalName switch { "h1" => "# ", "h2" => "## ", "h3" => "### ", "h4" => "#### ", "li" => "- ", _ => "" };
            builder.AppendLine(element.LocalName == "pre" ? $"```\n{element.TextContent.Trim()}\n```" : prefix + Normalize(element.TextContent));
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private static string RewriteLinks(string markdown, string source, Configuration configuration, string root)
    {
        var parsed = Markdown.Parse(markdown, new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build());
        var replacements = new List<(int Start, int Length, string Value)>();
        foreach (var link in parsed.Descendants<LinkInline>())
        {
            if (string.IsNullOrEmpty(link.Url) || link.Url.StartsWith('#') ||
                Uri.TryCreate(link.Url, UriKind.Absolute, out _) || link.Url.StartsWith('/'))
                continue;
            if (link.UrlSpan.Start < 0 || link.UrlSpan.Length == 0)
                continue;
            var resolved = new Uri(new Uri("https://repository.invalid/" + source), link.Url);
            var relative = resolved.AbsolutePath.TrimStart('/');
            var diskPath = Path.Combine(root, Uri.UnescapeDataString(relative));
            if (!File.Exists(diskPath) && !Directory.Exists(diskPath))
            {
                if (File.Exists(diskPath + ".md"))
                    relative += ".md";
                else if (File.Exists(diskPath + ".html"))
                    relative += ".html";
            }
            var fragment = resolved.Query + resolved.Fragment;
            replacements.Add((link.UrlSpan.Start, link.UrlSpan.Length,
                $"{configuration.Repository.TrimEnd('/')}/blob/{Uri.EscapeDataString(configuration.Revision)}/{relative}{fragment}"));
        }
        var result = new StringBuilder(markdown);
        foreach (var replacement in replacements.OrderByDescending(r => r.Start).DistinctBy(r => r.Start))
            result.Remove(replacement.Start, replacement.Length).Insert(replacement.Start, replacement.Value);
        return result.ToString();
    }

    private static void ExtractTypeDoc(JsonElement node, string path, string parent, List<ApiEntry> api)
    {
        var name = node.TryGetProperty("name", out var property) ? property.GetString() ?? "" : "";
        var qualifiedName = string.IsNullOrEmpty(parent) ? name : $"{parent}.{name}";
        var isPrivate = node.TryGetProperty("flags", out var flags) && flags.TryGetProperty("isPrivate", out var value) && value.GetBoolean();
        if (isPrivate)
            return;
        if (node.TryGetProperty("kind", out var kind) && kind.GetInt32() != 1)
        {
            var summary = node.TryGetProperty("comment", out var comment) && comment.TryGetProperty("summary", out var parts)
                ? string.Concat(parts.EnumerateArray().Select(p => p.TryGetProperty("text", out var text) ? text.GetString() : "")) : "";
            var signature = qualifiedName;
            if (node.TryGetProperty("parameters", out var parameters))
                signature += "(" + string.Join(", ", parameters.EnumerateArray().Select(p =>
                    p.GetProperty("name").GetString() + ": " + (p.TryGetProperty("type", out var type) ? RenderType(type) : "unknown"))) + ")";
            if (node.TryGetProperty("type", out var returnType) && returnType.GetProperty("type").GetString() != "reflection")
                signature += ": " + RenderType(returnType);
            api.Add(new($"TS:{qualifiedName}:{kind.GetInt32()}", signature, summary, path, 1, "typedoc"));
        }
        foreach (var collection in new[] { "children", "signatures" })
            if (node.TryGetProperty(collection, out var children))
                foreach (var child in children.EnumerateArray())
                    ExtractTypeDoc(child, path, qualifiedName, api);
        if (node.TryGetProperty("type", out var reflection) && reflection.TryGetProperty("declaration", out var declaration))
            ExtractTypeDoc(declaration, path, qualifiedName, api);
    }

    private static string RenderType(JsonElement type)
    {
        var kind = type.GetProperty("type").GetString();
        return kind switch
        {
            "intrinsic" or "reference" => type.GetProperty("name").GetString()! +
                (type.TryGetProperty("typeArguments", out var arguments) ? "<" + string.Join(", ", arguments.EnumerateArray().Select(RenderType)) + ">" : ""),
            "array" => RenderType(type.GetProperty("elementType")) + "[]",
            "union" => string.Join(" | ", type.GetProperty("types").EnumerateArray().Select(RenderType)),
            "intersection" => string.Join(" & ", type.GetProperty("types").EnumerateArray().Select(RenderType)),
            "literal" => type.GetProperty("value").GetRawText(),
            "reflection" => "{ ... }",
            _ => type.GetRawText()
        };
    }

    private static string Normalize(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
