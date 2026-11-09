# Panacea · round 8 · playtest by "Tycoon fan"

I've played every business sim from Railroad Tycoon to Big Pharma and Offworld Trading Company. I want a growth curve I can steer, trade-offs I can read, and numbers that add up. I'm new to Panacea.

**Verdict:** The money side of Panacea holds up. Every month's books reconcile to the cent, the ventures prospectus says exactly what each improvement costs and how close you are to the next one, and almost every letter reply prints its price and its effect before you choose. I steered the shop from $150 and 12 regulars to $716 and 68 regulars, with three stills and a branch, in 26 months. Two things hurt the tycoon loop. The capacity upgrade I paid $200 for isn't used by the one-click brew button. And the death and suspicion side moves from "nothing" to "the Board of Health is asking" in a single month, with no number on the proof that warned me it was coming.

**Setup:**

- Build: frozen copy at `scratchpad/build-r8`, commit `80e045d`. Port 47083. Seed `tycoon-8`, run with `--player --profile tycoon`.
- No jump-ahead. One game, all of it played by hand from the first edition.
- Played 26 months (January 1885 to February 1887; March 1887 was on the proof when I stopped). Active play ran about 43 minutes, 19:22 to 20:05 UTC.
- I saved and quit through the menu twice and came back with CONTINUE both times: once with SAVE, AND QUIT followed by a full relaunch, once with SAVE, AND THE TITLE PAGE.
- Tooling: small shell wrappers in my scratch dir (`d.sh` for driver calls and screenshots, `press.sh` to send to press and shoot the paper, books, trade column and proof, `brew.sh` to find and click the "ALL/BOTH STILLS · ORDER AND LIGHT" button). I also kept a month-by-month ledger in `log.txt` in the same folder. All of it is in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r8/tycoon/`.

## Run log

| Seed     | Months | What I tried                                                                                                                                                                               | Result                                                                                    |
| -------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| tycoon-8 | 1–2    | Base syrup plus ½ oz laudanum at 60c. Claims SOOTHES THE NERVES and BANISHES RHEUMATISM. Brewed one still of 48. Printed a testimonial. Added CURES CONSUMPTION !!! in month 2.            | 14, then 21 bottles. Cash $150 → $155. 15 regulars.                                       |
| tycoon-8 | 3–6    | Took the Almanac venture ($40) and a ½ column ad ($6). Raised the price 60 → 70 → 80 → 85c. Paid the Board of Pharmacy and the police captain.                                             | Sold out three months running (57, 48, 48). Cash $176. 23 regulars, capped at "23 of 25". |
| tycoon-8 | 7–12   | Second still unlocked at 200 bottles sold. Paid $50 for the diploma, which unlocked ENDORSED BY EMINENT PHYSICIANS. Took the shop next door ($5/mo). Third still unlocked at 600 bottles.  | Monthly sales 66 → 108. Cash $348. 50 regulars. Suspicion 15.                             |
| tycoon-8 | 13–17  | Price 95c. Renewed the Almanac. Took the engraving, the physician's retainer and the gentleman's morphia parcel (+$25).                                                                    | 120–138 bottles, about +$70–78 a month. Cash $663. Regulars flat at 50 (capped).          |
| tycoon-8 | 18–22  | BRANCH IN ANOTHER WARD ($600). Funded the medicine show ($15). First deaths and first inquests (testified, cleared). Put the Alderman's picture in the window.                             | 149 → 173 bottles, +$90–131 a month. Cash $596. 69 regulars.                              |
| tycoon-8 | 23–26  | LARGER COPPER ($200). 72 bottles "poured away". Four deaths in one month. Three inquests (paid $25, testified and was NAMED, paid $30). Removed the laudanum and struck CURES CONSUMPTION. | Suspicion 33 → 55 → 77 → 67 ("alarming"). Cash $717. 68 regulars.                         |

**Key moments**

- **Month 1:** the formula book showed per-bottle cost ("Costs 21c. a bottle to make") and the still showed the order cost before I committed. That was a good start for a tycoon.
- **Month 3:** the Almanac was the first venture I could take, two months in. The callers line in THE TRADE immediately credited it: "ten by the almanac".
- **Month 4:** SAVE, AND QUIT, a relaunch, then CONTINUE. Everything was restored.
- **Month 6:** "THE SHOP NEXT DOOR … So far: 23 of 25 regulars." This is the clearest capacity signal I've seen in a narrative sim.
- **Months 7 and 12:** the second and third stills arrived on their own at 200 and 600 bottles sold.
- **Month 18:** I bought the branch, and the paper's banner read "149 bottles sold; the books down $505.19, $603.00 of it laid out on the business". It separates capital spending from operating loss. I loved that.
- **Month 20:** the first death came, after 19 clean months on an unchanged recipe.
- **Month 25:** "A SICKLY SEASON. FOUR DEATHS IN THE WARD THIS MONTH". Suspicion opened month 26 at 55 ("alarming").
- **Month 26:** I testified at one inquest: "The jury sat at once and named your bottle. Suspicion is now 77 of 100." The lead story became "THE CORONER NAMES A REMEDY".

## Metrics

**First game, month by month** (cash at the close of each month's books; "regulars" as the footer showed it on the following proof):

| Mo  | Date           | Bottles sold     | Cash   | Regulars | Ventures held          | Deaths | Suspicion (approx.) |
| --- | -------------- | ---------------- | ------ | -------- | ---------------------- | ------ | ------------------- |
| 0   | Jan 1885 start | –                | 150.00 | 12       | –                      | –      | –                   |
| 1   | Jan 85         | 14               | 145.56 | 12       | –                      | 0      | low                 |
| 2   | Feb 85         | 21               | 155.16 | 15       | –                      | 0      | low                 |
| 3   | Mar 85         | 52               | 127.52 | 20       | Almanac                | 0      | ~3                  |
| 4   | Apr 85         | 57 (sold out)    | 148.33 | 23       | Almanac                | 0      | ~4                  |
| 5   | May 85         | 48 (sold out)    | 161.14 | 23       | Almanac                | 0      | ~4                  |
| 6   | Jun 85         | 48 (sold out)    | 176.35 | 23       | + 2nd still            | 0      | 0                   |
| 7   | Jul 85         | 66               | 151.02 | 26       |                        | 0      | ~4                  |
| 8   | Aug 85         | 73               | 174.64 | 32       | + Shop next door       | 0      | 6                   |
| 9   | Sep 85         | 97               | 219.66 | 41       |                        | 0      | 8                   |
| 10  | Oct 85         | 106              | 268.33 | 48       |                        | 0      | 10                  |
| 11  | Nov 85         | 107              | 313.35 | 47       |                        | 0      | 11                  |
| 12  | Dec 85         | 108              | 347.88 | 50       | + 3rd still            | 0      | 15                  |
| 13  | Jan 86         | 138              | 369.75 | 47       |                        | 0      | 16                  |
| 14  | Feb 86         | 123              | 439.83 | 49       |                        | 0      | 17                  |
| 15  | Mar 86         | 138              | 510.66 | 49       | Almanac renewed        | 0      | 21                  |
| 16  | Apr 86         | 134              | 588.72 | 50       |                        | 0      | 22                  |
| 17  | May 86         | 120              | 662.98 | 50       |                        | 0      | 24                  |
| 18  | Jun 86         | 149              | 157.79 | 59       | + Branch ($600)        | 0      | 24                  |
| 19  | Jul 86         | 153              | 258.40 | 69       |                        | 0      | 25                  |
| 20  | Aug 86         | 158              | 348.76 | 69       |                        | 1      | 32                  |
| 21  | Sep 86         | 156              | 480.22 | 68       |                        | 1      | 33                  |
| 22  | Oct 86         | 173 (sold out)   | 595.83 | 69       |                        | 0      | 26                  |
| 23  | Nov 86         | 161              | 503.74 | 68       | + Larger copper ($200) | 1      | 33                  |
| 24  | Dec 86         | 144 (shelf bare) | 581.53 | 67       |                        | 1      | 30–37               |
| 25  | Jan 87         | 179              | 678.97 | 65       |                        | 4      | 37 → 55             |
| 26  | Feb 87         | 149 (104 left)   | 716.68 | 68       |                        | 0      | 77 → 67             |

**Growth curve**

- Operating profit went from about −$5/month (month 1) to about +$75/month (months 13–17), then to +$90–131/month after the branch (months 19–22).
- The branch ($600 + $5/mo) took monthly takings from about $135 to $170. By my figures it pays back in roughly 15–20 months.
- The Almanac ($40 for 12 months) brought "ten by the almanac" callers plus 5–6 bottles by post every month. Payback was under two months. It was the best buy in the game.

**Do the numbers add up?** Yes, every time I checked.

- Month 26 ledger: opened $678.97, +$122.55 +$14.25 +$4.75 in sales, −$103.84 in costs, closed $716.68. Exact.
- Month 18: $662.98 + takings $141.55 − outgoings $646.74 = $157.79. Exact.

**Unit economics as the game shows them**

- Ingredients: 21c a bottle at the start, 18c after the Pearl Street discount, 12c with no laudanum.
- Bottles, corks and labels add 3c a bottle, charged when you brew. The brew button doesn't show this (see finding #4).
- At 95c a bottle the margin is about 74c.
- Raising the price from 60c to 95c cost only 2–4 "callers who asked the price and went away" a month. Price was the cheapest lever in the game.

**Clicks to brew a month:** 3 (THE STILL → ALL THREE STILLS · ORDER AND LIGHT → PUT IT DOWN). That is excellent for a monthly loop.

**Letters**

- I answered 33 letters, plus 6 inquest notices.
- Total letter spending was about $250. The biggest items were the $50 diploma, the $60 lease, the $55 in coroner fees and the $15 for the medicine show.

## Letters: were they worth the money and attention?

The reply notes nearly always told me the cost and the effect beforehand, often as a number: "$15 now. What comes back depends on the summer, and on the Professor."; "Suspicion −8, and you become better known."; "Pencilled in the margin: about 5 in 10 for naming." For a tycoon player this is the best part of the game. Each letter is a small priced decision.

| Letter (month)                                  | My reply                      | Cost               | What came back                                                                 | Could I tell beforehand?                                      |
| ----------------------------------------------- | ----------------------------- | ------------------ | ------------------------------------------------------------------------------ | ------------------------------------------------------------- |
| Testimonial (2)                                 | Print it                      | 0                  | Standing 10 → 13                                                               | Yes                                                           |
| Benefit society (5)                             | Subscribe                     | $1.50/mo           | "five by the society" callers in month 5                                       | Yes. Paid.                                                    |
| Captain of the precinct (6)                     | Pay                           | $5                 | Suspicion 4 → 0                                                                | Yes                                                           |
| Medical diploma (7)                             | Send $50                      | $50                | ENDORSED BY EMINENT PHYSICIANS appeared in the type case. Standing up about 6. | Partly. I didn't know it would unlock a claim.                |
| Liberty pedestal fund (7)                       | $1                            | $1                 | Nothing I could see                                                            | Yes (cheap goodwill). Right for 1885.                         |
| Price war, Feeney (11)                          | Handbills                     | $10                | Nothing I could trace in sales                                                 | No                                                            |
| Landlord's lease (13)                           | Buy it                        | $60                | No later rent hike seen                                                        | Hard to judge                                                 |
| Physician's retainer (14)                       | Agree                         | $1/mo              | "one on the doctor's prescriptions", later "two"                               | Yes. About break-even.                                        |
| Gentleman's morphia parcel (15)                 | Sell                          | +$25, suspicion +3 | As stated                                                                      | Yes. Paid.                                                    |
| Engraving in the History (16)                   | Subscribe                     | $8                 | "one by the engraving in the History" every month                              | Yes. Pays back in about 9 months.                             |
| Medicine show (20) → The Professor Returns (21) | Fund it, then "see the books" | $15 out, $29 back  | Net +$14, standing −1. **The chain remembered me.**                            | Yes. Both notes were exact.                                   |
| Alderman's picture (22) → Election Day (25)     | Picture; bottles and dollars  | 0, then $15        | Suspicion −8, then −6. **The chain remembered.**                               | Yes                                                           |
| Coroner (21, 24)                                | Testify                       | 0                  | Cleared both times (5 in 10 odds)                                              | Yes                                                           |
| Coroner (26)                                    | Pay $25, testify, pay $30     | $55                | Testifying got my bottle NAMED: suspicion 55 → 77                              | Yes. The 6 in 10 odds were printed. I gambled and lost. Fair. |
| Temperance League (25, 26)                      | PURELY VEGETABLE; $5          | $5                 | Suspicion −5                                                                   | Yes                                                           |

- **Didn't pay, or I couldn't tell:** the Feeney price-war handbills, the rival-rumour reply, the lease and the pedestal.
- **Paid clearly:** the Almanac (a venture, but the same idea), the engraving, the morphia parcel, the medicine show and the alderman.
- The margin note "with the shop suspected as it is, the ward credits none of the standing a reply says" was good systemic honesty.
- **Repeats:** I saw no letter twice. Two correspondents (the Temperance League and Feeney) wrote twice, with different letters.

**Wrong for its year or timing** (see finding #5):

- P. Haggerty signs off "Your customer these four years" in December 1886. The shop opened in January 1885.
- The Alderman's man asks for the picture "come November" (October 1886 letter), but "Re: Election Day … Election next Tuesday" arrives in late January 1887.
- The medicine-show letter is dated "Elmira, August 25th, 1886" and says "My company takes the road on the first of May … this summer's remedy could be yours". The return letter is dated September 14th: "The company is back in winter quarters … Your third of the summer's take."

## Save, quit and CONTINUE

- **First round trip (month 4, April 1885):** ☰ MENU → SAVE, AND QUIT. The process exited. I relaunched with the same arguments. The title page now offered "☞ CONTINUE" above A NEW SHOP (`53_title_continue.png`). CONTINUE brought back the April 1885 paper. On the proof, cash, regulars, the price, the claims, the ½ column ad and the 48 bottles brewing were all as I left them.
- **Second round trip (month 23, November 1886):** Escape → SAVE, AND THE TITLE PAGE. The title card read "☞ CONTINUE / November 1886 · $595.83 · 69 regulars" (`m23_titlepage.png`). CONTINUE brought back the same date, $595.83, 69 regulars, the one unanswered letter ("A Pen-Wiper") still waiting, and the same drawers and ventures.
- **Nothing was lost either time.**

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Where                                                                                  | How to reproduce                                                           | Suggested fix                                                                                                                                                                                                                          |
| --- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | I paid $200 for A LARGER COPPER ("seventy-two bottles a batch instead of forty-eight"). The following month the still still opened with 48 marked, and the one-click button still read "ALL THREE STILLS · ORDER AND LIGHT 48 EACH · $22.32". I brewed 144 instead of 216 in the month I bought it. Only after clicking the 72nd bottle by hand did the button change to "72 EACH · $33.48". Its header text did say "seventy-two bottles a batch at most".        | THE STILL drawer, months 23–24 (`m23_g_still.png`, `m24_still.png`, `m24_still72.png`) | Take A LARGER COPPER in VENTURES, then open THE STILL.                     | Default the marked batch to the new still size after an upgrade, or make the ALL STILLS button always fill to capacity. Say "72 each" in the button the month it's bought.                                                             |
| 2   | Major    | Deaths and suspicion weren't readable as numbers. The recipe was unchanged (½ oz laudanum per bottle) for 19 clean months. Then deaths came at 1, 1, 0, 1, 1, 4 a month, and suspicion jumped 33 → 55 in one press. The only warning was a footnote, "Death waits at the counter beside Mr. Michael Callahan.", which sat there for 8 months. As a tycoon I couldn't tell whether the cause was dose, customer age, the number of gravely ill regulars, or volume. | Proof footnotes; banner of 29 Jan 1887 "A SICKLY SEASON" (`m26_a_paper.png`)           | Play about 25 months on a ½ oz laudanum recipe with 3 stills and a branch. | Put a "gravely ill: N" count and a per-month expected-deaths or "risk" figure in THE WORKS. Or name in the books _why_ suspicion rose (the books already do this: "Deaths among customers +6"). Show it on the proof before you press. |
| 3   | Minor    | "AT THE LABORATORY.—72 bottles of Dr. ——'s Celebrated Panacea came out wrong this month and were poured away." I own no laboratory. The loss isn't itemised in the Month's Books, and the shelf ran bare ("callers went away empty-handed"). The proof gave no warning of bad-batch risk. A month later P. Haggerty wrote "Not the Same Bottle", which hints at the mechanic.                                                                                      | Gazette of 25 Dec 1886, NOTICES (`m25_a_paper.png`, `m25_b_books.png`)                 | Brew 216 a month with 3 stills on this seed in December 1886.              | Call it "AT THE BACK ROOM" (or similar) when no lab is owned. Add a books line, "−72 bottles poured away (cost $x)".                                                                                                                   |
| 4   | Minor    | The brew button's price leaves out bottles, corks and labels. "BOTH STILLS · ORDER AND LIGHT 48 EACH · $16.80", but cash fell by $19.68 (3c × 96 bottles = $2.88 extra).                                                                                                                                                                                                                                                                                           | THE STILL, month 7                                                                     | Note cash before and after pressing the ALL/BOTH STILLS button.            | Show the full price: "$16.80 + $2.88 glass".                                                                                                                                                                                           |
| 5   | Minor    | Letter dates and continuity are off: "Your customer these four years" (P. Haggerty, December 1886, when the shop is 23 months old); an Election Day letter in January after "come November"; a medicine-show offer for "the first of May" that arrives on August 25th, with the tour over by September 14th.                                                                                                                                                       | Morning's Post, months 20, 21, 24, 25                                                  | Play this seed to month 25 and answer as I did.                            | Gate those letters by month, or make "four years" depend on the shop's age.                                                                                                                                                            |
| 6   | Minor    | Price didn't push back. Going from 60c to 95c cost 2–4 walkaways a month while sales doubled. There was no reason to ever lower it.                                                                                                                                                                                                                                                                                                                                | THE TRADE, "Callers who asked the price and went away"                                 | Raise the price 5c a month from 60c.                                       | Steeper elasticity, or a rival that undercuts you. Feeney's price war was a letter, not a market force I could see.                                                                                                                    |
| 7   | Low      | `System.NullReferenceException … at Panacea.UI.FrontCard._Input … FrontDoor.cs:line 154`, twice. It fires when Escape closes the pause menu: `OnEscape` frees the card, then `GetViewport().SetInputAsHandled()` runs on a node no longer in the tree. Nothing visible happened in game.                                                                                                                                                                           | `/tmp/panacea-driver-47083.log`; pause menu                                            | Open the menu with Escape, then close it with Escape.                      | Call `GetViewport().SetInputAsHandled()` before `OnEscape?.Invoke()`, or null-check the viewport.                                                                                                                                      |
| 8   | Low      | There's no history view. To see my growth curve I had to keep my own ledger: each month's books replace the last one's.                                                                                                                                                                                                                                                                                                                                            | THE LEDGER popup                                                                       | Try to compare month 10 with month 20 in game.                             | Add a "Books, by the month" page in the ledger (cash, bottles, regulars, suspicion) as a woodcut-style table.                                                                                                                          |
| 9   | Low      | The second and third stills arrive free at sales milestones (200 and 600 bottles). It's pleasant, but it isn't a decision, and the VENTURES "So far: for a second still, 192 of 200 bottles sold; $156.14 of the $200.00 in hand" line made me think I had to pay $200.                                                                                                                                                                                            | VENTURES drawer, month 6                                                               | Read the A LARGER COPPER and A FOURTH STILL entries before month 7.        | Word the milestone stills as "comes with the trade at 200 bottles" or similar.                                                                                                                                                         |

## What works

- **The Month's Books ledger.** Every line, every month, sums exactly. The capital-spend wording in the banner ("the books down $505.19, $603.00 of it laid out on the business") is better than most real tycoon games.
- **THE TRADE column** breaks callers down by source ("nineteen from the ward, nineteen by the Gazette, twelve at the branch, ten by the almanac, one by the engraving in the History, two on the doctor's prescriptions"). This is how I knew which investments paid. Keep it.
- **The VENTURES prospectus.** Terms, monthly upkeep, expected effect ("It brings about twelve more callers a month") and a "So far" progress line toward each unlock. "So far: 23 of 25 regulars" told me exactly why growth had stalled.
- **Reply notes with numbers.** "$15 now", "Suspicion −8", "about 6 in 10 for naming". Letters are priced decisions, and two chains (Professor Marsh, the Alderman) came back months later and remembered what I did.
- **The two-step confirm on ventures** ("☞ PAY $600.00 DOWN? PRESS AGAIN") is the right weight for big purchases.
- **The three-click monthly brew** with ALL STILLS. Even at 26 months the loop never felt like a chore.
- **Save and continue** were exact, including a pending letter. The CONTINUE button shows the date, cash and regulars before you commit.
- **The satire lands as data.** "The silent partner keeps a second set of books. They are not shown." and "Coffins, pine, $4.60. Lilies, scarce." both appear in THE MARKETS line.

## Disclosures

- No jump-ahead.
- I used `state` a few times to confirm cash, unit cost and suspicion precisely, for example that removing the laudanum took the unit cost from 18c to 12c and that suspicion was 70.5. The same figures were on screen in THE WORKS. I used `targets` and `read` to find button coordinates and to quote the ventures text.
- I read source once, after play, to check the exception at `FrontDoor.cs:154`.
- My keyboard Shift+Enter (to pour ¼ oz) probably didn't register, so every dose I tested was in ½ oz steps.
- Once (month 26) a click on TO THE PROOF ☞ didn't take me to the proof. I clicked again and it did. I couldn't tell why, so I haven't listed it as a finding.

## First hour

- **19:22, title page.** PANACEA, "A business in patent medicines · New York, 1885–1906", with a content note that is clear and short. I chose A NEW SHOP.
- **19:23, first edition.** I read it as a newspaper about my shop. The "AMONG THE CALLERS" clippings (Mr. Friedrich Mueller, dyspepsia; Mrs. Gertrude Mueller, teething infant…) told me who the market was. I clicked one; it's flavour. The footer said "$150 · 12 regulars", so I understood regulars to be my customer base.
- **19:25, TO THE PROOF.** I understood that I was building next month's paper. Ad sizes were greyed out, so I couldn't advertise yet.
- **19:26, FORMULA.** Drug shelf, a glass and THE ASSAY (Kick, Calm, Habit, Danger, Taste, Lingers, "In an assay", "Costs 21c. a bottle to make"). It clicked at once: a product-design screen with a unit cost. I added ½ oz laudanum and entered it at 60c. I misread one thing: I thought a price change needed CHANGE THE RECIPE. It applies at once.
- **19:29, THE STILL.** It defaulted to 24, so I marked 48, then ORDER FOR 48 and LIGHT THE FIRE. I opened SUPPLIES (stock on hand) and VENTURES, where the whole prospectus with every unlock condition was visible from day one. That was my "this is a tycoon game" moment.
- **19:33, first press.** "Fourteen bottles sold; the books down $4.44." I opened the books, and the ledger made every cent visible.
- **19:36, months 2–3.** First letter (testimonial). The ½ column ad became available, and I took the Almanac. Sales jumped from 21 to 52.
- **19:41, month 4.** I raised the price, saved and quit, relaunched and continued. Everything was intact.
- **19:45, month 6.** "23 of 25 regulars" told me the shop was full. This is when the growth curve clicked: capacity, stills and callers by source.
- **19:50, month 12.** Three stills, 108 bottles a month, cash $348. The loop was fast and readable.
- **19:53, month 18.** Branch bought, cash down to $157, and the paper said why.
- **19:56, months 20–22.** First deaths and inquests. The game changed tone.
- **20:02, months 25–26.** Four deaths, then named at an inquest. I cut the laudanum and the boldest claim. 104 bottles were left on the shelf and regulars were "shaking".

## Score

**7 / 10.** As a business sim, the economy is honest and legible: the ledger reconciles, the callers are attributed by source, and the ventures are priced. Steering growth felt good.

**What would move it one point up:**

- Make paid capacity upgrades actually apply through the one-click brew.
- Put the death and suspicion risk on the proof as a number before you press, so the second act is something I can steer, not a cliff I discover.
- Add a month-by-month history page so the growth curve is something I can see, not something I keep in a notebook.
