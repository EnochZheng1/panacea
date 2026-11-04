# Panacea · round 5 · playtest by "Narrative-management player"

A first-time player who loves small-operation moral-choice sims (border checkpoint, city survival, newspaper/shop sims). I bought it knowing only "dark-satirical patent-medicine business, 1880s New York".

**Verdict:** The newspaper as the feedback loop is wonderful. When my harmless tonic picked up half an ounce of laudanum and a "COMFORT FOR TEETHING BABES" claim, I read about an 81-year-old regular who "slept soundly to the last", and then that a mother had started my tonic "for a teething infant". That was the moment the game clicked for me, and it is exactly the guilt-by-spreadsheet feeling I play this genre for. Two things hurt trust. The bribe receipt says "the verdict will be the one you paid for" even in a month when the bribe was found out and suspicion went from 20 to 50. And beyond the almanac, the ventures look out of reach for a modest proprietor: three scripted years still had less than $200.

## Setup

- Build: frozen copy `build-r5` (commit `ea3dd20`), port 47016.
- **Game 1 (blind, no jump-ahead):** seed `5016`, played by hand from the first edition (Jan 1885) into the Sep 1885 proof. A machine restart then stopped the game mid-proof (the coordinator told me about it). I took about 30 minutes of active play, all before the restart. Screenshots: `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r5/cold-sim-player/01.png` to `87.png`.
- **Game 2 (jump-ahead):** seed `5016 --play 24 --dose 0.25`, so it starts at Jan 1887. I played Jan to Mar 1887 by hand, took up the Almanac and ran two stills. Screenshots are in `.../cold-sim-player/g2/`.
- **Game 3 (jump-ahead):** seed `5016h --play 30 --dose 1.5`. The scripted proprietor was already **bankrupt at Apr 1886** ("A PROPRIETOR FAILS"), so I only looked at the ending. Screenshots are in `.../g3/`.
- **Game 4 (jump-ahead):** seed `5016m --play 36 --dose 0.6`, starting at Jan 1888. I only inspected the VENTURES drawer. Screenshots are in `.../g4/`.
- Total active testing was about 45 minutes. My only tooling was a one-line wrapper around `drive.py`.

## Run log

| Game | Seed / args | Months | What I tried | Result |
|---|---|---|---|---|
| 1 | 5016, none | Jan–Sep 1885 (8 sends) | Honest vegetable tonic, then a second laudanum recipe, then a laudanum reformulation of the first tonic and a teething claim. ¼ col then ½ col ads, the benefit society, the Almanac, all three inquest replies. | Cash $150 → $166 (Aug) → $97. 6 deaths, suspicion 0 → 74 ("the Board of Health is asking"). Cut off by the restart. |
| 2 | 5016 `--play 24 --dose 0.25` | Jan–Mar 1887 (2 sends) | Took up the Almanac, brewed in both stills (96 bottles). | Sold 48 then 58. Regulars 19 → 26. Cash $181 → $174. Branch and Laboratory not affordable. |
| 3 | 5016h `--play 30 --dose 1.5` | – | Read the game-over edition. | Bankrupt in month 16. "THE SHOP ON MULBERRY STREET IS SOLD UP; THE STILL GOES FOR SCRAP." |
| 4 | 5016m `--play 36 --dose 0.6` | – | Read VENTURES. | $194 cash after 3 years. Wholesale not offered; Laboratory "NOT TO BE HAD · Not enough cash." |

