$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'artifacts/site'
$aiOutput = Join-Path $output 'ai'
$generated = Join-Path $repo '.libmentor'
$knowledgePath = Join-Path $generated 'knowledge.json'
if (-not (Test-Path -LiteralPath $knowledgePath)) { throw 'Generate the documentation before building the website.' }

$knowledge = Get-Content -LiteralPath $knowledgePath -Raw | ConvertFrom-Json
[xml]$props = Get-Content -LiteralPath (Join-Path $repo 'Directory.Build.props')
if ($knowledge.version -cne $props.Project.PropertyGroup.Version) { throw 'Website and package versions differ.' }

foreach ($file in @('llms.txt', 'llms-full.txt', 'knowledge.json', 'api.md', 'AGENTS.md', 'skills/libmentor/SKILL.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $generated $file) -PathType Leaf)) {
        throw "Generated documentation is incomplete: $file"
    }
}

New-Item -ItemType Directory -Path $output, $aiOutput -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repo 'website') -Force |
    Copy-Item -Destination $output -Recurse -Force
Get-ChildItem -LiteralPath $generated -Force |
    Copy-Item -Destination $aiOutput -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repo 'assets/icon.png') -Destination (Join-Path $output 'icon.png') -Force

$template = Get-Content -LiteralPath (Join-Path $repo 'website/index.html') -Raw
$html = $template.Replace('@@LIBMENTOR_VERSION@@', [System.Net.WebUtility]::HtmlEncode([string]$knowledge.version))
Set-Content -LiteralPath (Join-Path $output 'index.html') -Value $html -Encoding utf8NoBOM -NoNewline
Set-Content -LiteralPath (Join-Path $output '.nojekyll') -Value '' -NoNewline

Write-Output "Website: $output"
Write-Output "Documentation version: $($knowledge.version)"
