# Existing solutions and design decisions

Reviewed on 2026-11-11 before implementation.

| Solution | Existing capability | LibMentor decision |
| --- | --- | --- |
| [DocAgent.McpServer](https://www.nuget.org/packages/DocAgent.McpServer) | Polyglot documentation MCP server | Different delivery model; no competing MCP service in this release |
| [DocFX](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html) | Project/assembly-based API documentation with compiler metadata | Keep it as a complementary website/metadata tool; use Roslyn for lightweight declaration extraction and accept compiler XML |
| [Roslyn](https://github.com/dotnet/roslyn) | C# parser and symbol/documentation APIs | Reuse official parser rather than regular-expression C# parsing |
| [Markdig](https://github.com/xoofx/markdig) | Markdown AST and source locations | Reuse for Markdown links and headings |
| [AngleSharp](https://github.com/AngleSharp/AngleSharp) | HTML parsing | Reuse for HTML-based documentation such as AirWorks |
| [TypeDoc](https://typedoc.org/documents/Overview.html) | TypeScript documentation reflection JSON | Accept reflection JSON rather than building a TypeScript parser |
| [llms.txt](https://llmstxt.org/) | Proposed documentation index convention | Emit index and full text; do not imply universal agent adoption |
| [Agent Skills](https://agentskills.io/specification) | Portable skill folder with YAML-frontmatter SKILL.md | Emit self-contained skills and references |
| [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) | GitHub OIDC exchange for short-lived publishing keys | Use NuGet/login rather than storing a NuGet API key |
| [Context7 llms.txt ingestion](https://context7.com/docs/api-reference/add-library/add-an-llmstxt-file) | Ingest a public llms.txt URL | Optional post-deployment integration; requires account API access |

The reusable product is the extraction/validation/export pipeline and versioned knowledge model. Existing libraries own their existing websites. Their deployments copy generated files into a dedicated `ai/` directory rather than introducing competing Pages deployments.

No telemetry, hosted service, automatic LLM calls or indexing guarantees are included.
