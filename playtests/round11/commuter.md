# Panacea · round 11 · playtest by "The Busy Commuter"

A first-time player who plays in ten-minute bursts, forgets the game for days, and needs it to say "here is where you were and here is what to do next" every time it opens.

**Verdict:** Panacea is very good at picking up where I left off. Every one of my eight relaunches came back to exactly the shop I left, and THE MONTH SO FAR and NOTES FOR THE PROPRIETOR told me what I'd already done and what to do next without my having to remember anything. The one place I lost the thread was the narcotic. After I took laudanum out of the recipe, cravings went _up_ for a month and four regulars left. The only full explanation of how craving works is a tooltip that runs off both edges of the screen.

**Setup:**

- Build: the frozen copy at `build-r11`, commit `64dc5d8`.
- Port 47134, seed `commute-1`, always launched with `--player --profile commuter`. No jump-ahead at all.
- Game 1: Jan–Nov 1885, 10 sends. Game 2 was started with A NEW SHOP: Jan–Jun 1885, 5 sends.
- Eight separate launches. About 30 minutes wall time (23:06–23:36 UTC), plus reading.
- Tooling: `d.sh`, a wrapper that sends driver actions and takes a screenshot, and `rd.py`, which sorts the driver's `read` dump so I could quote the paper exactly. Both are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r11/commuter/`, with every screenshot.
- Before my first launch a 122-byte `panacea-commuter.save` and a `settings-commuter.cfg` (TEXT SIZE: LARGER) were already on disk from an earlier run with my profile name. I started fresh with A NEW SHOP and played at LARGER until month 8, then switched to REGULAR.

## Run log

| #   | Launch / game | Months         | What I tried                                                                                                                                          | How I stopped                                                      | What CONTINUE gave back                                                                                                                                                                                    |
| --- | ------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Game 1        | Jan → Feb 1885 | First edition, a clipping, the default formula entered at 60c, SOOTHES THE NERVES, 24 brewed. In Feb: price cut to 50c, 24 more brewed.               | Menu → SAVE, AND QUIT, on the Feb proof                            | Feb proof, $150.36, 50c, 24 brewing, 13 regulars: exact                                                                                                                                                    |
| 2   | Game 1        | Feb → Mar      | ½ oz laudanum added with CHANGE THIS RECIPE, price raised to 55c, 24 brewed                                                                           | `drive_stop.sh` mid-month, right after the recipe and price change | Mar proof. MONTH SO FAR listed "Laid out so far: $4.92 … The recipe … changed. Price … 50c., now 55c. Stock ordered … $4.20. A fire lit: 24 bottles…": exact (s3_01)                                       |
| 3   | Game 1        | Mar → May      | Read the regulars, brewed 36, answered the gilt-sign letter (ordered the $12 sign)                                                                    | Stopped after answering, before sending                            | May proof: "THE POST IS ANSWERED", MONTH SO FAR "Laid out so far: $12.00 … One letter answered", $145.01: exact (s4_01)                                                                                    |
| 4   | Game 1        | May → Jun      | Testimonial printed, the diploma letter burned, laudanum taken out (recipe changed), Gentian poured into the glass but not entered                    | Stopped with the FORMULA drawer open                               | Jun proof, drawer closed. Recipe without laudanum kept. The Gentian I hadn't entered was silently gone. Good name 14 kept (s5_01)                                                                          |
| 5   | Game 1        | Jun → Oct      | Break-in letter, pedestal box, ¼ col ad, Mrs Kehoe's letter, parish raffle. Also tested SAVE, AND THE TITLE PAGE → CONTINUE mid-August, and TEXT SIZE | Menu → SAVE, AND QUIT, on the Oct _paper_                          | Oct paper, $176.70, 2 letters waiting: exact (s6_01)                                                                                                                                                       |
| 6   | Game 1        | Oct → Nov      | The detective's follow-up letter, the jury summons, both stills fired                                                                                 | A NEW SHOP (pressed twice) → `drive_stop.sh` 2 s later             | Title page said "January 1885 · $150.00 · 12 regulars". CONTINUE opened the new shop's first edition, with different regulars (Dora Lipman, George Pratt…). The old shop was gone, as the warning had said |
| 7   | Game 2        | Jan → Jun      | 1.5 oz laudanum, "fatal in a large dose", for two batches, then taken out in March. Inquest. Watched the cravings                                     | `drive_stop.sh` on the June edition with 2 letters unanswered      | June edition, same state                                                                                                                                                                                   |
| 8   | Game 2        | –              | Relaunched only, to check the stop above                                                                                                              | Final `drive_stop.sh`                                              | –                                                                                                                                                                                                          |

