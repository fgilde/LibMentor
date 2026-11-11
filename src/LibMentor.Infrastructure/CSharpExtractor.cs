using System.Xml.Linq;
using LibMentor.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LibMentor.Infrastructure;

internal static class CSharpExtractor
{
    internal static IReadOnlyList<ApiEntry> Extract(string source, string path, CancellationToken cancellationToken)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview,
            documentationMode: DocumentationMode.Parse), path, cancellationToken: cancellationToken);
        var errors = tree.GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
            throw new InvalidDataException($"Cannot parse {path}: {string.Join("; ", errors.Select(e => e.ToString()))}");
        var compilation = CSharpCompilation.Create("Declarations", [tree]);
        var semantic = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        var entries = new List<ApiEntry>();
        foreach (var node in tree.GetRoot(cancellationToken).DescendantNodes().Where(n => n is MemberDeclarationSyntax or VariableDeclaratorSyntax))
        {
            var symbol = semantic.GetDeclaredSymbol(node, cancellationToken);
            if (symbol is null || symbol is INamespaceSymbol || symbol.IsImplicitlyDeclared || !IsVisible(symbol))
                continue;
            var id = symbol.GetDocumentationCommentId();
            if (id is null || symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove })
                continue;
            var summary = "";
            var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                try { summary = XElement.Parse(xml).Element("summary")?.Value.Trim() ?? ""; }
                catch (System.Xml.XmlException) { throw new InvalidDataException($"Invalid XML documentation in {path}: {id}"); }
            }
            var signature = symbol.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeConstantValue,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            entries.Add(new(id, signature, summary, path, tree.GetLineSpan(node.Span).StartLinePosition.Line + 1, "source-declaration"));
        }
        return entries;
    }

    private static bool IsVisible(ISymbol symbol)
    {
        for (var current = symbol; current is not null && current is not INamespaceSymbol; current = current.ContainingType)
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
                return false;
        return true;
    }
}
