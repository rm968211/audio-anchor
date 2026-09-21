param([switch]$SkipUi)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    & ./scripts/Test-Versioning.ps1
    & ./scripts/Test-Releases.ps1
    dotnet restore AudioAnchor.slnx --configfile NuGet.config
    if ($LASTEXITCODE) { throw 'Restore failed' }
    dotnet build AudioAnchor.slnx -c Release --no-restore
    if ($LASTEXITCODE) { throw 'Build failed' }
    dotnet test tests/AudioAnchor.Tests -c Release --no-build --logger 'trx;LogFileName=unit-integration.trx' --collect 'XPlat Code Coverage' --results-directory artifacts/test-results
    if ($LASTEXITCODE) { throw 'Unit/integration tests failed' }
    if (-not $SkipUi) {
        $env:AUDIOANCHOR_TEST_RESULTS = Join-Path $repo 'artifacts/test-results'
        dotnet test tests/AudioAnchor.UiTests -c Release --no-build --logger 'trx;LogFileName=ui.trx' --results-directory artifacts/test-results
        if ($LASTEXITCODE) { throw 'UI automation failed' }
    }
} finally { Pop-Location }
