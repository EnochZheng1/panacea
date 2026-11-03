# Generate style variants of the Satisfied Customer from the grim-ink template by swapping the
# style layer (classes, filters, hard-coded fills, backdrop, caption). Geometry stays identical,
# so the comparison is purely about style.
import re
T = __import__('os').path.join(__import__('os').path.dirname(__file__), '..', 'art', 'templates') + '/'
OUT = __import__('os').path.join(__import__('os').path.dirname(__file__), '..', 'art', 'src') + '/'
src = open(T + 'satisfied_customer.svg').read()
ticks = ''.join(f'M186,{y} L{191 if (y-176)%60 else 195},{y} ' for y in range(176, 880, 12)).strip()
src = src.replace('<path id="ticks" d=""', f'<path id="ticks" d="{ticks}"')

def swap(s, pairs):
    for a, b in pairs:
        assert a in s, a[:60]
        s = s.replace(a, b)
    return s

def body_of(s):
    a = s.index('  <g id="backdrop">'); b = s.index('  <g id="caption">')
    return s[a:b]

def strip_backdrop(body):
    a = body.index('  <g id="backdrop">'); b = body.index('  </g>', a) + len('  </g>\n')
    return body[b:]

FONTS = '''      @font-face { font-family: Fell; src: url(../../assets/fonts/IMFeENrm28P.ttf); }
      @font-face { font-family: FellIt; src: url(../../assets/fonts/IMFeENit28P.ttf); }
      @font-face { font-family: Rye; src: url(../../assets/fonts/Rye-Regular.ttf); }'''

# ---------------- shadow play ----------------
sil_defs = f'''  <defs>
    <style>
{FONTS}
      .k, .bone, .mid {{ fill: #0d0a08; }}
      .green {{ fill: #3fe08f; }}
      .line {{ fill: none; stroke: #0d0a08; stroke-width: 2.2; stroke-linecap: round; }}
    </style>
    <radialGradient id="lamp" gradientUnits="userSpaceOnUse" cx="300" cy="420" r="560">
      <stop offset="0" stop-color="#f7e2ad"/><stop offset="0.45" stop-color="#e2b062"/>
      <stop offset="0.8" stop-color="#9c5d2a"/><stop offset="1" stop-color="#4a2a14"/>
    </radialGradient>
    <linearGradient id="liquid" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#1fa864"/><stop offset="0.45" stop-color="#b9ffd9"/><stop offset="1" stop-color="#2cc47a"/>
    </linearGradient>
    <filter id="glow" x="-100%" y="-100%" width="300%" height="300%"><feGaussianBlur stdDeviation="26"/></filter>
    <filter id="screen">
      <feTurbulence type="fractalNoise" baseFrequency="0.55" numOctaves="2" seed="4"/>
      <feColorMatrix type="matrix" values="0 0 0 0 0.3  0 0 0 0 0.2  0 0 0 0 0.1  0 0 0 0.12 0"/>
    </filter>
  </defs>'''
b = strip_backdrop(body_of(src))
b = b.replace(' filter="url(#ink)"', '').replace(' filter="url(#dry)"', '')
b = b.replace('#efe6cc', 'url(#lamp)').replace('#ddd0ae', 'url(#lamp)')
b = b.replace('<g class="bone">', '<g fill="url(#lamp)">')
b = b.replace('<path d="M100,222 L126,217 L126,240 L112,247 L100,240 Z M134,215 L156,211 L156,233 L142,240 L134,234 Z" class="k"/>',
              '<path d="M100,222 L126,217 L126,240 L112,247 L100,240 Z M134,215 L156,211 L156,233 L142,240 L134,234 Z" fill="url(#lamp)"/>')
b = b.replace('<path d="M124,250 L133,268 L118,268 Z" class="k"/>', '<path d="M124,250 L133,268 L118,268 Z" fill="url(#lamp)"/>')
b = b.replace('<circle cx="122" cy="225" r="2.6" fill="url(#lamp)"/><circle cx="153" cy="220" r="2.2" fill="url(#lamp)"/>',
              '<circle cx="122" cy="226" r="3" fill="#0d0a08"/><circle cx="153" cy="221" r="2.6" fill="#0d0a08"/>')
