Set-StrictMode -Version Latest

function ConvertTo-ProductVersion([string]$Value) {
    if ($Value -cnotmatch '\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z') {
        throw "Invalid version '$Value'. Use stable SemVer MAJOR.MINOR.PATCH without leading zeros."
    }
    $parts = $Value.Split('.')
    foreach ($part in $parts) {
        [uint16]$number = 0
        if (-not [uint16]::TryParse($part, [ref]$number) -or $number -gt 65534) {
            throw 'Version components must be 0..65534 for Windows assembly/installer metadata.'
        }
    }
    return [version]$Value
}

function Read-VersionXml([string]$Content) {
    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($Content), $settings)
    try {
        $document = [System.Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        $nodes = $document.SelectNodes('/Project/PropertyGroup/Version')
        if ($nodes.Count -ne 1) { throw 'The manifest must contain exactly one Project/PropertyGroup/Version.' }
        return ConvertTo-ProductVersion $nodes[0].InnerText.Trim()
    } finally { $reader.Dispose() }
}

function Get-ProductVersion([string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent)) {
    return Read-VersionXml (Get-Content -LiteralPath (Join-Path $RepositoryRoot 'version.props') -Raw -ErrorAction Stop)
}

function Assert-VersionIncrease([version]$Current, [version]$Previous) {
    if ($Current -le $Previous) {
        throw "Every PR to master MUST increase version.props. Current: $Current; target branch: $Previous. The developer chooses major, minor, or patch."
    }
}
