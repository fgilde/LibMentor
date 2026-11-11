# Library integrations

| Repository | Inputs | Existing hosting |
| --- | --- | --- |
| `fgilde/Nextended` | README, package READMEs, VitePress Markdown, C# | VitePress Pages |
| `fgilde/CoworkeeLib` | README, website Markdown, C# modules | MkDocs Pages with custom domain |
| `fgilde/AirWorks` | README, English HTML docs, optional TypeDoc JSON | Vite Pages |
| `fgilde/MudBlazor.Extensions` | README, MkDocs Markdown, C# declarations | MkDocs gh-pages |
| `fgilde/BlazorJS` | README, HTML docs, C# | Existing static docs directory |
| `fgilde/Playzor` | README, package READMEs, C# | No dedicated documentation deployment detected |

All six integrations are merged into their default branches (main, or master for BlazorJS). Each pins the public LibMentor 0.1.0 package, restores it in CI, generates into `.libmentor`, checks output drift and uploads the generated documentation as an artifact. Documentation deployment workflows include the generated files under `ai/`. Four consumer sites are verified online; BlazorJS and Playzor still have hosting issues. See the [verified rollout status](implementation-status.md) for direct URLs, successful checks and remaining work.

Documentation versions are explicit snapshots. They must be updated as part of each library's release process; a source snapshot must not be described as a published package version without checking that release. Earlier version directories require persistent hosting retention.

## Automated integration verification

The Library integrations workflow builds the actual CLI package, installs it in isolated jobs, and runs generation plus drift checks against pinned consumer revisions for all six libraries. AirWorks first produces TypeDoc reflection JSON from its TypeScript entry point. Generated documentation is retained as a workflow artifact per library.
