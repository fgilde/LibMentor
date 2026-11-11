using System.Text.Json;
using LibMentor.Application;
using LibMentor.Core;
using LibMentor.Infrastructure;
using Xunit;

namespace LibMentor.Tests;

/// <summary>Verifies extraction, validation and deterministic publication.</summary>
public sealed class DocumentationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "LibMentor.Tests", Guid.NewGuid().ToString("N"));
    private static Configuration Config => new()
    {
        Name = "Example",
        Description = "Example library.",
        Version = "1.2.3",
        Repository = "https://github.com/example/library",
        Sources = ["src/**/*.cs"]
    };

    /// <summary>Creates an isolated repository fixture.</summary>
    public DocumentationTests() => Directory.CreateDirectory(root);

    /// <summary>Removes the isolated fixture.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Preserves overloads and excludes inaccessible declarations.</summary>
    [Fact]
    public async Task ExtractsPublicApiAndProvenance()
    {
        Write("src/Public.cs", """
            namespace Demo;
            /// <summary>A useful service.</summary>
            public class Service
            {
                public string Run(int value) => "";
                public string Run(string value) => value;
                protected void Extend() { }
                private void Hide() { }
                public int Count { get; private set; }
                public const int Limit = 5;
                private class Hidden { public void Leak() { } }
            }
            internal class Internal { public void Leak() { } }
            """);
        var result = await new RepositoryExtractor().ExtractAsync(Config, root, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Api.Count(a => a.Id.StartsWith("M:Demo.Service.Run", StringComparison.Ordinal)));
        Assert.Contains(result.Api, a => a.Id == "T:Demo.Service" && a.Summary == "A useful service.");
        Assert.Contains(result.Api, a => a.Id == "F:Demo.Service.Limit");
        Assert.DoesNotContain(result.Api, a => a.Signature.Contains("Hide", StringComparison.Ordinal) || a.Id.Contains("Leak", StringComparison.Ordinal));
        Assert.All(result.Api, a => { Assert.Equal("src/Public.cs", a.Path); Assert.True(a.Line > 0); Assert.Equal("source-declaration", a.Evidence); });
    }

    /// <summary>Uses glob exclusions for generated and private source files.</summary>
    [Fact]
    public async Task ExcludesGeneratedAndConfiguredInputs()
    {
        Write("src/Visible.cs", "public class Visible {}");
        Write("src/bin/Generated.cs", "public class Generated {}");
        Write("src/Private.cs", "public class Private {}");
        var result = await new RepositoryExtractor().ExtractAsync(Config with { Exclude = ["**/Private.cs"] }, root, TestContext.Current.CancellationToken);
        Assert.Single(result.Api);
        Assert.Equal("T:Visible", result.Api[0].Id);
    }

    /// <summary>Rejects syntax errors instead of silently omitting APIs.</summary>
    [Fact]
    public async Task RejectsMalformedCSharp()
    {
        Write("src/Broken.cs", "public class {");
        await Assert.ThrowsAsync<InvalidDataException>(() => new RepositoryExtractor().ExtractAsync(Config, root, TestContext.Current.CancellationToken));
    }

    /// <summary>Converts HTML without scripts or navigation.</summary>
    [Fact]
    public async Task ConvertsHtml()
    {
        Write("docs/index.html", "<h1>Example</h1><nav>Navigation</nav><p>Hello <b>world</b></p><pre>var x = 1;</pre><script>secret()</script>");
        var result = await new RepositoryExtractor().ExtractAsync(Config with { Documents = ["docs/*.html"] }, root, TestContext.Current.CancellationToken);
        var doc = Assert.Single(result.Documents);
        Assert.Contains("Hello world", doc.Content);
        Assert.Contains("var x = 1;", doc.Content);
        Assert.DoesNotContain("secret", doc.Content);
        Assert.DoesNotContain("Navigation", doc.Content);
        Assert.Equal(64, doc.Sha256.Length);
    }

    /// <summary>Imports compiler and TypeDoc evidence without inventing types.</summary>
    [Fact]
    public async Task ImportsExistingMetadata()
    {
        Write("api.xml", "<doc><members><member name=\"T:Demo.Widget\"><summary>A widget.</summary></member></members></doc>");
        Write("typedoc.json", """{"id":0,"name":"Demo","kind":1,"children":[{"id":1,"name":"Widget","kind":128,"comment":{"summary":[{"kind":"text","text":"A widget."}]}},{"id":2,"name":"Hidden","kind":128,"flags":{"isPrivate":true}}]}""");
        var result = await new RepositoryExtractor().ExtractAsync(Config with { XmlDocumentation = ["api.xml"], TypeDoc = ["typedoc.json"] }, root, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Api.Count);
        Assert.Contains(result.Api, a => a.Evidence == "compiler-xml" && a.Summary == "A widget.");
        Assert.Contains(result.Api, a => a.Evidence == "typedoc" && a.Signature == "Demo.Widget");
    }

    /// <summary>Imports members of exported TypeScript objects and function signatures.</summary>
    [Fact]
    public async Task ImportsTypeDocReflectionMembers()
    {
        Write("typedoc.json", """{"id":0,"name":"Library","kind":1,"children":[{"id":1,"name":"Api","kind":32,"type":{"type":"reflection","declaration":{"id":2,"name":"__type","kind":65536,"children":[{"id":3,"name":"run","kind":2048,"signatures":[{"id":4,"name":"run","kind":4096,"parameters":[{"name":"value","type":{"type":"intrinsic","name":"string"}}],"type":{"type":"intrinsic","name":"boolean"}}]}]}}}]}""");
        var result = await new RepositoryExtractor().ExtractAsync(Config with { TypeDoc = ["typedoc.json"] }, root, TestContext.Current.CancellationToken);
        Assert.Contains(result.Api, a => a.Signature.EndsWith("run(value: string): boolean", StringComparison.Ordinal));
    }

    /// <summary>Rejects missing relative links and unresolved symbol links.</summary>
    [Fact]
    public async Task ValidatesLinksAndSymbols()
    {
        Write("README.md", "# Example\n[Missing](docs/missing.md)\n[API](xref:T:Missing)\n[Remote](https://example.org)");
        var knowledge = await new RepositoryExtractor().ExtractAsync(Config, root, TestContext.Current.CancellationToken);
        var diagnostics = await new KnowledgeValidator().ValidateAsync(knowledge, Config, root, TestContext.Current.CancellationToken);
        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Code == "LM101");
        Assert.Contains(diagnostics, d => d.Code == "LM102");
    }

    /// <summary>Rewrites copied relative links to repository sources.</summary>
    [Fact]
    public async Task RewritesLinksWithoutChangingCode()
    {
        Write("README.md", "# Example\n[Guide](docs/guide.md#usage)\n`[Guide](docs/guide.md)`\n");
        Write("docs/guide.md", "# Usage\n");
        var result = await Build();
        Assert.Contains("https://github.com/example/library/blob/main/docs/guide.md#usage", result.Files["llms-full.txt"]);
        Assert.Contains("`[Guide](docs/guide.md)`", result.Files["llms-full.txt"]);
    }

    /// <summary>Rejects documentation builds without matching inputs.</summary>
    [Fact]
    public async Task RejectsEmptyBuild()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => Build());
        Assert.Contains(exception.Diagnostics, d => d.Code == "LM100");
    }

    /// <summary>Exports stable context, self-contained skills and version snapshots.</summary>
    [Fact]
    public async Task GeneratesDeterministically()
    {
        Write("README.md", "# Example\nSome documentation.\n");
        Write("src/Example.cs", "public class Example {}");
        var first = await Build();
        var second = await Build();
        Assert.Equal(first.Files.ToArray(), second.Files.ToArray());
        Assert.Contains("skills/example/SKILL.md", first.Files.Keys);
        Assert.Contains("skills/example/references/api.md", first.Files.Keys);
        Assert.Equal(first.Files["knowledge.json"], first.Files["versions/1.2.3/knowledge.json"]);
        var knowledge = JsonSerializer.Deserialize<Knowledge>(first.Files["knowledge.json"], ConfigurationFile.Json);
        Assert.Equal("1.2.3", knowledge!.Version);
    }

    /// <summary>Detects drift while preserving unrelated output files.</summary>
    [Fact]
    public async Task TracksOutputOwnership()
    {
        Write("README.md", "# Example");
        var build = await Build();
        OutputDirectory.Write(root, Config, build.Files);
        Write(".libmentor/keep.txt", "Keep");
        Assert.Empty(OutputDirectory.Differences(root, Config, build.Files));
        Write(".libmentor/api.md", "Changed");
        Assert.Contains("api.md", OutputDirectory.Differences(root, Config, build.Files));
        OutputDirectory.Write(root, Config, new Dictionary<string, string> { ["new.md"] = "New" });
        Assert.True(File.Exists(Path.Combine(root, ".libmentor", "keep.txt")));
        Assert.False(File.Exists(Path.Combine(root, ".libmentor", "api.md")));
    }

    /// <summary>Rejects traversal and reserved output directories.</summary>
    [Theory]
    [InlineData("../outside")]
    [InlineData(".git/generated")]
    [InlineData("docs/ai")]
    [InlineData("src/generated")]
    [InlineData("/absolute")]
    [InlineData(".")]
    public void RejectsUnsafeOutput(string output) => Assert.Throws<InvalidDataException>(() =>
        ConfigurationFile.Validate(Config with { Output = output }, root));

    /// <summary>Rejects unsupported schemas and versions.</summary>
    [Theory]
    [InlineData("latest")]
    [InlineData("1.2")]
    [InlineData("01.2.3")]
    [InlineData("1.2.3/../../x")]
    public void RejectsInvalidVersions(string version) => Assert.Throws<InvalidDataException>(() =>
        ConfigurationFile.Validate(Config with { Version = version }, root));

    /// <summary>Detects removals and signature changes against a baseline.</summary>
    [Fact]
    public async Task DetectsCompatibilityChanges()
    {
        Write("src/Api.cs", "public class Api { public void Run(int value) {} }");
        var baseline = (await Build()).Knowledge;
        Write("src/Api.cs", "public class Api { public void Run(string value) {} }");
        var current = (await Build()).Knowledge;
        Assert.Contains(Compatibility.Compare(baseline, current), d => d.Code == "LM202");
        Assert.Empty(Compatibility.Compare(current, current));
        Assert.Contains(Compatibility.Compare(baseline with { Name = "Other" }, current), d => d.Code == "LM201");
    }

    /// <summary>Reports missing example projects before starting a build.</summary>
    [Fact]
    public async Task ValidatesExamplePaths()
    {
        Write("README.md", "# Example");
        var config = Config with { Examples = ["missing.csproj"] };
        var knowledge = await new RepositoryExtractor().ExtractAsync(config, root, TestContext.Current.CancellationToken);
        Assert.Contains(await new KnowledgeValidator().ValidateAsync(knowledge, config, root, TestContext.Current.CancellationToken), d => d.Code == "LM103");
    }

    /// <summary>Rejects typos in configuration rather than ignoring them.</summary>
    [Fact]
    public void RejectsUnknownSettings()
    {
        Write("libmentor.json", """{"name":"Example","description":"Example","version":"1.0.0","repository":"https://github.com/example/library","documnts":[]}""");
        Assert.Throws<JsonException>(() => ConfigurationFile.Load(Path.Combine(root, "libmentor.json")));
    }

    /// <summary>Compiles runnable examples and rejects compilation errors.</summary>
    [Theory]
    [InlineData("public class Example {}", true)]
    [InlineData("public class Example { public void Run() => Missing(); }", false)]
    public async Task CompilesExamples(string source, bool succeeds)
    {
        Write("README.md", "# Example");
        Write("example/Example.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        Write("example/Example.cs", source);
        var config = Config with { Examples = ["example/Example.csproj"] };
        var knowledge = await new RepositoryExtractor().ExtractAsync(config, root, TestContext.Current.CancellationToken);
        var diagnostics = await new KnowledgeValidator().ValidateAsync(knowledge, config, root, TestContext.Current.CancellationToken);
        Assert.Equal(succeeds, diagnostics.Count == 0);
    }

    /// <summary>Rejects a malicious manifest before deleting external files.</summary>
    [Fact]
    public void RejectsManifestTraversal()
    {
        Write("keep.txt", "Keep");
        Write(".libmentor/manifest.json", """{"schemaVersion":1,"files":{"../keep.txt":"hash"}}""");
        Assert.Throws<InvalidDataException>(() => OutputDirectory.Write(root, Config, new Dictionary<string, string>()));
        Assert.Equal("Keep", File.ReadAllText(Path.Combine(root, "keep.txt")));
    }

    /// <summary>Rejects collisions from independently registered exporters.</summary>
    [Fact]
    public async Task RejectsExporterCollisions()
    {
        Write("README.md", "# Example");
        var build = new BuildDocumentation(new RepositoryExtractor(), new KnowledgeValidator(), [new PortableExporter(), new PortableExporter()]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => build.ExecuteAsync(Config, root, cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Bounds skill names according to the portable skills specification.</summary>
    [Fact]
    public async Task NormalizesSkillNames()
    {
        Write("README.md", "# Example");
        var configuration = Config with { Name = new string('A', 80) + "--Example" };
        var knowledge = await new RepositoryExtractor().ExtractAsync(configuration, root, TestContext.Current.CancellationToken);
        var files = new PortableExporter().Export(knowledge, configuration);
        var path = Assert.Single(files.Keys, k => k.StartsWith("skills/", StringComparison.Ordinal) && k.EndsWith("/SKILL.md", StringComparison.Ordinal));
        Assert.Equal(64, path.Split('/')[1].Length);
    }

    /// <summary>Keeps versioned discovery links within the matching snapshot.</summary>
    [Fact]
    public async Task KeepsSnapshotLinksVersioned()
    {
        Write("README.md", "# Example");
        var configuration = Config with { BaseUrl = "https://example.org/ai" };
        var knowledge = await new RepositoryExtractor().ExtractAsync(configuration, root, TestContext.Current.CancellationToken);
        var files = new PortableExporter().Export(knowledge, configuration);
        Assert.Contains("https://example.org/ai/versions/1.2.3/api.md", files["versions/1.2.3/llms.txt"]);
        Assert.Contains("https://example.org/ai/api.md", files["llms.txt"]);
    }

    /// <summary>Accepts extensionless site links and compiler XML in build output.</summary>
    [Fact]
    public async Task SupportsSiteLinksAndCompilerOutput()
    {
        Write("README.md", "# Example\n[Guide](docs/guide)");
        Write("docs/guide.md", "# Guide");
        Write("src/bin/Release/Example.xml", "<doc><members><member name=\"T:Demo.Widget\"><summary>A widget.</summary></member></members></doc>");
        var configuration = Config with { XmlDocumentation = ["src/bin/**/*.xml"] };
        var build = new BuildDocumentation(new RepositoryExtractor(), new KnowledgeValidator(), [new PortableExporter()]);
        var result = await build.ExecuteAsync(configuration, root, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Knowledge.Api, a => a.Id == "T:Demo.Widget");
        Assert.Contains("/blob/main/docs/guide.md", result.Files["llms-full.txt"]);
    }

    /// <summary>Produces matching source hashes across checkout line endings.</summary>
    [Fact]
    public async Task NormalizesSourceHashes()
    {
        Write("README.md", "# Example\r\nText\r\n");
        var windows = await Build();
        Write("README.md", "# Example\nText\n");
        var unix = await Build();
        Assert.Equal(windows.Files.ToArray(), unix.Files.ToArray());
    }

    private Task<BuildResult> Build() => new BuildDocumentation(new RepositoryExtractor(), new KnowledgeValidator(),
        [new PortableExporter()]).ExecuteAsync(Config, root, cancellationToken: TestContext.Current.CancellationToken);

    private void Write(string path, string content)
    {
        var absolute = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
    }
}
