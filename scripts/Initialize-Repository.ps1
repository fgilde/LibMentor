param(
    [string]$Destination = 'C:\dev\privat\github\LibMentor',
    [string]$Repository = 'fgilde/LibMentor'
)

$ErrorActionPreference = 'Stop'
$source = Split-Path -Parent $PSScriptRoot
if (Test-Path -LiteralPath $Destination) { throw "Destination already exists: $Destination" }
gh auth status
if ($LASTEXITCODE) { throw 'An authenticated GitHub CLI session is required.' }
git clone --no-hardlinks --branch main $source $Destination
if ($LASTEXITCODE) { throw 'Cloning the completed working copy failed.' }
git -C $Destination remote remove origin
if ($LASTEXITCODE) { throw 'Removing the local clone remote failed.' }
gh repo view $Repository --json name 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    git -C $Destination remote add origin "https://github.com/$Repository.git"
    if ($LASTEXITCODE) { throw 'Adding the remote failed.' }
    git -C $Destination push -u origin main
    if ($LASTEXITCODE) { throw 'Push failed; remote history was preserved.' }
} else {
    gh repo create $Repository --public --source $Destination --remote origin --push --description 'Versioned AI documentation and agent skills for libraries.'
    if ($LASTEXITCODE) { throw 'Creating and publishing the repository failed.' }
}
Write-Output "Project: $Destination"
Write-Output "Repository: https://github.com/$Repository"
Write-Output 'Enable GitHub Pages with GitHub Actions and configure NuGet Trusted Publishing as described in docs/releasing.md before tagging a release.'
