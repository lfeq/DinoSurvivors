#!/usr/bin/env bash
# Builds a single-file, self-contained macOS (Apple Silicon / arm64) build and zips it.
# Output: DinoSurvivors-macOS.zip in the project root.
set -euo pipefail

# Resolve project root (parent of this script's directory) regardless of where it's called from.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

RID="osx-arm64"
PUBLISH_DIR="bin/Release/net9.0/$RID/publish"
ZIP_NAME="DinoSurvivors-macOS.zip"

echo "==> Cleaning previous publish output..."
rm -rf "$PUBLISH_DIR"

echo "==> Publishing single-file self-contained $RID build..."
dotnet publish DinoSurvivors.csproj \
    -c Release \
    -r "$RID" \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=none \
    -p:DebugSymbols=false

echo "==> Marking binary executable and clearing quarantine flag..."
chmod +x "$PUBLISH_DIR/DinoSurvivors"
xattr -cr "$PUBLISH_DIR/DinoSurvivors" 2>/dev/null || true

echo "==> Zipping..."
rm -f "$ZIP_NAME"
( cd "$PUBLISH_DIR" && ditto -c -k --sequesterRsrc . "$ROOT/$ZIP_NAME" )

echo ""
echo "Done. Share this file:"
echo "  $ROOT/$ZIP_NAME"
echo ""
echo "Mac friend: unzip, then in Terminal run:  ./DinoSurvivors"
echo "Or in Finder: right-click DinoSurvivors -> Open -> Open (first time only,"
echo "to get past Gatekeeper since the app is unsigned)."