Timeline of key moments (game 1):
- **Jan 1885:** Front page "A NEW CONCERN … He Promises Much." The proof's type case says "No remedy yet: open the formula book." That pointed me to my first click.
- **Jan:** I added valerian and capsicum to the default glass, named it "Mulberry Honest Tonic" (60c., 18c. to make, Danger "harmless") and brewed 24. 17 sold.
- **Feb:** Mrs. Zofia Kaminski died while on my *harmless* tonic, and an inquest arrived. I testified and printed a testimonial ("standing rises 3"). The type case said "Both conditions are met: space can be bought from next month."
- **Mar:** Bought ¼ col: "A RECORD MONTH", +$9.60.
- **Apr:** Moved up to ½ col and 48 bottles. Two regulars left: "CUSTOMERS DESERT A REMEDY", -$3.24.
- **May:** Joined the benefit society. Made a laudanum recipe as a *new* remedy and learned that new remedies start with no claims and no ad.
- **Jun:** Reformulated the main tonic with laudanum ½ oz. The assay said "most will come back", Danger "a risk to the frail". The formula book's line "Reformulated. The next batch will be the new recipe; the customers will not be told." chilled me in a good way.
- **Jul:** Added COMFORT FOR TEETHING BABES. I forgot to brew, so the shelf was bare mid-month. Mr. Conrad Mueller, 81, "slept soundly to the last."
- **Aug:** Two inquests. I paid the $15 fee for Mueller and said nothing for the unknown man, then took up the Almanac for $40.
- **Sep edition:** "A SICKLY SEASON": three dead, including Widow Kaminski, my first regular. "Mrs. Rebecca Lowenthal … has begun upon Mulberry Honest Tonic for a teething infant." The books were down $49.18.
- **Sep proof:** I paid $20 for Kaminski. The receipt said "the verdict will be the one you paid for", but the Works box went from "a good deal (20 of 100)" to "alarming (50 of 100)" with no explanation. I said nothing on the second inquest: "The jury sat at once and named your bottle. Suspicion is now 74 of 100." The restart came here.

## Metrics

| Month (game 1) | Bottles sold | Cash at month end | Deaths | Suspicion (Works box) |
|---|---|---|---|---|
| Jan 1885 | 17 | $150.72 | 0 | 0 |
| Feb | 12 | $150.60 | 1 | 3 |
| Mar | 26 | $160.20 | 0 | 0–3 |
| Apr | 24 | $156.96 | 0 | 0–3 |
| May | 34 | $159.42 | 0 | 0–3 |
| Jun | 34 | $156.84 | 0 | 0–3 |
| Jul | 33 | $166.14 | 2 | 11 |
| Aug | 48 | $116.96 | 3 | 20 |
| Sep (proof, after replies) | – | $96.96 | – | 74 |

