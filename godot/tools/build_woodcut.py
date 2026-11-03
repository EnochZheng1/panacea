#!/usr/bin/env python3
"""
Build the woodcut ("Gazette") asset set: compose each body in art/templates/wc/<name>.body.svg
with the shared woodcut style and a presentation card, and write art/src/<name>.svg.
Then render with: node tools/render_art.mjs <names...>

Style rules (see art/README.md): black ink and Scheele's green on pink newsprint; gouge
pattern for midtones; carved, unevenly inked edges; woodtype headings.

Classes available to bodies:
  k  ink        p  paper (lit)      g / g2  gouged midtone (two angles)
  gr green      ln ink line         cut     paper line carved into ink
  grl green ray line                k-text etc. use fonts Rye / Fell / FellIt
Wrap drawing in <g filter="url(#carve)">. Use #soft for lettering that must stay legible.
"""
import os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
TPL = os.path.join(ROOT, 'art', 'templates', 'wc')
OUT = os.path.join(ROOT, 'art', 'src')

INK, PAPER, GREEN, CREAM = '#141212', '#efece4', '#23a862', '#ebe4d4'

DEFS = f'''  <defs>
    <style>
      @font-face {{ font-family: Fell; src: url(../../assets/fonts/IMFeENrm28P.ttf); }}
      @font-face {{ font-family: FellIt; src: url(../../assets/fonts/IMFeENit28P.ttf); }}
      @font-face {{ font-family: Rye; src: url(../../assets/fonts/Rye-Regular.ttf); }}
      .k {{ fill: {INK}; }}
      .p {{ fill: {PAPER}; }}
      .cr {{ fill: {CREAM}; }}
      .g {{ fill: url(#gouge); }}
      .g2 {{ fill: url(#gouge2); }}
      .gr {{ fill: {GREEN}; }}
      .ln {{ fill: none; stroke: {INK}; stroke-linecap: round; stroke-linejoin: round; }}
      .cut {{ fill: none; stroke: {PAPER}; stroke-linecap: round; }}
      .grl {{ fill: none; stroke: {GREEN}; stroke-linecap: square; }}
      /* defaults only: a stroke-width attribute on the element wins */
      .ln:not([stroke-width]) {{ stroke-width: 2.6; }}
      .cut:not([stroke-width]) {{ stroke-width: 2.4; }}
      .grl:not([stroke-width]) {{ stroke-width: 5; }}
    </style>
    <filter id="carve" x="-5%" y="-5%" width="110%" height="110%">
      <feTurbulence type="fractalNoise" baseFrequency="0.028" numOctaves="2" seed="6" result="n"/>
      <feDisplacementMap in="SourceGraphic" in2="n" scale="6" xChannelSelector="R" yChannelSelector="G" result="d"/>
      <feTurbulence type="fractalNoise" baseFrequency="0.6" numOctaves="2" seed="9" result="s"/>
      <feColorMatrix in="s" type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -14 0 0 0 9.6" result="m"/>
      <feComposite in="d" in2="m" operator="in"/>
    </filter>
    <filter id="soft" x="-5%" y="-5%" width="110%" height="110%">
      <feTurbulence type="fractalNoise" baseFrequency="0.09" numOctaves="2" seed="4" result="n"/>
      <feDisplacementMap in="SourceGraphic" in2="n" scale="1.6" xChannelSelector="R" yChannelSelector="G" result="d"/>
      <feTurbulence type="fractalNoise" baseFrequency="0.7" numOctaves="2" seed="11" result="s"/>
      <feColorMatrix in="s" type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -10 0 0 0 7.4" result="m"/>
      <feComposite in="d" in2="m" operator="in"/>
    </filter>
    <filter id="paper">
      <feTurbulence type="fractalNoise" baseFrequency="0.8" numOctaves="3" seed="3" result="f"/>
      <feColorMatrix in="f" type="matrix" values="0 0 0 0 0.42  0 0 0 0 0.4  0 0 0 0 0.37  0 0 0 0.14 0" result="f1"/>
      <feTurbulence type="fractalNoise" baseFrequency="0.004" numOctaves="3" seed="8" result="b"/>
      <feColorMatrix in="b" type="matrix" values="0 0 0 0 0.5  0 0 0 0 0.46  0 0 0 0 0.4  -1.2 0 0 0 0.5" result="f2"/>
      <feMerge><feMergeNode in="SourceGraphic"/><feMergeNode in="f2"/><feMergeNode in="f1"/></feMerge>
    </filter>
    <pattern id="gouge" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(-30)">
      <rect width="7" height="7" fill="{INK}"/><path d="M0,3.5 L7,3.5" stroke="{PAPER}" stroke-width="2.1"/>
    </pattern>
    <pattern id="gouge2" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(32)">
      <rect width="7" height="7" fill="{INK}"/><path d="M0,3.5 L7,3.5" stroke="{PAPER}" stroke-width="1.8"/>
    </pattern>
    <pattern id="plaid" width="18" height="18" patternUnits="userSpaceOnUse">
      <rect width="18" height="18" fill="{PAPER}"/>
      <path d="M0,4 H18 M0,7 H18 M4,0 V18 M7,0 V18" stroke="{INK}" stroke-width="1.6"/>
      <path d="M0,13 H18 M13,0 V18" stroke="{INK}" stroke-width="3.2"/>
    </pattern>
    <pattern id="tint" width="8" height="7" patternUnits="userSpaceOnUse">
      <path d="M0,6 L8,6" stroke="{INK}" stroke-width="1.3" opacity="0.28"/>
    </pattern>
  </defs>'''