b = b.replace(f'<path id="ticks" d="{ticks}" class="line" stroke-width="1"/>', f'<path id="ticks" d="{ticks}" fill="none" stroke="url(#lamp)" stroke-width="1.6"/>')
# cut-paper: the collar gap, sleeve and waistcoat edges read as slits of light
slits = '''  <g fill="none" stroke="url(#lamp)" stroke-width="2.2" stroke-linecap="round">
    <path d="M300,372 L296,394 M340,372 L344,394"/>
    <path d="M268,424 L256,650 M372,424 L384,650"/>
    <path d="M252,486 C262,470 272,456 282,452"/>
    <path d="M384,440 C400,452 412,474 418,506"/>
    <path d="M318,664 L310,896 M322,664 L332,896"/>
    <path d="M150,330 L160,470 L160,860"/>
    <path d="M90,424 L120,419 M92,432 L118,428"/>
  </g>
'''
sil = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 1000" width="600" height="1000">
  <!-- Style test: shadow play (Victorian cut-paper silhouette). Same geometry as satisfied_customer. -->
{sil_defs}
  <g id="backdrop">
    <rect width="600" height="1000" fill="url(#lamp)"/>
    <rect width="600" height="1000" filter="url(#screen)"/>
  </g>
  <g transform="translate(40 66) scale(0.88)">
{b}{slits}  </g>
  <g id="frame" fill="#0d0a08">
    <path fill-rule="evenodd" d="M0,0 H600 V1000 H0 Z M24,60 Q24,24 60,24 H540 Q576,24 576,60 V896 H24 Z"/>
    <path d="M24,24 C90,40 120,90 110,150 C80,120 50,110 24,118 Z M576,24 C510,40 480,90 490,150 C520,120 550,110 576,118 Z"/>
    <path d="M24,896 H576 V912 C540,904 520,918 490,908 C460,918 440,904 410,912 C380,904 360,918 330,908 C300,918 280,904 250,912 C220,904 200,918 170,908 C140,918 120,904 90,912 C60,904 44,916 24,910 Z"/>
  </g>
  <g id="frame-beads" fill="none" stroke="url(#lamp)" stroke-width="3" stroke-dasharray="1 9" stroke-linecap="round" opacity="0.8">
    <path d="M36,880 V64 Q36,36 64,36 H536 Q564,36 564,64 V880"/>
  </g>
  <g id="caption">
    <text x="300" y="955" font-family="Fell" font-size="26" letter-spacing="4" text-anchor="middle" fill="#e2b062">A SATISFIED CUSTOMER</text>
    <text x="300" y="982" font-family="FellIt" font-size="17" text-anchor="middle" fill="#9c7a4a">“Never felt better!” — Mr. A. Pike, clerk</text>
  </g>
