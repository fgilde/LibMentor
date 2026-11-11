# Library integrations

| Repository | Inputs | Existing hosting |
| --- | --- | --- |
| `fgilde/Nextended` | README, package READMEs, VitePress Markdown, C# | VitePress Pages |
| `fgilde/CoworkeeLib` | README, website Markdown, C# modules | MkDocs Pages with custom domain |
| `fgilde/AirWorks` | README, English HTML docs, optional TypeDoc JSON | Vite Pages |
| `fgilde/MudBlazor.Extensions` | README, MkDocs Markdown, C# declarations | MkDocs gh-pages |
| `fgilde/BlazorJS` | README, HTML docs, C# | Existing static docs directory |
| `fgilde/Playzor` | README, package READMEs, C# | No dedicated documentation deployment detected |

Each integration pins the CLI package version and adds a configuration and pull-request validation workflow. Existing Pages workflows should generate into `.libmentor` and copy into their own output `ai/` directory. BlazorJS and Playzor can publish a documentation-only Pages site after Pages is configured. Deployment requires the first LibMentor package to be available.

Documentation versions are explicit snapshots. They must be updated as part of each library's release process; a source snapshot must not be described as a published package version without checking that release. Earlier version directories require persistent hosting retention.

## Automated integration verification

The Library integrations workflow builds the actual CLI package, installs it in isolated jobs, and runs generation plus drift checks against pinned revisions of all six integration branches. AirWorks first produces TypeDoc reflection JSON from its TypeScript entry point. Generated documentation is retained as a workflow artifact per library.
