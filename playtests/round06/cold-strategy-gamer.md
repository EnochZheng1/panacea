# Panacea · round 6 · playtest by "Strategy gamer"

I play tycoon, 4X and management games for their systems. I want to read the economy, find the levers that matter, and plan a long game.

**Verdict:** This is a real management game under the satire. The month's books, the "Callers at the counter: X from the ward, Y by the Gazette, Z by the almanac" breakdown and the warning lines in the Works let me run actual experiments and read the results. The economy has a hard capacity cap: from month 8 on I sold every bottle the stills could make. Once you see that, the answer is to cut the advertising and raise the price, and the interesting choices (ventures, ads) go flat until the next still arrives. The long game does arrive: my first venture came in month 7, the knock-through in month 9 and a third still in month 16. Two hidden or unannounced rules took away some of my ability to plan: a "standing" stat that letters change but that is never shown, and the shop next door being knocked through (rent up $5 a month) without my being asked.

**Setup:** build `3e627bf` (frozen copy `build-r6`), port 47021, seed 6021. One fresh game, no jump-ahead, played January 1885 to May 1886 (16 months sent to press, about 50 minutes of active play). Tooling: a small helper script, `scratchpad/r6/cold-strategy-gamer/brew.sh`, which opens the still, clicks "ORDER AND LIGHT BOTH", sends to press and takes screenshots. I used it only from month 11, once I had learned that flow by hand. The game did not restart during the session. Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r6/cold-strategy-gamer/`.

## Run log

| Seed | Months | What I tried | Result |
|---|---|---|---|
| 6021 | 1–3 (Jan–Mar 1885) | Starter recipe plus ½ oz laudanum, renamed "Mulberry Balm", price 60c then 65c. Set four claims. Printed the testimonial. | 11, 13 and 13 bottles sold. Books −$1.32, +$0.60, +$2.27. In month 2 I lost a whole batch to a misclick (finding 2). |
| 6021 | 4–6 | ½ col ad, then full col, then back to ½ col. Paid the sanitary inspector $3, refused the $50 diploma. | Sales of 42, 46 and 55. The full column ($11) earned less than the half column did (+$3.06 against +$13.38). |
| 6021 | 7–10 | Took up **The Proprietor's Almanac** ($40, month 7). A second still appeared. 96 bottles a month from both stills. Price 70c. Answered Feeney's price war with $10 of handbills. Joined St. Bridget's society ($1.50 a month). | Sales of 48, 74, 70 and 90. The shop next door was knocked through in month 9 without my choosing it. Shelf bare in most months. |
| 6021 | 11–14 | Front-page notice ($20). Paid the Jersey distiller $6. Took the laudanum out ("Death waits at the counter beside Mrs. Greta Kraus"). | Sales of 96, 102, 96 and 96, every month capped by the stills. Greta's infant recovered and she left. One "shaking for want of a dose" regular and a "Something Stronger" letter. Regulars held at 45. |
| 6021 | 15–16 | Ad down to ¼ col, price 80c then 90c. | Still sold out at 80c (+$47.98, my best month). At 90c, "Callers who asked the price and went away: fourteen" (+$52.18). Widow Becker died, aged 75, on the opiate-free formula. A third still arrived (about 1,000 bottles sold). |

**Key moments**
- Jan 1885: the first edition shows the books. The month-end line "books close the month at $150.00" opens the ledger, and the ledger is what I came for.
- Feb: I lost the month's batch because the LIGHT THE FIRE button moves after you order (finding 2).
- Apr: ads unlocked. At ½ col, walk-ins tripled.
- Jul (month 7): **first venture taken, the Almanac for $40.** The ventures drawer had been readable since month 1, with "So far: …" progress lines, which is excellent for planning.
- Sep (month 9): the shop next door was knocked through. "The landlord has put the rent up five dollars a month." I made no choice.
- Jan 1886: a clear warning ("Death waits at the counter beside Mrs. Greta Kraus", "the infant gravely ill"). I took the opiate out and the infant lived. This was the best trade-off in the game: I saved a customer's child and lost the customer.
- Apr 1886: a death on a formula the assay calls "harmless", with no cause given (finding 5).

## Metrics

**Cash and sales, by month (closing books)**

| Month | Close | Δ | Sold | Regulars | Ad | Price | Note |
|---|---|---|---|---|---|---|---|
| Jan 85 | $148.68 | −1.32 | 11 | 12 | – | 60c | |
| Feb | $149.28 | +0.60 | 13 | ~12 | – | 60c | still not lit (misclick) |
| Mar | $151.55 | +2.27 | 13 | 13 | – | 65c | testimonial |
| Apr | $164.93 | +13.38 | 42 | 17 | ½ | 65c | |
| May | $167.99 | +3.06 | 46 | 22 | full | 65c | full col did not pay |
| Jun | $184.90 | +16.91 | 55 | 23 | ½ | 65c | shelf bare |
| Jul | $157.26 | −27.64 | 48 | 25 | ½ | 65c | Almanac −$40; 2nd still |
| Aug | $180.38 | +23.12 | 74 | 30 | ½ | 70c | 2 stills |
| Sep | $210.54 | +30.16 | 70 | 29 | ½ | 70c | knock-through, rent $3 → $8 |
| Oct | $228.36 | +17.82 | 90 | 33 | ½ | 70c | handbills −$10 |
| Nov | $260.38 | +32.02 | 96 | 38 | ½ | 70c | capped |
| Dec | $282.60 | +22.22 | 102 | 44 | front | 70c | capped |
| Jan 86 | $297.98 | +15.38 | 96 | 45 | front | 70c | opiate removed |
| Feb | $319.36 | +21.38 | 96 | 45 | front | 70c | capped |
| Mar | $367.34 | +47.98 | 96 | 43 | ¼ | 80c | capped; ¼ col no loss |
| Apr | $419.52 | +52.18 | 90 | 45 | ¼ | 90c | 14 walked on price; 1 death |

- **Deaths:** 1 (month 16). **Inquests:** 0. Suspicion rose from 1 to 10.7 of 100 over 16 months.
- **Time to first venture:** month 7, and it felt worth it. The Almanac brought about 10 callers and 5–10 post orders a month for $40. Post sales alone (about $5–7 a month) paid it back within about 7 months, before counting the extra callers.
- **Branch in Another Ward ($600):** still out of reach in month 16 at $419. **Druggist's Standing Order ($100):** offered around month 10 but useless to me, because it only takes "what is left on the shelf" and my shelf was always bare.
- **Capacity:** one still makes at most 48 bottles a batch. From month 11 every month sold out at 96. Demand exceeded supply from about month 6.
- **Ad attribution** (front page, month 12): "fourteen from the ward, thirty-five by the Gazette, five by the society, ten by the almanac". At ¼ col (month 15): "thirteen … thirteen by the Gazette". Cutting $17 of advertising cost 22 Gazette callers and **no sales**, because I was capped.
- **Price elasticity:** 70c → 80c, no lost sales. 80c → 90c, 14 callers walked away, 6 bottles left over, profit still up.
- **Clicks for a routine month** once learned: about 6 (open the paper → TO THE PROOF → THE STILL → ORDER AND LIGHT BOTH → PUT IT DOWN → SEND TO PRESS), plus letters.
- **Time until it clicked:** the whole loop (brew → claims → ads → read the books) by month 4, about 15 minutes in. The capacity ceiling by month 9 (about 30 minutes).

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | Once production is capped, which happens by month 8–11, the right play is obvious: put the ad on ¼ col, keep raising the price, ignore the Standing Order. Ads and the wholesale ventures are not real trade-offs while every bottle sells. Month 15 went from +$21 to +$48 just by cutting the ad and adding 10c. | Economy / The Works | Run 2 stills at 48 each from month 8. Every edition says "The shelf was bare … callers went away empty-handed". Drop to ¼ col and raise the price: sales stay at 96. | Let one still's batch size grow with use, or sell a still upgrade as a venture, so capacity is something you buy. Or make unmet demand cost something (regulars drift to Feeney when the shelf is bare). Also say outright in the books that sales were capped by supply. |
| 2 | Major | In month 2 I clicked ORDER ENOUGH FOR 24 · $4.20, then clicked where LIGHT THE FIRE had been. After ordering, the button had moved from [648,460] to [406,433] and changed to "LIGHT THE FIRE · 24 BOTTLES", so my click hit nothing. I paid for the ingredients, the still stayed cold, and I lost a month's production. The only signs were the small "the still is cold" and a hint line on the proof. | THE STILL drawer | Fresh game, month 2: open THE STILL, click ORDER ENOUGH…, then click the spot where LIGHT THE FIRE was. Compare 44.png and 45.png. | Keep LIGHT THE FIRE in the same place before and after ordering. Or make ORDER ENOUGH order and light in one click, as ORDER AND LIGHT BOTH already does once you have two stills. Warn on SEND TO PRESS when ingredients are on hand but the still is cold. |
| 3 | Major | A hidden stat. Letter replies state "Standing +1 for now", "Standing −3: the ward takes his side" and "suspicion +4 and standing −2". Suspicion has a gauge in THE WORKS ("suspicion: a little (6 of 100)"), but standing is shown nowhere I could find. I can't plan around a number I can't see. | Letters (117.png, 105.png); THE WORKS | Open any rival letter (Feeney, month 10 or 14). | Add a "standing" line under suspicion in THE WORKS, with the same "(n of 100)" wording. |
| 4 | Minor | The shop next door was knocked through automatically in month 9 and the rent went from $3 to $8 a month. The prospectus listed it only as a precondition of the Branch ("Offered once the shop next door has been knocked through"), not as something I would be charged for or could refuse. | Edition, month 9 ("IN THE TRADE.—The wall between the shop… has been knocked through… rent up five dollars a month"); ledger | Reach 25 regulars and $200. | Offer it as a venture, or as a letter from the landlord with accept and decline, stating the rent change. |
| 5 | Minor | Widow Katharina Becker, aged 75, died while on a formula the assay rated "Danger: harmless, Habit: none", two months after I removed the laudanum. Neither the edition nor the death notice gives a cause, so I couldn't tell whether withdrawal, age or my remedy killed her. Earlier she was "poorly · asks for it by name". | Edition of Apr 24, 1886 (m16-ed1.png, m16-ed2.png) | Seed 6021: take laudanum out in Jan 1886 and play two months. | When a regular dies, say in the obituary or the books whether the remedy was implicated (for example "of old age" or "after giving up the tonic"). |
| 6 | Minor | Escape doesn't close a drawer after you click a button inside it. Formula book: −5c/+5c, then Escape, and the book stays open (50.png, 99.png). Ventures: TAKE IT UP twice, then Escape, and the drawer stays (85.png → 86.png, where my next click on FORMULA did nothing). Escape with no clicks inside does close it. | Formula book, Ventures drawer | Open FORMULA, click +5c, press Escape. | Release focus from in-drawer buttons, or handle ui_cancel in `_Input` rather than `_UnhandledInput`. |
| 7 | Minor | Customer tooltips from the counter row show on top of the open formula book (for example "Mrs. Gertrude Becker: neuralgia. A regular for 8 months…" across the Assay panel). | Formula book over the counter row (48.png, 116.png) | Open FORMULA and move the mouse below the assay panel, around y≈580. | Block mouse input to the counter while a drawer is open. |
| 8 | Low | The front-page notice ran the same headline, "SOOTHES THE NERVES!", with the same William Pruitt testimonial and art, two editions running. | Editions of Jan 30 and Feb 27, 1886 (m13-ed1.png, m14-ed1.png) | Keep FRONT for three or more months. | Rotate the quoted customer, or vary the deck line. |
| 9 | Low | Two free stills arrived as story text ("A second copper still was carried into the back") with no prompt. The second had no "TRADE TALK" lead-up that I saw. The third did ("halfway to a third copper… a house that has sold a thousand bottles"), which was good. | Edition, month 7 | Play to about 400 bottles sold. | Give the second still the same "said to be near…" foreshadowing, and list milestone unlocks in the Prospectus. |
| 10 | Low | Grain spirit letter: paying $6 against roughly +4c a bottle × about 100 × 6 months (about $24) is not a real choice. | Letter, Jan 1886 | – | Make the Jersey option risky (quality or suspicion), so it is a trade-off. |

The driver log had no exceptions. It shows only ALSA errors and "All audio drivers failed, falling back to the dummy driver" (headless audio).

## What works

- **The ledger.** "The Month's Books" itemises sales to regulars, post and walk-ins, then rent, ads, ingredients, outlays and the suspicion deltas ("Suspicion in the ward rose 2 · Bold advertising claims"). That is what a tycoon player wants, and it fits the fiction.
- **Attribution lines** in THE TRADE: "Callers at the counter: nine from the ward, thirty-five by the Gazette, five by the society, ten by the almanac", and later "Callers who asked the price and went away: fourteen." With these I could run controlled experiments on ad size and price. Keep them, and add more like them.
- **The ventures prospectus is readable from month 1**, with "So far: 180 of 400 bottles sold" progress lines and stated preconditions. I could plan around it. It is the best planning tool in the game.
- **Ventures with stated risks**: the Almanac's "the bolder they are, the more suspicion it brings each month", the Laboratory's "about one month in 25 something catches fire". These are clear risk-and-reward statements.
- **The warning system**: "Death waits at the counter beside Mrs. Greta Kraus" plus the customer status "infant gravely ill" gave me a decision with a clear cost, and the outcome ("RECOVERED.—Mrs. Greta Kraus… will not be needing Mulberry Balm") made it feel earned. "shaking for want of a dose" shows the habit mechanic without saying so.
- **The assay panel** (Kick, Calm, Habit, Danger, Taste, Lingers, In an assay, cost per bottle) explains the formula space well enough to design remedies on purpose.
- **ORDER AND LIGHT BOTH** shows the batch cost in one click, which is good routine-month ergonomics.

## Disclosures

- No jump-ahead. Everything was played by hand from the first edition.
- I used `targets` and `read` to find button rects and to quote text exactly. I used `state` to note cash, regulars and suspicion after each month, which made the metrics table more precise than a player could easily make it. Every number in the table also appeared on screen (in the books or the bottom bar), except the raw "reputation" value, which I didn't use in my judgements.
- From month 11 the routine brew-and-press was scripted (brew.sh). All letters, ads, prices and ventures were chosen by hand.
- After the session I read only `scripts/Main.cs` lines 280–330, to look at Escape handling for finding 6.

## First hour

- **0–3 min (Jan 1885 edition):** handsome paper. The only clickable line is the books, and I read the ledger at once. At first I thought the game was "read the news". The bottom bar and TO THE PROOF button made it clear I play on the proof.
- **3–10 min (first proof):** I found the four drawers (THE STILL, FORMULA, SUPPLIES, VENTURES). The drug shelf tooltips are clear. The Assay made formula design legible straight away. I added ½ oz laudanum to the starter and renamed it. The type case said "Claims … up to four. Bolder type sells more, and the coroner reads it too", which is a clear trade-off. I opened Ventures and read all five improvements and their preconditions. My plan: get to 25 regulars and $200.
- **10–15 min (months 2–3):** the misclick in month 2 meant no batch (finding 2). I misread this as weak demand until I saw "the still is cold". The testimonial letter unlocked ads, which I understood from the reply text.
- **15–25 min (months 4–6):** ads arrived. ½ col tripled sales, full col didn't pay. **It clicked here:** the loop is brew → claims → space → read the books. The shelf went bare in month 6, and I realised production is the constraint.
- **25–35 min (months 7–10):** the Almanac was my first venture. A free second still. ORDER AND LIGHT BOTH. The knock-through surprised me and raised my rent. The letters (price war, benefit society) were real choices with costs printed.
- **35–50 min (months 11–16):** I hit the ceiling. Front page was wasted spend. The death warning led to an opiate-free reformulation. With price and ad tuning I nearly tripled monthly profit. The third still arrived as I stopped.

## Score

**7 / 10.** The systems are readable, the feedback is unusually good for the genre, and the first venture arrived by month 7 and paid for itself. But from about month 8 the capacity cap turns ads and the shelf-dependent ventures into non-decisions, and one stat you are told about is invisible. **One point up:** make production something you grow and choose, for example still upgrades as ventures or a batch size that rises with use, so demand and supply stay in tension. Show "standing" in THE WORKS. Put the knock-through to the player as a choice.
