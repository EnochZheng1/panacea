import * as THREE from 'three';

/**
 * Canvas-drawn textures: bottle labels, the gauge dial, the calendar, the shop sign. Drawn in
 * IM Fell (bundled, OFL) once fonts have loaded.
 */

export const FONT = '"IM Fell English", Georgia, serif';
export const FONT_SC = '"IM Fell English SC", "IM Fell English", Georgia, serif';

const cache = new Map<string, THREE.CanvasTexture>();

function canvasTexture(
  key: string,
  w: number,
  h: number,
  draw: (c: CanvasRenderingContext2D) => void,
) {
  const hit = cache.get(key);
  if (hit) return hit;
  const canvas = document.createElement('canvas');
  canvas.width = w;
  canvas.height = h;
  const ctx = canvas.getContext('2d');
  if (ctx) draw(ctx);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 4;
  cache.set(key, tex);
  return tex;
}

/** Fits text to a width by shrinking the font. */
function fitText(
  ctx: CanvasRenderingContext2D,
  text: string,
  maxWidth: number,
  size: number,
  font = FONT,
) {
  let s = size;
  ctx.font = `${s}px ${font}`;
  while (ctx.measureText(text).width > maxWidth && s > 8) {
    s -= 1;
    ctx.font = `${s}px ${font}`;
  }
  return s;
}

export function labelTexture(title: string, subtitle = '') {
  return canvasTexture(`label:${title}:${subtitle}`, 256, 160, (c) => {
    c.fillStyle = '#f1e4c4';
    c.fillRect(0, 0, 256, 160);
    c.strokeStyle = '#2a1a10';
    c.lineWidth = 4;
    c.strokeRect(8, 8, 240, 144);
    c.lineWidth = 1.5;
    c.strokeRect(15, 15, 226, 130);
    c.fillStyle = '#2a1a10';
    c.textAlign = 'center';
    c.textBaseline = 'middle';
    fitText(c, title, 220, 44, FONT_SC);
    c.fillText(title, 128, subtitle ? 62 : 80);
    if (subtitle) {
      fitText(c, subtitle, 210, 26);
      c.fillText(subtitle, 128, 112);
    }
  });
}

export function dialTexture() {
  return canvasTexture('dial', 256, 256, (c) => {
    c.fillStyle = '#efe3c8';
    c.beginPath();
    c.arc(128, 128, 124, 0, Math.PI * 2);
    c.fill();
    c.strokeStyle = '#2a1a10';
    c.lineWidth = 6;
    c.stroke();
    c.fillStyle = '#2a1a10';
    c.textAlign = 'center';
    c.textBaseline = 'middle';
    for (let i = 0; i <= 10; i++) {
      const a = Math.PI * (0.75 + 1.5 * (i / 10));
      const r1 = i % 5 === 0 ? 88 : 98;
      c.lineWidth = i % 5 === 0 ? 5 : 2.5;
      c.beginPath();
      c.moveTo(128 + Math.cos(a) * r1, 128 + Math.sin(a) * r1);
      c.lineTo(128 + Math.cos(a) * 112, 128 + Math.sin(a) * 112);
      c.stroke();
      if (i % 5 === 0) {
        c.font = `26px ${FONT}`;
        c.fillText(String(i * 10), 128 + Math.cos(a) * 66, 128 + Math.sin(a) * 66);
      }
    }
    c.font = `22px ${FONT_SC}`;
    c.fillText('Pressure', 128, 180);
    c.fillStyle = '#7a1e12';
    c.beginPath();
    c.arc(128, 128, 112, Math.PI * 0.75 + Math.PI * 1.5 * 0.8, Math.PI * 2.25, false);
    c.lineWidth = 8;
    c.strokeStyle = '#7a1e12';
    c.stroke();
  });
}

export function calendarTexture(month: string, year: number) {
  return canvasTexture(`cal:${month}:${year}`, 256, 320, (c) => {
    c.fillStyle = '#f3e7cc';
    c.fillRect(0, 0, 256, 320);
    c.fillStyle = '#5a1810';
    c.fillRect(0, 0, 256, 70);
    c.fillStyle = '#f3e7cc';
    c.textAlign = 'center';
    c.textBaseline = 'middle';
    c.font = `40px ${FONT_SC}`;
    c.fillText(String(year), 128, 38);
    c.fillStyle = '#2a1a10';
    c.font = `92px ${FONT}`;
    fitText(c, month, 230, 92);
    c.fillText(month, 128, 190);
  });
}

export function signTexture() {
  return canvasTexture('sign', 1024, 192, (c) => {
    c.fillStyle = '#1f130b';
    c.fillRect(0, 0, 1024, 192);
    c.strokeStyle = '#c79a43';
    c.lineWidth = 8;
    c.strokeRect(14, 14, 996, 164);
    c.fillStyle = '#e6c27a';
    c.textAlign = 'center';
    c.textBaseline = 'middle';
    c.font = `96px ${FONT_SC}`;
    c.fillText('Panacea', 512, 84);
    c.font = `34px ${FONT}`;
    c.fillText('Proprietary Remedies · Est. 1885', 512, 150);
  });
}

export function ledgerTexture(lines: string[]) {
  return canvasTexture(`ledger:${lines.join('|')}`, 512, 360, (c) => {
    c.fillStyle = '#f3e7cc';
    c.fillRect(0, 0, 512, 360);
    c.strokeStyle = '#b9a98a';
    c.lineWidth = 1;
    for (let y = 44; y < 360; y += 38) {
      c.beginPath();
      c.moveTo(20, y);
      c.lineTo(492, y);
      c.stroke();
    }
    c.strokeStyle = '#8a2a1a';
    c.beginPath();
    c.moveTo(380, 10);
    c.lineTo(380, 350);
    c.stroke();
    c.fillStyle = '#2a1a10';
    c.font = `30px ${FONT}`;
    lines.slice(0, 9).forEach((l, i) => c.fillText(l, 28, 36 + i * 38));
  });
}
