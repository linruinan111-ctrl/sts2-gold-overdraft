#!/bin/sh
set -eu

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
game_app="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app"
game_data="$game_app/Contents/Resources/data_sts2_macos_arm64"
mod_dir="$game_app/Contents/MacOS/mods/OverdraftMod"

if command -v dotnet >/dev/null 2>&1; then
    dotnet_bin=$(command -v dotnet)
else
    dotnet_bin="$HOME/.dotnet/dotnet"
fi

if [ ! -x "$dotnet_bin" ]; then
    echo "Missing .NET 9 SDK: https://dotnet.microsoft.com/download/dotnet/9.0" >&2
    exit 1
fi

if [ ! -f "$game_data/sts2.dll" ]; then
    echo "Slay the Spire 2 is not installed at the expected Steam location." >&2
    exit 1
fi

"$dotnet_bin" build "$project_dir/OverdraftMod.csproj" \
    -c Release -p:Sts2DataDir="$game_data"

mkdir -p "$mod_dir"
install -m 644 "$project_dir/OverdraftMod.json" "$mod_dir/OverdraftMod.json"
install -m 644 "$project_dir/bin/Release/net9.0/OverdraftMod.dll" "$mod_dir/OverdraftMod.dll"

echo "Installed Gold Overdraft to $mod_dir. Restart the game to load it."
