# Panacea · round-10 fix pass · the drawers and the front door

Owner: THE STILL, SUPPLIES, VENTURES, BOOKS, THE MONTH SO FAR, the footer, CONTINUE, and the desk model
(`Main.cs`, `StillBench.cs`, `OrderForm.cs`, `Prospectus.cs`, `BooksDrawer.cs`, `MonthBook.cs`, `IGame.cs`, the desk
part of `SimGame.cs`). Verified with driver runs on port 47122 and screenshots in
`/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r10fix/drawers/`.

## 1. Figures that disagree with the books (min-maxer #3, #11)

> "With Lehmann & Stroud's 8% discount, the still said 'ORDER FOR 48 · $6.96' and MONTH SO FAR said 'Stock ordered
> from Pearl street: $6.96' … the BOOKS say '$6.63 for drugs'."

**Cause.** The desk handed the screens each drug's price rounded to whole cents (`(long)Math.Round(...)`), so a
discounted 4.6c. ounce was priced at 5c. The sim bills `JsMath.Round(ounces × exact price)` per article.

**Change.**

- `IngredientRow` now carries the exact price (`CostPerOunceExact`, `CostPerOunce`), `Free` (only what really costs
  nothing, i.e. the water) and `OrderCents(oz)`, which bills an article as the sim does.
- THE STILL's ORDER and ALL STILLS buttons, SUPPLIES' amounts and +8/+32 tooltips all price through `OrderCents`.
- THE MONTH SO FAR's "Stock ordered" figure is now read from the month's own ledger deltas
  (`DeskModel.StockOrderedCents`: cash, cause "ingredients", detail an ingredient), so it is the books' figure to the
  cent. A letter's trial order, booked as supplies, is not counted as an order of stock.
- SUPPLIES prints the exact price an ounce ("1.54c.", "½c.", "$1.925"), so the rate times the ounces is the amount.
- **Supplier.** `DeskModel.Supplier` is Lehmann & Stroud of Maiden Lane while their trial order's 8% runs (the
  `jobber_rival` letter was drawn and an 8-point timed discount is live), else J. & W. Hobart of 81 Pearl Street.
  The SUPPLIES letterhead (name, address, seal; Lehmann & Stroud have none), the still's ORDER tooltip ("Orders from
  Maiden Lane …") and THE MONTH SO FAR ("Stock ordered from Maiden Lane") follow it. BOOKS says "drugs and stock from
  the wholesale house", which is true whoever it was.

**Verified.** Seed r10d, `--play 27` (autoplay takes the trial; 15% bulk + 8% = 23% off). ORDER FOR 24 · $1.84;
pressed it: cash $173.27 → $171.43 (−$1.84); THE MONTH SO FAR "Stock ordered from Maiden Lane: $1.84."; after the
press BOOKS: "$1.84 for drugs and stock from the wholesale house". SUPPLIES heads LEHMANN & STROUD
(`supplies.png`, `still1.png`, `monthsofar.png`, `books.png`).

## 2. WHAT A BOTTLE EARNS uses the recipe in the book (min-maxer #4)

> "In Feb 1886 the shop sold 33c laudanum bottles, but the books said '… costs 18c. to make: 57c. clear'."

**Change.** At each press `SimGame` notes every lot on the shelf and every batch on the fire with its cost a bottle
(its own recipe, at the prices at press), and after the month sees how many of each left the shelf. The weighted
cost of what was sold goes to `FormulaRow.SoldUnitCostCents` (and `SoldLastMonth`). It is rebuilt on loading, as
every press is replayed. When last month's bottles differ from the book (recipe or price), BOOKS says both:

> Dr. ——'s Celebrated Panacea: the twenty-four bottles sold last month went at 60c. and cost 14c. each to make: 46c.
> clear on every bottle. The recipe in the book now costs 11c. to make: 49c. clear on every bottle.

The break-even line ("the first N bottles a month only pay for them") uses last month's margin when there is one.

**Verified.** Seed r10d: brewed 24 to the ¼ oz laudanum recipe, took the laudanum out, pressed; BOOKS as quoted
(`books.png`). `--savetest 30`: "SAVETEST OK".

## 3. THE MONTH SO FAR's order (min-maxer #7)

> "'The recipe of Mulberry Tonic changed.' above 'A fire lit: 48 bottles', though the fire was lit first."

**Change.** After the money line, the entries are listed in the order they were done (by their place in the month's
log). Fires lit to the same recipe are run together; a change of recipe starts a new line. In a month whose recipe
changed, each fire says which: "to the old recipe", "to the new recipe" (or "to a recipe since changed").

**Verified.** "· A fire lit: 24 bottles of Dr. ——'s Celebrated Panacea, to the old recipe." then "· The recipe of
Dr. ——'s Celebrated Panacea changed." (driver read).

## 4. The still's keyboard (gentle #7, historian #10)

> "▲ took the batch _down_ a dozen (36 → 24) and ▼ took it _up_." "Once the bottle grid showed 6 hatched while the
> text said 'You have marked 24'."

**Change.** ▲/Page Up add a dozen, ▼/Page Down take a dozen off; the legend says so ("▶ a bottle more, ◀ one fewer;
▲ a dozen more, ▼ a dozen fewer") and the tooltip too. The grid's hover preview (the bottles pencilled up to the one
under the cursor) took precedence over the count, and the shelf is rebuilt under a resting cursor after every key, so
the grid showed the bottle under the mouse rather than the count. A key or a click now puts the hand "at rest": the
preview follows the cursor again only when it really moves.