**Timeline of key moments**

- Game 1, March edition: "THE MERRIAM INFANT / A Small Coffin". Tooltip-style notes on the proof said "The remedy is harmless by the assay; the bottle is not blamed." I trusted the honest bottle after that.
- Game 1, June: "1 CRAVING" appeared, one month after the laudanum batch. July: 4 CRAVING, and four regulars "ASKS BY NAME". August: none.
- Game 1, July: the Gazette sells space, because I had $100 and a printed testimonial. The ¼ col ad brought "thirteen sent by the notice in the Gazette" in each of the next three months.
- Game 1, October: the second still arrived after 200 bottles, announced in NOTICES. I never took up a VENTURE. THE SHOP NEXT DOOR needs 25 regulars (I peaked at 21), and A LARGER COPPER needs $200 (I peaked at $178).
- Game 2, February: two deaths from the 1.5 oz bottle ("Found insensible after a dose: the register blames the bottle"). March: inquest on Mr. Hogan. I said nothing and was cleared: "perfectly harmless, in moderation."

## Metrics

**Cash and regulars at each edition**

| Game 1   | Jan    | Feb    | Mar    | Apr    | May    | Jun    | Jul    | Aug    | Sep    | Oct    | Nov    |
| -------- | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ |
| Cash     | 150.00 | 153.48 | 153.86 | 158.59 | 157.01 | 150.26 | 161.01 | 165.13 | 170.49 | 176.70 | 178.41 |
| Regulars | 12     | 13     | 13     | 15     | 15     | 13     | 12     | 16     | 17     | 19     | 21     |

Game 2 cash, Jan–Jun: 150.00 → 148.68 → 150.96 → 162.12 → 173.40 → 179.68.

**Deaths and inquests**

- Game 1: one death (an infant, not blamed), no inquests.
- Game 2: three deaths (two in Feb, one in Mar) and one inquest, cleared.

**How long the regulars took to recover from the narcotic**

| Shop   | Narcotic in the recipe                                     | Craving count, month by month             | Months to clear        |
| ------ | ---------------------------------------------------------- | ----------------------------------------- | ---------------------- |
| Game 1 | ½ oz laudanum, brewed in March, taken out in June          | Jun 1 → Jul 4 → Aug 0                     | 2 months after removal |
| Game 2 | 1.5 oz laudanum, brewed in Jan and Feb, taken out in March | Feb 0 → Mar 2 → **Apr 7** → May 2 → Jun 0 | 3 months after removal |

- Craving rose for a month after I took the drug out, because the old bottles on the shelf sold first.
- In game 2, four regulars left in April (Lipman, Schultz, Rubin, Pratt). One line of the paper hints at a reason ("Mr. George Pratt … will not take another spoonful of anything from Mulberry street"). Nothing ties the departures to the recipe change.

**Restores:** 8 of 8 relaunches came back exactly. The only loss was an ingredient poured into the glass and never entered in the book.

**Effort**

