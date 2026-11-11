# Implementation status

Checked on 2026-11-11.

LibMentor 0.1.0 is publicly available on NuGet. All six consumer integrations have been merged into their default branches, and generation plus output-drift checks pass against the public package in every repository. Four consumer documentation sites are publicly verified; two hosting issues remain.

## Package and verification

- [Public package](https://www.nuget.org/packages/LibMentor/0.1.0): published with the existing NuGet Trusted Publishing policy.
- [Public-package verification](https://github.com/fgilde/LibMentor/actions/runs/43038213157): installs 0.1.0 from NuGet into a fresh tool directory and successfully runs init, generation and the output-drift check.
- [Windows and Linux CI](https://github.com/fgilde/LibMentor/actions/runs/43040687438): both jobs passed, including 32 tests per platform, formatting, online dependency auditing, packaging, installation and CLI smoke tests.
- [Six-library matrix](https://github.com/fgilde/LibMentor/actions/runs/43040687600): all six configurations passed generation and output-drift verification with the package built by the workflow. AirWorks first exports real TypeDoc reflection JSON.
- [Next-package branding](https://github.com/fgilde/LibMentor/commit/7eb7140a2c069e4fbe52dd51a7c9e089b3cce570): embeds assets/icon.png as icon.png and sets PackageProjectUrl to https://libmentor.gilde.org/. The source repository remains https://github.com/fgilde/LibMentor. The actual CI-produced nupkg was opened and both URLs, the icon entry and the icon checksum were verified. The PNG is 128 x 128 pixels and 20,041 bytes. These metadata changes are prepared for the next package version; published 0.1.0 is unchanged.
- Commits and merge commits created for this implementation have no Co-Authored-By trailer.

## Default-branch integrations

| Repository | Default branch | Merged pull request | Public-package validation |
| --- | --- | --- | --- |
| Nextended | main | [#29](https://github.com/fgilde/Nextended/pull/29) | [Passed](https://github.com/fgilde/Nextended/actions/runs/43042485065) |
| CoworkeeLib | main | [#2](https://github.com/fgilde/CoworkeeLib/pull/2) | [Passed](https://github.com/fgilde/CoworkeeLib/actions/runs/43040419385) |
| AirWorks | main | [#1](https://github.com/fgilde/AirWorks/pull/1) | [Passed](https://github.com/fgilde/AirWorks/actions/runs/43040440924) |
| MudBlazor.Extensions | main | [#214](https://github.com/fgilde/MudBlazor.Extensions/pull/214) | [Passed](https://github.com/fgilde/MudBlazor.Extensions/actions/runs/43040464715) |
| BlazorJS | master | [#1](https://github.com/fgilde/BlazorJS/pull/1) | [Passed](https://github.com/fgilde/BlazorJS/actions/runs/43040484656) |
| Playzor | main | [#1](https://github.com/fgilde/Playzor/pull/1) | [Passed](https://github.com/fgilde/Playzor/actions/runs/43040508983) |

Each repository contains libmentor.json, a local tool manifest pinned to LibMentor 0.1.0, a reusable generation action and a workflow for pull requests and default-branch pushes. The action restores the public package, resolves the consumer's existing version metadata, generates a source snapshot identified by the commit SHA, and runs generate --check. Generated files are retained as workflow artifacts and included under ai/ in documentation deployments.

Outputs include llms.txt, llms-full.txt, knowledge.json, api.md, AGENTS.md, self-contained agent skills, version snapshots and an ownership manifest. Generated output is intentionally excluded from source control and rebuilt in CI.

## Public hosting

The following URLs were checked directly. All five common files (llms.txt, llms-full.txt, knowledge.json, api.md and AGENTS.md) return HTTP 200 at each verified site.

| Repository | llms.txt | Current result |
| --- | --- | --- |
| Nextended | [Open](https://fgilde.github.io/Nextended/ai/llms.txt) | HTTP 200 |
| CoworkeeLib | [Open](https://framework.coworkee.de/ai/llms.txt) | HTTP 200 |
| AirWorks | [Open](https://fgilde.github.io/AirWorks/ai/llms.txt) | HTTP 200 |
| MudBlazor.Extensions | [Open](https://fgilde.github.io/MudBlazor.Extensions/ai/llms.txt) | HTTP 200 |
| BlazorJS | [Configured URL](https://fgilde.github.io/BlazorJS/ai/llms.txt) | HTTP 404 despite a successful Pages deployment |
| Playzor | [Configured URL](https://fgilde.github.io/Playzor/ai/llms.txt) | HTTP 404; Pages deployment rejected because Pages is not enabled |

[BlazorJS deployment](https://github.com/fgilde/BlazorJS/actions/runs/43040484879) reports success, but the public AI-documentation URLs still return 404. Public availability has not been marked complete.

[Playzor deployment](https://github.com/fgilde/Playzor/actions/runs/43040509072) successfully builds and uploads the site artifact, then fails when creating the Pages deployment. GitHub reports that Pages must be enabled in [repository settings](https://github.com/fgilde/Playzor/settings/pages).

[LibMentor's own documentation workflow](https://github.com/fgilde/LibMentor/actions/runs/43040687433) also builds successfully and fails at Pages deployment while Pages is disabled. The separate project website at https://libmentor.gilde.org/ is reachable.

The existing [MudBlazor.Extensions preview and test-app workflow](https://github.com/fgilde/MudBlazor.Extensions/actions/runs/43040464664) published its preview NuGet package successfully, then failed in its Azure App Service deployment with HTTP 403 (Site Disabled). Its LibMentor validation, documentation deployment and TryMudEx deployment passed.

## Integration matrix snapshot

These counts refer to the reproducible consumer revisions pinned in .github/workflows/integrations.yml.

| Library | Documents | API entries | Generated files |
| --- | ---: | ---: | ---: |
| BlazorJS | 2 | 110 | 23 |
| Playzor | 6 | 376 | 31 |
| MudBlazor.Extensions | 39 | 6,343 | 97 |
| Nextended | 75 | 6,604 | 169 |
| AirWorks | 2 | 742 | 23 |
| Coworkee | 138 | 9,204 | 295 |

## Implemented behavior

Core/Application/Infrastructure/CLI dependency boundaries separate the model and use case from parsers, files and processes. Markdown uses Markdig, HTML uses AngleSharp, C# declarations use Roslyn, compiler XML uses the platform XML parser, and TypeDoc reflection JSON uses System.Text.Json.

Generation is deterministic across LF/CRLF inputs and preserves unrelated output files. Validation covers local Markdown targets, explicit API references, configured compilable example projects, declaration baselines, contained paths and output drift. AngleSharp 1.5.0 addresses the advisory found during the online audit; auditing remains enabled.

CI/CD covers cross-platform checks, real package installation, the six-library matrix, documentation deployment and OIDC NuGet publication. Version changes on main, matching tags and validated manual runs use release.yml. See [release setup](releasing.md).

## Remaining rollout

1. Resolve BlazorJS public Pages availability and enable Pages with GitHub Actions as the source for Playzor and LibMentor.
2. Recheck the affected public URLs after their deployments succeed.
3. Optionally configure a Context7 API key and register the public llms.txt URLs. No Context7 registration or automatic search/LLM indexing has been claimed.
4. Publish the next package version when desired to make the prepared icon and project-website metadata visible on NuGet.

## Local checkout and history

The original checkout is at C:\dev\privat\github\LibMentor and retains its two original commits. The canonical remote was initialized separately. Subsequent CI corrections, package metadata and integration results are in the remote repository; the original local checkout has not been reset, overwritten or synchronized. No history-import workflow was enabled.

## Deliberate limits

C# extraction analyzes source declarations rather than a complete MSBuild semantic compilation or binary compatibility proof. Compiled Razor APIs can be supplied as compiler XML. TypeDoc imports provide API evidence rather than a full TypeScript compatibility checker. Remote URLs and heading anchors are not checked, and Markdown fences are not executed as tests.

Earlier version snapshots require retention across clean hosting deployments. No new MCP service, database, LLM-generated documentation or discoverability/ranking guarantee is included.
