$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$tools = Join-Path $repo '.tools'
$compiler = Join-Path $tools 'inno/ISCC.exe'
if (Test-Path -LiteralPath $compiler) { Write-Output $compiler; return }
New-Item -ItemType Directory -Force -Path $tools | Out-Null
$installer = Join-Path $tools 'innosetup-7.1.0-x64.exe'
Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe' -OutFile $installer
$expected = '0362a383ed217d4c4239b5933866dd96d3eb2102737da92f80f6057a4b40df2f'
if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Inno compiler installer checksum mismatch' }
if ((Get-AuthenticodeSignature -LiteralPath $installer).Status -ne 'Valid') { throw 'Inno installer signature is invalid' }
$target = Join-Path $tools 'inno'
$process = Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER',"/DIR=`"$target`"") -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(120000) -or $process.ExitCode -ne 0) { throw 'Inno Setup compiler installation failed' }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Inno compiler missing after installation' }
Write-Output $compiler
