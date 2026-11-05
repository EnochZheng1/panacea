# Panacea · round 6 · playtest by "Cozy-game player"

A player of gentle shop and farming sims, drawn in by the woodcut look without knowing how dark the game is. I judge whether it is welcoming, whether a month's routine is pleasant or a chore, whether the dark humour lands or just upsets me, and whether I'd play another session.

**Verdict:** Surprisingly cosy, if you play it honestly. Running a harmless cordial shop on Mulberry Street works as a warm, slow-growing shop sim, and the newspaper is a lovely way to be told how you did. The jokes mostly charmed me rather than upset me. The routine turns samey around month 10, once the stills cap my output and the same four headlines come round again. A few contradictions and leaking tooltips chipped at my trust. I would play another session.

**Setup:**
- Build: frozen copy at commit `3e627bf`, port 47022.
- Game 1: seed 2206, a fresh game with no jump-ahead. I played 17 sends to press (Jan 1885 to the edition of May 29, 1886), about 27 minutes.
- Game 2: seed 3131 with `--play 10 --dose 1.5` (jump-ahead). I played 4 months (Nov 1885 to Mar 1886), about 5 minutes, to see the dark side.
- Total active testing was about 32 minutes (19:23 to 19:53 UTC).
- Tooling: only the provided `drive.py`, plus one PIL crop. Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r6/cold-cozy-player/`.

## Run log

| Seed | Months | What I tried | Result |
|---|---|---|---|
| 2206 (fresh) | 1–6 (Jan–Jun 1885) | Kept the harmless starter mix and added valerian and sugar. Named it "Mother Hen's Soothing Cordial" at 60c, steeped. Gave kind letter answers. Got a testimonial and bought ¼ col, then ½ col of ad space. | 19, 20, 26, 32 bottles a month. Cash crept from $150.00 to about $166 because the ads ate the gains. |
| 2206 | 7–9 (Jul–Sep) | Testified at Widow Byrne's inquest. Answered a price war by dropping to 65c. Paid the Jersey distiller $6. | 1 death (Widow Byrne), verdict "Died by the Visitation of God". Shelf bare two months running. Second still. |
| 2206 | 9 | **VENTURES:** took The Proprietor's Almanac ($40). | +$7.15 a month by post and about 10 callers a month, from the next edition. |
| 2206 | 10–12 | Lit both stills each month ("ORDER AND LIGHT BOTH"). Paid the sanitary inspector $3 and the landlord's extra dollar. **VENTURES:** took the Druggist's Standing Order ($100). | 69, 70, 76, 101 bottles. The standing order paid only $2.88 its first month because my shelf was always bare. |
| 2206 | 13–17 (Jan–May 1886) | Percolator arrived and I switched to percolation at 75c. Ignored a rival's rumour. | 116, then 96 a month (the cap of 2 stills × 48). Third still announced in the May 29 edition. Cash $337.77, 41 regulars, suspicion 3/100. |
| 3131 (`--play 10 --dose 1.5`) | 11–14 | Inherited a laudanum "Panacea" at suspicion 52. Kept my head down at an inquest, then took the laudanum out. | "FOUND DEAD IN LUDLOW STREET", then "THE CORONER NAMES A REMEDY". Suspicion rose 52, 54, 78 (after the silent inquest), then eased to 72 and 66 once the formula was harmless. |

**Key moments:**
- **Jan 1885.** The first edition is gorgeous. THE MONTH'S BOOKS ledger opens from "The proprietor's books close the month at $150.00." The hint line under THE WORKS told me exactly what was missing: "Nothing on the shelf and nothing on the still: the shop will sell nothing this month."
- **Feb.** Mrs Esposito's grateful letter became a printed testimonial, which unlocked ad space. It is a lovely, legible goal.
- **Mar.** Her testimonial was printed with her name misspelled "Espsoito", right after she asked for it to be spelled correctly. That made me laugh out loud.
- **Jul.** "DEATH OF WIDOW BYRNE", my first regular, on a harmless honest mixture (assay Danger: harmless, Habit: none). The memoriam clipping was touching. It upset me a little that I could never tell whether it was my fault.
- **Sep.** "Callers ... sent away empty-handed ... and one, being told there was none, wept." This was the first time I felt the business was alive. The VENTURES drawer offered the Almanac.
- **Oct.** "A RECORD MONTH ON MULBERRY STREET ... THE BOOKS WELL UP; THE PROPRIETOR ORDERS A NEW HAT", directly under "the books down $21.91". Confusing.
- **Jun 1886 (the May 29 edition).** NOTICES: "A third still has gone into the shop on Mulberry street." This is a nice, diegetic upgrade announcement.
- **Game 2.** Death's skeleton stands at the counter among my customers (`g2-ed3.png`). A "SEND IT ANYWAY" confirm appears when an inquest is unanswered. A young mother is "found at the foot of a stair ... A bottle, green and empty, was in the pocket." This is where the game stopped being cosy for me, and I think that is intended.

## Metrics

**Cash at each edition, game 1:**

| Edition month | Cash |
|---|---|
| Jan | $150.00 |
| Feb | $150.39 |
| Jul | $188.27 |
| Aug | $203.73 |
| Sep | $211.05 |
| Oct | $189.14 (after the $40 venture) |
| Nov | $207.88 |
| Dec | $234.40 |
| Jan 86 | $161.49 (after the $100 venture) |
| Feb | $198.33 |
| Mar | $225.65 |
| Apr | $261.89 |
| May | $299.49 |
| Jun | $337.77 |

**Bottles sold each month:** 19, 20, 26, 32, n/r (the June edition; I didn't note it), 31, 37, 48, 69, 70, 76, 101, 116, 96, 96, 96. The shelf was bare at month's end 7 times in 12.

**Regulars:** 12 → 16 (Apr) → 18 (Jul) → 23 (Oct) → 28 (Dec) → 36 (Feb 86) → 41 (Jun 86).

**Deaths and inquests:**
- Game 1: 1 death, 1 inquest. Suspicion peaked at 4/100.
- Game 2: 2 deaths in 3 months, 1 inquest that named my remedy, and suspicion peaked at 78/100.

**Ventures:**
- The drawer was first opened in Jan; nothing could be had yet, but each entry showed conditions and "So far" progress.
- The first venture I could afford and was offered was the Almanac in month 9, about 14 minutes into play. It paid back in about 5 months and felt worth it.
- The second, the Druggist's Standing Order, came in month 12 (about 20 minutes in). It was not worth it while I was still-capped: "the druggists out of town ordered twenty-one bottles and had eight, paying $2.88."

**Clicks for a routine month (from month 10 on):** about 6. TO THE PROOF, open a letter and click a reply, THE STILL, ORDER AND LIGHT BOTH, close, SEND TO PRESS. Plus about 3 scrolls to read the paper.

**Time to understand:**
- The core loop (brew, still, send) took about 5 minutes, and clicked on the first send.
- Ads took about 15 minutes, until the testimonial.
- The still cap took about 18 minutes: I eventually read the batch grid as 48 slots, the most one still can brew.

**Errors in `/tmp/panacea-driver-47022.log`:** only ALSA and audio-open errors (`ERR_CANT_OPEN`, no sound device). No exceptions in either game.

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Minor | The lead story says "THE BOOKS WELL UP; THE PROPRIETOR ORDERS A NEW HAT" in the same edition whose MONTH IN BRIEF says "the books down $21.91". In Jan 86 it says the same over "the books down $72.91". Both drops came from my venture outlays. | Edition lead story vs. THE MONTH IN BRIEF (`75.png`, `76.png`, `88.png`) | Seed 2206: take a venture (Almanac $40, or Standing Order $100) in a month that sells a record, then read the next edition. | Write the lead from trading profit and leave out outlays, or say "the books up, less the proprietor's new venture". |
| 2 | Minor | The Druggist's Standing Order is offered and sold to me while my shelf is bare every month. It paid $2.88 in its first month ("had eight ... The rest went short"). The fine print warns about this, but the game kept the offer open when it could not pay. | VENTURES drawer, Dec 1885; THE TRADE, Mar 86 edition (`94.png`) | Seed 2206: sell out every month with 2 stills, then take the Standing Order. | Show "your shelf has been bare 3 of the last 4 months" next to the offer, or let the still drawer reserve bottles for the druggist. |
| 3 | Minor | Hover tooltips from the counter name cards leak over open drawers. "Mrs. Filomena Esposito: for a teething infant..." showed over the Prospectus (`72.png`), and "Mr. Lars Halvorsen: rheumatism..." showed over the still's buttons, half-hiding "ORDER AND LIGHT BOTH" (`82.png`). | Proof screen, any drawer open | Open THE STILL, then move the pointer over the lower part of the drawer, which sits on top of the counter cards. | Block hover on the counter while a drawer is open. |
| 4 | Minor | Output caps at 48 bottles per still. From month 7 to month 17, six of the leads were "THE SHELF BARE" or "STILL NO BOTTLES TO BE HAD". The only thing I could do was wait for a still, and nothing on screen told me when the next one would come. TRADE TALK hints ("very near to a third copper, which the coppersmith keeps for a house that has sold a thousand bottles") were the only clue. | THE STILL drawer; edition leads | Seed 2206: play honestly to month 7 or later. | Put the next-still condition in the still drawer ("Still No. 3: after 1,000 bottles sold, 870 so far"), as the ventures already do. |
| 5 | Minor | After pressing "PAY $40.00 DOWN? PRESS AGAIN", the drawer's content jumped down about 60 px. A strip of the proof showed through at the top, and the confirmation line ("Taken up: ...") sits in a box overlapping the proof. | VENTURES drawer (`72.png`, `87.png`) | Take any venture. | Keep the confirmation inside the drawer frame and keep the scroll position. |
| 6 | Minor | "POUR IT INTO THE BOTTLE" read to me as "bottle a batch". It actually re-formulates the existing remedy (its tooltip says "Change what goes in the bottle. The label stays the same"). The tooltip only shows on hover, and I found it on month 14. | FORMULA drawer, assay column | Open FORMULA. | Rename it to "CHANGE THE RECIPE", or show the tooltip text as a caption. |
| 7 | Low | SIMPLE MIXTURE, STEEPING and PERCOLATION have no explanation. PERCOLATION appeared one month without a notice I could see in game 1. In game 2 it was announced in NOTICES. | FORMULA drawer | Open FORMULA in month 1, and again after the percolator. | Add a one-line caption or tooltip for each method (cost, yield, effect). |
| 8 | Low | The LIGHT THE FIRE button moves after "ORDER ENOUGH FOR 43" is pressed, so my second click hit empty paper. | THE STILL, Jan 1885 | Mark bottles, order, then click where LIGHT THE FIRE was. | Keep the buttons in fixed positions. "ORDER AND LIGHT BOTH" later solved this. |
| 9 | Low | Widow Byrne died on a harmless mixture, and I never learned whether my bottle was involved. The memoriam was ambiguous and the verdict was "Visitation of God". It made me feel guilty without teaching me anything. | Jul 1885 edition, memoriam clipping (`57.png`) | Seed 2206, honest play to month 7. | One line in THE MONTH'S BOOKS ("the jury found nothing in your bottle") would reassure a gentle player and still keep the dread. |
| 10 | Low | Early ads cost more than they earned for a small shop. ½ col at $6.00 left May's books up only about $3.54, and nothing on the proof says whether the ad paid. | TYPE CASE; ledger | Seed 2206: buy ½ col in months 4–5. | Add a line in the books: "Callers by the Gazette: 13 (about $X)". The callers line exists, but without money attached. |

## What works

- **The newspaper as the results screen.** Reading "THE SHELF BARE ON MULBERRY STREET" or "NEW FACES AT THE COUNTER" is far nicer than a stats table, and THE MONTH IN BRIEF strip gives the numbers in one line.
- **The hint lines under THE WORKS** ("Nothing on the shelf and nothing on the still...", "one letter unanswered: the usual reply will go out for you") are a gentle and effective tutor.
- **Gentle, funny letters with costs stated plainly** ("Free, and Feeney takes nothing, but suspicion +4 and standing -2"). The coroner's pencilled odds in the margin ("about 9 in 10 for naming") are brilliant.
- **The humour.** "Espsoito". The Personals' "on the recommendation of a neighbor, who has not been seen about lately". The weather box ("Cold rain. Pine coffins advance.", "Snow. Goodwill toward men, and toward the trade."). "Must be small now." It lands as wry rather than cruel, at least in the honest game.
- **The ventures prospectus** with "So far: 247 of 400 bottles sold" progress lines. The two-press confirm is the right amount of friction.
- **"ORDER AND LIGHT BOTH · 48 EACH"** turned a fiddly chore into one click: exactly the kind of quality of life a cosy player needs.
- **"SEND IT ANYWAY" when an inquest is unanswered** is a good safety net.
- **The art.** The woodcut counter crowd with their green bottles, Death in a top hat standing in the queue, and the Hobart & Co. price-current order blank.
- **Redemption is possible.** Taking the laudanum out let suspicion ease (78, 72, 66), and the shelf still sold out.

## Disclosures

- Game 2 used the jump-ahead `--play 10 --dose 1.5` on seed 3131.
- I used `state` to note exact cash, regulars and suspicion numbers for the metrics, and `targets` to find button rects, such as the moved LIGHT THE FIRE button and the venture TAKE IT UP button after scrolling.
- I used `read` once to quote the ventures text in Jan.
- I did not read the source, docs or other playtests.
- Early screenshots (01–57) dropped out of my context partway through the session. The notes for those months were written live, as I saw each screen.

## First hour

- **0:00.** First edition. I loved the look straight away. I wasn't sure what to click; only THE TRADE line was clickable, and it opened The Month's Books.
- **0:02.** On the proof screen it is a lot at once: TYPE CASE, LETTERS, WORKS, the counter. The bottom hint told me the shop would sell nothing, which pointed me at the still and the formula.
- **0:03.** First letter, "Something Stronger", from an addict. This was the first sign of the game's darkness. I chose "Recommend rest and fresh air."
- **0:05.** In the formula book, the assay bars (Kick, Calm, Habit, Danger) instantly told me what "good" meant. I kept it harmless and named it Mother Hen's Soothing Cordial.
- **0:07.** The still: marked 43 bottles, ordered, missed the moved fire button, lit it. Sent to press. **It clicked here**, when the paper came back with my 19 bottles in THE TRADE.
- **0:10.** The testimonial unlocked ads. I understood that bolder claims sell more and are riskier.
- **0:15.** A death on a harmless bottle. I felt bad and a little confused.
- **0:17.** Selling out, a second still, a price war. It now felt like a proper little shop sim.
- **0:19.** First venture (Almanac). Worth it.
- **0:24.** Second venture (Standing Order). Not worth it while capped. By now the monthly routine was 6 clicks and pleasantly calm, but the headlines repeated.
- **0:27.** Third still announced. I stopped game 1 at about 0:27 wanting to see what 3 stills would do: a good sign.

## Score

**7/10.** It is a beautifully made, genuinely funny printed-matter shop sim that is kind to an honest player. It loses points because its middle months stall on a hidden still cap and its paper sometimes contradicts its own books. To move it up a point, show the next still's condition in the still drawer the way ventures show theirs, and stop the lead story from contradicting THE MONTH IN BRIEF.
