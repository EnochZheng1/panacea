#!/usr/bin/env python3
"""Fail if any cut in assets/art/cuts/ is stale: not what make_cuts.py would make from today's
sprites. Run after rendering art (and before committing it), so a re-drawn sprite can never sit
unused behind an old crop again: python3 tools/check_cuts.py"""
import io, os, sys
from PIL import Image, ImageChops
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_cuts as mc

stale = []
for n in mc.SPRITES:
    im = Image.open(os.path.join(mc.ART, n + '.png'))
    x0, y0, x1, y1 = im.getbbox()
    m = 12
    want = im.crop((max(0, x0 - m), max(0, y0 - m), min(im.width, x1 + m), min(im.height, y1 + m)))
    have = Image.open(os.path.join(mc.OUT, n + '.png'))
    if want.size != have.size or ImageChops.difference(want.convert('RGBA'), have.convert('RGBA')).getbbox():
        stale.append(n)
if stale:
    print('stale cuts (run tools/make_cuts.py):', ', '.join(stale))
    sys.exit(1)
print('cuts up to date:', len(mc.SPRITES))
