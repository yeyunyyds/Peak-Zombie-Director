#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore tests/PeakZombieDirector.Tests --locked-mode
dotnet test tests/PeakZombieDirector.Tests -c Release --no-restore --logger 'trx;LogFileName=core.trx'