- Clicks for one routine month (paper → proof → still → order → light → send): about 6.
- Starting a new shop takes two presses, and the second press shows the shop you are about to lose.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                         | Where                                                               | How to reproduce                                                                                                | Suggested fix                                                                                                                                                                          |
| --- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | The tooltip on the "7 CRAVING" tag is a single line 2,691 px wide. It starts at x=0 and is cut off at the right: "…Craving: they ask for the remedy by name and take". The part cut off holds the only full statement of the habit rule: "A bottle whose habit the assay calls 'slight' never does it; a stronger habit does, a little at a time." | THE WORKS, the craving tag at about (992,451), TEXT SIZE: REGULAR   | Game 2, April 1885: hover "7 CRAVING". Screenshot s7_16_craving_tip.png                                         | Wrap the tooltip at about 600 px, like the regulars' tooltips.                                                                                                                         |
| 2   | Minor    | After I removed laudanum, craving climbed from 2 to 7 and four regulars left. Nothing on the proof or in the paper said why: that the old strong bottles still on the shelf were selling first, or that people craving it were leaving the milder bottle. The formula's tooltip, "stock already brewed is unaffected", is the only clue.           | NOTES FOR THE PROPRIETOR; the April and May 1885 editions in game 2 | Brew a 1.5 oz laudanum batch, take the laudanum out next month with bottles still on the shelf, then send twice | When the shelf holds the old recipe, add a note such as "8 bottles of the old, stronger recipe still on the shelf". In PERSONAL, give a reason when someone who was craving it leaves. |
| 3   | Minor    | The same edition names two different Nora Sullivans: "Mrs. Nora Sullivan, seamstress" as a new customer, and "Nora Sullivan, a candy store keeper" in POLICE COURT.                                                                                                                                                                                | Game 2 edition of April 25, 1885 (p7_5.txt)                         | Game 2, the May proof's LAST EDITION                                                                            | Leave regulars' names out of the filler pool, or check for clashes within an edition.                                                                                                  |
| 4   | Minor    | THE STILL's batch goes back to 24 every month, even after I picked 36 or 48. A player who forgets has to re-pick every session, and the paper then reports "SOLD OUT A SECOND MONTH".                                                                                                                                                              | THE STILL drawer (s5_22, s6_05)                                     | Mark 48 one month; open THE STILL the next month                                                                | Remember the last batch size, or default to last month's sales.                                                                                                                        |
| 5   | Minor    | A different TEXT SIZE gives a different proof. At LARGER: 3 counter cards, no YOUR SPACE preview and no rival-ad boxes, and the drug shelf has 2 columns. At REGULAR: 5 cards, the ad preview, and 3 columns. My remembered click positions on the shelf were wrong after the switch: I poured Oil of Peppermint instead of laudanum.              | Proof and FORMULA drawer (s5_19 vs s4_04, s7_02)                    | SETTINGS → TEXT SIZE, then open FORMULA                                                                         | Keep the jar order and the panels the same at every size, and only reflow them.                                                                                                        |
| 6   | Low      | Ingredients poured into the glass but not entered in the book vanish on a stop, with no warning.                                                                                                                                                                                                                                                   | FORMULA drawer                                                      | Pour Gentian, then `drive_stop` and CONTINUE                                                                    | Fine as it is, or save the glass, or warn "the glass is not entered" when closing the drawer.                                                                                          |
| 7   | Low      | Some text repeats: the FIRE "careless smoker" item came up three times in ten months (new address each time); THE SPRING CLEANING item and the Grateful Letter were word for word the same in both games. The rival ads repeat, but each time with a fresh second punchline, which is good.                                                        | City items, BRIEFLY, the post                                       | Play 10 months, then start a new shop                                                                           | Rotate the filler items. Give the Grateful Letter a few bodies to choose from.                                                                                                         |
| 8   | Low      | THE MONTH SO FAR read "· The claims on Dr. ——'s Celebrated Panacea reset." after I _set_ a claim, which made me think I had cleared it.                                                                                                                                                                                                            | MONTH SO FAR, January 1885 in game 1                                | Enter a formula, then click SOOTHES THE NERVES                                                                  | Say "Claims set: Soothes the Nerves."                                                                                                                                                  |
| 9   | Low      | At exit the log shows "12 ObjectDB instances were leaked" and "6 RIDs of type CanvasItem were leaked". There were no script errors.                                                                                                                                                                                                                | `/tmp/panacea-driver-47134.log`                                     | Any `drive_stop`                                                                                                | Free the nodes on quit.                                                                                                                                                                |

## What works

