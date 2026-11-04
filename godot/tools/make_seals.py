#!/usr/bin/env python3
"""
Write the engraved seal templates (art/templates/wc/seal_*.body.svg): a double-ruled roundel with
the sender's legend set round the rim and an emblem cut in the middle, the way a letterhead's
vignette was engraved. Then build and render them like any woodcut:
  python3 tools/make_seals.py && python3 tools/build_woodcut.py seal_coroner ... &&
  node tools/render_art.mjs seal_coroner ... && python3 tools/make_cuts.py
Each seal is 240 x 240, ink only on a transparent ground (the letter's paper shows through).
"""
import os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
TPL = os.path.join(ROOT, 'art', 'templates', 'wc')

# the emblems, drawn about the centre (120, 120) inside a radius of about 70
EMBLEMS = {
    # an hourglass, run nearly out, with a laurel under it
    'coroner': '''
    <path class="k" d="M84,64 L156,64 L156,74 L84,74 Z M84,166 L156,166 L156,176 L84,176 Z"/>
    <path class="ln" d="M92,74 L92,166 M148,74 L148,166" stroke-width="5"/>
    <path class="p" d="M98,76 C98,104 116,112 118,120 C116,128 98,136 98,164 L142,164 C142,136 124,128 122,120 C124,112 142,104 142,76 Z"/>
    <path class="ln" d="M98,76 C98,104 116,112 118,120 C116,128 98,136 98,164 L142,164 C142,136 124,128 122,120 C124,112 142,104 142,76 Z" stroke-width="3.4"/>
    <path class="k" d="M104,92 C110,104 130,104 136,92 Z M101,162 C104,146 114,138 120,136 C126,138 136,146 139,162 Z"/>
    <path class="ln" d="M120,104 L120,138" stroke-width="1.6"/>
    <path class="ln" d="M72,182 C90,196 108,198 118,192 M168,182 C150,196 132,198 122,192" stroke-width="3"/>
    <path class="k" d="M80,184 l-8,-6 l10,-1 Z M92,190 l-7,-7 l10,0 Z M160,184 l8,-6 l-10,-1 Z M148,190 l7,-7 l-10,0 Z"/>''',
    # the rod and serpent
    'health': '''
    <path class="ln" d="M120,56 L120,184" stroke-width="8"/>
    <path class="k" d="M112,52 C112,44 128,44 128,52 L124,60 L116,60 Z"/>
    <path class="ln" d="M120,176 C96,166 96,148 120,140 C144,132 144,114 120,106 C100,99 100,84 116,76" stroke-width="7"/>
    <path class="cut" d="M120,176 C96,166 96,148 120,140 C144,132 144,114 120,106 C100,99 100,84 116,76" stroke-width="2"/>
    <path class="k" d="M112,68 C118,60 132,64 130,74 C128,80 118,80 114,76 Z"/>
    <path class="ln" d="M84,188 L156,188" stroke-width="4"/>''',
    # a broom crossed with a shovel, over a bucket
    'sanitary': '''
    <path class="ln" d="M78,70 L160,164 M162,70 L84,164" stroke-width="6"/>
    <path class="k" d="M150,152 L176,182 L160,190 L142,164 Z"/>
    <path class="cut" d="M152,164 L166,180 M146,170 L158,184" stroke-width="1.6"/>
    <path class="k" d="M74,158 C66,170 70,186 84,188 C98,190 100,176 94,166 Z"/>
    <path class="k" d="M100,178 L140,178 L136,204 L104,204 Z"/>
    <path class="cut" d="M104,186 L136,186" stroke-width="1.6"/>
    <path class="ln" d="M100,178 C104,166 136,166 140,178" stroke-width="2.4"/>''',
    # a diploma, rolled and beribboned, with its motto: money teaches
    'college': '''
    <path class="p" d="M70,92 L170,92 L170,144 L70,144 Z"/>
    <path class="ln" d="M70,92 L170,92 L170,144 L70,144 Z" stroke-width="3.4"/>
    <ellipse cx="70" cy="118" rx="9" ry="27" class="k"/>
    <ellipse cx="170" cy="118" rx="9" ry="27" class="p"/>
    <ellipse cx="170" cy="118" rx="9" ry="27" class="ln" stroke-width="3"/>
    <path class="ln" d="M86,106 L154,106 M86,116 L154,116 M86,126 L140,126" stroke-width="1.6"/>
    <path class="k" d="M112,138 L128,138 L134,170 L120,162 L106,170 Z"/>
    <circle cx="120" cy="140" r="11" class="k"/>
    <path class="ln" d="M64,184 C90,174 150,174 176,184" stroke-width="16"/>
    <path class="cut" d="M66,184 C92,175 148,175 174,184" stroke-width="12"/>
    <text x="120" y="186" font-family="Rye" font-size="11" text-anchor="middle" class="k" textLength="92" lengthAdjust="spacingAndGlyphs">PECUNIA DOCET</text>''',
    # the town pump and a tin cup: cold water, and plenty of it
    'league': '''
    <path class="k" d="M100,82 L136,82 L136,184 L100,184 Z"/>
    <path class="cut" d="M106,96 L106,176 M130,96 L130,176" stroke-width="1.8"/>
    <path class="k" d="M94,72 L142,72 L146,84 L90,84 Z"/>
    <path class="k" d="M96,184 L140,184 L146,196 L90,196 Z"/>
    <path class="ln" d="M136,100 C152,92 168,80 178,62" stroke-width="6"/>
    <path class="ln" d="M100,118 L74,118 L74,128" stroke-width="7"/>
    <path class="grl" d="M74,134 L74,160" stroke="#141212" stroke-width="3" stroke-dasharray="5 6"/>
    <path class="k" d="M62,164 L86,164 L84,184 L64,184 Z"/>
    <path class="ln" d="M86,168 C94,170 94,180 86,180" stroke-width="2.4"/>''',
    # a mortar and pestle, the druggist's sign
    'hobart': '''
    <path class="ln" d="M128,58 L104,130" stroke-width="12"/>
    <path class="cut" d="M126,64 L106,124" stroke-width="2.4"/>
    <path class="k" d="M64,114 L176,114 L170,124 C166,156 146,174 120,176 C94,174 74,156 70,124 Z"/>
    <path class="cut" d="M80,130 C86,150 100,162 118,166" stroke-width="2.6"/>
    <path class="k" d="M100,176 L140,176 L148,194 L92,194 Z"/>
    <path class="cut" d="M76,120 L164,120" stroke-width="2"/>''',
}

