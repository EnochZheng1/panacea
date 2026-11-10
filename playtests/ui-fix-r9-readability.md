# Round-9 fix pass · readability, front door and art

The readability, front-door and art agent's fixes for review round 9 (`playtests/round09/SUMMARY.md`), the low-vision
reader's report first (`playtests/round09/low-vision.md`). Files changed are listed at the end, with the changes the
other UI agent's files still need.

Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r9read/`:
`before/` (the build as reviewed), and `reg/`, `large/`, `larger/` (this build at each text size). Each holds the same 18
screens and drawers: the title page, the menu, the settings, the first edition, the proof in months 1, 8 and 24 and in a
heavy month 6, every drawer (formula, still, supplies, ventures, letters), a heavy edition and the ledger cutting. The
script that takes them is `suite.sh` in that folder.

## 1. A text-size setting

> "No text-size, zoom or contrast option anywhere. SETTINGS has only THE WINDOW and SOUND … For a low-vision player this
> is the single biggest barrier." (low-vision #1; the cards, THE WORKS, the drawer sub-labels and the jars wanted "about
> 1.5×")

**What changed.** SETTINGS has a third choice, **TEXT SIZE: REGULAR / LARGE / LARGER**, saved with the other settings
(`[print] size=` in the settings file) and applied at once to everything on screen, the card included.

The rule lives in a new `godot/scripts/UI/LargePrint.cs`. `Main` hooks the scene tree's `node_added`, so every Label,
Button and RichTextLabel is set as it enters the tree, on every screen and drawer however it was built. At LARGE no IM
Fell type is set below 17 px, at LARGER below 19 px; type already larger is left alone. IM Fell's x-height is 0.445 em
(italic) to 0.458 em (roman), so the floors give **7.6 and 8.5 px of x-height against 4.9 px** at the smallest regular
size (11 px italic): 1.5× and 1.7×. Rye, the woodtype, is left as designed: it is set in capitals, and its 11 px
capitals already stand 8.4 px tall; enlarging the tickets widened their rows and squeezed the proof (tried and seen in
screenshots). A control remembers its designed size, so the setting can be changed back and forth without creeping.

Larger type must never cost the player text, so a fitting pass runs after each change (one frame later, when the
containers have laid it out). It finds:

- a line cut off by its clipping frame (the proof's columns), or a box set at a fixed place that now runs past its
  parent or grows wider than it was given (a card at the counter);
- a word broken mid-word ("Pepperm / int" on a jar) or a box squeezed to a letter a line;
- a one-line label grown wide enough to squeeze its neighbours: THE WORKS at LARGER set "0 / b / o / t / t / l / e /
  s" one letter a line beside "suspicion: the Board of Health is asking (87 of 100)". Such a label is held to its
  designed width and runs on to a second line instead. Where its row has room (the price list's columns) it widens;
- a scroll squeezed to nothing beside larger type (the type case beside THE WORKS and the warnings at LARGER).

It steps the larger type in that column back towards its designed size until nothing is cut, and remembers what it
learned, so a slip rebuilt after a click comes back at the fitted size with no flicker. The tooltips follow the setting.
The footer grows to 19 px at LARGER.

**Verified.** A dev check, `-- --typecheck` (`godot/scripts/UI/TypeCheck.cs`), prints for each screenshot the smallest
type on screen and anything out of place: off the window, cut by a frame, trimmed, rich text cut short, two lines of
type over each other in one container. Results for the 18 screens:

| Level   | Smallest IM Fell on the title, menu, settings, editions and cuttings | Smallest IM Fell on the proof | New problems |
| ------- | -------------------------------------------------------------------- | ----------------------------- | ------------ |
| REGULAR | 12 px (5.3 px x-height)                                              | 11 px (4.9 px): jar prices    | none         |
| LARGE   | 17 px (7.6 px)                                                       | 12 px: the counter cards      | none         |
| LARGER  | 19 px (8.5 px)                                                       | 12 px: the counter cards      | none         |

Measured on the pixels as well (rows of the dense lowercase band, THE WORKS' suspicion line in month 8, 13 px italic as
designed): 6 px at REGULAR, 7 px at LARGE, 8 px at LARGER (`works_cmp.png`).

In the drawers (the letters, the drug shelf and the assay, the order form, the still, the prospectus) the type is at the
floor throughout; the drawers scroll, so it always has room.

The only problems the check reports at LARGE and LARGER are the four or five counter-card status lines that already run
2–5 px past the counter strip at REGULAR (y 711–727 in a strip ending at 722; they are drawn, nothing covers them). They
are the same at all three sizes. I looked at every screenshot at LARGE and LARGER: no text is clipped, overlapping or
broken mid-word, and SEND TO PRESS, the drawer tabs and the type case stay in reach.

What grew (LARGE, then LARGER): the jar names and prices on the drug shelf (11–13 px → 17/19), the assay's words, THE
WORKS (13–14 → 17/19, suspicion and standing wrapping onto a second line), the warnings under it, the type case's rates
and claim notes, the letters, the prospectus's "So far" lines, the still's sentences, the order
form, every edition's small print, the ledger and every cutting, the title page, menu and settings.

**Left open (needs `ProofView.cs`, owned by the other UI agent).** The counter cards ("peaky · delighted", the health
line the reader needed most) stay at 12 px: the cards are laid at a fixed height (`CounterTop + 2`) in a 292 px strip and
already overflow it at REGULAR, so the fitting pass holds them back. The proof's left column ("set after press", the
struck-out names) is also held back in long months, because it fills its clipped frame even at REGULAR. At LARGER in a
busy month (three letters, three warnings) the bench is full: the fitting pass steps the warnings and THE WORKS back a
point or two so the type case keeps about 60 px in view (it scrolls). Cash and suspicion are in the footer at 19 px. The suggested
change is in "Changes needed in other files" below. Text drawn by hand with `DrawString` (the measuring glass's labels in
`MeasuringGlass.cs`, the still's caption and the counter in `StillBench.cs`) cannot be reached by the tree hook; each
call should pass its size through `LargePrint.Size(n)`.

## 2. Lining figures

> "The old-style (hanging) figures make "1" look like a small-cap "I" and "0" look like "o" … "$19.80" reads "$I9.8o"."
> (low-vision #5)

IM Fell has no `lnum` feature (its GSUB has aalt, dlig, hist, liga, locl, salt, ss01–ss04, swsh, none of which touch the
figures). A new `godot/tools/make_lining_figures.py` writes copies of the roman and the italic,
`IMFeENrm28P-Lining.ttf` and `IMFeENit28P-Lining.ttf`, whose ten figures are Liberation Serif's lining figures (SIL OFL
1.1, as IM Fell is), scaled to 1270 units (above IM Fell's x-height of 938, so a 0 is never an o; a little short of its
capitals, 1384), emboldened by 22 units to match Fell's colour, all of one width so columns of money line up, and with
the old figures' kerning removed. `Print.Fell` and `Print.FellIt` load these, so every price, cash figure, count,
standing, suspicion and date in IM Fell has lining figures. The licence and the change are recorded in
`assets/fonts/OFL-Liberation.txt` and the art README's asset log. See `lining.png` and `lining_zoom.png` in the
screenshot folder (old and new at 13 and 26 px), and the ledger cutting (`reg/14_clip_books.png`: "$168.35", "+$17.40").

## 3. An open drawer dims cash and suspicion

> "An open drawer dims the side panels to 2.4–3.2:1, so I could not check cash or suspicion while reading a letter that
> costs money." (low-vision #6)

The dimming is `bench.Modulate = new Color(1, 1, 1, 0.6f)` in `ProofView.Rebuild`, not in my files. What I changed: on
the proof, the footer now carries **cash and suspicion** ("September 1885 · cash now $204.80 · suspicion: a little (4 of 100) · 2 letters waiting · 25 regulars"). The footer sits on its own canvas layer, above any drawer, and is never dimmed:
paper (239,236,228) on the table (13,12,11) is 16.6:1. So cash and suspicion are readable at 16.6:1 with every drawer
open. The bench behind the drawer still needs lifting in `ProofView` (below).

## 4. The footer: one line, and two cash figures that explain themselves

> "· 34 / regulars · 2 letters waiting" (wrapped); "after a mid-month resume it showed "cash in hand $119.42" while the
> edition said "$163.38" with no explanation." (lapsed #5, SUMMARY "Coming back mid-month")

The footer is one line at every size: it is built from pieces in order of need and leaves out the least needed (the
regulars, then "now <month>", then the letters) until it fits, measured in its own font. It never wraps. The cash
wording says which figure is which:

- on the paper, when nothing has been spent since: "The Gazette of May 30 · cash $180.56, the books up $12.21 · now June
  1885 · 2 letters waiting";
- on the paper after spending mid-month: "The Gazette of January 3 · the books closed at $150.00 · cash now $149.36,
  64c. spent since" (driven: ordered 32 oz of grain alcohol, went back to the paper; `drv_edition_midmonth.png`);
- on the proof: "cash now …" with suspicion.

## 5. A NEW SHOP showed the title page again

> "A NEW SHOP from the title page opened the title page again, now showing CONTINUE … and needed a second click." (cozy
> #6; satire reader #11)

Cause: the autosave (`SaveIfChanged`, every 1.5 s) saved the fresh backdrop shop laid out behind a first title page.
Going to SETTINGS and back rebuilt the title page with a CONTINUE, and A NEW SHOP then asked to be pressed twice. Now
nothing is saved until the shop is the player's: one they continued or began. Driven on a fresh profile: title → wait →
SETTINGS → TEXT SIZE → BACK (no CONTINUE, no save file on disk) → A NEW SHOP → straight to the first edition
(`drv_newshop_after.png`), and the save then exists. The menu now says "The game is saved as you go." rather than "at
every edition" (satire reader #11: a mid-month kill came back exactly).

## 6. "A Thread object is being destroyed" on quit

> lapsed #13: on every quit.

Not a thread of ours (the game starts none). It appeared when the game was stopped by a signal (SIGTERM from
`drive_stop.sh`, a window manager or a terminal's Ctrl+C): .NET's default handler ended the process from another thread,
with the engine's threads unjoined. Reproduced with `--fake` and no driver (1 warning per SIGTERM; none on SAVE, AND QUIT
or a `--shot` quit). `Main` now registers SIGTERM and SIGINT handlers that save the shop and quit from the main loop.
After: 0 warnings over three driven stops, and the stop saves the month (a mid-month order, stopped with
`drive_stop.sh`, came back on CONTINUE: cash $149.36).

## 7. Older items: the bottle's own label; growth not shown

> "The bottle engravings carry their own labels ("PANACEA … ONE DOLLAR"), so a notice for another remedy shows the wrong
> bottle. It needs blank-label variants and the label set in type." (round 6–9 "Still open")

- **The bottle.** `wc_bottle_panacea`'s engraving now has a blank label (the template and its card caption changed,
  rendered with `render_art.mjs`, cut with `make_cuts.py`; `check_cuts.py` passes). A new `LabelledCut` texture
  (`godot/scripts/UI/LabelledCut.cs`), which `Print.Cut("wc_bottle_panacea")` returns, draws the engraving and then sets
  the shop's own remedy on the label in woodtype, with its price under it when there is room, wherever the cut is
  printed: the paper's notices and lead stories, the proof's space, the clippings. Below a readable size (a jar on the
  drug shelf, 54 px) the label is left blank. No other file had to change. See `art/clip_notice.png` ("DR. ——'S
  CELEBRATED PANACEA" on the label of YOUR NOTICE) and `bottles_before.png` for the old engravings. The rivals' bottles
  keep their own labels.
- **More stills and a growing counter.** Both pictures are drawn in files I don't own (`StillDrawing` in
  `StillBench.cs`; `Counter` in `ProofView.cs`; `ShopBuilder.cs` is the unused 3D spike, `DeskView.cs` is unused but for
  `Talk`). So I made the pieces, ready to drop in, in a new `godot/scripts/UI/ShopGrowth.cs`:
  - `StillRow`: the house's other stills, printed small in a row under the first ("No. 2. Cold.", "No. 3. On the
    fire.", with a fire in the door when lit);
  - `CounterDressing`: behind the figures, a shelf of the remedy's bottles that fills as the regulars do (two, then one
    more for every three regulars; a second shelf once the shop has room for more than 25), and a card on the wall for
    each venture taken up (THE SHOP NEXT DOOR, THE BRANCH, THE ALMANAC, …), clear of AT THE COUNTER.

  `-- --growth` lays them out alone for a screenshot, for this shop and for the same counter with 50 regulars and every
  venture: `art/growth24.png`. Each needs one or two lines in its owner's file (below).

## 8. The drawer tabs move

They are laid out in `ProofView.cs` (`WorksSlip`, under the letters box and over the warnings), so I left them, as the
brief says. Note for that agent: the text size moves them too, by up to 14 px between REGULAR and LARGER in the same
month, because the letters and works slips grow.

## Changes needed in other files

`ProofView.cs`:

1. Dimming (finding 3): `bench.Modulate = new Color(1, 1, 1, 0.6f)` → `0.85f` or more, or leave THE WORKS slip
   undimmed. The footer now repeats cash and suspicion at 16.6:1 either way.
2. Counter cards at the larger sizes: the card is placed at `CounterTop + 2` with no room below. Let it rise by what it
   grows, e.g. after `strip.AddChild(tag)`:
   `tag.Resized += () => tag.Position = new Vector2(tag.Position.X, Math.Min(CounterTop + 2, strip.Size.Y - tag.Size.Y - 2));`.
   The fitting pass then leaves the health line at 17/19 px.
3. Counter growth: `strip.AddChild(new CounterDressing(m));` right after the `tint` ColorRect is added (before the
   figures).

`StillBench.cs`:

4. More stills: a field `private readonly StillRow _others = new();`, `left.AddChild(_others);` after
   `left.AddChild(_still);`, and in `RefreshBody` after `_still.Stills = …`:
   `_others.Set(_still.Stills - 1, Math.Max(0, d.Brewing.Count - 1));`.
5. Its `DrawString` sizes (the caption at 15, the receiver's label at 6) through `LargePrint.Size(n)`.

`MeasuringGlass.cs`:

6. The glass's labels and scale (`DrawString` at 12 and 13 px) through `LargePrint.Size(n)`; they are the "glass
   labels" the reader measured at 5 px.

## Checks

All pass in this worktree: `pnpm -s test` (140 tests; under the machine's load from the screenshot runs the harness
tests first timed out at 5 s, and passed when run again on their own), `pnpm -s typecheck`, `pnpm -s lint`,
`npx prettier --check .`, `pnpm content:lint`, `cd sim-cs && dotnet test` (117), `cd godot && dotnet build` (0 warnings,
0 errors), `python3 godot/tools/check_cuts.py`. No sim or content change, so the parity fixtures are untouched. The
test save profile used for driving was deleted.
