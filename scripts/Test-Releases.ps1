# Exercise publication behavior against a fake GitHub CLI; no network calls or releases are made.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$productVersion = (Get-ProductVersion).ToString()
$commit = 'a' * 40
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('AudioAnchor-ReleaseTests-' + [guid]::NewGuid().ToString('N'))
$previousRepo = $env:GH_REPO
$env:GH_REPO = 'test/audio-anchor'
$global:AudioAnchorReleaseTest = [pscustomobject]@{ passed=0; merged=$true; hasTag=$false; tagCommit=""; releases="[]"; badHash=$false; uploadFails=$false; calls=$null }
function gh {
    $a = @($args)
    $global:LASTEXITCODE = 0
    $global:AudioAnchorReleaseTest.calls.Add(($a -join ' '))
    if ($a[0] -eq 'api') {
        $route = $a[1]
        if ($route.EndsWith('/pulls')) {
            if ($global:AudioAnchorReleaseTest.merged) { return '[{"number":4,"merged_at":"2026-09-21T00:00:00Z","base":{"ref":"master"},"merge_commit_sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]' }
            return '[]'
        }
        if ($route.Contains('/git/matching-refs/')) {
            if ($global:AudioAnchorReleaseTest.hasTag) { return ('[{"ref":"refs/tags/v' + $productVersion + '"}]') }
            return '[]'
        }
        if ($route.Contains('/commits/v')) { return $global:AudioAnchorReleaseTest.tagCommit }
        if ($route.Contains('/releases?')) { return $global:AudioAnchorReleaseTest.releases }
        throw "Unexpected API request: $route"
    }
    if ($a[0] -eq 'run' -and $a[1] -eq 'download') {
        $path = $a[[Array]::IndexOf($a, '-D') + 1]
        New-Item -ItemType Directory -Force -Path $path | Out-Null
        $lines = foreach ($name in @("AudioAnchor-$productVersion-win-x64-Portable.zip", "AudioAnchor-$productVersion-win-x64-Setup.exe")) {
            $file = Join-Path $path $name
            Set-Content -LiteralPath $file -Value 'package fixture'
            $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($global:AudioAnchorReleaseTest.badHash) { $hash = '0' * 64 }
            "$hash  $name"
        }
        $lines | Set-Content -LiteralPath (Join-Path $path 'SHA256SUMS.txt')
        return
    }
    if ($a[0] -eq 'release' -and $a[1] -eq 'upload' -and $global:AudioAnchorReleaseTest.uploadFails) { $global:LASTEXITCODE = 1; return }
    if ($a[0] -eq 'release') { return }
    throw "Unexpected gh invocation: $a"
}
function Scenario([string]$Name, [scriptblock]$Setup, [bool]$ShouldFail, [scriptblock]$Verify) {
    $global:AudioAnchorReleaseTest.merged = $true; $global:AudioAnchorReleaseTest.hasTag = $false; $global:AudioAnchorReleaseTest.tagCommit = $commit
    $global:AudioAnchorReleaseTest.releases = '[]'; $global:AudioAnchorReleaseTest.badHash = $false; $global:AudioAnchorReleaseTest.uploadFails = $false
    $global:AudioAnchorReleaseTest.calls = [Collections.Generic.List[string]]::new()
    & $Setup
    $failed = $false
    try { & "$PSScriptRoot/Publish-Release.ps1" -Version $productVersion -Commit $commit -RunId 123 -ArtifactDirectory (Join-Path $fixture $global:AudioAnchorReleaseTest.passed) }
    catch { $failed = $true; if (-not $ShouldFail) { throw } }
    if ($failed -ne $ShouldFail) { throw "Unexpected result: $Name" }
    & $Verify
    $global:AudioAnchorReleaseTest.passed++; Write-Host "PASS $Name"
}
function NoPublish { if ($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release edit*' }) { throw 'Published unexpectedly' } }
try {
    Scenario 'Direct pushes do not release' { $global:AudioAnchorReleaseTest.merged = $false } $false {
        if ($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release *' }) { throw 'Direct push mutated releases' }
    }
    Scenario 'Valid merged PR stages assets then publishes' {} $false {
        $writes = @($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release *' })
        if ($writes.Count -ne 3 -or $writes[0] -notlike 'release create*' -or $writes[1] -notlike 'release upload*' -or $writes[2] -notlike 'release edit*--draft=false') { throw 'Wrong publication order' }
    }
    Scenario 'Reject corrupted downloaded package' { $global:AudioAnchorReleaseTest.badHash = $true } $true { NoPublish }
    Scenario 'Reject version tag on another commit' { $global:AudioAnchorReleaseTest.hasTag = $true; $global:AudioAnchorReleaseTest.tagCommit = 'b' * 40 } $true { NoPublish }
    Scenario 'Resume same-commit draft without a tag' {
        $global:AudioAnchorReleaseTest.releases = '[{"tag_name":"v' + $productVersion + '","draft":true,"target_commitish":"' + $commit + '"}]'
    } $false {
        if ($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release create*' }) { throw 'Recreated existing draft' }
        if (-not ($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release edit*' })) { throw 'Draft was not published' }
    }
    Scenario 'Reject another commit''s untagged draft' {
        $global:AudioAnchorReleaseTest.releases = '[{"tag_name":"v' + $productVersion + '","draft":true,"target_commitish":"' + ('b' * 40) + '"}]'
    } $true { NoPublish }
    Scenario 'Published same-commit release is idempotent' {
        $global:AudioAnchorReleaseTest.hasTag = $true
        $global:AudioAnchorReleaseTest.releases = '[{"tag_name":"v' + $productVersion + '","draft":false,"target_commitish":"' + $commit + '"}]'
    } $false { if ($global:AudioAnchorReleaseTest.calls | Where-Object { $_ -like 'release *' }) { throw 'Published release was mutated' } }
    Scenario 'Failed upload leaves the release unpublished' { $global:AudioAnchorReleaseTest.uploadFails = $true } $true { NoPublish }
} finally {
    $env:GH_REPO = $previousRepo
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase) -or (Split-Path $resolved -Leaf) -notlike 'AudioAnchor-ReleaseTests-*') { throw 'Unexpected cleanup path' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
Write-Host "Release automation tests passed: $($global:AudioAnchorReleaseTest.passed)"
$global:LASTEXITCODE = 0
Remove-Variable -Name AudioAnchorReleaseTest -Scope Global
