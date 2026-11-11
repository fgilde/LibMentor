# LibMentor

Turn library documentation and API declarations into versioned context for AI coding assistants.

LibMentor is a .NET 10 CLI tool. It produces `llms.txt`, `llms-full.txt`, API Markdown, a portable JSON knowledge model, `AGENTS.md` and installable agent-skill folders. Everything runs locally or in CI. No model credentials, database or MCP server are required.

## Quick start

```sh
dotnet tool install --global LibMentor --version 0.1.0
libmentor init
libmentor generate
libmentor generate --check
```

Until the first public release, build and install from source:

```sh
dotnet pack src/LibMentor.Cli -c Release -o artifacts
dotnet tool install LibMentor --tool-path artifacts/tool --add-source artifacts --version 0.1.0
```

Edit `libmentor.json` before generating. All input and output paths are relative to this file, independently of the working directory.

```json
{
  "schemaVersion": 1,
  "name": "MyLibrary",
  "description": "A short explanation of what the library does.",
  "version": "1.2.3",
  "repository": "https://github.com/owner/MyLibrary",
  "revision": "main",
  "baseUrl": "https://owner.github.io/MyLibrary/ai",
  "output": ".libmentor",
  "documents": ["README.md", "docs/**/*.md"],
  "sources": ["src/**/*.cs"],
  "xmlDocumentation": [],
  "typeDoc": [],
  "exclude": ["**/Tests/**"],
  "examples": [],
  "packages": ["MyLibrary"],
  "guidance": ["Use the documented registration method before resolving services."]
}
```

The generated directory is owned through `manifest.json`. Generation preserves unrelated files and removes only stale files previously listed in that manifest. Use a dedicated output directory; do not point it at your source or website root. Repository traversal and symbolic-link inputs/outputs are rejected or skipped.

## Inputs and evidence

| Input | Adapter | Meaning |
| --- | --- | --- |
| Markdown | Markdig | Documents, links, code fences and source provenance |
| HTML | AngleSharp | Text, headings, lists and code blocks; scripts/styles/navigation omitted |
| C# | Roslyn | Public/protected declarations, XML summaries and source locations |
| Compiler XML | System.Xml.Linq | Compiler-emitted documentation member IDs and summaries |
| TypeDoc JSON | System.Text.Json | TypeDoc reflection names and summaries |

C# extraction parses declarations without loading MSBuild projects or resolving dependencies. It does not expand Razor-generated components, resolve all aliases, evaluate project preprocessor symbols or prove the code compiles. Supply compiler XML for built Razor APIs. TypeDoc imports retain reflection identity; they are not a complete TypeScript type-signature compatibility checker. Never present these outputs as binary compatibility guarantees.

To validate compilable examples, list standalone `.csproj` or `.fsproj` files in `examples`. LibMentor invokes `dotnet build -c Release` with a two-minute timeout per project. Markdown code fences are reference material and are not executed.

Validation checks existing relative Markdown link targets (including extensionless Markdown/HTML pages) and explicit `[API](xref:T:Namespace.Type)` references. Remote URLs, site-root links and heading anchors are not fetched or validated. Old documentation with broken relative links must be repaired or explicitly excluded. Output document links point to repository source files so source-relative assets remain reachable.

## Commands

| Command | Behavior |
| --- | --- |
| `init --config path` | Creates a configuration; refuses to overwrite an existing file |
| `generate --config path` | Validates sources and writes all outputs |
| `generate --check` | Validates and fails on generated-output drift without writing |
| `validate --baseline path` | Validates inputs and reports removed/changed declarations against earlier `knowledge.json` |
| `--version` | Prints the tool version |

Exit codes: `0` success, `1` validation or compatibility failure, `2` configuration/usage/I/O failure, `3` output drift. Compatibility compares declaration evidence exactly and reports changes even across major versions; release policy remains with the maintainer.

Use `--revision <commit>` and `--library-version <semver>` on `generate` or `validate` to identify CI snapshots without editing the configuration. These override library metadata, not the CLI package version.

## Outputs and hosting

Publish the complete output directory under a stable HTTPS URL. Set `baseUrl` to that directory, then copy it into your existing documentation deployment before upload. Do not add a second Pages deployment that would replace your existing website. Outputs include a current snapshot and `versions/<version>/` snapshot. To preserve earlier versions across deployments, retain prior version folders in your publishing pipeline; a clean checkout only contains the current version.

Skills are self-contained under `skills/<library>/`, with `SKILL.md` and `references/`. Copy that folder to your agent client's supported skills location. The generated `AGENTS.md` is consumer guidance and is intentionally kept in the output directory; existing repository contributor instructions are preserved.

Context7 can ingest a publicly reachable `llms.txt`. Configure the optional workflow with `CONTEXT7_API_KEY` only after deployment; an artifact upload alone does not make documentation publicly reachable. Indexing is an external service operation and does not guarantee ranking, inclusion in model training or increased package usage.

## Development

```sh
dotnet restore
dotnet test -c Release
dotnet pack src/LibMentor.Cli -c Release -o artifacts
dotnet run --project src/LibMentor.Cli -- generate
```

Architecture: Core contains versioned records, Application coordinates extraction/validation/export, Infrastructure implements file/parser/process adapters, and Cli owns arguments and exit codes. New exporters implement `IKnowledgeExporter`; duplicate output paths fail explicitly. Dependencies point inward.

See [research](docs/research.md), [release setup](docs/releasing.md), [integration overview](docs/integrations.md), and [implementation status](docs/implementation-status.md).

MIT licensed. Maintained by Florian Gilde.
