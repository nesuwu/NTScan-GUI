param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$apiUrl = "https://api.github.com/repos/nesuwu/NTScan/releases/latest"
$headers = @{
    "User-Agent" = "NTScanGUI-Build"
}

$release = Invoke-RestMethod -Uri $apiUrl -Headers $headers
$asset = $release.assets | Where-Object { $_.name -ieq "ntscan.dll" } | Select-Object -First 1

if (-not $asset) {
    $asset = $release.assets | Where-Object { $_.name -imatch "\.dll$" } | Select-Object -First 1
}

if (-not $asset) {
    throw "No .dll asset found in latest NTScan release ($($release.tag_name))."
}

$outputDir = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

Write-Host "Downloading $($asset.name) from NTScan release $($release.tag_name)..."
Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $OutputPath
Write-Host "Saved native library to $OutputPath"
