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
$available = $false
while ([DateTimeOffset]::UtcNow -lt $deadline) {
    try {
        $index = Invoke-RestMethod -Uri 'https://api.nuget.org/v3-flatcontainer/libmentor/index.json' -TimeoutSec 15 -ErrorAction Stop
        $available = @($index.versions) -contains $feedVersion
    } catch {
        $available = $false
    }
    if ($available) { break }
    Write-Host "Waiting for LibMentor $Version to become available on nuget.org."
    Start-Sleep -Seconds 15
}
if (!$available) {
    throw "NuGet accepted the upload, but LibMentor $Version is not publicly restorable yet. Check its validation status in Manage packages."
}

$verificationRoot = Join-Path ([IO.Path]::GetTempPath()) ('libmentor-public-' + [Guid]::NewGuid().ToString('N'))
$toolPath = Join-Path $verificationRoot 'tool'
$fixturePath = Join-Path $verificationRoot 'fixture'
$configPath = Join-Path $verificationRoot 'NuGet.Config'
$cachePath = [Security.SecurityElement]::Escape((Join-Path $verificationRoot 'cache'))
New-Item -ItemType Directory -Path $fixturePath -Force | Out-Null
$config = "<configuration><packageSources><clear/><add key='nuget.org' value='https://api.nuget.org/v3/index.json'/></packageSources><config><add key='globalPackagesFolder' value='$cachePath'/></config></configuration>"
[IO.File]::WriteAllText($configPath, $config, [Text.UTF8Encoding]::new($false))
dotnet tool install LibMentor --version $Version --tool-path $toolPath --configfile $configPath --no-http-cache
if ($LASTEXITCODE) { throw 'Installation from public NuGet failed.' }
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
