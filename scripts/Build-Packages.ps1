# SignToolCommand is an optional Authenticode command line using Inno Setup's placeholders: $f is
# the file being signed and $q is a double quote, so paths with spaces are written $q...$q. The same
# string signs the published executable here and is handed to Inno Setup for the setup and
# uninstaller. Without it the packages stay unsigned and Windows reports an unknown publisher.
# See docs/SIGNING.md.
param([string]$InnoCompiler, [string]$SignToolCommand)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')
$Version = (Get-ProductVersion).ToString()
$repo = Split-Path $PSScriptRoot -Parent

function Invoke-SignTool {
    param([Parameter(Mandatory)][string]$Command, [Parameter(Mandatory)][string]$Path)
    if ($Command -notmatch '\$f') { throw 'SignToolCommand must contain the $f placeholder for the file to sign' }
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    # Expand Inno Setup's placeholders the same way Inno does, so one command works for every file.
    $expanded = $Command.Replace('$q', '"').Replace('$f', $resolved)
    # The call operator lets the command start with a quoted executable path.
    if ($expanded.TrimStart() -notmatch '^[&.]\s') { $expanded = '& ' + $expanded }
    $global:LASTEXITCODE = 0
    & ([scriptblock]::Create($expanded))
    if ($LASTEXITCODE) { throw "Signing failed for $resolved" }
}

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
    dotnet publish src/AudioAnchor.App -c Release -r win-x64 --self-contained true -p:DebugType=None -o $publish
    if ($LASTEXITCODE) { throw 'Publish failed' }
    Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination $publish
    if ($SignToolCommand) {
        Invoke-SignTool -Command $SignToolCommand -Path (Join-Path $publish 'AudioAnchor.exe')
    }
    Compress-Archive -Path "$publish/*" -DestinationPath (Join-Path $packages "AudioAnchor-$Version-win-x64-Portable.zip") -Force
    if (-not $InnoCompiler) {
        $candidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "${env:ProgramFiles}\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe")
        $InnoCompiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $InnoCompiler) { throw 'Install Inno Setup or specify -InnoCompiler' }
    $innoArguments = @("/DAppVersion=$Version", "/DPublishDir=$publish")
    # Inno signs the setup and the uninstaller itself through a named sign tool.
    if ($SignToolCommand) { $innoArguments += @('/DSignToolName=audioanchor', "/Saudioanchor=$SignToolCommand") }
    & $InnoCompiler @innoArguments installer/AudioAnchor.iss
    if ($LASTEXITCODE) { throw 'Installer compilation failed' }
    Get-ChildItem -LiteralPath $packages -File | Where-Object { $_.Name -like "AudioAnchor-$Version-*" } | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    } | Set-Content -LiteralPath (Join-Path $packages 'SHA256SUMS.txt') -Encoding utf8
} finally { Pop-Location }
