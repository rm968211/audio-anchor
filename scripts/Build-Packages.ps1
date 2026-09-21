param([string]$InnoCompiler)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$Version = (Get-ProductVersion).ToString()
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    $publish = Join-Path $repo 'artifacts/publish'
    $packages = Join-Path $repo 'artifacts/packages'
    # The publish directory is generated output, always resolved within this repository.
    if (Test-Path -LiteralPath $publish) {
        $resolved = (Resolve-Path -LiteralPath $publish).Path
        if ($resolved -ne [IO.Path]::GetFullPath($publish)) { throw 'Unexpected publish directory' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $packages | Out-Null
    dotnet publish src/SoundAnchor.App -c Release -r win-x64 --self-contained true -p:DebugType=None -o $publish
    if ($LASTEXITCODE) { throw 'Publish failed' }
    Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination $publish
    Compress-Archive -Path "$publish/*" -DestinationPath (Join-Path $packages "SoundAnchor-$Version-win-x64-Portable.zip") -Force
    if (-not $InnoCompiler) {
        $candidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "${env:ProgramFiles}\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe")
        $InnoCompiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $InnoCompiler) { throw 'Install Inno Setup or specify -InnoCompiler' }
    & $InnoCompiler "/DAppVersion=$Version" "/DPublishDir=$publish" installer/SoundAnchor.iss
    if ($LASTEXITCODE) { throw 'Installer compilation failed' }
    Get-ChildItem -LiteralPath $packages -File | Where-Object { $_.Name -like "SoundAnchor-$Version-*" } | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    } | Set-Content -LiteralPath (Join-Path $packages 'SHA256SUMS.txt') -Encoding utf8
} finally { Pop-Location }
