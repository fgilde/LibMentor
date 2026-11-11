param(
    [string]$Version,
    [ValidateRange(1, 3600)]
    [int]$TimeoutSeconds = 1800
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$props = Get-Content (Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Build.props')
    $Version = $props.Project.PropertyGroup.Version
}
$feedVersion = $Version.Split('+')[0].ToLowerInvariant()
$deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
$verificationRoot = Join-Path ([IO.Path]::GetTempPath()) ('libmentor-public-' + [Guid]::NewGuid().ToString('N'))
$fixturePath = Join-Path $verificationRoot 'fixture'
New-Item -ItemType Directory -Path $fixturePath -Force | Out-Null

$installed = $false
while ([DateTimeOffset]::UtcNow -lt $deadline) {
    $available = $false
    try {
        $index = Invoke-RestMethod -Uri 'https://api.nuget.org/v3-flatcontainer/libmentor/index.json' -TimeoutSec 15 -ErrorAction Stop
        $available = @($index.versions) -contains $feedVersion
    } catch {
        $available = $false
    }

    if ($available) {
        $attemptRoot = Join-Path $verificationRoot ([Guid]::NewGuid().ToString('N'))
        $toolPath = Join-Path $attemptRoot 'tool'
        $configPath = Join-Path $attemptRoot 'NuGet.Config'
        $cachePath = [Security.SecurityElement]::Escape((Join-Path $attemptRoot 'cache'))
        New-Item -ItemType Directory -Path $attemptRoot -Force | Out-Null
        $config = "<configuration><packageSources><clear/><add key='nuget.org' value='https://api.nuget.org/v3/index.json'/></packageSources><config><add key='globalPackagesFolder' value='$cachePath'/></config></configuration>"
        [IO.File]::WriteAllText($configPath, $config, [Text.UTF8Encoding]::new($false))
        dotnet tool install LibMentor --version $Version --tool-path $toolPath --configfile $configPath --no-http-cache
        $installed = $LASTEXITCODE -eq 0
        if ($installed) { break }
    }

    Write-Host "Waiting for LibMentor $Version to become restorable from nuget.org."
    $remaining = ($deadline - [DateTimeOffset]::UtcNow).TotalSeconds
    if ($remaining -gt 0) { Start-Sleep -Seconds ([Math]::Min(15, [Math]::Ceiling($remaining))) }
}

if (!$installed) {
    throw "NuGet accepted the upload, but LibMentor $Version is not publicly restorable yet. Check its validation status in Manage packages."
}

$executable = Join-Path $toolPath $(if ($IsWindows) { 'libmentor.exe' } else { 'libmentor' })
Set-Content (Join-Path $fixturePath 'README.md') '# Public package verification'
Push-Location $fixturePath
try {
    & $executable init
    if ($LASTEXITCODE) { throw 'Public package init failed.' }
    & $executable generate
    if ($LASTEXITCODE) { throw 'Public package generation failed.' }
    & $executable generate --check
    if ($LASTEXITCODE) { throw 'Public package drift check failed.' }
} finally {
    Pop-Location
}
