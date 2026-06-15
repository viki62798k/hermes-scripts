$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $root "bin"
$outFile = Join-Path $outDir "AIOptimizeTool.exe"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    throw "Cannot find .NET Framework C# compiler csc.exe"
}

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

& $csc `
    /codepage:65001 `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    /win32manifest:"$root\app.manifest" `
    /win32icon:"$root\ai-assistant.ico" `
    /out:"$outFile" `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$root\Program.cs"

Write-Host "Build complete: $outFile" -ForegroundColor Green
