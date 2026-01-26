#!/bin/bash
# Build script for creating MDK Hub AppImage

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/appimage-build"
APPDIR="$BUILD_DIR/AppDir"

echo "Building MDK Hub AppImage..."

# Clean previous build
rm -rf "$BUILD_DIR"
mkdir -p "$APPDIR"

# Get version from PackageVersion.txt
VERSION=$(cat "$SCRIPT_DIR/PackageVersion.txt" | tr -d '\r' | tr -d '\n' | xargs)
echo "Version: $VERSION"

# Publish the application (multi-file, not single-file to avoid Avalonia issues)
echo "Publishing application..."
cd "$SCRIPT_DIR"
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o "$BUILD_DIR/publish"

# Create AppDir structure
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$APPDIR/usr/share/icons/hicolor/scalable/apps"

# Copy all published files (multi-file publish)
echo "Copying published files..."
cp -r "$BUILD_DIR/publish/"* "$APPDIR/usr/bin/"
chmod +x "$APPDIR/usr/bin/Mdk.Hub"

# Copy desktop file
cp "$SCRIPT_DIR/mdk-hub.desktop" "$APPDIR/usr/share/applications/"
cp "$SCRIPT_DIR/mdk-hub.desktop" "$APPDIR/"

# Copy icon
cp "$SCRIPT_DIR/Assets/malware256.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/mdk-hub.png"
cp "$SCRIPT_DIR/Assets/malware256.png" "$APPDIR/mdk-hub.png"

# Create AppRun
cat > "$APPDIR/AppRun" << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin:${PATH}"
exec "${HERE}/usr/bin/Mdk.Hub" "$@"
EOF
chmod +x "$APPDIR/AppRun"

# Download appimagetool if not present
APPIMAGETOOL="$BUILD_DIR/appimagetool-x86_64.AppImage"
if [ ! -f "$APPIMAGETOOL" ]; then
    echo "Downloading appimagetool..."
    wget -q "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" -O "$APPIMAGETOOL"
    chmod +x "$APPIMAGETOOL"
fi

# Build AppImage
echo "Creating AppImage..."
OUTPUT="$SCRIPT_DIR/MDKHub-$VERSION-x86_64.AppImage"
ARCH=x86_64 "$APPIMAGETOOL" "$APPDIR" "$OUTPUT"

echo "AppImage created: $OUTPUT"
