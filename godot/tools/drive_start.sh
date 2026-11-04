#!/bin/bash
# Start the game for scripted playtesting, listening for driver commands on 127.0.0.1:PORT.
# Usage: tools/drive_start.sh PORT [seed] [extra game args...]   (run from the godot/ folder)
# Logs to /tmp/panacea-driver-PORT.log; the PID is in /tmp/panacea-driver-PORT.pid.
# Stop with: kill $(cat /tmp/panacea-driver-PORT.pid)
PORT=$1; SEED=${2:-ward-7}; shift 2 || shift $#
cd "$(dirname "$0")/.."
LOG=/tmp/panacea-driver-$PORT.log
nohup xvfb-run -a -s "-screen 0 1280x800x24" ~/tools/godot.sh --path . --rendering-driver vulkan --resolution 1280x800 -- --driver "$PORT" --seed "$SEED" "$@" > "$LOG" 2>&1 &
echo $! > /tmp/panacea-driver-$PORT.pid
for i in $(seq 1 120); do grep -q "DRIVER listening" "$LOG" 2>/dev/null && { echo "ready on $PORT"; exit 0; }; sleep 1; done
echo "game did not start; see $LOG"; exit 1
