// Render art sources (godot/art/src/*.svg) to PNGs in godot/assets/art/.
// Each source gives <name>.png (sprite, transparent: #backdrop and #caption hidden) and
// <name>.card.png (presentation, everything shown). SVG filters need a browser, so this uses
// the Playwright Chromium; Godot only ever sees the PNGs.
// Usage: node tools/render_art.mjs [name ...] [--scale 2]
import { createRequire } from 'node:module';
import { readdirSync, mkdirSync } from 'node:fs';
import { join, dirname, basename } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

// The machine-wide Playwright is resolved CommonJS-style (ESM imports ignore NODE_PATH).
const { chromium } = createRequire(import.meta.url)('playwright');
const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const src = join(root, 'art', 'src');
const out = join(root, 'assets', 'art');
mkdirSync(out, { recursive: true });

const args = process.argv.slice(2);
const si = args.indexOf('--scale');
const scale = si >= 0 ? Number(args.splice(si, 2)[1]) : 2;
const names = args.length ? args : readdirSync(src).filter((f) => f.endsWith('.svg')).map((f) => basename(f, '.svg'));

const browser = await chromium.launch();
const page = await browser.newPage({ deviceScaleFactor: scale });
for (const name of names) {
  await page.goto(pathToFileURL(join(src, `${name}.svg`)).href);
  await page.evaluate(() => document.fonts.ready);
  const svg = page.locator('svg').first();
  await svg.screenshot({ path: join(out, `${name}.card.png`) });
  await page.evaluate(() => {
    for (const id of ['backdrop', 'caption', 'frame', 'frame-beads', 'grain-overlay']) document.getElementById(id)?.setAttribute('display', 'none');
  });
  await svg.screenshot({ path: join(out, `${name}.png`), omitBackground: true });
  console.log('rendered', name);
}
await browser.close();
