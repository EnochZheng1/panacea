#!/usr/bin/env python3
"""Send driver actions to a running game (see scripts/Driver.cs) and print each reply.
Usage: tools/drive.py PORT '[{"click":[640,400]},{"wait":500},{"shot":"/abs/a.png"}]'
       tools/drive.py PORT targets      (shortcut for [{"targets":true}])
       tools/drive.py PORT read | state
"""
import json, socket, sys

port = int(sys.argv[1])
arg = sys.argv[2] if len(sys.argv) > 2 else 'state'
actions = [{arg: True}] if arg in ('targets', 'read', 'state') else json.loads(arg)
s = socket.create_connection(('127.0.0.1', port), timeout=120)
f = s.makefile('rw', encoding='utf-8', newline='\n')
for a in actions:
    f.write(json.dumps(a) + '\n'); f.flush()
    line = f.readline()
    if not line:
        print('connection closed'); break
    print(line.strip())
s.close()
