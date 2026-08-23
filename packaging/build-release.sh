#!/usr/bin/env bash
#
# Assemble a Thunderstore-ready release zip.
#
# manifest.json's version_number is the ONLY place to bump the version, this
# script syncs it into EffectPreview.csproj and PluginInfo.cs before building
# (CHANGELOG.md stays hand-maintained, this only warns if the new version
# has no entry yet).
#
# Output: dist/EffectPreview-<version>.zip with everything at the zip ROOT:
#   manifest.json
#   icon.png            (256x256)
#   README.md
#   CHANGELOG.md
#   LICENSE             (if present)
#   EffectPreview.dll
#
# r2modman installs the whole package into BepInEx/plugins/<Team>-EffectPreview/,
# so a root-level DLL lands correctly and BepInEx loads it.
#
# Also writes two Nexus Mods distributions, each the same file set as the
# Thunderstore zip but nested one level under an OnlyCook-EffectPreview/
# folder, so extracting it straight into BepInEx/plugins/ produces the
# correct layout for a manual install:
#     dist/nexus/EffectPreview-<version>-nexus.zip (normal defaults)
#     dist/nexus/EffectPreview-<version>-nexus-bars-on.zip (ShowGhostBarCounts
# and ShowVanillaBarCounts default to true, via the BARS_DEFAULT_ON
# compile-time define in PluginConfig.cs)
#
# Usage:  bash packaging/build-release.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PKG="$REPO_ROOT/packaging"
PROJ="$REPO_ROOT/src/EffectPreview"
DIST="$REPO_ROOT/dist"
NEXUS_FOLDER="OnlyCook-EffectPreview"

# Version comes from manifest.json (single source of truth for the package).
# Bump it there ONLY, everything below mirrors it, nothing else should ever be
# hand-edited to a new version number.
VERSION="$(grep -oE '"version_number"[[:space:]]*:[[:space:]]*"[^"]+"' "$PKG/manifest.json" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')"
if [[ -z "$VERSION" ]]; then echo "ERROR: could not read version_number from manifest.json" >&2; exit 1; fi
echo "Packaging EffectPreview v$VERSION"

# 0. Sync the version into every other place it's declared.
echo "Syncing version $VERSION into csproj / PluginInfo.cs..."

sed -i -E "s#(<Version>)[0-9]+\.[0-9]+\.[0-9]+(</Version>)#\1$VERSION\2#" \
  "$PROJ/EffectPreview.csproj"

sed -i -E "s#(public const string Version = \")[0-9]+\.[0-9]+\.[0-9]+(\";)#\1$VERSION\2#" \
  "$PROJ/PluginInfo.cs"

# CHANGELOG.md is hand-maintained (one heading per release, old entries must
# never be touched), so this is just a nudge, not an auto-edit.
if ! grep -q "^## $VERSION" "$PKG/CHANGELOG.md"; then
  echo "WARNING: packaging/CHANGELOG.md has no '## $VERSION' entry yet, add one before publishing." >&2
fi

# 0.5. Keep the repo-root README.md in sync with the packaged README (single source).
bash "$PKG/gen-readme.sh"

# 1. Build the DLL (Release).
echo "Building..."
dotnet build "$PROJ/EffectPreview.csproj" -c Release >/dev/null
DLL="$PROJ/bin/Release/EffectPreview.dll"
[[ -f "$DLL" ]] || { echo "ERROR: build output not found: $DLL" >&2; exit 1; }

# 2. Validate the icon is exactly 256x256 (Thunderstore requirement).
if command -v python3 >/dev/null 2>&1; then
  python3 - "$PKG/icon.png" <<'PY'
import sys
from PIL import Image
w,h = Image.open(sys.argv[1]).size
assert (w,h)==(256,256), f"icon.png must be 256x256, got {w}x{h}"
PY
fi

# 3. Stage.
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp "$PKG/manifest.json" "$PKG/icon.png" "$PKG/README.md" "$PKG/CHANGELOG.md" "$STAGE/"
[[ -f "$REPO_ROOT/LICENSE" ]] && cp "$REPO_ROOT/LICENSE" "$STAGE/LICENSE" || echo "NOTE: no LICENSE file yet (pick one before publishing)."
cp "$DLL" "$STAGE/EffectPreview.dll"

# 4. Zip (files at the root of the archive).
mkdir -p "$DIST"
OUT="$DIST/EffectPreview-$VERSION.zip"
rm -f "$OUT"
( cd "$STAGE" && zip -r -q "$OUT" . )
echo "Wrote $OUT"
unzip -l "$OUT"

# 5. Nexus dists: same file set as the Thunderstore zip, nested under a
#    mod-name folder so a manual extract into BepInEx/plugins/ lands
#    correctly. Builds one zip per DLL passed in.
NEXUS_DIST="$DIST/nexus"
mkdir -p "$NEXUS_DIST"

package_nexus_zip() {
  local dll="$1" suffix="$2"
  local stage nexus_stage out
  stage="$(mktemp -d)"; nexus_stage="$(mktemp -d)"
  trap "rm -rf '$stage' '$nexus_stage'" RETURN
  cp "$PKG/manifest.json" "$PKG/icon.png" "$PKG/README.md" "$PKG/CHANGELOG.md" "$stage/"
  [[ -f "$REPO_ROOT/LICENSE" ]] && cp "$REPO_ROOT/LICENSE" "$stage/LICENSE"
  cp "$dll" "$stage/EffectPreview.dll"
  mkdir -p "$nexus_stage/$NEXUS_FOLDER"
  cp -r "$stage/." "$nexus_stage/$NEXUS_FOLDER/"
  out="$NEXUS_DIST/EffectPreview-$VERSION$suffix.zip"
  rm -f "$out"
  ( cd "$nexus_stage" && zip -r -q "$out" . )
  echo "Wrote $out"
  unzip -l "$out"
}

package_nexus_zip "$DLL" "-nexus"

# 5.5. Bars-on variant: rebuild with ShowGhostBarCounts / ShowVanillaBarCounts
#      defaulting to true (BARS_DEFAULT_ON), for players who want the bar
#      count numbers on out of the box
echo "Building bars-on variant..."
dotnet build "$PROJ/EffectPreview.csproj" -c Release -p:DefineConstants=BARS_DEFAULT_ON >/dev/null
BARS_ON_DLL="$PROJ/bin/Release/EffectPreview.dll"
[[ -f "$BARS_ON_DLL" ]] || { echo "ERROR: bars-on build output not found: $BARS_ON_DLL" >&2; exit 1; }

package_nexus_zip "$BARS_ON_DLL" "-nexus-bars-on"