- **Profit over 7 months of honest-ish play: about +$16.** Rent, ads and bottles eat almost everything. With a $600 branch, a $400 laboratory and a $360 wholesale contract, I could see no realistic path to any venture but the $40 almanac. Games 2 and 4 bear this out: the scripted proprietor had $181 after 24 months and $194 after 36.
- **Time to understand the core loop:** about 4 minutes, at the first proof. The type case's "open the formula book" hint and the "Nothing on the shelf … the shop will sell nothing" warning did the work.
- **Time to understand the moral stakes:** month 7, at Mueller's death. Before that the game felt like a gentle bookkeeping sim.
- **Clicks to brew a month (steady state):** 5. That is THE STILL, mark 48 (one click on the last bottle), ORDER ENOUGH, LIGHT THE FIRE, then Escape. Good.
- **Inquest odds shown (testify / say nothing):** 1 in 20 / 1 in 10 on the harmless tonic, rising to 9 in 10 / 6–8 in 10 on the laudanum tonic. That change is readable and weighty.
- **Errors in the driver log:** none. The only lines were the expected ALSA/dummy-audio warnings.

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | Paying the coroner's fee always reports "The coroner has your envelope; the verdict will be the one you paid for." In Sep 1885 suspicion rose 20 → 50 and standing fell from about 15 to 0 right after I paid $20, with no explanation (the envelope must have come to light). In Aug the same reply lowered suspicion 11 → 9. Afterwards I checked the source: in `LettersDesk.cs`, around line 267, the bribe branch returns a fixed string and never looks at `talk`/`standing`, unlike the other replies. | Morning's Post, receipt line (`84.png`) | Game 1, Sep 1885 proof, first inquest, "Pay the coroner's 'consulting fee' of $20.00." | When suspicion jumps after a bribe, say so at once, e.g. "Your envelope was found out: suspicion is now 50 of 100." |
| 2 | Major | Ventures past the almanac are out of reach. Three scripted years at a mild or middling dose left $181–$194 in cash, against $360/$400/$600 prices. The Wholesale card says "Offered after fifteen hundred bottles sold" but nowhere shows how many I have sold. The Laboratory card shows no payoff line ("It brings…"), only the fire risk. | VENTURES drawer (`76.png`, `77.png`, `g4/03.png`) | `--play 36 --dose 0.6`, then open VENTURES | Show progress ("1,240 of 1,500 bottles sold"). Give the Laboratory a payoff line in money. Retune prices or margins so a careful house can afford one big venture by year 2–3. |
| 3 | Minor | Seasonal standing matter is wrong: "THE WEATHER.—Hot and close. The Health Department urges the poor to keep out of the sun…" appears in a February 1887 edition (masthead "Fog") and in April 1886. | Paper, right column (`g2/16.png`, `g3/02.png`) | Game 2, the Feb 26, 1887 edition | Tag weather items by season. |
| 4 | Minor | "No name is used twice" holds for full names, but given names cluster: Salvatore Greco, Salvatore Marino and Salvatore Ferrara were all regulars within 8 months, along with Rebecca Abrams and Rebecca Lowenthal, Conrad Mueller and Conrad Hoffmann, and Zofia Kaminski and Widow Stanislawa Kaminski. With three Salvatores on the counter I lost track of who was who, which blunted the deaths. | Counter tags, Personal column | Game 1, months 1–8 | Avoid reusing a given name among living regulars. |
| 5 | Minor | A new remedy entered from the formula book gets no claims and no ad. The type case only ever offers "Claims for Mulberry Honest Tonic", and I found no way to switch it to the second remedy. My 24 bottles of Soothing Syrup sold without an ad, and the type case never says so. | Type case with 2 remedies (`45.png`, `50.png`) | Enter a second recipe as a new remedy, then look at the type case | Add tabs for remedies in the type case like the still has, or a line: "Only your first remedy is advertised." |
| 6 | Minor | The right-hand column moves. The Works box and drawer buttons sit at different heights from month to month (y≈548, 575, 589, 606 and 617), depending on the letters and hint lines. When 2+ letters are waiting, the type case's SPACE IN THE GAZETTE buttons scroll out of view under the letters box (`67.png`, `g2/03.png`). I misclicked twice. | Proof desk, right column | Any month with 2+ letters | Pin the Works box and drawers to the bottom. Keep the ad-size row always visible. |
| 7 | Minor | In game 2 my click on "½ COL" did not take: the proof and the next month still showed ¼ COL and $3. I didn't notice until the next proof. | Type case (`g2/12.png`) | Game 2, Feb 1887 proof: click ½ COL, then go to the still | Check the scroll container eats no clicks. Echo the ad size on the SEND TO PRESS button. |
| 8 | Low | Taking up a venture ($40 Almanac) needs no confirmation and can't be undone this month. That's fine at $40, but worrying for the $600 Branch. | VENTURES (`78.png`) | Click TAKE IT UP | Confirm the larger ventures, or allow undo until press. |
| 9 | Low | On the bankruptcy edition, TRADE TALK still says "The proprietor is said to be halfway to terms from an importer on Pearl street…" for a shop that has just been sold up. | Game-over paper (`g3/03.png`) | `--play 30 --dose 1.5` seed 5016h | Suppress TRADE TALK on the last edition. |
| 10 | Low | Standing matter repeats noticeably within two years: the Living Skeleton wedding, the man asleep on the Elevated, the 'Vital Bitters' peddler, the forty-cent purse in Chatham Square. | Paper | Compare the 1885 and 1887 editions | Rotate a longer list, or retire items after they've been printed. |

## What works

