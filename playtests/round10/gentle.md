# Panacea · round 10 · playtest by "The Gentle Player"

A player of calm life-sims and shopkeeping games who rarely plays anything dark, was told this was "a shop game with a newspaper", had never seen it, and hates being punished without warning.

**Verdict:** I ran an honest, mild shop for 20 months by hand and it worked: cash went from about $157 to $372, regulars from 4 to 50, and in August 1886 the front page read **"THE BOTTLE THAT HURT NOBODY"**. That was the most rewarding headline I have had in a shop game. The content note prepared me for the dark parts. The books, THE MONTH SO FAR and the letters' printed costs nearly always told me why things moved. What still made me anxious: a coroner warning tacked onto my kindest replies, suspicion that rose twice with no reason given, and the LARGER text size clipping the formula book.

**Setup:**

- Build: frozen copy at `scratchpad/build-r10`, commit `f988c58` ("Large print, lining figures, and a shop that shows its growth").
- Port 47113, seed 1113, `--player --profile gentle`. **No jump-ahead at any point.**
- Played January 1885 to September 1886: 20 sends to press, all by hand. About 60 minutes of driving.
- Tooling: small bash wrappers in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r10/gentle/`.
  - `press.sh`: send to press, screenshot, dump the paper's text.
  - `letters.sh`: open the letters and list the reply buttons.
  - `brew2.sh` / `both.sh`: open THE STILL, mark a batch, order and light.
  - `month.sh`: chains the two.
  - Every reply and venture was still chosen by me, after reading the letter.

## Run log

| Seed | Months               | What I tried                                                                                                                                                                                                                                | Result                                                                                                                                                                                                   |
| ---- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1113 | Jan–Jun 1885 (1–6)   | Mild recipe: water, grain alcohol, sugar, peppermint, 1 oz valerian, ½ oz capsicum. Named it "Mulberry Comfort Tonic" at 50c, one claim, SOOTHES THE NERVES. 24–36 bottles a month. ¼ col ad from March. Price to 60c in May. Kind replies. | Slow: 35–39 bottles a month, cash flat at about $155. "TRADE DULL ON MULBERRY STREET." Regulars 4 → 15.                                                                                                  |
| 1113 | Jul–Dec 1885 (7–12)  | Kept brewing. Second still came in September (200 bottles sold), percolator in October (10 batches). Took up THE SHOP NEXT DOOR (nothing down) and THE PROPRIETOR'S ALMANAC ($40) in December.                                              | Sold out twice ("THE STILL CANNOT KEEP UP"). 42 → 68 bottles a month. Cash $162 → $219, dipped to $197 after the almanac.                                                                                |
| 1113 | Jan–Aug 1886 (13–20) | Two, then three stills, 36–48 bottles each. Bought the lease ($60). Joined the Board of Pharmacy honestly. Served jury duty. Testified at an inquest.                                                                                       | 88–129 bottles a month. Cash $163 → $372, regulars 50. Two regulars died, neither blamed on my bottle. One inquest: "Died of Natural Causes". March 1886: sold 10 (my scripting error, see Disclosures). |

**Timeline of key moments**

- **Title page.** "A NOTE BEFORE THE SHOP OPENS" read: "Panacea is a satire of the patent-medicine trade. It shows drug use and addiction, deaths (among them the deaths of infants) and the frauds the trade practised. The joke is on the sellers and the men who let them sell, never on the people they sold to." That last sentence is why I kept going.
- **May 1885.** "TRADE DULL ON MULBERRY STREET / The Bell Seldom Rings." The paper spelled out "WHERE THE MONEY WENT": takings $21.00, outgoings $23.48, itemised. This is when the money side clicked.
- **June 1885.** "THIRTY-NINE BOTTLES SOLD AND NOT ONE LEFT". I learned to brew more.
- **August 1885.** Mrs. M. Kehoe wrote again, "The careful hand again, on better paper": "Nora's teeth are through and she is walking… I did as you wrote, the cold spoon and the dispensary." She remembered my earlier advice. This was the best moment of the game for me.
- **September 1885.** "IN THE TRADE.—Two hundred bottles having gone over the counter, the coppersmith has let the proprietor … have a second copper". The progress counter "(156 of 200 bottles sold so far)" in TRADE TALK the month before had told me it was coming.
- **December 1885.** Ventures taken. **January 1886:** "NO MONTH LIKE IT", 88 bottles.
- **February 1886.** The Secret Service wrote back about the counterfeit note I had described in December. Correspondents do remember.
- **March 1886.** "DEATH OF MR. LEFFERTS". The deaths column said: "Of a fever going about, the register says. The remedy is harmless by the assay; the bottle is not blamed. No inquest was called." Reassuring and clear. Suspicion still went from 0 to 3, with no line saying why.
- **June 1886.** "DEATH OF MRS. ROTH… An inquest sits: see your letters." I chose "Testify to the purity of your preparations", whose margin note read "fewer than one in 20 for naming". The reply strip said "The jury sat at once and found nothing against you", and suspicion fell to 0. Fair, and I was told the odds.
- **August 1886.** "THE BOTTLE THAT HURT NOBODY / A TWELVEMONTH WITHOUT A DEATH LAID AT THE SHOP'S DOOR; THE TRADE IS SUSPICIOUS."

## Metrics

| Month (edition) | Bottles sold               | Cash at month close                        | Regulars (proof footer) |
| --------------- | -------------------------- | ------------------------------------------ | ----------------------- |
| May 1885        | 35                         | $154.40                                    | 13 → 15                 |
| Jun 1885        | 39                         | $162.32                                    | 19                      |
| Jul 1885        | 36                         | $165.34                                    | 22                      |
| Aug 1885        | 42                         | $175.90                                    | 24                      |
| Sep 1885        | 59                         | $194.50                                    | 23                      |
| Oct 1885        | 50                         | $203.04                                    | 24                      |
| Nov 1885        | 57                         | $219.26                                    | 28                      |
| Dec 1885        | 68                         | $197.26 (−$22, after $40 for the almanac)  | 32                      |
| Jan 1886        | 88                         | $163.28 (−$33.98, after $60 for the lease) | 36                      |
| Feb 1886        | 94                         | $191.90                                    | 39                      |
| Mar 1886        | 10 (no fire lit: my error) | $188.40                                    | 37 (1 died)             |
| Apr 1886        | 94                         | $202.48                                    | 45                      |
| May 1886        | 112                        | $240.36                                    | 47                      |
| Jun 1886        | 129                        | $284.74                                    | 48 (1 died)             |
| Jul 1886        | 115                        | $319.72                                    | 50                      |
| Aug 1886        | 129                        | $372.23                                    | 50                      |

- **Deaths:** 2, both of regulars, both "not blamed". **Inquests:** 1, verdict "Died of Natural Causes" after I testified.
- **Suspicion:** 0–3 of 100 the whole game.
- **Good name in the ward:** 13 → 40.
- **Margin** (BOOKS, "WHAT A BOTTLE EARNS"): "sells at 60c. and costs 17c. to make: 43c. clear on every bottle… the first twenty-three bottles a month only pay for them."
- **Time to understand the loop:** about 3 months.
  - From month 1 I knew to brew, write the claims and press.
  - In month 5 the books paragraph made the money side click.
  - In month 7 I learned that the shelf, not demand, was my limit.
- **Time to the first venture:** offered and taken in month 12 (December 1885). THE SHOP NEXT DOOR unlocks at 25 regulars; I took it with the almanac.
  - Worth it: the almanac brought "ten by the almanac" callers every month after, plus about 12 bottles a month by post.
  - Sales went 57 → 68 → 88 → 94 over the next three months.
- **Unlocks earned by honest play:**
  - second still: month 9;
  - percolator: month 10;
  - third still: month 15;
  - wholesale discount: month 17.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Where                                                                                  | How to reproduce                                                                                                        | Suggested fix                                                                                                                                                                                                                                 |
| --- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | My kindest replies carried an ominous rider: "Better known +1: if you ever buy a verdict from the coroner, it is likelier to come out." It appeared on Nora's letter, the Sunday-school tonic, the History of Leading Men, the mare, the Leader's dinner and the counterfeit note. I never meant to bribe anyone, and I could not tell whether "better known" was good or bad. For months it read like a threat attached to being nice. This was the most anxious I felt in the game. | Letter reply notes, all year (e.g. `m08_letters.png`, `m09_letters.png`, `m14_l2.png`) | Answer almost any letter kindly and read the italic line under the reply.                                               | Say what "better known" does for an honest shop first (e.g. "more callers come by word of mouth"). Show the coroner caveat only once the player has seen that a verdict can be bought, or put it in a tooltip on "better known" in THE WORKS. |
| 2   | Minor    | Suspicion rose twice with no reason I could find. March 1886: 0 → 3. June 1886: 1 → 3. Both months a regular died "of a fever… the bottle is not blamed." The suspicion tooltip only says what suspicion is. Nothing in the paper, the books or THE MONTH SO FAR named the cause.                                                                                                                                                                                                     | THE WORKS suspicion bar (`m16_susp.png`)                                               | Play to March 1886 on seed 1113 with my recipe (or wait for any unblamed death) and compare suspicion before and after. | Add a line wherever deaths are explained, e.g. "Suspicion +3: the ward talks when a customer dies, even of a fever."                                                                                                                          |
| 3   | Minor    | At TEXT SIZE: LARGER the formula book clips: glass labels read "Valerian Roo", "Oil of Pepper", "Grain Alcoh". The proof header is cut off at "CORRECT AND RETURN T". THE WORKS wraps "better / known: 1" over three lines.                                                                                                                                                                                                                                                           | FORMULA at LARGER (`m21_formula.png`); THE WORKS (`m12_larger.png`)                    | SETTINGS → TEXT SIZE: LARGER, open FORMULA.                                                                             | Let the glass labels wrap or shorten them. Let the proof header shrink to fit.                                                                                                                                                                |
| 4   | Minor    | At LARGE and LARGER, the "MORE CLAIMS BELOW ▼ (scroll)" banner covers the claim buttons. Only about one row of claims shows above it, so I could not see my own set claim without scrolling.                                                                                                                                                                                                                                                                                          | Type case (`63_proof_large.png`, `m12_larger.png`)                                     | Set LARGE, go to the proof.                                                                                             | Make the type case taller at large sizes, or move the banner below the last visible row.                                                                                                                                                      |
| 5   | Minor    | "Standing" (in the letters) and "good name in the ward" (in THE WORKS) seem to be the same number. Neither screen says so. I thought they were two different meters for a while.                                                                                                                                                                                                                                                                                                      | Letters vs THE WORKS                                                                   | Compare a "standing +1" reply with the good-name figure the next month.                                                 | Use one name, e.g. "good name +1".                                                                                                                                                                                                            |
| 6   | Minor    | Some honest or kind replies are penalised. The penalty is warned, which is fair, but it stung. "Fill in the line truthfully" for the temperance League: "suspicion +2". "Ignore him" (not slandering the rival Feeney): "Standing −3: the ward takes his side." I wanted at least one gentle route that did not cost good name.                                                                                                                                                       | Letters, August 1886 (`m20_l2.png`)                                                    | Wait for the League and Feeney letters.                                                                                 | Consider a kinder third option for Feeney (e.g. "Lower your price a penny"), or say why ignoring him costs standing.                                                                                                                          |
| 7   | Low      | Keyboard in THE STILL: ▲ took the batch _down_ a dozen (36 → 24) and ▼ took it _up_. I expected the opposite.                                                                                                                                                                                                                                                                                                                                                                         | THE STILL                                                                              | Mark 36, press Up.                                                                                                      | Swap them, or say "▲ fewer, ▼ more".                                                                                                                                                                                                          |
| 8   | Low      | Masthead at LARGER read "VOL. XVI.--NO. 680." with a double hyphen. At regular size it was "VOL. XV.—NO. 620." The date also wrapped onto two lines.                                                                                                                                                                                                                                                                                                                                  | Paper top (`m21_a.png`)                                                                | LARGER text, any edition.                                                                                               | Use the em dash; let the date line shrink.                                                                                                                                                                                                    |
| 9   | Low      | The almanac's first edition was reported as "The almanac of Mulberry Comfort Tonic is printed again".                                                                                                                                                                                                                                                                                                                                                                                 | December 1885 paper, IN THE TRADE                                                      | Take up THE PROPRIETOR'S ALMANAC.                                                                                       | Say "is printed" the first time.                                                                                                                                                                                                              |
| 10  | Low      | Words I did not understand and had to guess at: calomel, Fowler's Solution (I guessed from "arsenical"), court-plaster, carron oil, percolation, steeping, petit juror, phrenologist, assay, "the careful households". The jar tooltips helped with most drugs. The callers line ("four who will not have a narcotic in the house") explained "careful households" only by luck.                                                                                                      | Jars, letters, callers line                                                            | —                                                                                                                       | Consider a one-line gloss in the letter option notes for court-plaster and carron oil. Name "careful households" once in plain words.                                                                                                         |

## What works

- **The content note.** It is short, honest and on the title page. It told me where the joke points, and the game kept that promise: the regulars were never the butt.
- **Every letter prints its cost and effect under each reply.** Examples: "$2; standing +1.", "Costs nothing.", "Standing −1.". Nothing that happened from a letter surprised me. The inquest's pencilled odds ("fewer than one in 20 for naming") are exactly what a cautious player needs.
- **THE MONTH IN BRIEF strip, plus "WHERE THE MONEY WENT" and the BOOKS paragraph.** "The first twenty-three bottles a month only pay for them" taught me break-even without a tutorial.
- **The callers breakdown** ("thirteen with the Gazette's notice in hand, ten by the almanac…"). It made the ad and the almanac feel worth their price.
- **NOTES FOR THE PROPRIETOR on the proof.**
  - "Nothing on the shelf and nothing on the still: light THE STILL, or the shop will sell nothing this month."
  - "New: a second still is yours…"
  - These are a gentle tutor that never nags.
- **Progress counters in the trade gossip** ("156 of 200 bottles sold so far"), and the matching line in THE STILL ("Next: a third still: 468 of 600 bottles sold").
- **Correspondents remember.** Mrs. Kehoe and Nora, and the Secret Service follow-up.
- **The honest shop is viable and gets its own payoff headline**, "THE BOTTLE THAT HURT NOBODY". Please keep it.
- **Save and continue.**
  - I stopped the game with `drive_stop.sh` in June 1885, after answering both letters and lighting a fire.
  - The relaunched title page said "June 1885 · $144.92 · 15 regulars" and "The Gazette of May 30 · the books closed at $154.40 · cash now $144.92, $9.48 spent since".
  - CONTINUE gave back exactly that shop: same cash, 36 brewing, "Two letters answered".
  - MENU → "SAVE, AND THE TITLE PAGE" → CONTINUE in February 1885 also came back intact.
- **The paper doesn't repeat itself.** Over 15 editions I checked, no news item repeated; only the rival advertisements did, which suits them. The jokes stay mild ("The pamphlet is said to be the more encouraging of the two").
- **The look.** The woodcut regulars at the counter, their cards ("peaky · delighted"), and the "AT THE COUNTER: Widow Minnie Abrams: a regular for nine months…" line all made the regulars feel like people I was caring for.

## Disclosures

- **No jump-ahead.** Every month was played by hand from the first edition.
- I used `targets` and `read` dumps to find buttons and to quote text accurately. Every reply was chosen after reading the letter on screen or in the dump. The cash and sales figures above come from the paper's own text.
- **March 1886 (sold 10):** my still script did not recognise the new "ALL THREE STILLS" button, so no fire was lit. The game had warned me clearly ("Low stock: ten bottles on the shelf and nothing on the fire… Light THE STILL"). That is my error, not the game's.
- **April 1886:** my script pressed Escape with no drawer open, which opened THE MENU. A later click landed on "SAVE, AND THE TITLE PAGE". CONTINUE restored the shop intact. Also an accidental click: on the Hobart "Exclusive Dealing" letter I meant "Stay free" and hit "Sign for the year, and pay the deposit" ($10).
- From about month 3, screenshots I had already viewed scrolled out of my context. Early quotes are from my notes and the text dumps, not re-read images.
- The driver log (`/tmp/panacea-driver-47113.log`) showed only the expected audio-driver errors (ALSA, `ERR_CANT_OPEN` in `audio_driver_alsa.cpp`). There were no script exceptions.

## First hour

- **0:00 Title page.** I read the content note and felt warned but not scared off. In SETTINGS I saw TEXT SIZE, sound and window options. Clicked A NEW SHOP.
  - Odd moment: the callers on the title-page backdrop (Mr. Jacobus Remsen, Miss Giovanna Barbieri…) were not the ones in my new shop (Max Goldberg, Assunta Conti, Salvatore Ferri, Agnieszka Kaminski). Low, but it made me wonder whether I had lost a shop.
- **0:03 First edition.** I understood the paper is the scoreboard. I clicked an "AMONG THE CALLERS" clipping and it opened that caller.
- **0:05 TO THE PROOF.** Labelled drawers: THE STILL, FORMULA, SUPPLIES, VENTURES, BOOKS. Each tooltip says what it does. I guessed the order: formula, then still, then claims.
- **0:07 FORMULA.** The glass came pre-poured (water, alcohol, syrup, peppermint). The jar tooltips told me which drugs were dangerous (laudanum "brings the customer back for more", calomel "lingers in the body"). I added valerian ("habit-free") and capsicum, named the tonic and entered it in the book. The assay read "harmless" and "An honest mixture, by the standards of the trade". Reassuring.
- **0:10 THE STILL.** "LIGHT THE FIRE" was disabled with the tooltip "Not enough stock on the shelf: order first." "ORDER FOR 24 · $3.60" fixed that. Clear.
- **0:12** Type case: set SOOTHES THE NERVES only. Looked at SUPPLIES, VENTURES and BOOKS, then sent to press.
- **0:15 February.** First letter, answered kindly. Brewed again. Tested MENU → save to title → CONTINUE. Clicked a regular's card.
- **0:20 March.** Opened VENTURES and saw what was coming. Bought ¼ col space ($3).
- **0:25 May.** "TRADE DULL". I was **anxious** that the honest shop could not pay. Then "WHERE THE MONEY WENT" showed it was the letters ($11) that sank the month, not the tonic. It **clicked** here. I raised the price to 60c.
- **0:30 June.** Answered the Fourth of July committee (kept the door open with lint and oil) and the Sunday-school excursion (court-plaster: I had to guess what it was). Stopped the game mid-month and relaunched; it came back exactly. Switched to TEXT SIZE: LARGE. Readable and pleasant, though the claims banner overlapped the claims.
- **0:35 July.** Sold out. I felt **lost** only briefly: the note told me to brew more.
- **0:45 September.** Second still. From here the loop felt like a cozy shop game: brew to demand, answer the post, watch the regulars' cards.
- **Remaining unease:** the "buy a verdict from the coroner" rider on kind replies (finding 1), and the two unexplained suspicion ticks (finding 2).

## Score

**7 / 10.** As a gentle player I could run a kind shop, understand nearly every swing in cash and customers, and get rewarded for it. Clearer wording on "better known" and suspicion, and a LARGER text size that doesn't clip, would make it an 8.
