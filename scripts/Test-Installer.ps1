param([Parameter(Mandatory)][string]$Installer, [switch]$AllowInstalledAppChanges)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -and -not $AllowInstalledAppChanges) {
    throw 'Run in a disposable Windows test account, or explicitly pass -AllowInstalledAppChanges.'
}
$installerPath = (Resolve-Path -LiteralPath $Installer).Path
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{82B72C23-769B-48AD-8174-B5ACED8384E2}_is1'
$startupKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Test-Path -LiteralPath $uninstallKey) { throw 'An existing AudioAnchor installation must not be overwritten by tests.' }
if (Get-ItemProperty -LiteralPath $startupKey -Name AudioAnchor -ErrorAction SilentlyContinue) { throw 'Existing startup registration must not be overwritten.' }
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('AudioAnchor-Package-' + [guid]::NewGuid().ToString('N'))
$installDir = Join-Path $testRoot 'app'
$demoDir = Join-Path $testRoot 'demo'
New-Item -ItemType Directory -Path $demoDir -Force | Out-Null
function Run-Hidden([string]$File, [string[]]$Arguments) {
    $process = Start-Process -FilePath $File -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(120000)) { throw "Timed out: $File" }
    if ($process.ExitCode -ne 0) { throw "Failed with exit code $($process.ExitCode): $File" }
}
$uninstalled = $false
try {
    Run-Hidden $installerPath @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/TASKS=startup', "/DIR=`"$installDir`"", "/LOG=`"$testRoot\install.log`"")
    $exe = Join-Path $installDir 'AudioAnchor.exe'
    if (-not (Test-Path -LiteralPath $exe)) { throw 'Application executable missing' }
    if (-not (Test-Path -LiteralPath $uninstallKey)) { throw 'Uninstall registration missing' }
    $startupValue = (Get-ItemProperty -LiteralPath $startupKey -Name AudioAnchor).AudioAnchor
    if ($startupValue -ne "`"$exe`" --background") { throw 'Startup registration is incorrect' }
    # Real user preferences are never touched by the launched app.
    $marker = '{"SchemaVersion":1,"Paused":true,"Playback":{"Id":"demo-speakers","Name":"Desk speakers"}}'
    Set-Content -LiteralPath (Join-Path $demoDir 'settings.json') -Value $marker
    $app = Start-Process -FilePath $exe -ArgumentList @('--demo', '--background', '--data-dir', "`"$demoDir`"") -WindowStyle Hidden -PassThru
    $deadline = [datetime]::UtcNow.AddSeconds(20)
    while (-not (Test-Path -LiteralPath (Join-Path $demoDir 'demo-status.json')) -and [datetime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if ($app.HasExited -or -not (Test-Path -LiteralPath (Join-Path $demoDir 'demo-status.json'))) { throw 'Installed application did not start' }
    Run-Hidden $exe @('--demo', '--data-dir', "`"$demoDir`"", '--exit')
    if (-not $app.WaitForExit(10000)) { throw 'Application did not exit' }
    Run-Hidden $installerPath @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=`"$installDir`"", "/LOG=`"$testRoot\upgrade.log`"")
    if ((Get-Content -LiteralPath (Join-Path $demoDir 'settings.json') -Raw).Trim() -ne $marker) { throw 'Upgrade changed preferences' }
    Run-Hidden (Join-Path $installDir 'unins000.exe') @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$testRoot\uninstall.log`"")
    $uninstalled = $true
    if (Test-Path -LiteralPath $exe) { throw 'Uninstall left the application executable' }
    if (Test-Path -LiteralPath $uninstallKey) { throw 'Uninstall registration remains' }
    if (Get-ItemProperty -LiteralPath $startupKey -Name AudioAnchor -ErrorAction SilentlyContinue) { throw 'Startup registration remains' }
    if (-not (Test-Path -LiteralPath (Join-Path $demoDir 'settings.json'))) { throw 'Uninstall deleted unrelated demo preferences' }
    Write-Output "Installer lifecycle passed. Logs: $testRoot"
} finally {
    if (-not $uninstalled -and (Test-Path -LiteralPath (Join-Path $installDir 'unins000.exe'))) {
        Run-Hidden (Join-Path $installDir 'unins000.exe') @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
    }
    $results = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/test-results/installer'
    New-Item -ItemType Directory -Force -Path $results | Out-Null
    Get-ChildItem -LiteralPath $testRoot -Filter '*.log' | Copy-Item -Destination $results
}
