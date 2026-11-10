#!/usr/bin/env python3
"""Give IM Fell English lining figures.

IM Fell's figures are old-style (hanging): its 1 is a small-capital I and its 0 a lower-case o, so
"$19.80" reads "$I9.8o" at the bench's sizes, and the font has no `lnum` feature to ask for others.
This writes copies of the roman and the italic whose ten figures are lining figures, all of one
width (so columns of money line up), taken from Liberation Serif (SIL OFL 1.1, as IM Fell is),
scaled to stand a little short of IM Fell's capitals and weighted to match its colour.

    python3 tools/make_lining_figures.py [path/to/LiberationSerif-Regular.ttf path/to/...-Italic.ttf]

Writes assets/fonts/IMFeENrm28P-Lining.ttf and IMFeENit28P-Lining.ttf. The new families are named
"IM FELL English Lining" (IM Fell reserves no font name; Liberation's reserved name is not used).
"""
import os
import sys

from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
FONTS = os.path.join(ROOT, 'assets', 'fonts')
LIB = '/usr/share/fonts/truetype/liberation'
DIGITS = ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine']
# the figures stand at this height in IM Fell's units: above its x-height (938), so a 0 is never an
# o, and a little short of its capitals (1384), as a book face's lining figures do
FIGURE_HEIGHT = 1270
# stroke added round each figure (font units), so Liberation's even strokes print as dark as Fell's
EMBOLDEN = 22


def embolden(glyph, glyf, amount):
    """Pushes each point outward along the bisector of its neighbouring edges (a cheap offset)."""
    if amount == 0 or glyph.numberOfContours <= 0:
        return
    coords = glyph.coordinates
    ends = glyph.endPtsOfContours
    start = 0
    out = list(coords)
    for end in ends:
        pts = list(range(start, end + 1))
        n = len(pts)
        # contour orientation: TrueType outer contours run clockwise, so "outward" is to the left of travel
        area = 0
        for k in range(n):
            x0, y0 = coords[pts[k]]
            x1, y1 = coords[pts[(k + 1) % n]]
            area += x0 * y1 - x1 * y0
        sign = -1 if area < 0 else 1
        for k in range(n):
            px, py = coords[pts[k - 1]]
            cx, cy = coords[pts[k]]
            nx, ny = coords[pts[(k + 1) % n]]

            def norm(dx, dy):
                d = (dx * dx + dy * dy) ** 0.5 or 1
                return dx / d, dy / d
            ax, ay = norm(cx - px, cy - py)
            bx, by = norm(nx - cx, ny - cy)
            # left normals of the two edges, averaged
            mx, my = (-ay - by) / 2, (ax + bx) / 2
            ml = (mx * mx + my * my) ** 0.5 or 1
            k_scale = min(2.0, 1 / ml)
            mx, my = mx / ml * k_scale, my / ml * k_scale
            out[pts[k]] = (round(cx - sign * mx * amount), round(cy - sign * my * amount))
        start = end + 1
    from fontTools.ttLib.tables._g_l_y_f import GlyphCoordinates
    glyph.coordinates = GlyphCoordinates(out)
    glyph.recalcBounds(glyf)


def build(fell_name, lib_path, out_name, family, style):
    fell = TTFont(os.path.join(FONTS, fell_name))
    lib = TTFont(lib_path)
    lib_set = lib.getGlyphSet()
    lib_cmap = lib.getBestCmap()
    fell_cmap = fell.getBestCmap()
    glyf = fell['glyf']
    hmtx = fell['hmtx']
    lib_glyf = lib['glyf']
    tall = max(lib_glyf[lib_cmap[ord(c)]].yMax for c in '0123456789' if hasattr(lib_glyf[lib_cmap[ord(c)]], 'yMax'))
    s = FIGURE_HEIGHT / tall * fell['head'].unitsPerEm / lib['head'].unitsPerEm
    advance = round(lib['hmtx'][lib_cmap[ord('0')]][0] * s) + 2 * EMBOLDEN
    for c in '0123456789':
        src = lib_cmap[ord(c)]
        dst = fell_cmap[ord(c)]
        rec = DecomposingRecordingPen(lib_set)
        lib_set[src].draw(rec)
        pen = TTGlyphPen(None)
        rec.replay(TransformPen(pen, (s, 0, 0, s, EMBOLDEN, 0)))
        g = pen.glyph()
        g.recalcBounds(glyf)
        embolden(g, glyf, EMBOLDEN)
        glyf[dst] = g
        hmtx[dst] = (advance, g.xMin if g.numberOfContours else 0)
    # per-size advance caches would now be wrong; the renderer works them out without them
    for tag in ('hdmx', 'LTSH', 'VDMX'):
        if tag in fell:
            del fell[tag]
    # kerning tuned for the old-style shapes no longer fits them: figures are set solid
    if 'kern' in fell:
        for table in fell['kern'].kernTables:
            for pair in list(table.kernTable):
                if pair[0] in DIGITS or pair[1] in DIGITS:
                    del table.kernTable[pair]
    name = fell['name']
    for rec in name.names:
        if rec.nameID in (1, 16):
            rec.string = family
        elif rec.nameID == 4:
            rec.string = f'{family} {style}'
        elif rec.nameID == 6:
            rec.string = f'{family.replace(" ", "")}-{style}'
        elif rec.nameID == 3:
            rec.string = f'{family} {style}; lining figures for Panacea'
    fell.save(os.path.join(FONTS, out_name))
    print('wrote', out_name, f'figures scaled {s:.3f}, advance {advance}')


def main():
    roman = sys.argv[1] if len(sys.argv) > 2 else os.path.join(LIB, 'LiberationSerif-Regular.ttf')
    italic = sys.argv[2] if len(sys.argv) > 2 else os.path.join(LIB, 'LiberationSerif-Italic.ttf')
    build('IMFeENrm28P.ttf', roman, 'IMFeENrm28P-Lining.ttf', 'IM FELL English Lining', 'Roman')
    build('IMFeENit28P.ttf', italic, 'IMFeENit28P-Lining.ttf', 'IM FELL English Lining', 'Italic')


if __name__ == '__main__':
    main()
