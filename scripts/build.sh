#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${PEAK_MANAGED_DIR:?Set PEAK_MANAGED_DIR to PEAK_Data/Managed}"
: "${PEAK_MOD_REFERENCE_DIR:?Set PEAK_MOD_REFERENCE_DIR to BepInEx/Harmony/PEAK Zombies reference directory}"
dotnet restore src/PeakZombieDirector.Plugin --locked-mode
dotnet build src/PeakZombieDirector.Plugin -c Release --no-restore
