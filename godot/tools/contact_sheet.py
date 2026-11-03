#!/usr/bin/env python3
"""Lay rendered art cards side by side, with their transparent sprites at in-game size beneath.
Usage: contact_sheet.py out.jpg name[=Label] ...   (names refer to assets/art/<name>.card.png)"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(__file__), '..')
ART = os.path.join(ROOT, 'assets', 'art')
FONT = os.path.join(ROOT, 'assets', 'fonts', 'IMFeENrm28P.ttf')
FONT_IT = os.path.join(ROOT, 'assets', 'fonts', 'IMFeENit28P.ttf')


def main(out, specs):
    items = [(s.split('=', 1) + [s])[:2] for s in specs]
    H, pad, sh = 900, 24, 230
    cards = [Image.open(os.path.join(ART, n + '.card.png')).convert('RGB') for n, _ in items]
    cards = [c.resize((int(c.width * H / c.height), H), Image.LANCZOS) for c in cards]
    sprites = [Image.open(os.path.join(ART, n + '.png')).convert('RGBA') for n, _ in items]
    sprites = [s.crop(s.getbbox()) for s in sprites]
    sprites = [s.resize((int(s.width * sh / s.height), sh), Image.LANCZOS) for s in sprites]
    W = sum(c.width for c in cards) + pad * (len(cards) + 1)
    f, fs = ImageFont.truetype(FONT, 30), ImageFont.truetype(FONT_IT, 24)
    sheet = Image.new('RGB', (W, H + sh + pad * 5 + 90), (28, 24, 20))
    d = ImageDraw.Draw(sheet)
    x = pad
    for c, (_, label) in zip(cards, items):
        d.text((x, pad - 2), label, fill=(221, 208, 174), font=f)
        sheet.paste(c, (x, pad + 40))
        x += c.width + pad
    y = H + pad * 3 + 40
    d.text((pad, y - 34), 'Sprites alone, at roughly in-game size, on a neutral ground:', fill=(221, 208, 174), font=fs)
    sheet.paste(Image.new('RGB', (W - 2 * pad, sh + pad), (120, 110, 92)), (pad, y))
    x = pad
    for c, s in zip(cards, sprites):
        sheet.paste(s, (x + (c.width - s.width) // 2, y + pad // 2), s)
        x += c.width + pad
    sheet.save(out, quality=88)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2:])