</svg>
'''
open(OUT + 'style_shadowplay.svg', 'w').write(sil)

# ---------------- Police Gazette woodcut ----------------
PINK = '#f0c4bd'
wood_defs = f'''  <defs>
    <style>
{FONTS}
      .k {{ fill: #161112; }}
      .bone {{ fill: {PINK}; }}
      .mid {{ fill: url(#gouge); }}
      .green {{ fill: #23a862; }}
      .line {{ fill: none; stroke: #161112; stroke-width: 2.6; stroke-linecap: round; }}
    </style>
    <!-- carved block: chunky edge wobble and uneven ink pickup -->
    <filter id="ink" x="-5%" y="-5%" width="110%" height="110%">
      <feTurbulence type="fractalNoise" baseFrequency="0.028" numOctaves="2" seed="6" result="n"/>
      <feDisplacementMap in="SourceGraphic" in2="n" scale="7" xChannelSelector="R" yChannelSelector="G" result="d"/>
      <feTurbulence type="fractalNoise" baseFrequency="0.6" numOctaves="2" seed="9" result="s"/>
      <feColorMatrix in="s" type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -14 0 0 0 9.6" result="m"/>
      <feComposite in="d" in2="m" operator="in"/>
    </filter>
    <filter id="dry" x="-5%" y="-5%" width="110%" height="110%">
      <feTurbulence type="fractalNoise" baseFrequency="0.035" numOctaves="2" seed="12" result="n"/>
      <feDisplacementMap in="SourceGraphic" in2="n" scale="6" xChannelSelector="R" yChannelSelector="G"/>
    </filter>
    <filter id="paper">
      <feTurbulence type="fractalNoise" baseFrequency="0.8" numOctaves="3" seed="3" result="f"/>
      <feColorMatrix in="f" type="matrix" values="0 0 0 0 0.55  0 0 0 0 0.3  0 0 0 0 0.3  0 0 0 0.18 0" result="f1"/>
      <feTurbulence type="fractalNoise" baseFrequency="0.005" numOctaves="3" seed="8" result="b"/>
      <feColorMatrix in="b" type="matrix" values="0 0 0 0 0.6  0 0 0 0 0.35  0 0 0 0 0.3  -1.2 0 0 0 0.65" result="f2"/>
      <feMerge><feMergeNode in="SourceGraphic"/><feMergeNode in="f2"/><feMergeNode in="f1"/></feMerge>
    </filter>
    <pattern id="gouge" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(-30)">
      <rect width="7" height="7" fill="#161112"/>
      <path d="M0,3.5 L7,3.5" stroke="{PINK}" stroke-width="2.1"/>
    </pattern>
    <linearGradient id="liquid" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#23a862"/><stop offset="1" stop-color="#23a862"/>
    </linearGradient>
  </defs>'''
b = strip_backdrop(body_of(src))
b = b.replace('#efe6cc', PINK).replace('#ddd0ae', PINK)
b = b.replace('<ellipse cx="420" cy="270" rx="80" ry="90" fill="#3fe08f" opacity="0.5" filter="url(#glow)"/>', '')
rays = '  <g id="rays" stroke="#23a862" stroke-width="5" stroke-linecap="square" transform="translate(3 2)">\n'
import math
for i in range(18):
    a = i / 18 * math.tau
    r0, r1 = 62, 108 + (i % 2) * 34
    rays += f'    <path d="M{420+r0*math.cos(a):.0f},{272+r0*math.sin(a):.0f} L{420+r1*math.cos(a):.0f},{272+r1*math.sin(a):.0f}"/>\n'
rays += '  </g>\n'
wood = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 1000" width="600" height="1000">
  <!-- Style test: Police Gazette woodcut (two-colour block print on pink newsprint). Same geometry as satisfied_customer. -->
{wood_defs}
  <g id="backdrop">
    <rect width="600" height="1000" fill="{PINK}" filter="url(#paper)"/>
    <g filter="url(#ink)">
      <rect x="18" y="18" width="564" height="964" fill="none" stroke="#161112" stroke-width="9"/>
      <rect x="32" y="32" width="536" height="936" fill="none" stroke="#161112" stroke-width="2.5"/>
      <rect x="32" y="32" width="536" height="96" class="k"/>
    </g>
    <text x="300" y="96" font-family="Rye" font-size="38" text-anchor="middle" fill="{PINK}">NEVER FELT BETTER!</text>
    <text x="300" y="120" font-family="FellIt" font-size="15" text-anchor="middle" fill="{PINK}">A Clerk's Remarkable Testimonial for Dr. ——'s Panacea</text>
  </g>
  <g transform="translate(60 126) scale(0.8)">
{rays}{b}  </g>
  <g id="caption">
    <path d="M32,926 L568,926" stroke="#161112" stroke-width="2.5"/>
    <text x="300" y="958" font-family="Rye" font-size="20" text-anchor="middle" fill="#161112">THE UNDERTAKER TAKES HIS MEASURE</text>
  </g>
</svg>
'''
open(OUT + 'style_woodcut.svg', 'w').write(wood)
print('ok')
