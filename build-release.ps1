#!/usr/bin/env pwsh

# Build Release Script for Hex Wargame
# This script builds optimized release versions for distribution

param(
    [switch]$Clean,
    [switch]$SingleFile,
    [switch]$FrameworkDependent,
    [switch]$All
)

$ProjectFile = "HexWargame.csproj"
$DistDir = "dist"

Write-Host "🎮 Building Hex Wargame Release Versions" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Clean previous builds if requested
if ($Clean -or $All) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $DistDir) {
        Remove-Item $DistDir -Recurse -Force
    }
    dotnet clean $ProjectFile --configuration Release
    Write-Host "✅ Clean completed" -ForegroundColor Green
}

# Create dist directory
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

# Build Single File Release (recommended for distribution)
if ($SingleFile -or $All) {
    Write-Host "📦 Building Single File Release..." -ForegroundColor Yellow
    $OutputPath = "$DistDir/win-x64-single-file"
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --self-contained true `
        --runtime win-x64 `
        --output $OutputPath `
        /p:PublishSingleFile=true `
        /p:PublishReadyToRun=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true
    
    if ($LASTEXITCODE -eq 0) {
        $ExeSize = (Get-Item "$OutputPath/HexWargame.exe").Length / 1MB
        Write-Host "✅ Single File build completed" -ForegroundColor Green
        Write-Host "   📍 Location: $OutputPath" -ForegroundColor Cyan
        Write-Host "   📏 Size: $([math]::Round($ExeSize, 2)) MB" -ForegroundColor Cyan
        Write-Host "   🚀 Ready for distribution - no .NET runtime required!" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Single File build failed" -ForegroundColor Red
    }
}

# Build Framework Dependent Release (smaller, requires .NET runtime)
if ($FrameworkDependent -or $All) {
    Write-Host "📦 Building Framework Dependent Release..." -ForegroundColor Yellow
    $OutputPath = "$DistDir/framework-dependent"
    
    dotnet publish $ProjectFile `
        --configuration Release `
        --self-contained false `
        --output $OutputPath
    
    if ($LASTEXITCODE -eq 0) {
        $ExeSize = (Get-Item "$OutputPath/HexWargame.exe").Length / 1KB
        Write-Host "✅ Framework Dependent build completed" -ForegroundColor Green
        Write-Host "   📍 Location: $OutputPath" -ForegroundColor Cyan
        Write-Host "   📏 Size: $([math]::Round($ExeSize, 2)) KB" -ForegroundColor Cyan
        Write-Host "   ⚠️  Requires .NET 8 runtime on target machine" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Framework Dependent build failed" -ForegroundColor Red
    }
}

# Show usage if no parameters
if (-not ($Clean -or $SingleFile -or $FrameworkDependent -or $All)) {
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -All                    # Build all versions" -ForegroundColor Cyan
    Write-Host "  .\build-release.ps1 -SingleFile            # Build single executable" -ForegroundColor Cyan
    Write-Host "  .\build-release.ps1 -FrameworkDependent    # Build framework dependent" -ForegroundColor Cyan
    Write-Host "  .\build-release.ps1 -Clean                 # Clean builds" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Recommended for distribution: -SingleFile" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎯 Build process completed!" -ForegroundColor Green