**Verified.** Clicked bottle 6, left the mouse there, pressed ▲ twice: "You have marked 30", and the grid shows 24
inked and 6 hatched (`still_up2.png`); ▼ then ▶ gave 19.

## 5. SUPPLIES offers free goods; "sulphuric acid from the pump" (min-maxer #13, historian #11)

**Cause.** Dilute sulphuric acid costs half a cent an ounce; .NET's banker's rounding made it 0c., so every screen
called it free, "from the pump", and the still drew it unbilled (the sim then charged it anyway).

**Change.** Only the water is `Free`. SUPPLIES prints "free · the pump · not ordered" for the water with no marks to
make; the acid is priced ("0.39c." at 23% off, "½c." at list) and ordered like any drug. "FILL IN FOR 24 BOTTLES"
skips the water. `CostPerOunceCents` (whole cents, used by screens I don't own) is never 0 for a good that costs
anything, so the formula book's jar now reads "Dilute Sulphuric Acid: 1c. the ounce" rather than "free, from the
pump". The content description ("A sour tang that tells the customer the microbes are being killed.") never said
pump, so no content change is needed.

## 6. CONTINUE opens last month's edition (commuter #5, low-vision #10)

**Change.** When a save is laid out at launch, `Main` notes whether anything has been done since the last press. If
so, CONTINUE opens on the proof (the footer's "☜ LAST EDITION" is one press away); at a month's start it opens on the
edition, as before. SAVE, AND THE TITLE PAGE → CONTINUE still lifts the card on whatever was on the table.

**Verified** (`--player --profile r10drawers`, deleted afterwards): entered a remedy, `drive_stop`, relaunch,
CONTINUE → `screen: proof` (`continue_mid.png`); sent to press, `drive_stop`, relaunch, CONTINUE → `screen: edition`.

## 7. Ventures' terms (historian #5, min-maxer #8)

> "THE SHOP NEXT DOOR: 'for five dollars more a month' … 'Terms: nothing down, and $3.00 a month'." "A FOURTH STILL:
> 'a boy…at two dollars a month' against '$3.00 a month after'."

The Terms line was already built from the numbers. The descriptions now are too: any sum the description names "a
month" ("five dollars more a month", "$2 a month") is set to the upkeep the terms charge. Both now read "three
dollars" (`ventures_top.png`).

**Flag for the content owner** (`packages/content/strings/en.json`): `investment.shop_next_door.description` says
"five dollars more a month" (upkeep 300) and `investment.fourth_still.description` "two dollars a month" (upkeep
300). The other descriptions name no monthly sum.

Also in VENTURES:

- "Its price: $145.92 of the $200.00 in hand" now reads "Its price: $145.92 in hand of the $200.00" (historian).
- A FOURTH STILL no longer says "waits on a second still" inside the third still's line when the second still has a
  line of its own.
- The prospectus opens at its head (it opened ~80 px down, past the first notice).
- THE SHOP NEXT DOOR and THE BRANCH, once on offer, say "Mark well: the shop has room for thirty regulars and has
  twelve; more room pays only once the room it has is taken" while the shop is under 85% full (min-maxer #8).

## 8. The roof letter's $5 as "ventures taken up" (min-maxer #6)

Letters tagged "outlay" are booked by the sim under "Outlays on the business", with the ventures. BOOKS now calls
that line "outlays on the shop itself (ventures, repairs, signs)".

## 9. The footer at LARGER drops "· 11 regulars" (low-vision #7)

**Change.** Each piece of the footer has a short form. While the line is too long: the date takes its short form
("Mar. 1885", "Gazette of Mar. 26"), then is left out; then the needed pieces take theirs ("suspicion 5 of 100", "1
letter", "books closed at …"). Cash, suspicion, the letters waiting and the regulars are never left out.

**Verified** at LARGER: `--play 5`: "cash now $144.00 · suspicion: none to speak of (1 of 100) · 2 letters waiting · 12
regulars" (date gone, all four kept: `footer_crop_5.png`); edition after spending: "Gazette of Mar. 26 · books closed
at $173.27 · cash now $164.99 · 1 letter waiting · 16 regulars" (`larger_edition_footer.png`).

## 10. Stale drug-shelf tooltip (historian #11)

Not in my files: the jar tooltips are built in `FormulaBench.cs`. See below.

## Changes needed in files I don't own

- **FormulaBench.cs** (formula agent): the stale jar tooltip (historian #11: hovering Oil of Peppermint showed the
  acid's). Also use `IngredientRow.CostPerOunce` / `Free` / `OrderForm.PerOunce` rather than `CostPerOunceCents`
  in the jar tooltip and label (lines ~145, 159, 166), so a discounted price reads exactly ("1.54c./oz", "½c.").
- **ProofView.cs**: the SUPPLIES button's tooltip says "the wholesale druggist on Pearl street"; use
  `DeskModel.Supplier.Street`. The `bulk_supplier` notice ("a wholesale supplier on Pearl street") is fine.
- **DeskView.cs** (lines ~193–346, the older desk): still prices orders with `CostPerOunceCents` and says "Pearl
  street"; if it is still reachable, use `OrderCents` and `Supplier`.
- **Content** (`en.json`): the two venture descriptions in §7.

## Checks

`cd godot && dotnet build` (0 errors; the one warning is in Cuts.cs, not mine) · `cd sim-cs && dotnet test` (134
passed) · `python3 godot/tools/check_cuts.py` (up to date) · `--savetest 30` (OK) · `pnpm -s test`, `typecheck`,
`lint`, `content:lint`, `npx prettier --check .` (all pass). No sim or content change, so no parity fixtures to
regenerate.
