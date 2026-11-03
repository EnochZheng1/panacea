#!/bin/bash
# Build the C# project and render one frame of the main scene to a PNG (headless, software Vulkan).
# Usage: tools/shot.sh out.png [frames] [extra user args, e.g. --cam glass]
set -e
cd "$(dirname "$0")/.."
export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet
dotnet build -nologo -v q | grep -E "error" && exit 1 || true
xvfb-run -a -s "-screen 0 1280x800x24" ~/tools/godot.sh --path . --rendering-driver vulkan -- --shot "$1" --frames "${2:-24}" "${@:3}" 2>&1 | grep -E "ERROR|SCRIPT|Saved" | grep -v ERR_CANT_OPEN
