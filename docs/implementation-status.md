# Implementation status

Checked on 2026-11-11.

The public [fgilde/LibMentor](https://github.com/fgilde/LibMentor) repository contains the CLI, tests, workflows, JSON schemas and all six integration change sets. The first NuGet publication and production documentation deployment are still pending.

## Verified on GitHub Actions

- [Windows and Linux CI](https://github.com/fgilde/LibMentor/actions/runs/43024680859): both jobs succeeded, with 32 passing tests per platform, whitespace checks, normal online package auditing, packaging, installation of the actual tool and CLI smoke tests.
- [Six-library integration matrix](https://github.com/fgilde/LibMentor/actions/runs/43024681065): all six jobs succeeded using the CLI package built by that workflow. Each job generated documentation and then verified output drift without writing.
- AirWorks generated real TypeDoc reflection JSON from its TypeScript entry point before running LibMentor.
- The documentation workflow builds successfully. Its deployment step currently returns HTTP 404 because GitHub Pages is not enabled for this repository.
- AngleSharp was updated to 1.5.0 after the online audit identified an advisory affecting 1.3.0. Vulnerability auditing remains enabled.
- Checkout line endings are enforced through .gitattributes, and the PowerShell smoke test resets its expected failure exit code after successful assertions.

| Library | Documents | API entries | Generated files |
| --- | ---: | ---: | ---: |
| BlazorJS | 2 | 110 | 23 |
| Playzor | 6 | 376 | 31 |
| MudBlazor.Extensions | 39 | 6,343 | 97 |
| Nextended | 75 | 6,604 | 169 |
| AirWorks | 2 | 742 | 23 |
| Coworkee | 138 | 9,204 | 295 |

The integration workflow retains the built package and generated documentation as named artifacts. Its pinned consumer revisions are recorded directly in .github/workflows/integrations.yml.

## Implemented

Core/Application/Infrastructure/CLI dependency boundaries separate the model and use case from parsers, files and processes. Markdown uses Markdig, HTML uses AngleSharp, C# declarations use Roslyn, compiler XML uses the platform XML parser, and TypeDoc reflection JSON uses System.Text.Json.

Outputs include llms.txt, llms-full.txt, API Markdown, schema-versioned portable knowledge, AGENTS.md, self-contained skills, version snapshots and an ownership manifest. Generation is deterministic across LF/CRLF inputs and preserves unrelated output files. Validation covers local Markdown targets, explicit API references, configured compilable example projects, declaration baselines, contained paths and output drift.

CI/CD includes cross-platform tests, real package smoke tests, six-library integration tests, Pages deployment, tag-triggered and manually requested OIDC NuGet publishing, and optional Context7 registration. The release workflow validates its requested package version before testing and publishing.

## Integration pull requests

| Repository | Pull request | State |
| --- | --- | --- |
| Nextended | [#29](https://github.com/fgilde/Nextended/pull/29) | Draft; awaiting first NuGet release |
| CoworkeeLib | [#2](https://github.com/fgilde/CoworkeeLib/pull/2) | Draft; awaiting first NuGet release |
| AirWorks | [#1](https://github.com/fgilde/AirWorks/pull/1) | Draft; awaiting first NuGet release |
| MudBlazor.Extensions | [#214](https://github.com/fgilde/MudBlazor.Extensions/pull/214) | Draft; awaiting first NuGet release |
| BlazorJS | [#1](https://github.com/fgilde/BlazorJS/pull/1) | Draft; awaiting first NuGet release |
| Playzor | [#1](https://github.com/fgilde/Playzor/pull/1) | Draft; awaiting first NuGet release |

Each existing documentation deployment includes generated files under ai/. BlazorJS and Playzor have dedicated documentation deployment workflows. The consumers pin LibMentor 0.1.0; their own restore steps require its first NuGet publication. They remain drafts until the package is published and those workflows can be verified against NuGet itself.

The main integration matrix already verifies the exact configurations against the built package, independently of public NuGet availability. All published commits have no Co-Authored-By trailer.

## Remaining account setup

1. Enable GitHub Pages with GitHub Actions as the source.
2. Configure the fgilde NuGet Trusted Publishing policy for repository LibMentor, workflow release.yml, package scope LibMentor, with first-package and new-version publishing allowed. No environment is configured.
3. Start the Release workflow with version 0.1.0, or push a matching version tag.
4. After publication, rerun consumer validation, complete the pull requests and verify their deployed AI-documentation URLs.
5. Optionally supply a Context7 API key and register the public llms.txt URLs after deployment.

See [release setup](releasing.md) for exact commands and policy values.

## Local checkout and history

The original checkout exists at C:\dev\privat\github\LibMentor and retains its two original commits. Source files were read from that checkout and published through GitHub. Further CI corrections are currently in the remote repository; the local checkout has not been reset or overwritten.

The remote was initialized through GitHub's contents API, so its history is currently separate from those two local commits. Importing the original Git bundle was blocked by an automatic approval review because Git histories may contain sensitive historical content. That import has not happened and requires explicit approval. No automatic formatting or history-import workflow was enabled.

## Deliberate limits

C# source extraction is declaration analysis rather than a complete MSBuild semantic compilation or binary compatibility proof. Compiled Razor APIs can be supplied as compiler XML. TypeDoc imports are evidence-bearing API references rather than a full TypeScript compatibility checker. Remote URLs and heading anchors are not checked, and Markdown fences are not executed as tests.

Earlier version snapshots require retention across clean hosting deployments. No new MCP service, database, LLM-generated documentation or discoverability/ranking guarantee is included.