LEGENDS = {
    'coroner': 'OFFICE OF THE CORONER ★ CITY AND COUNTY OF NEW-YORK ★',
    'health': 'BOARD OF HEALTH ★ CITY OF NEW-YORK ★ SALUS POPULI ★',
    'sanitary': 'SANITARY BUREAU ★ INSPECTOR OF NUISANCES ★ SIXTH WARD ★',
    'college': 'ECLECTIC MEDICAL COLLEGE ★ CINCINNATI ★ DIPLOMAS BY POST ★',
    'league': "LADIES' TEMPERANCE LEAGUE ★ SIXTH WARD ★ TOUCH NOT ★",
    'hobart': 'J. & W. HOBART & CO. ★ 81 PEARL STREET ★ ESTD. 1852 ★',
}


def body(name):
    return f'''  <defs><path id="rim-{name}" d="M120,120 m-92,0 a92,92 0 1,1 184,0 a92,92 0 1,1 -184,0"/></defs>
  <g id="seal" filter="url(#soft)">
    <circle cx="120" cy="120" r="114" class="ln" stroke-width="7"/>
    <circle cx="120" cy="120" r="106" class="ln" stroke-width="1.8"/>
    <circle cx="120" cy="120" r="82" class="ln" stroke-width="3.4"/>
    <text font-family="Rye" font-size="15.5" class="k" letter-spacing="0.5">
      <textPath href="#rim-{name}" textLength="566" lengthAdjust="spacing">{LEGENDS[name].replace("&", "&amp;")}</textPath>
    </text>
  </g>
  <g id="emblem" filter="url(#carve)">{EMBLEMS[name]}
  </g>
'''


if __name__ == '__main__':
    for n in EMBLEMS:
        with open(os.path.join(TPL, f'seal_{n}.body.svg'), 'w') as f:
            f.write(body(n))
        print('wrote', f'seal_{n}.body.svg')
