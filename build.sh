#!/usr/bin/env bash
# Build and (with --install) copy into Valheim. Don't install while the game runs.
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_ROOT=~/.local/opt/dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$DOTNET_ROOT/dotnet" build -c Release -v q
if [ "${1:-}" = --install ]; then
  pgrep -f '[v]alheim.x86_64' >/dev/null && { echo "Valheim is running - close it first"; exit 1; }
  d=~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/Remi-CombatStamina
  mkdir -p "$d" && cp bin/Release/net462/CombatStamina.dll "$d/" && echo "installed -> $d"
fi
