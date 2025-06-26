# Hex Wargame - Distribution Guide

## Building Release Versions

### Quick Start (Recommended)
**For single executable distribution:**
```bash
# PowerShell
.\build-release.ps1 -SingleFile

# Command Prompt  
build-release.bat

# VS Code
Ctrl+Shift+P → "Tasks: Run Task" → "Publish Release (Single File)"
```

### Build Options

#### 1. Single File Executable (Recommended)
- **Size**: ~15-25 MB
- **Requirements**: None (includes .NET runtime)
- **Best for**: Distribution to end users

```bash
dotnet publish --configuration Release --self-contained true --runtime win-x64 /p:PublishSingleFile=true
```

#### 2. Framework Dependent
- **Size**: ~200 KB
- **Requirements**: .NET 8 runtime on target machine  
- **Best for**: Corporate environments with .NET already installed

```bash
dotnet publish --configuration Release --self-contained false
```

### Distribution Files
After building, you'll find:
- `dist/win-x64-single-file/HexWargame.exe` - Single file executable (includes custom icon)
- `dist/framework-dependent/` - Framework dependent version (includes custom icon)

### Performance Optimizations Applied
- **ReadyToRun**: Faster startup time
- **Compression**: Smaller file size
- **Native libraries**: Included in single file
- **Trimming**: Removed unused code (disabled for Windows Forms compatibility)

### System Requirements
- **Operating System**: Windows 10/11 (x64)
- **Memory**: 512 MB RAM minimum
- **Storage**: 50 MB available space
- **.NET Runtime**: Not required for single file version

### Troubleshooting
If the game doesn't start:
1. Ensure Windows is up to date
2. Try running as administrator
3. Check Windows Defender hasn't quarantined the file
4. For framework dependent version, install .NET 8 runtime