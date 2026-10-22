#!/usr/bin/env bash
# Fast inner loop: core tests outside Unity. Usage: scripts/test.sh [--filter Name]
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test dotnet/DeskButler.Core.Tests.csproj --nologo -v q "$@"
