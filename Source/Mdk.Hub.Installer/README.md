# MDK Hub Installers

This directory contains the installer configuration for the MDK Hub application.

## Windows Installer (MSI)

The Windows installer is built using **WiX Toolset v4** and creates a professional MSI package.

### Features:
- Full MSI installer with proper Windows integration
- Start Menu shortcut automatically created
- Appears in Add/Remove Programs
- Launch application checkbox on final installer screen
- Automatic upgrade of previous versions
- Clean uninstall support

### Building Locally:

1. Install WiX Toolset v6:
   ```bash
   dotnet tool install --global wix --version 6.0.2
   ```

2. Build the installer:
   ```bash
   cd Source
   wix build Mdk.Hub.Installer/Mdk.Hub.Installer.wixproj -arch x64 -o output-msi/
   ```

The MSI will be created in the `output-msi` directory.

### Customization:

- **Desktop shortcut**: Uncomment the DesktopShortcut section in `Product.wxs`
- **Icon**: Automatically uses `Assets/malware.ico`
- **Version**: Automatically extracted from the executable

## Linux AppImage

The Linux installer creates a portable AppImage that runs on most Linux distributions.

### Features:
- Single-file executable
- No installation required
- Works across distributions (Ubuntu, Fedora, openSUSE, etc.)
- Includes desktop integration files
- Self-contained with all dependencies

### Building Locally:

1. Make sure you have .NET 9 SDK installed

2. Run the build script:
   ```bash
   cd Source/Mdk.Hub
   chmod +x build-appimage.sh
   ./build-appimage.sh
   ```

The AppImage will be created in the `Source/Mdk.Hub` directory with the name `MDKHub-{version}-x86_64.AppImage`.

### Running the AppImage:

```bash
chmod +x MDKHub-*.AppImage
./MDKHub-*.AppImage
```

## Continuous Integration

Both installers are built automatically via GitHub Actions when code is pushed to the repository:

- **Windows MSI**: Built on `windows-latest` runner
- **Linux AppImage**: Built on `ubuntu-latest` runner

The artifacts are available for download from the GitHub Actions run page, and are automatically attached to GitHub Releases.

## Version Management

Both installers automatically read version information from `Source/Mdk.Hub/PackageVersion.txt`. 

To release a new version:
1. Update `PackageVersion.txt`
2. Commit and push
3. GitHub Actions will build new installers with the updated version

## Troubleshooting

### Windows MSI Build Fails

- Ensure WiX Toolset v6 is installed: `wix --version`
- Check that .NET 9 SDK is installed: `dotnet --version`
- Verify the Hub application builds: `dotnet build Mdk.Hub/Mdk.Hub.csproj`

### Linux AppImage Build Fails

- Ensure .NET 9 SDK supports linux-x64 runtime
- Check that `wget` is available for downloading appimagetool
- Verify script has execute permissions: `chmod +x build-appimage.sh`

## Notes

- The **UpgradeCode** GUID in `Product.wxs` must remain constant across all versions for proper upgrade functionality
- The MSI installer requires the application to be published before building the installer
- AppImage requires `appimagetool` which is automatically downloaded during build
- Using WiX Toolset v6.0.2 (latest stable version)
