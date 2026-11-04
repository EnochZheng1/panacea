#!/bin/bash
# Stop the game started by drive_start.sh on PORT (xvfb-run, Godot and all).
PORT=$1
pkill -f -- "--driver $PORT( |$)" 2>/dev/null
[ -f /tmp/panacea-driver-$PORT.pid ] && kill $(cat /tmp/panacea-driver-$PORT.pid) 2>/dev/null
rm -f /tmp/panacea-driver-$PORT.pid
for i in $(seq 1 20); do pgrep -f -- "--driver $PORT( |$)" >/dev/null || { echo "stopped $PORT"; exit 0; }; sleep 0.5; done
pkill -9 -f -- "--driver $PORT( |$)"; echo "stopped $PORT (forced)"