- **Coming back.** All eight relaunches were exact. They covered:
  - mid-month stops after changing the recipe, the price and the still;
  - a stop after answering letters but before sending;
  - a stop with a drawer open;
  - menu saves, on both the proof and the paper.
- **CONTINUE always tells you where you are.** It returns you to the screen you saved on, and the title page shows "August 1885 · $158.89 · 16 regulars" under CONTINUE, so you know which shop you are resuming.
- **THE MONTH SO FAR** is exactly what a commuter needs. It is a running list of what I did this month: "Laid out so far…", "One letter answered", "A fire lit: 24 bottles…".
- **NOTES FOR THE PROPRIETOR** gives the next step in plain words: "Low stock: twenty bottles on the shelf and nothing on the fire… Light THE STILL", "One letter unanswered: the usual reply will go out for you". I never had to remember what to do.
- **A NEW SHOP is safe.** The second press reads "A NEW SHOP? PRESS AGAIN" and shows "The shop on the books will be lost. November 1885 · $178.41 · 21 regulars".
- **Correspondents remember you.**
  - Detective Lynch came back in October: "Your till was found in Hester street, as I said it might be… your name is in it against the consideration."
  - Advising Mrs Kehoe brought "one on the word of a careful mother".
  - The parish raffle brought "three sent from the pulpit".
  - Mrs Donnelly's testimonial was printed and misspelled ("Donenlly"), which is a good joke.
- **The paper is dated well.** It covered Chinese New Year, St. Patrick's Day, Decoration Day, the Fourth, Grant's death and funeral, and the pedestal fund closing in August 1885. I found nothing that contradicted my books. THE TRADE reconciles the takings to the cent.
- **Letters show their price.** Each reply states its cost up front ("$12, and about two more callers a month, for good"). The inquest letter shows the odds ("about 9 in 10 for naming").
- **Both honest and dishonest shops are real choices.** The honest shop grew slowly, $150 to $178 in ten months with no inquests. The 1.5 oz laudanum shop sold well but killed three people in two months.

## Disclosures

- No jump-ahead.
- I used `state` dumps to confirm that restores were exact, and `read` dumps (through `rd.py`) to quote the paper and tooltips, including the full text of the clipped tooltip in finding 1.
- I relied on the screenshots for layout and judgements.
- Earlier files for my profile name already existed (see Setup).
- I did not read the source, `docs/` or `playtests/`.

## First hour

- **0:00** The title page carries the content note. I pressed A NEW SHOP and liked that it asked "press again". The first edition, "A NEW CONCERN … He Promises Much", lists four callers in PERSONAL; I clicked one open as a clipping.
- **0:02** On the proof I wasn't sure where to start. FORMULA already held a filled glass (water, spirit, syrup, peppermint), and I understood "ENTER IT IN THE BOOK" to mean "this is your product". THE STILL said I had no stock; ORDER FOR 24, then LIGHT THE FIRE, made sense straight away.
- **0:04** I set SOOTHES THE NERVES. The type case says "CLAIMS, UP TO FOUR: THE CORONER READS THEM", which told me the claims carry a risk. I sent to press.
- **0:06** February: 16 bottles sold, books up. It clicked here: **the paper is the result screen and the proof is the turn.**
- **0:08** I cut the price to 50c, then quit from the menu. CONTINUE brought me back exactly.
- **0:10** March brought the infant death. I was relieved to read "the bottle is not blamed". I added ½ oz laudanum and saw the assay change ("Habit: most will come back", "A chemist might find it").
- **0:15** The first letter (the gilt sign) arrived and I got how letters work: each answer costs something, with its effect stated.
- **0:20** June: "1 CRAVING". I took the laudanum out. The Gazette started selling me space once I had $100 and a printed testimonial; NOTES announced it.
- **0:25** VENTURES drawer: I understood the gating ("So far: 12 of 25 regulars") but could not reach any venture. The second still came automatically at 200 bottles.

## Score

**7 / 10.** For someone who plays in short bursts it is unusually forgiving: exact restores, a running record of the month, and a plain next step. Fix the craving tooltip and add a note saying the shelf still holds the old recipe, and the narcotic arc would make as much sense as the rest of the game. That is the next point.