def card(title, sub, w, h, paper=PAPER):
    """Presentation backdrop (boxed engraving with a ruled tint) and caption plate."""
    bx, by, bw, bh = 40, 40, w - 80, h - 160
    back = f'''  <g id="backdrop">
    <rect width="{w}" height="{h}" fill="{paper}" filter="url(#paper)"/>
    <rect x="{bx}" y="{by}" width="{bw}" height="{bh}" fill="url(#tint)"/>
    <g filter="url(#soft)">
      <rect x="{bx}" y="{by}" width="{bw}" height="{bh}" fill="none" stroke="{INK}" stroke-width="4"/>
      <rect x="{bx+7}" y="{by+7}" width="{bw-14}" height="{bh-14}" fill="none" stroke="{INK}" stroke-width="1.4"/>
    </g>
  </g>'''
    cap = f'''  <g id="caption" filter="url(#soft)">
    <text x="{w/2}" y="{h-78}" font-family="Rye" font-size="30" text-anchor="middle" fill="{INK}">{title}</text>
    <text x="{w/2}" y="{h-46}" font-family="FellIt" font-size="18" text-anchor="middle" fill="{INK}">{sub}</text>
  </g>'''
    return back, cap


# name: (title, cutline, width, height, card paper or None for full-bleed layouts)
ASSETS = {
    'wc_proprietor': ('THE PROPRIETOR.', 'Hand on heart. The other hand is his own business.', 600, 1000, PAPER),
    'wc_death': ('THE SILENT PARTNER.', 'Keeps the books. Takes his share in kind.', 600, 1000, PAPER),
    'wc_clerk': ('THE CONSUMPTIVE CLERK.', 'Eleven years of catarrh. Three bottles of hope.', 600, 1000, PAPER),
    'wc_mother': ('THE WEARY MOTHER.', 'The baby has not cried since Tuesday.', 600, 1000, PAPER),
    'wc_longshoreman': ('THE LONGSHOREMAN.', 'A lifelong temperance man, medicinally speaking.', 600, 1000, PAPER),
    'wc_lady': ('THE LADY OF FASHION.', 'Her complexion is the talk of the cemetery.', 600, 1000, PAPER),
    'wc_seamstress': ('THE SEAMSTRESS.', 'Fourteen hours at the machine. A spoonful at each hour.', 600, 1000, PAPER),
    'wc_widow': ('THE WIDOW.', 'In mourning since the spring, and a good customer since the summer.', 600, 1000, PAPER),
    'wc_oldman': ('THE OLD GENTLEMAN.', 'Takes it for his chest, his knees and his nerves.', 600, 1000, PAPER),
    'wc_bottle_panacea': ('THE PANACEA.', 'One dollar the bottle. Cures all ills.', 400, 760, PAPER),
    'wc_bottle_syrup': ('SOOTHING SYRUP.', 'Quiets fretful infants. Thoroughly.', 400, 760, PAPER),
    'wc_bottle_bitters': ('TEMPERANCE BITTERS.', 'Purely medicinal. Take hourly.', 400, 760, PAPER),
    'wc_pills': ('LITTLE LIVER PILLS.', 'Purely vegetable, mostly.', 400, 760, PAPER),
    'wcad_reading_notice': (None, None, 460, 1000, None),
    'wcad_display': (None, None, 600, 800, None),
    'wcad_testimonial': (None, None, 700, 900, None),
    'wcad_almanac': (None, None, 700, 1000, None),
    'wcad_tradecard': (None, None, 500, 800, None),
}


def build(name):
    title, sub, w, h, paper = ASSETS[name]
    body = open(os.path.join(TPL, name + '.body.svg')).read()
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">',
             f'  <!-- Panacea woodcut asset "{name}". Generated by tools/build_woodcut.py from art/templates/wc. -->',
             DEFS]
    if paper:
        back, cap = card(title, sub, w, h, paper)
        parts += [back, body, cap]
    else:
        parts += [body]
    parts.append('</svg>\n')
    open(os.path.join(OUT, name + '.svg'), 'w').write('\n'.join(parts))


if __name__ == '__main__':
    import sys
    names = sys.argv[1:] or [n for n in ASSETS if os.path.exists(os.path.join(TPL, n + '.body.svg'))]
    for n in names:
        build(n)
        print('built', n)
