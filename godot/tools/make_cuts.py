#!/usr/bin/env python3
"""Crop rendered woodcut sprites to their ink (plus a margin) into assets/art/cuts/, so layout
assets (adverts, almanac, the newspaper) can place them as engraved "cuts".
Run after render_art.mjs has rendered the sprites at --scale 2."""
import os
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ART = os.path.join(ROOT, 'assets', 'art')
OUT = os.path.join(ART, 'cuts')
SPRITES = ['wc_proprietor', 'wc_death', 'wc_clerk', 'wc_mother', 'wc_longshoreman', 'wc_lady',
           'wc_seamstress', 'wc_widow', 'wc_oldman',
           # customer variants, two per trade (see scripts/UI/Cuts.cs)
           'wc_longshoreman_b', 'wc_longshoreman_c', 'wc_clerk_b', 'wc_clerk_c', 'wc_seamstress_b', 'wc_seamstress_c',
           'wc_widow_b', 'wc_widow_c', 'wc_oldman_b', 'wc_oldman_c', 'wc_mother_b', 'wc_mother_c', 'wc_lady_b', 'wc_lady_c',
           'wc_bottle_panacea', 'wc_bottle_syrup', 'wc_bottle_bitters', 'wc_pills',
           'seal_coroner', 'seal_health', 'seal_sanitary', 'seal_college', 'seal_league', 'seal_hobart']
# named sub-crops in sprite pixels (2x): name -> (sprite, box)
BUSTS = {'wc_clerk_bust': ('wc_clerk', (380, 300, 820, 840))}


def main():
    os.makedirs(OUT, exist_ok=True)
    for n in SPRITES:
        im = Image.open(os.path.join(ART, n + '.png'))
        x0, y0, x1, y1 = im.getbbox()
        m = 12
        im.crop((max(0, x0 - m), max(0, y0 - m), min(im.width, x1 + m), min(im.height, y1 + m))).save(os.path.join(OUT, n + '.png'))
    for n, (src, box) in BUSTS.items():
        Image.open(os.path.join(ART, src + '.png')).crop(box).save(os.path.join(OUT, n + '.png'))
    print('cuts:', len(SPRITES) + len(BUSTS))


if __name__ == '__main__':
    main()
