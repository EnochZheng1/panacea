# Art pipeline (no artist yet)

## Chosen direction: the Gazette

The direction is the sensational weekly of 1880s–1900s New York, printed as a woodcut. **Black ink on white newsprint is the main theme**; Scheele's green appears only on the product. Headlines are set in woodtype. The `wc_*` set is printed this way. The earlier pink-newsprint explorations (`style_woodcut`, `gazette_page`) are kept only for reference.

The newspaper is the game's main surface, not just a style. Patent medicine was the period's largest newspaper advertiser, so each turn is an edition. The humour comes from juxtaposition on the page, for example the player's advert dressed as news next to the obituary of the man it describes.

Period material to draw on, and the game use for each:

| Period element                                                                      | Use in the game                                     |
| ----------------------------------------------------------------------------------- | --------------------------------------------------- |
| Reading notices (adverts typeset as news, ending in "Adv.")                         | An ad format the player can buy                     |
| Testimonials (named, sometimes bought, sometimes outliving the writer)              | Bought, and they can backfire                       |
| Deaths and coroner's court columns                                                  | Where consequences appear                           |
| Personals and classifieds                                                           | Hiring, widows, hints                               |
| Rival advertisers                                                                   | AI competitors                                      |
| Stock cuts (one engraving sold to many advertisers)                                 | A recurring visual gag, and a cheap way to make art |
| The "red clause" (ad contracts void if the state regulated patent medicine)         | Press influence: papers lobby for you               |
| The markets column                                                                  | Ingredient prices                                   |
| Almanacs, trade cards, handbills, sandwich men, medicine shows                      | Further ad channels                                 |
| _Collier's_ "The Great American Fraud" (1905) and the Pure Food and Drug Act (1906) | The endgame                                         |

Earlier explorations are kept for reference: grim ink (`silent_partner`, `satisfied_customer`, `proprietor`, `panacea_bottle`), shadow play and the trade card.

## The woodcut set

| Group      | Assets                                                                                                                          |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------- |
| Characters | `wc_proprietor`, `wc_death`, `wc_clerk`, `wc_mother`, `wc_longshoreman`, `wc_lady`, `wc_seamstress`, `wc_widow`, `wc_oldman`    |
| Products   | `wc_bottle_panacea`, `wc_bottle_syrup`, `wc_bottle_bitters`, `wc_pills`                                                         |
| Ad formats | `wcad_reading_notice`, `wcad_display`, `wcad_testimonial`, `wcad_almanac`, `wcad_tradecard`                                     |
| Apparatus  | `wc_still` (the game animates the fire, steam, drip and receiver over it)                                                       |
| Seals      | `seal_coroner`, `seal_health`, `seal_sanitary`, `seal_college`, `seal_league`, `seal_hobart` (written by `tools/make_seals.py`) |

To rebuild the set:

1. `python3 tools/build_woodcut.py` composes each `art/templates/wc/<name>.body.svg` with the shared woodcut style and a card.
2. `node tools/render_art.mjs <names> --scale 2` renders them.
3. `python3 tools/make_cuts.py` crops the sprites into `assets/art/cuts/` for the layouts to use.

Render the sprites before the ad formats, because the layouts embed the cuts.

## How assets are made

1. Each asset is authored as layered SVG in `art/src/`. The layers are black masses, dry-brush lit areas, details and the accent. Group ids name the puppet parts (`hat`, `skull`, `arm-r`, `bottle`, …) for rigging later.
2. `node tools/render_art.mjs [name] [--scale 2]` renders each source with Chromium, because SVG filters do the brush texture. It writes two files to `assets/art/`:
   - `<name>.png`: a transparent sprite.
   - `<name>.card.png`: a presentation card with backdrop and caption.
3. Godot only imports the PNGs.

## Asset log

| Asset                          | Source                                    | Licence       |
| ------------------------------ | ----------------------------------------- | ------------- |
| All art in `art/src/`          | Original, authored in this repo           | Project-owned |
| IM Fell English font           | Google Fonts (`assets/fonts/OFL.txt`)     | SIL OFL 1.1   |
| Rye font                       | Google Fonts (`assets/fonts/OFL-Rye.txt`) | SIL OFL 1.1   |
| Poly Haven models and textures | `assets/polyhaven/LICENSES.json`          | CC0 1.0       |
