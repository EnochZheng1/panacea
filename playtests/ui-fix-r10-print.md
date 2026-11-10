# Round-10 fix pass · the large print and the proof

The large-print and proof agent's fixes for review round 10 (`playtests/round10/SUMMARY.md` items 1, 4 (display side), 6
and the low-vision, gentle, commuter and min-maxer reports). Files changed are listed at the end, with what other
agents' files still need.

Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r10fix/print/`:
`before/` (the build as reviewed) and `final/` (this build), each with `REGULAR/`, `LARGE/` and `LARGER/`, and the same
19 screens in each: the title page, the first edition, the proof in months 1, 8 and 24, a heavy laudanum month, the
low-vision tester's jump game (seed 2207, month 24, dose 1.5), every drawer (formula, still, supplies, ventures, letters,
books, and the new regulars list), and a formula book after pours. The script is `suite.sh` (one size) and `all.sh`
(all three) in that folder; each shot also prints the dev type check (`-- --typecheck`). `after/` is the pass before the
last small change. The driven checks are `pin.py` (formula book movement), `pin-*.txt` and `drv2.png`.

## The approach: the proof is laid out for each size, and scrolls instead of shrinking

The cause of most of the round's large-print findings was one mechanism. Round 9's TEXT SIZE raised every IM Fell label
to a floor (17 px at LARGE, 19 at LARGER) and then a fitting pass stepped type back towards its designed size wherever it
was cut off by a frame. The proof page, the counter and the bench were all clipped frames already full at REGULAR, so
the pass stepped their type back, often to below what LARGE gave it (the "left column gets _smaller_ at LARGER"), and
the cards, the notes and the deaths column ended at 5–6 px x-height at every size.

Now `LargePrint.NoFit(node)` marks a layout that is designed for every size: type under it is set at the floor and the
fitting pass never touches it. The proof page, the counter, the bench and the formula book are marked, and laid out by
size (`LargePrint.Pick(regular, large, larger)`), with what can run long in scrolls that say so at their foot. The
drawers of other agents (letters, still, supplies, ventures, books) keep the round-9 fitting pass unchanged.

## 1. TEXT SIZE reaches the danger print

> "TEXT SIZE LARGER leaves the danger text at 5–6 px x-height: NOTES FOR THE PROPRIETOR …, the regulars' cards, and the
> deaths column … A diff between REGULAR and LARGER shows almost no change on the cards." (low-vision #1; gentle,
> historian, commuter, min-maxer)

Measured on the heavy laudanum month (seed ward-7, month 6, dose 1.5), x-height in pixels (rows of the dense lowercase
band, the low-vision tester's own `lv.py xh`), before and after at LARGER:

| Text                                                   | Before, LARGER | After, LARGER           |
| ------------------------------------------------------ | -------------- | ----------------------- |
| NOTES FOR THE PROPRIETOR ("Two inquests unanswered …") | 6              | 8                       |
| Deaths column ("Found insensible after a dose: …")     | 5              | 8                       |
| Card, complaint ("a persistent cough")                 | 6              | 9                       |
| Card, habit ("· shaking", now "SHAKING")               | 5 (lowercase)  | 12 (capitals, reversed) |
| "set after press"                                      | 5              | 8                       |

The type checker agrees: before, the smallest IM Fell on the LARGER proof was 12 px (5.4 px x-height, the cards); after,
it is 19 px (8.6 px) on every proof screen, and the smallest woodtype is the 11 px tickets, unchanged by design (their
capitals are 8.4 px).

**The counter at the larger sizes**: fewer, larger cards (five at REGULAR, four at LARGE, three at LARGER, each wider),
whose woodtype grows too (names 12/14/15 px, health and habit chips 11/13/15). The counter stands higher and the people
are drawn a little shorter (the counter top at 226/206/190 px of the 292 px strip) so the cards have the room. Each card
stands on the foot of the strip and grows upward over the counter's front; none can run past the strip.

**THE REGULARS** (item 9, below) lists the rest.

## 2. Habit states escalate like health, and are never cut short

> "The addiction state appears only as a lowercase italic word after a dot … 5 px x-height … "GRAVELY ILL · shak…" is
> truncated" (low-vision #2)

The habit has a line of its own on the card, set heavier as it worsens, as the health is: **ASKS BY NAME** (craving) in
ruled capitals; **SHAKING** (withdrawal) reversed, white on black; **☞ RUINED BY IT** reversed with a hand. It is never
trimmed (no ellipsis on any card line now). Each chip has a tooltip that says what it means. A card with no habit keeps
its mood beside the health ("peaky · content"). THE WORKS counts them the same way: `1 GRAVELY ILL` and `4 SHAKING`
reversed, `2 POORLY` in capitals, `9 CRAVING` ruled, wrapping onto a second line rather than being cut. A card printed
in reverse for the keyboard keeps its chips readable (they carry their own paper or ink).

## 3. Layout at LARGE and LARGER

Each finding, and what changed. All verified at all three sizes in `final/`, with the type check reporting **0
problems on every screen at every size** (before: 8 "out of its frame" problems on every proof at every size, the
counter cards, plus a trimmed letters line).

- **The type case shrank to one row of claims under the banner, which covered the buttons** (gentle #4, low-vision #3,
  min-maxer #14). The case now scrolls under its own title, and the "MORE CLAIMS BELOW ▼" slug has a row of its own at
  the foot that is always kept (inked when there is more below, blank or "▲ THE TOP OF THE CASE ABOVE" otherwise), so it
  never covers a claim and nothing moves when it changes. At the larger sizes each space ticket carries its rate under
  it ("¼ COL / $3.00") in place of the line of rates, the claims' note is a one-line woodtype head ("CLAIMS, UP TO FOUR:
  THE CORONER READS THEM", the full sentence in its tooltip), and space not yet for sale is a printed sentence in place
  of five shut tickets. The case shows five rows of claims at LARGE and at LARGER in the heavy month
  (`final/LARGE/11_proof_heavy.png`, `drv2.png`) and four at REGULAR, the rest a scroll away; set claims are inked.
- **The formula book clipped its glass labels and pushed price and ENTER below the fold** (gentle #3, historian #6,
  low-vision #3). At LARGE and LARGER the book is laid across the whole table (the bench is out of reach while a drawer
  is open anyway, and the footer keeps cash and suspicion at 16:1). The glass is wider (280/300 px), the name, price and
  the book's buttons sit under the glass, and the assay has the rest of the width. The glass labels are never cut: a
  label that does not fit on one line is set on two, and labels are kept a line apart, bottom up, with a hairline to
  their band when a thin band's label stands off it (`final/LARGER/20_formula_jump.png`: "Oil of Peppermint ½ oz",
  "Laudanum 1½ oz"). Every band is now labelled, the thin ones too. The jars are two to a row at the larger sizes, so no
  name breaks mid-word.
- **The proof header clipped ("CORRECT AND RETURN T")**: at the larger sizes the header is the date alone ("PROOF FOR
  SATURDAY, JULY 25, 1885"), and the masthead gives up a few pixels (40/32/28 px).
- **THE WORKS wrapped "better / known: 16"** (gentle #3, min-maxer): phrases are set with non-breaking spaces, so a line
  breaks only between them; at the larger sizes the works is a line each ("0 bottles on the shelf · the still is
  cold" beside the cash; "good name: 1 of 100 · better known: 16"), and suspicion's words keep the slip's width, with its
  gauge under them. At the larger sizes last month's till line moves to the cash's tooltip (THE BOOKS has it in full).
- **The proof's left column got smaller at LARGER** (commuter #4): fixed by the approach above. The three columns of
  the proof each scroll when they run long, with "▼ more below (scroll)" at the foot, and keep their place when the bench
  is rebuilt under the hand. At LARGE the rivals' notices give way, and at LARGER the YOUR SPACE column too (the type
  case shows the space bought and the claims), so the deaths and the notes have the width.
- **Drawer buttons moved between sizes** (commuter #6): THE STILL … BOOKS are now a row of their own on a strip of paper
  just above the lever, and the post slip (with OPEN THE LETTERS on its title line) just above them, each of one height
  at every size and in every month. Measured with the driver: THE STILL at (889, 629, 70×22), OPEN THE LETTERS at
  (1087, 540, 159×25) and SEND TO PRESS at (886, 664) at REGULAR and at LARGER alike. (The tickets are woodtype, which
  the setting leaves alone, so they are the same size too.) The bench is now: the type case, THE WORKS, THE POST, the
  drawers, the lever.
- **The counter cards drew over the foot of open drawers** (low-vision #4): the cards no longer run past the strip,
  and an open drawer now covers the whole of the proof column down to its foot (it stopped 6 px short, over cards that
  ran 5 px past the strip).

## 4. Buttons and jars that moved

> "Pouring laudanum lengthens the verdict line, and CHANGE THE RECIPE / price / ENTER buttons shift ~20–35 px."
> (min-maxer #5) "The rects jumped about 95 px … and jars change size." (low-vision #5)

- The assay's cost line and verdict keep one height whatever they say (the verdict's box is measured for the longest
  verdict at the width and size it is set in), so the process tickets and the book's buttons under them never move.
- The drug shelf is built once and updated in place: a pour changes a jar's words and ink, never its place, its size or
  the shelf's scroll. Every jar is one size (room for the cut, two lines of name, two of "in the glass").
- Driven check (`pin.py`: scroll the shelf, pour valerian, three laudanum, take one back, pour aloes, comparing every
  target's rectangle after each step): **0 px movement of every jar and button, at REGULAR and at LARGER**
  (`pin-REGULAR.txt`, `pin-LARGER.txt`, `pin-LARGER.png`). Before, the same pours moved the buttons 20–35 px and the jars
  95 px.
- **The stale jar tooltip** (historian #11, "while I hovered Oil of Peppermint, the tooltip described Dilute Sulphuric
  Acid"): the cause was the shelf being rebuilt under the mouse after every pour. Each jar now keeps its control and its
  own tooltip, said afresh at each pour. Only water is "free, from the pump"; another free drug is "free, and always on
  hand". The jar prices are untouched, as asked.

## 5. Low contrast

> "Low-contrast greys. "set after press" and "No deaths last month." are grey italic at 5–6 px, 4.67:1. The disabled
> ad-space buttons in months 1–3 are 2.51:1, with the reason only in a tooltip." (low-vision #6)

- The proof's pencilled lines ("set after press", "No deaths last month.", "last month's, struck out by the printer")
  are set at 13 px (17/19 at the larger sizes) in ink at 0.9: measured **7.1:1 at REGULAR and 9.5:1 at LARGER**
  (before 4.3:1 measured the same way). The struck-out names went from 0.82 to 0.9 ink.
- A shut ticket's words are ink at 0.78 instead of 0.6 (about 7:1 on the paper, still told from a live ticket by its
  hairline border and no ink fill). This is in `Print.Button`, so it also lifts "TAKE IT UP · $200.00" in the
  prospectus (4.0:1 reported).
- The reason for shut space is printed where the tickets are: under them at REGULAR ("Not yet: the Gazette wants $100 in
  savings and a printed testimonial first.", now directly under the tickets), in their place at the larger sizes.

## 6. THE MONTH SO FAR always visible

> "After a relaunch in January 1886, THE MONTH SO FAR heading sat on the last line of the lead-story column, and its
> entries were clipped below the frame, out of sight." (commuter #1)

THE MONTH SO FAR has a dashed box of its own under the lead story, sized to its lines (measured at the width and size
they are set in) up to about a third of the column, after which it scrolls; the notes above it scroll in their own box
and can never push it down. Driven at LARGER in the heavy month after ordering, lighting the still and answering an
inquest: four lines visible under three notes (`drv2.png`). At every size in `final/`. (CONTINUE opening the edition
rather than the proof is `Main.cs`, not mine.)

## 7. What moved suspicion

> "Suspicion rose twice with no reason I could find. March 1886: 0 → 3 … Both months a regular died "of a fever… the
> bottle is not blamed."" (gentle #2) "Choosing "Keep your head down" took suspicion from 38 to 75 the moment I clicked
> … the slow rise before it (5, 10, 38) gave little sense of how close the cliff was." (min-maxer #1)

- The proof's left column has a **SUSPICION.** section under DEATHS: last month's changes by cause, as the ledger
  records them (`PaperFacts.HeatLines`, already filled from the month's report by `SimGame`, so **no sim edit was
  needed**), largest first, each in plain words: "Rose 3: a death among the regulars. The ward talks of every death at
  the shop, whether the bottle is blamed or not, and of a blamed one twice as much." "Fell 4: the talk dying down, as it
  does in a quiet month." "Rose 1: the claims on the label and in the paper: the bolder the claim, the more it is talked
  of." The same lines are in THE WORKS suspicion tooltip. If the sim agent changes the rules, these lines follow the
  ledger.
- **Warned ahead**: an unanswered inquest's note now says how far a verdict moves the meters, before the reply: "Two
  inquests unanswered: the coroner will proceed without you. Each jury that names the bottle, suspicion rises 24 (it is
  76 now) and the good name falls 12; if both name it, it will stand at 100 of 100, and at 100 the Board of Health closes
  the shop. The letters print each reply's odds." The 24 and 12 are the sim's `Implicate(24, 12, 8)` in
  `Scrutiny.ResolveInquest`, held in one constant pair in `ProofView` (`InquestHeat`, `InquestGoodName`); if the sim
  agent changes them, those two numbers must change with them.

What this does not show: suspicion moved _this_ month (by letters answered since the edition). That would need the
month-in-progress deltas on `DeskModel` (`state.Month.Deltas`, a field in `IGame.cs` and two lines in `SimGame.Desk()`);
it is not needed for the reports' cases, which the last-month lines and the warning cover, so I left it.

## 8. One name: "good name"

The works says "good name in the ward" (REGULAR) or "good name" (larger sizes); its tooltip now begins "Good name in
the ward: what the ward thinks of the shop, out of 100 … A letter's "good name +1" adds to this figure." (it said
"Standing: …"). The unused `DeskView` desk said "Standing in the ward"; it now says "Good name in the ward". No
"standing" remains in my files. The letters' "standing +1" wording is the letters agent's.

## 9. Every regular, listed

> "I found no way to see the other regulars ("and twenty-four more regulars")." (low-vision #9)

A ticket on the counter, **ALL THE REGULARS ▸** (beside AT THE COUNTER, above the tallest hat), opens a new drawer,
**THE REGULARS**: every regular, the frailest first, each with name, complaint, how long they have come ("new this
month", "a regular for twenty-one months"), bottles last month, mood, and the health and habit chips as on the cards,
in the drawers' large print (`final/*/21_regulars_jump.png`). The keyboard reaches it as any ticket, and Escape puts it
back on the ticket.

## 10. "A regular for 0 months"

The card and figure tooltips say "new this month" (`ProofView.Since`), and spell the months in words as the bench does.

## Also

- A change of TEXT SIZE in the settings now rebuilds the proof (and the formula book) at once; before, the proof kept
  the old layout until the next action.
- `Cuts.cs` had a nullable warning (CS8603) on a full rebuild; fixed.

## Checks

All pass in this worktree: `pnpm -s test` (159), `pnpm -s typecheck`, `pnpm -s lint`, `npx prettier --check .`,
`pnpm content:lint`, `cd sim-cs && dotnet test` (134), `cd godot && dotnet build` (0 warnings, 0 errors, full rebuild),
`python3 godot/tools/check_cuts.py`. No sim or content change, so the parity fixtures are untouched. No save profile was
created (the driver runs used dev mode).

## Left open, and changes needed in other files

- `Main.cs`: the footer drops its last item at LARGER (low-vision #7); CONTINUE opens the edition, not the proof
  (commuter, low-vision #10).
- `StillBench.cs`: the ▲/▼ batch keys (gentle, historian), and its `DrawString` sizes through `LargePrint.Size(n)`
  (round 9's note, still open).
- Letters (`LettersDesk.cs`): "standing ±n" to "good name ±n", to match THE WORKS.
- The sim's inquest numbers: if `Scrutiny.ResolveInquest`'s `Implicate(24, 12, 8)` changes, change `InquestHeat` and
  `InquestGoodName` in `ProofView.cs` to match (or expose them on `DeskModel` and I will read them).
- At LARGER in a heavy month the notes box shows about three notes and scrolls for the rest; the cards show three
  regulars and THE REGULARS has the rest. That is the trade at 1280×800 with 19 px type.

## Files changed

- `godot/scripts/UI/ProofView.cs`: the proof page (scrolling columns, the SUSPICION section, THE MONTH SO FAR's own box,
  header and masthead by size), the counter (cards by size, the habit chips, ALL THE REGULARS), the bench (type case,
  works, post, the drawer row, by size), the inquest warning, contrast, "good name", "new this month".
- `godot/scripts/UI/FormulaBench.cs`: across the table at the larger sizes; pinned cost, verdict and buttons; jars built
  once and updated in place at one size; two jars to a row at the larger sizes; the stale tooltip.
- `godot/scripts/UI/MeasuringGlass.cs`: labels never cut, kept apart, every band labelled.
- `godot/scripts/UI/LargePrint.cs`: `NoFit` and `Pick`.
- `godot/scripts/UI/Print.cs`: shut tickets darker; `InkScrollbar` for scrolls printed on the paper.
- `godot/scripts/UI/DeskView.cs`: "Good name in the ward".
- `godot/scripts/UI/Cuts.cs`: the nullable warning.
- New: `godot/scripts/UI/RegularsList.cs` (and its `.uid`), THE REGULARS drawer.
