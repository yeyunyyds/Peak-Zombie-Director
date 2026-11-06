#!/usr/bin/env bash
# Tested Linux x64 onboarding; retains no live services and creates no worktrees.
set -euo pipefail
cd /workspace/Peak-Zombie-Director
mkdir -p /workspace/tooling /workspace/peak-reference/managed /workspace/peak-reference/mods
cache=/workspace/tooling/downloads
mkdir -p "$cache"
if [[ ! -x /workspace/tooling/dotnet/dotnet ]]; then
  curl -fL --retry 2 https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.427/dotnet-sdk-8.0.427-linux-x64.tar.gz -o "$cache/dotnet-sdk.tar.gz"
  printf '%s  %s\n' '85725692f465f5024b9b42a159a29d9f73e8c57ad9e8bfe1e7028a505d18031d68931e8aa3f9de44145b08170dde897c8a78e1aa18d4219698b6891af7095d39' "$cache/dotnet-sdk.tar.gz" | sha512sum -c -
  mkdir -p /workspace/tooling/dotnet
  tar -xzf "$cache/dotnet-sdk.tar.gz" -C /workspace/tooling/dotnet
fi
export DOTNET_ROOT=/workspace/tooling/dotnet
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PEAK_MANAGED_DIR=/workspace/peak-reference/managed
export PEAK_MOD_REFERENCE_DIR=/workspace/peak-reference/mods
# Public reference artifacts are for compilation/research only, never packaged.
if [[ ! -f "$PEAK_MANAGED_DIR/Assembly-CSharp.dll" ]]; then
  curl -fL --retry 2 https://codeload.github.com/xiaofe12/ShootZombies/tar.gz/2f2f844408c25588c22a95c0383d91336e7d04e8 -o "$cache/shoot-reference.tar.gz"
  printf '%s  %s\n' bd6b2c008cd68798f498cad15eb85549eb49891d6ec887c74c6329c30b0d8417 "$cache/shoot-reference.tar.gz" | sha256sum -c -
  mkdir -p "$cache/reference-source"
  tar -xzf "$cache/shoot-reference.tar.gz" -C "$cache/reference-source" --strip-components=1
  cp "$cache/reference-source"/libs/*.dll "$PEAK_MANAGED_DIR/"
fi
if [[ ! -f "$PEAK_MOD_REFERENCE_DIR/Zombies Anywhere.dll" ]]; then
  curl -fL --retry 2 https://thunderstore.io/package/download/tony4twenty/PEAK_Zombies/2.0.0/ -o "$cache/zombies.zip"
  printf '%s  %s\n' 905a369dad057f5f6f7ecf246ca092793a986e7b4ff0844c64b94d4af7a75e3d "$cache/zombies.zip" | sha256sum -c -
  unzip -p "$cache/zombies.zip" 'Zombies Anywhere.dll' > "$PEAK_MOD_REFERENCE_DIR/Zombies Anywhere.dll"
fi
for assembly in BepInEx.dll 0Harmony.dll; do
  if [[ ! -f "$PEAK_MOD_REFERENCE_DIR/$assembly" ]]; then cp "$PEAK_MANAGED_DIR/$assembly" "$PEAK_MOD_REFERENCE_DIR/"; fi
done
bash scripts/build.sh
bash scripts/test.sh
python3 scripts/package.py
