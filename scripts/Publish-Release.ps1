param([Parameter(Mandatory)][string]$Version, [Parameter(Mandatory)][string]$Commit, [Parameter(Mandatory)][string]$RunId)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$validated = ConvertTo-ProductVersion $Version
if ($Commit -notmatch '\A[a-f0-9]{40}\z' -or $RunId -notmatch '\A[0-9]+\z') { throw 'Invalid release source.' }
if ((Get-ProductVersion).ToString() -ne $validated.ToString()) { throw 'Release version differs from the checked-out manifest.' }
$pullsJson = gh api "repos/$env:GH_REPO/commits/$Commit/pulls"
if ($LASTEXITCODE) { throw 'Cannot verify the merged pull request.' }
$merged = @($pullsJson | ConvertFrom-Json | Where-Object { $_.merged_at -and $_.base.ref -eq 'master' -and $_.merge_commit_sha -eq $Commit })
if ($merged.Count -eq 0) { Write-Host 'No PR merged to master at this commit; no release will be published.'; return }

$directory = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/release'
gh run download $RunId -n SoundAnchor-win-x64 -D $directory
if ($LASTEXITCODE) { throw 'Cannot download this run''s verified packages.' }
$names = @("SoundAnchor-$Version-win-x64-Portable.zip", "SoundAnchor-$Version-win-x64-Setup.exe")
$checksums = Get-Content -LiteralPath (Join-Path $directory 'SHA256SUMS.txt')
foreach ($name in $names) {
    $entry = @($checksums | Where-Object { $_ -match ('\A[a-f0-9]{64}  ' + [regex]::Escape($name) + '\z') })
    if ($entry.Count -ne 1) { throw "Missing or ambiguous checksum for $name" }
    if ((Get-FileHash -LiteralPath (Join-Path $directory $name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry[0].Substring(0,64)) { throw "Checksum mismatch: $name" }
}
$tag = "v$Version"
$refsJson = gh api "repos/$env:GH_REPO/git/matching-refs/tags/$tag"
if ($LASTEXITCODE) { throw 'Cannot inspect the release tag.' }
$tagExists = @($refsJson | ConvertFrom-Json | Where-Object ref -eq "refs/tags/$tag").Count -gt 0
if ($tagExists) {
    $tagCommit = gh api "repos/$env:GH_REPO/commits/$tag" --jq .sha
    if ($LASTEXITCODE -or $tagCommit -ne $Commit) { throw 'This version tag already belongs to a different commit. Increase version.props.' }
}
$releases = gh api "repos/$env:GH_REPO/releases?per_page=100"
if ($LASTEXITCODE) { throw 'Cannot inspect existing releases.' }
$existing = @($releases | ConvertFrom-Json | Where-Object tag_name -eq $tag)
if ($existing.Count -gt 0) {
    $tagCommit = gh api "repos/$env:GH_REPO/commits/$tag" --jq .sha
    if ($LASTEXITCODE -or $tagCommit -ne $Commit) { throw 'This version already belongs to a different commit. Increase version.props.' }
    if (-not $existing[0].draft) { Write-Host "Release $tag already published for this commit."; return }
} else {
    # Stage assets in a draft, then publish only after the upload succeeds.
    gh release create $tag --target $Commit --draft --title "SoundAnchor $tag" --generate-notes
    if ($LASTEXITCODE) { throw 'Release creation failed.' }
}
$assets = @($names | ForEach-Object { Join-Path $directory $_ }) + (Join-Path $directory 'SHA256SUMS.txt')
gh release upload $tag @assets --clobber
if ($LASTEXITCODE) { throw 'Release asset upload failed; the release remains a draft for retry.' }
gh release edit $tag --draft=false
if ($LASTEXITCODE) { throw 'Release publication failed; rerun this workflow.' }
Write-Host "Published $tag from merged PR #$($merged[0].number) at $Commit"
