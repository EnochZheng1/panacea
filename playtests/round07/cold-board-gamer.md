# Panacea · round 7 · playtest by "Board gamer"

A heavy-euro board gamer, playing for the first time. I judge rules legibility first: can I learn the rules from what the screen shows, can I predict what an action will do, does every number mean one thing, and do the parts fit into a system I could explain to a friend.

**Verdict:** The rules can be learned from the table, which is rare. Every drawer says what it costs and what it does, the ledger closes to the cent, and the edition explains the month. By month 5 I could have taught the core loop to a friend. What lets it down is the bookkeeping across two remedies, where the paper's per-remedy counts are wrong and the formula book can only show one remedy. One button in the still also moves under the cursor and once bought me a second batch I didn't ask for.

**Setup:** build `5a1dbc9` (frozen copy `build-r7`), port 47044, seed 7044, one fresh game with no jump-ahead. I played 12 months by hand, January to December 1885 (12 sends to press), about 45 minutes of active play. My only tooling was a small wrapper around `tools/drive.py` (`scratchpad/r7/cold-board-gamer/d.sh`) for clicks and screenshots. The game never crashed or restarted. Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r7/cold-board-gamer/NN.png`.

---

## Run log

| Seed | Months | What I tried | Result |
|---|---|---|---|
| 7044 | Jan–Mar 1885 | The default glass plus ½ oz laudanum, steeped, 60c then 65c then 60c. Claims "Soothes the Nerves" and "Banishes Rheumatism". Printed a testimonial. | 18, 11 and 17 bottles sold. Cash flat at about $150. Gazette space opened in April. |
| 7044 | Apr–Jul | Took THE PROPRIETOR'S ALMANAC ($40), bought ¼ col, 48-bottle batches, price up to 70c. Paid the sanitary inspector $3, burned the diploma offer. Entered a second, honest remedy, "Valerian Bitters". | 50, 42, 50 and 52 sold. Sold out three times. The second still arrived free at 200 bottles sold. |
| 7044 | Aug–Oct | Two stills (Panacea at 75c, Valerian at 55c), ½ col, a third claim ("Purifies the Blood"). | 61, 61 and 74 sold. Got 96 bottles by mistake in October (F1). 26 regulars. |
| 7044 | Nov–Dec | Took THE SHOP NEXT DOOR (nothing down, $5 a month), joined St. Bridget's benefit society ($1.50 a month), took A LARGER COPPER ($200). | 68 and 87 sold. **One death** (Miss Wilhelmina Vogel, November). No inquest; suspicion at most 5 of 100. Ended January 1886 with $111.00 and 28 regulars. |

**Timeline of key moments**
- **Jan:** A hint under THE WORKS ("Nothing on the shelf and nothing on the still: light THE STILL, or the shop will sell nothing this month.") told me the first move. It was clear.
- **Mar:** A grateful letter. Printing it was the key that unlocked advertising. The rule was printed up front: "the Gazette wants $100 in savings and a printed testimonial first."
- **Apr:** Took my first venture (the almanac, $40). It took 3 months to unlock and one click plus a confirm. Sales jumped from 17 to 50 the next month ("ten by the almanac" callers, 12 bottles by post).
- **Jul:** "A second copper still was carried into the back". A free milestone at 200 bottles. The still drawer had said "Next: a second still: 188 of 200 bottles sold", so I saw it coming.
- **Oct:** I meant to light one still and lit both: 96 bottles, $16.80 (F1).
- **Nov:** Miss Vogel died, but the Death figure had stood beside Widow Lowenthal for three months (F7). Took the shop next door.
- **Dec:** The larger copper ($200). The still showed 72-bottle batches at once and the next edition ran "a coppersmith … has delivered a larger c[opper]".

---

## The rules as I inferred them (what I'd tell a friend)

1. **Turn:** each month you read the edition (last month's results), then edit the proof (this month's decisions), then SEND TO PRESS. Decisions take effect this month and results print in the next edition.
2. **Remedy:** in the FORMULA drawer you pour ingredients into an 8-oz glass, then pick a process (simple mixture or steeping). The assay shows seven bars: Kick, Calm, Habit, Danger, Taste, Lingers, In an assay. It also gives a cost per bottle. You enter the recipe in the book with a name and a price, in 5c steps. You can change the recipe or price later. The label stays the same, and brewed stock keeps its old recipe.
3. **Production:** THE STILL brews up to 48 bottles per still per month (72 with the larger copper), ready by press time. You have to order ingredients from SUPPLIES or with the still's "ORDER ENOUGH" button first. Bottles and corks are charged when the fire is lit.
4. **Demand:** regulars come back (the counter shows their health and how attached they are), walk-ins arrive, and ad space and ventures bring more callers. If you sell out, callers are turned away (the headline says so).
5. **Claims:** up to four per label. "Bolder type sells more, and the coroner reads it too." Each "!" is a level of boldness. Bold claims add about 1 suspicion a month, and "talk dies down" removes about 1.
6. **Suspicion** (0–100) rises with bold claims and deaths. **Standing** rises with satisfied customers and printed testimonials.
7. **Ad space:** none, ¼, ½ or full column, or the front page, from $3 to $20 a month. It unlocks at $100 savings plus a printed testimonial. ½ col and up prints the bottle and the price.
8. **Letters:** each choice shows its exact price and effect before you click ("Free, but the ward sees him at it: standing −2"). If you leave a letter unanswered, "the usual reply" goes out.
9. **Ventures:** each one shows its terms, its unlock condition and your progress ("So far: 436 of 1,200 bottles sold; $249.79 of the $300.00 in hand"). Some milestones are free and automatic: a second still at 200 bottles, a third at 600, a percolator after ten batches.
10. **Away orders:** post orders and druggists are filled after the regulars, at a wholesale or shelf price. They take "the bottle the shop has most of". I only learned that from the source. On screen, only the druggist's venture text hints at it.

That is a system I can explain: production feeds sales, sales feed milestones and cash, cash buys ventures, and claims trade sales against suspicion. Everything in it except rule 10 was learnable from the table.

**Where it surprised me**
- January sold 18 bottles but February only 11 with no change except +5c. The first month's new-customer bump isn't flagged as one-off, so I misread it as a price response.
- One click lit both stills (F1).
- The paper's per-remedy counts didn't match the ledger (F2).
- The Death figure stood beside the wrong customer (F7).
- The FORMULA drawer had no way to pick my second remedy (F3).
- The second still and the percolator just turned up. That was a pleasant surprise, because the trade-talk gossip ("halfway to a second copper") had foreshadowed both.

---

## Metrics

**Cash at the start of each month (bottom bar), the books' change, and bottles sold in that month's edition**

| Month | Cash | Books | Sold | Regulars |
|---|---|---|---|---|
| Jan 85 | $150.00 | — | 18 | 12 |
| Feb | $152.64 | +$2.64 | 11 | 12 |
| Mar | $151.63 | −$1.01 | 17 | 12 |
| Apr | $158.83 | +$7.20 | 50 | 12 |
| May | $132.51 | −$26.32 (almanac $40) | 42 | 13 |
| Jun | $145.59 | +$13.08 | 50 | 14 |
| Jul | $161.27 | +$15.68 | 52 | 16 |
| Aug | $181.35 | +$20.08 | 61 | 16 |
| Sep | $204.82 | +$23.47 | 61 | 19 |
| Oct | $223.93 | +$19.11 | 74 | 21 |
| Nov | $249.79 | +$25.86 | 68 | 26 |
| Dec | $280.69 | +$30.90 | 87 | 25 |
| Jan 86 | $111.00 | −$169.69 (copper $200) | — | 28 |

(The Sold column for each month is from the edition read at the start of the next month.)

- **Total bottles:** 591 (the game said "504 of 600" at the December proof, which matches my sum to November).
- **Deaths:** 1. **Inquests:** 0. **Peak suspicion:** 5 of 100. **Standing:** from 10 to 22.
- **Time to first venture:** month 4, the almanac, and it paid back in about 4 months. Later: the shop next door in month 11, the larger copper in month 12. Free milestones: a second still in month 7 and a percolator in month 9.
- **Ledger reconciliation:** I checked every month by hand: opening + lines = closing, to the cent, 12 of 12 times. Takings always equalled bottles × price across remedies. That earns trust.
- **Clicks for a routine month:** 6 (THE STILL → ORDER → LIGHT → Escape → SEND TO PRESS → TO THE PROOF), plus reading.
- **Time to understand the core loop:** about 3 months (I understood it fully when the testimonial-to-ad unlock came through). The claims/suspicion trade-off was clear from the type case's line. Deaths and the Death marker never became fully clear.
- **Driver log:** only ALSA "no sound device" errors at startup and Godot leak warnings at exit. No exceptions during play.

---

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| F1 | **Major** | After you press ORDER ENOUGH, the buttons move. LIGHT THE FIRE jumps up from y≈561–593 to y≈534–566, and **"BOTH STILLS · ORDER AND LIGHT 48 EACH" slides into the spot LIGHT THE FIRE had** (y≈574–598). Order-then-light in the same spot lit both stills. I got 96 bottles and $16.80 of ingredients when I meant 48 and $8.40. | THE STILL, with 2 stills | October 1885 (109.png, 117.png): mark 48 → click ORDER (703,577) → click (492,577) → "Every still is on the fire (48 … ; 48 …)". The target rects before and after ORDER show the shift. | Keep LIGHT THE FIRE fixed where ORDER was (or make ORDER turn into LIGHT in place). Put BOTH STILLS somewhere a repeat click can't land, or give it a two-press confirm like the ventures. |
| F2 | **Major** | With two remedies, the TRADE clipping's per-remedy counts don't match the ledger. August: "Dr. ——'s Celebrated Panacea moved 35 bottles at the shop", but the counter took $36.00 = 48 × 75c. The 13 post bottles were Valerian. November: "Panacea 48 bottles" and "Valerian Bitters moved 11 bottles at the shop"; the ledger implies 57 Panacea and 2 Valerian at the counter, plus 9 Valerian by post. Cause, checked in the source after my session: `EditionWriter.cs:225–229` takes away-orders off whichever remedy sold most in total. The sim gives them "the bottle the shop has most of" (`Market.cs` OrdersFromAway). | Edition → THE TRADE | Brew two remedies with the almanac held, so the second remedy has more left on the shelf after regulars. Compare the clipping with the ledger (97.png, 120.png, 96.png). | Report away-orders per remedy from the sim's actual deliveries (`SaleRecord` per formula and channel). Tell the player on the almanac card that post orders take the remedy with the most on the shelf. |
| F3 | **Major** | The FORMULA drawer has no remedy tabs. With "Valerian Bitters" in the book it always opens on the Panacea ("on the shelf at 75c"). I found no way to see the Valerian recipe or change its 55c price. THE STILL and the type case both have remedy tabs. | FORMULA | Enter a second remedy (June), then reopen FORMULA in August or September (93.png, 100.png) | Add the same remedy tabs as THE STILL to the formula book. "The book" should list every remedy. |
| F4 | Minor | On the first TAKE IT UP press, the prospectus list scrolls, and the "PAY … DOWN? PRESS AGAIN" confirm ends up off-screen. I had to scroll back to find it. It happened both times (Shop Next Door, Larger Copper). | VENTURES | November/December: click TAKE IT UP on the 1st or 2nd card (114.png, 121.png) | Don't scroll when the button changes state, or scroll the confirm into view. |
| F5 | Minor | A nothing-down lease reads "TAKE IT UP · 0c." and "PAY 0c. DOWN? PRESS AGAIN". Its $5 a month then shows in the ledger as "Branch rent, wages and upkeep" although I hold no branch. | VENTURES, ledger | Take The Shop Next Door, then read the next Month's Books (119.png) | Say "TAKE IT UP · $5.00 A MONTH". Name the ledger line after the venture, or call it "Rents and upkeep". |
| F6 | Minor | One batch shows several cost figures that don't reconcile. The still says "24 bottles · 72c. for bottles and corks when lit · takes $4.56 of stock". The ledger charges "Bottles, corks and labels 96c" (labels are never mentioned before lighting). The assay says "Costs 22c. a bottle" (= $5.28). | THE STILL vs ledger vs assay | January 1885 (19.png, ledger 30.png) | Print the full figure before lighting ("96c for bottles, corks and labels"). Give the assay's cost per bottle the same breakdown. |
| F7 | Minor | "Death waits at the counter beside Widow Rebecca Lowenthal" (and "He stands beside whoever is nearest the end") from September to December. But the one who died in November was Miss Wilhelmina Vogel, who was never shown at the counter. The only forecasting tool on the table predicted wrong. | AT THE COUNTER, THE WORKS hint | Seed 7044, September–November 1885 (70.png, 107.png, 118.png) | Let Death stand beside whoever is actually at risk this month, including occasional buyers. Or say "among your regulars". |
| F8 | Minor | An empty glass still shows the last recipe's assay bars (greyed) above "Not yet. Formula has no ingredients." It looks like stale data. | FORMULA | Open the drawer with an empty glass (72.png) | Draw empty bars when the glass is empty. |
| F9 | Minor | An honest mixture (water, alcohol, valerian, gentian, syrup) shows a non-zero "In an assay · a chemist might find it" bar. I never learned what a chemist finds, or when that matters (inquests? a rival's assay?). | FORMULA assay | 73.png | Add a tooltip on each bar saying what it feeds into ("raises the chance an inquest names your remedy"). |
| F10 | Low | With the larger copper, the still panel grows and the BOTH STILLS button is clipped by the panel's bottom edge. | THE STILL, 72-bottle batch | December (123.png) | Let the panel scroll, or tighten the bottle grid. |
| F11 | Low | When the list is scrolled, the prospectus shows an orphan "YOURS · four months to run" at the top with no venture name. | VENTURES | 121.png | Put the "yours" status line beside the card title. |
| F12 | Low | The prospectus has "The coppersmith on Elm street" but the delivery notice says "a coppersmith from Water street". | VENTURES vs NOTICES | December/January | Use one street. |
| F13 | Low | The first month's new-customer bump (12 walk-ins) isn't marked as one-off, so February's drop from 18 to 11 sold reads like a response to +5c. | Ledger, January to February | Raise the price in February | One line in the first edition: "the curious came for a first bottle". |

---

## What works

- **Every choice prints its price and effect before you click.** The letters ("$3, and he is gone by dinner. You become a little better known at the Department." / "Free, but the ward sees him at it: standing −2.") are textbook euro clarity.
- **Ventures show their unlock rule and live progress** ("So far: 436 of 1,200 bottles sold; $249.79 of the $300.00 in hand"), and the paper's trade talk foreshadows the free milestones ("halfway to a second copper"). This is the best part of the system for a planner.
- **The ledger closes to the cent every month.** "The Month in Brief" line plus the ledger popup is the right two-level summary.
- **The type case's single rule line**, "Bolder type sells more, and the coroner reads it too", together with ! / !! / !!! on each claim, is the whole claims rule in one sentence. The ledger then confirms it every month ("Suspicion in the ward rose 1 · Bold advertising claims").
- **The hint lines under THE WORKS** ("Nothing on the shelf and nothing on the still: light THE STILL…", "one letter unanswered: the usual reply will go out for you") taught me the month without a tutorial.
- **Two-press confirms on venture purchases.**
- **The look:** the woodcut gazette, the Death figure at the counter, and green reserved for the product (green ink in the glass and on the bottles) make the green read like a game component.

---

## Disclosures

- No jump-ahead. One continuous game; the container never restarted.
- I used `targets` and `read` to find button rects and quote text exactly, and `state` to confirm stock and price per remedy. That is how I spotted F2 and F3, which a player could only work out from the ledger with a pencil.
- After the session I read `godot/scripts/Paper/EditionWriter.cs` (lines 218–245), `sim-cs/Panacea.Sim/Systems/Market.cs` (OrdersFromAway) and two string keys in `packages/content/strings/en.json`, only to confirm F2 and F5. Rule 10 above comes from that reading.
- The driver did not apply Shift to clicks, so I never tested the ¼-oz pour.

---

## First hour

- **0–3 min (Jan edition):** First edition, then the proof. I read the page layout as the game board: the proof on the left, three drawers on the right, the counter below. The THE WORKS hint told me to light THE STILL. I thought the game wanted a recipe first.
- **3–8 min:** FORMULA. The glass was pre-filled (water 3, alcohol 2, syrup 1, peppermint ½). I added ½ oz laudanum and an ounce of water. The assay bars moved and read like a stat card. I entered it in the book at 60c with the default name "Dr. ——'s Celebrated Panacea".
- **8–10 min:** Claims. I picked the two un-banged claims, thinking bolder type means more risk. The ad space was locked behind "$100 in savings and a printed testimonial". I understood that rule at once.
- **10–13 min:** THE STILL. "ORDER ENOUGH FOR 24 · $4.20", then LIGHT THE FIRE. I looked at SUPPLIES (a price list with +8/+32) and VENTURES (cards with unlock rules), then sent to press. I hadn't expected the edition's trade report to be the results screen. **It clicked here:** the lead story, the trade numbers and the ledger popup are the scoring phase.
- **13–20 min (Feb–Mar):** I raised the price and sales fell. I misread why (F13). A testimonial letter arrived and I printed it.
- **20–28 min (Apr–Jun):** Took the almanac and ¼ col. Sales tripled. A bribe letter. I entered a second remedy and found the FORMULA drawer can only show the first (F3).
- **28–40 min (Jul–Oct):** The second still arrived free. Planning both stills, I hit F1. Reconciling August's numbers, I found F2.
- **40–50 min (Nov–Dec):** The first death, of someone the Death marker had not pointed to. I took two more ventures, and saw the prospectus scroll jump twice (F4).

---

## Score

**7 / 10.** For a game played entirely through newsprint, it is remarkably learnable. Costs, unlocks and consequences are almost all on the table, and the ledger always balances. It loses points because the second remedy breaks the formula book and the paper's own counts, and because a moving button spent my money. **One point up:** fix F1–F3, so that two remedies are as legible as one and no button moves under the cursor.
