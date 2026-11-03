# Art pipeline (no artist yet)

## Chosen direction: the Gazette

The player chose the pink-paper sensational weekly of 1880s–1900s New York, printed as a woodcut: black, plus Scheele's green on pink newsprint, with woodtype headlines (`style_woodcut`, `gazette_page`).

The newspaper is the game's main surface, not just a style. Patent medicine was the period's largest newspaper advertiser, so each turn is an edition. The humour comes from juxtaposition on the page, for example the player's advert dressed as news next to the obituary of the man it describes.

Period material to draw on, and the game use for each:

| Period element | Use in the game |
| --- | --- |
| Reading notices (adverts typeset as news, ending in "Adv.") | An ad format the player can buy |
| Testimonials (named, sometimes bought, sometimes outliving the writer) | Bought, and they can backfire |
| Deaths and coroner's court columns | Where consequences appear |
| Personals and classifieds | Hiring, widows, hints |
| Rival advertisers | AI competitors |
| Stock cuts (one engraving sold to many advertisers) | A recurring visual gag, and a cheap way to make art |
| The "red clause" (ad contracts void if the state regulated patent medicine) | Press influence: papers lobby for you |
| The markets column | Ingredient prices |
| Almanacs, trade cards, handbills, sandwich men, medicine shows | Further ad channels |
| *Collier's* "The Great American Fraud" (1905) and the Pure Food and Drug Act (1906) | The endgame |

Earlier explorations are kept for reference: grim ink (`silent_partner`, `satisfied_customer`, `proprietor`, `panacea_bottle`), shadow play and the trade card.

## How assets are made

1. Each asset is authored as layered SVG in `art/src/`. The layers are black masses, dry-brush lit areas, details and the accent. Group ids name the puppet parts (`hat`, `skull`, `arm-r`, `bottle`, …) for rigging later.
2. `node tools/render_art.mjs [name] [--scale 2]` renders each source with Chromium, because SVG filters do the brush texture. It writes two files to `assets/art/`:
   - `<name>.png`: a transparent sprite.
   - `<name>.card.png`: a presentation card with backdrop and caption.
3. Godot only imports the PNGs.

## Asset log

| Asset | Source | Licence |
| --- | --- | --- |
| All art in `art/src/` | Original, authored in this repo | Project-owned |
| IM Fell English font | Google Fonts (`assets/fonts/OFL.txt`) | SIL OFL 1.1 |
| Rye font | Google Fonts (`assets/fonts/OFL-Rye.txt`) | SIL OFL 1.1 |
| Poly Haven models and textures | `assets/polyhaven/LICENSES.json` | CC0 1.0 |
