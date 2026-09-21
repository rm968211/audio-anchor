param([string]$BaseRef, [string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$current = Get-ProductVersion $RepositoryRoot
if ($BaseRef) {
    if ($BaseRef -notmatch '\A[a-fA-F0-9]{40}\z' -or $BaseRef -eq ('0' * 40)) { throw 'BaseRef must be a nonzero full Git commit SHA.' }
    $files = @(git -C $RepositoryRoot ls-tree --name-only $BaseRef -- version.props)
    if ($LASTEXITCODE) { throw "Cannot read target commit $BaseRef" }
    # One-time migration: the original default branch stored its version in Directory.Build.props.
    $manifest = if ($files -contains 'version.props') { 'version.props' } else { 'Directory.Build.props' }
    $previousXml = git -C $RepositoryRoot show "${BaseRef}:$manifest"
    if ($LASTEXITCODE) { throw "Cannot read the version at $BaseRef" }
    $previous = Read-VersionXml ($previousXml -join "`n")
    Assert-VersionIncrease $current $previous
    Write-Host "Version increased: $previous -> $current"
}
if ($env:GITHUB_OUTPUT) { "version=$current" | Add-Content -LiteralPath $env:GITHUB_OUTPUT }
Write-Output $current.ToString()
