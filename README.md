# NTScanGUI

NTScanGUI is a WinUI 3 **tech demo** for the NTScan native library.

NTScan project: [https://github.com/nesuwu/NTScan](https://github.com/nesuwu/NTScan)

## What This Project Is

This app is meant to showcase and exercise NTScan functionality from a Windows desktop UI. It is not intended to be a polished end-user product. The focus is validating API integration, scan workflows, and result presentation.

## Demo Capabilities

- Run directory scans in fast or accurate mode
- Inspect per-entry logical, allocated, and ADS size data
- Detect duplicate groups and estimate reclaimable bytes

## Requirements

- Windows 10/11 (x64)
- .NET 8 SDK
- Network access during build (used to download the latest NTScan release `.dll`)

## Build

```powershell
dotnet build NTScanGUI.sln
```

During build, the project fetches the latest NTScan GitHub release metadata from:

```text
https://api.github.com/repos/nesuwu/NTScan/releases/latest
```

Then it downloads the release `.dll` asset and copies it into the application output directory as `ntscan.dll`.

## Run

```powershell
dotnet run --project NTScanGUI.csproj
```
