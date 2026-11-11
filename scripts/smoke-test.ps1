$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $repo 'Directory.Build.props')
$version = $props.Project.PropertyGroup.Version
$runId = [Guid]::NewGuid().ToString('N')
$toolPath = Join-Path $repo "artifacts/tool-$runId"
$fixture = Join-Path $repo "artifacts/smoke-$runId"
dotnet tool install LibMentor --tool-path $toolPath --add-source (Join-Path $repo 'artifacts') --version $version
if ($LASTEXITCODE) { throw 'Tool installation failed.' }
$executable = Join-Path $toolPath $(if ($IsWindows) { 'libmentor.exe' } else { 'libmentor' })
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
Set-Content (Join-Path $fixture 'README.md') '# Smoke test'
Push-Location $fixture
try {
    & $executable init
    if ($LASTEXITCODE) { throw 'Init failed.' }
    & $executable generate
    if ($LASTEXITCODE) { throw 'Generation failed.' }
    & $executable generate --check
    if ($LASTEXITCODE) { throw 'Output drift detected.' }
    Set-Content '.libmentor/api.md' 'Drift'
    & $executable generate --check
    if ($LASTEXITCODE -ne 3) { throw 'Expected output-drift exit code.' }
    & $executable invalid-command
    if ($LASTEXITCODE -ne 2) { throw 'Expected invalid-command exit code.' }
} finally { Pop-Location }
