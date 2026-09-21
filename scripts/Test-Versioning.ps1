$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$script:passed = 0
function Check([string]$Name, [scriptblock]$Body) { & $Body; $script:passed++; Write-Host "PASS $Name" }
function Fails([scriptblock]$Body) { $failed = $false; try { & $Body | Out-Null } catch { $failed = $true }; if (-not $failed) { throw 'Expected validation to reject this input.' } }
foreach ($value in @('0.0.0','0.2.0','1.0.0','12.34.56','65534.65534.65534')) {
    Check "Accept stable SemVer $value" { if ((ConvertTo-ProductVersion $value).ToString() -ne $value) { throw 'Version changed' } }
}
foreach ($value in @('','1','1.2','v1.2.3','01.2.3','1.02.3','1.2.03','-1.2.3','1.2.3.4','1.2.3-beta','1.2.3+build','1.2.65535','99999999999999999999.0.0',"1.2.3`n")) {
    Check "Reject invalid/unsupported version [$value]" { Fails { ConvertTo-ProductVersion $value } }
}
Check 'Allow developer-selected patch, minor, and major increases' {
    foreach ($current in @('1.2.4','1.3.0','2.0.0')) { Assert-VersionIncrease ([version]$current) ([version]'1.2.3') }
}
Check 'Reject unchanged or downgraded version' {
    foreach ($current in @('1.2.3','1.2.2','1.1.99','0.99.99')) { Fails { Assert-VersionIncrease ([version]$current) ([version]'1.2.3') } }
}
Check 'Compare numerically across multiple digits' { Assert-VersionIncrease ([version]'1.10.0') ([version]'1.9.99') }
Check 'Reject duplicate manifest versions' { Fails { Read-VersionXml '<Project><PropertyGroup><Version>1.0.0</Version><Version>2.0.0</Version></PropertyGroup></Project>' } }
Check 'Reject missing manifest version' { Fails { Read-VersionXml '<Project />' } }
Check 'Reject malformed XML' { Fails { Read-VersionXml '<Project>' } }
Check 'Reject DTD/entities' { Fails { Read-VersionXml '<!DOCTYPE Project [<!ENTITY v "1.0.0">]><Project><PropertyGroup><Version>&v;</Version></PropertyGroup></Project>' } }

$fixture = Join-Path ([IO.Path]::GetTempPath()) ('SoundAnchor-VersionTests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
try {
    git -C $fixture init --quiet
    git -C $fixture config user.name 'Version tests'
    git -C $fixture config user.email 'version-tests@example.invalid'
    '<Project><PropertyGroup><Version>0.1.0</Version></PropertyGroup></Project>' | Set-Content (Join-Path $fixture 'Directory.Build.props')
    git -C $fixture add .
    git -C $fixture commit --quiet -m baseline
    if ($LASTEXITCODE) { throw 'Fixture commit failed' }
    $legacyBase = git -C $fixture rev-parse HEAD
    '<Project><PropertyGroup><Version>0.2.0</Version></PropertyGroup></Project>' | Set-Content (Join-Path $fixture 'version.props')
    Check 'Validate migration from legacy manifest' { & "$PSScriptRoot/Validate-Version.ps1" -BaseRef $legacyBase -RepositoryRoot $fixture | Out-Null }
    git -C $fixture add .
    git -C $fixture commit --quiet -m migration
    $base = git -C $fixture rev-parse HEAD
    Check 'Reject unchanged manifest against actual Git base' { Fails { & "$PSScriptRoot/Validate-Version.ps1" -BaseRef $base -RepositoryRoot $fixture } }
    '<Project><PropertyGroup><Version>0.2.1</Version></PropertyGroup></Project>' | Set-Content (Join-Path $fixture 'version.props')
    Check 'Validate patch against actual Git base' { & "$PSScriptRoot/Validate-Version.ps1" -BaseRef $base -RepositoryRoot $fixture | Out-Null }
    Remove-Item -LiteralPath (Join-Path $fixture 'version.props')
    Check 'Reject deleted manifest' { Fails { & "$PSScriptRoot/Validate-Version.ps1" -BaseRef $base -RepositoryRoot $fixture } }
    Check 'Reject non-commit base input' { Fails { & "$PSScriptRoot/Validate-Version.ps1" -BaseRef '--help' -RepositoryRoot $fixture } }
} finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or (Split-Path $resolved -Leaf) -notlike 'SoundAnchor-VersionTests-*') { throw 'Unexpected fixture cleanup path' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Host "Versioning tests passed: $script:passed"
