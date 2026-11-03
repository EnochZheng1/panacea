# Art pipeline (no artist yet)

Direction: **Grim ink**, the 1880s–1900s satirical-cartoon sensibility translated for a game.

- **Shadow:** heavy black ink masses, with dry-brush lit edges in bone and aged paper.
- **Accent:** one colour only, Scheele's green, the arsenic pigment that killed Victorians in their wallpaper and sweets. It is reserved for the product and the light it throws.
- **Figures:** stiff, posed and angular, readable as a silhouette at small size.
- **Humour:** deadpan, carried by captions and a dry period narrator, plus the era's convention of labelling everything.

## How assets are made

1. Each asset is authored as layered SVG in `art/src/`. The layers are black masses, dry-brush lit areas, details and the accent. Group ids name the puppet parts (`hat`, `skull`, `arm-r`, `bottle`, …) for rigging later.
2. `node tools/render_art.mjs [name] [--scale 2]` renders each source with Chromium, because SVG filters do the brush texture. It writes two files to `assets/art/`:
   - `<name>.png`: a transparent sprite.
   - `<name>.card.png`: a presentation card with backdrop and caption.
3. Godot only imports the PNGs.

## Asset log

| Asset | Source | Licence |
| --- | --- | --- |
| `silent_partner` | Original, authored in this repo | Project-owned |
| IM Fell English font | Google Fonts (`assets/fonts/OFL.txt`) | SIL OFL 1.1 |
| Poly Haven models and textures | `assets/polyhaven/LICENSES.json` | CC0 1.0 |
