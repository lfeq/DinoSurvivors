#!/usr/bin/env bash
# Builds a single-file, self-contained Windows (x64) build and zips it for sharing.
# Output: DinoSurvivors-Windows.zip in the project root.
set -euo pipefail

# Resolve project root (parent of this script's directory) regardless of where it's called from.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

RID="win-x64"
PUBLISH_DIR="bin/Release/net9.0/$RID/publish"
ZIP_NAME="DinoSurvivors-Windows.zip"

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

echo "==> Zipping..."
rm -f "$ZIP_NAME"
# Zip the contents of publish/ (just DinoSurvivors.exe) at the top level of the archive.
( cd "$PUBLISH_DIR" && ditto -c -k --sequesterRsrc . "$ROOT/$ZIP_NAME" )

echo ""
echo "Done. Share this file:"
echo "  $ROOT/$ZIP_NAME"
echo ""
echo "Friends: unzip, then double-click DinoSurvivors.exe."
echo "SmartScreen may warn (unsigned) -> More info -> Run anyway."