- **The paper as consequence.** "Mr. Conrad Mueller, retired, aged 81 years. He slept soundly to the last." and "a bottle, green and empty, was in the pocket. It is unclaimed." are the best feedback I've had in a management game. I felt it before I understood the numbers.
- **The assay panel.** It names effects in period language: "most will come back", "a risk to the frail", "any chemist would find it". It updates as you pour, so the moral choice happens at the bench.
- **"Reformulated. … the customers will not be told."** One line, and it is the whole game.
- **Inquest letters with pencilled odds** ("about 9 in 10 for naming. The jury sits at once. —J.D."). The odds are clear and in-world, and you watch them change as your bottle gets stronger.
- **"THE MONTH IN BRIEF"** strip plus the itemised Trade column ("Takings $28.80; outgoings $77.98 (outlays on the business $40.00, consulting fees $15.00…)"). I could always reconcile my cash.
- **The still drawer:** click a bottle to mark the batch, then ORDER ENOUGH FOR 48 · $11.04, then LIGHT THE FIRE. That's quick, and the shortfall text "The shelf is short of water: 2 oz a bottle, 0 oz on hand" is helpful.
- **The Works hints** ("☞ Nothing on the shelf and nothing on the still: the shop will sell nothing this month." and "two inquests unanswered: the coroner will proceed without you.") saved me twice.
- **TRADE TALK** said exactly what the next unlock needs ("the cobbler's shop next door, which the landlord will let to a proprietor with twenty-five regulars and two hundred dollars").
- **Venture cards** read like a real prospectus, and the Almanac carries a fair warning ("every claim on your label is read in ten thousand kitchens"). The paper credits it later: "ten by the almanac".
- The game-over edition "A PROPRIETOR FAILS … He Is Said to Be Considering the Law." gives a fitting ending.

## Disclosures

- Games 2–4 used `--play 24/30/36` jump-ahead. Their starting cash, remedy and claims were set by the scripted proprietor, not by me.
- I used `state` to read exact cash, suspicion and stock for the metrics table, and `targets` for click coordinates. Everything in the findings was also visible on screen.
- After my first session I read `LettersDesk.cs` (lines 245–285) only, to confirm finding 1.
- Game 1 was cut off by a machine restart in the Sep 1885 proof, before I could send it to press.

## First hour

- **0–2 min, first edition:** A gorgeous woodcut paper: "A NEW CONCERN … A PROPRIETOR OF REMEDIES OPENS HIS DOORS". The rival ads (Mrs. Whittle's "correspondence continues brisk, notwithstanding her funeral in March") told me the tone at once. I wasn't sure whether the paper was just flavour or something I'd act on. TO THE PROOF ☞ was the obvious next step.
- **2–5 min, first proof:** At first I thought the proof was a newspaper-layout game. The type case's "No remedy yet: open the formula book" and the struck-out "dele" marks clarified that I am working on next month's paper. THE STILL, FORMULA, SUPPLIES and VENTURES read as an obvious toolbar.
- **5–10 min, formula book:** The drug shelf with prices and the assay was clear. I made a harmless tonic out of caution and because I wanted to "play fair" first. Naming it was a nice touch. The still auto-ordered ingredients, which I only realised from the cash drop.
- **Month 2:** A customer died on a *harmless* tonic and the coroner wrote anyway. I misread this as "I did something wrong". Later I understood that the poor die anyway and the coroner just asks. The testimonial letter was a nice moral prod.
- **Months 3–6:** I learned ad space from the type case note. The CUSTOMERS DESERT A REMEDY headline after a ½-column month confused me: did ads drive people away? I never found out. This stretch felt slow and safe. I was making pennies, and it was the moment I'd have put the game down if the paper weren't so good.
- **Month 6–7 (click moment):** I added laudanum and saw "most will come back" next to "a risk to the frail". I decided to do it anyway and print COMFORT FOR TEETHING BABES. The next edition killed an 81-year-old regular. From here on every choice felt heavy, and I wanted to keep playing.
- **Month 8:** Inquests with pencilled odds, bribes and the Almanac. I understood suspicion as the real clock. My one real confusion was the silent bribe exposure in month 9 (finding 1), which made me distrust the receipts.

## Score

**7 / 10.** The writing, the paper-as-feedback and the assay make the moral weight land, though slowly. One point up would come from fixing the bribe receipt so it reports when an envelope is found out, and from showing progress and payoff on ventures so that growing the house is a goal I can see from month one.
