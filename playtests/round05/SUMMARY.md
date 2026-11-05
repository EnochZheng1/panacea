# Panacea · review round 5 · summary

Before this round, a short fix batch went in:

- the wholesale contract's payback was slowed to two to three years;
- names are drawn by origin;
- export presets were added, and the Linux build was tested;
- the price current got leader dots;
- the counter tags were put in the Tab order;
- there were small copy fixes.

Eight persona testers then played the frozen build `ea3dd20`:

- **five returning testers**, re-checking their round-4 findings and judging the new ventures;
- **three first-time testers**, who had never seen the game and were barred from the earlier reports, the design
  documents and the source until after their first session.

The container restarted partway through. Every tester was resumed from where they stopped, restarting their game
with a jump-ahead, and each report says so.

## Scores

| Persona                   | R1      | R2      | R3      | R4      | **R5**  | Headline                                                                                                    |
| ------------------------- | ------- | ------- | ------- | ------- | ------- | ----------------------------------------------------------------------------------------------------------- |
| Casual first-timer        | 5.5     | 7       | 7.5     | 8       | **8**   | The drawer is clear and the leads vary (9 in 17), but buying a venture makes the front page call trade dull |
| Careful newcomer          | 6       | 6.5     | 7       | 7.5     | **7.5** | Every number still reconciles (14 of 14 books), and the ventures do what the prospectus says                |
| Management-sim veteran    | 5       | 7       | 7       | 7.5     | **8**   | "A real second act", but it starts late by hand; wholesale is a must-buy and the almanac nearly worthless   |
| UX / accessibility critic | 6       | 7       | 7.5     | 8       | **7.5** | Round 4 mostly fixed, but arrow keys walk out of every drawer and one Enter buys a venture                  |
| Art and tone director     | 6       | 7       | 7.5     | 8       | **8.5** | Seals, hyphenation, leader dots and names by origin all landed; the standing matter is scheduled badly      |
| **Returning mean**        | **5.7** | **6.9** | **7.3** | **7.8** | **7.9** |                                                                                                             |

| First-time tester            | **R5**  | Clicked                                     | Headline                                                                                  |
| ---------------------------- | ------- | ------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Narrative-management player  | **7**   | Month 7, the first death the paper reported | The paper telling you what your choices did is "excellent"; the bribe message lies        |
| Gilded Age reader            | **7**   | —                                           | The satire lands, but the paper reruns itself and the teething-syrup victim is the mother |
| Lapsed player (half an hour) | **6.5** | Minute 3, the assay                         | Would have quit around month 7; the first death headline pulled them back                 |
| **First-time mean**          | **6.8** |                                             |                                                                                           |

The mean across all eight is 7.5.

The cold panel did its job. The returning testers' mean crept up 0.1, but a first-time player lands about a point
lower, near 7. The gap is not in the look: no first-time tester faulted the art. It is in **routine** (the chores of a
month with two stills), **reach** (the ventures are out of a hand player's reach) and **repetition** (the standing
matter and Wants reprint, and the dated items stick).

## What the round agreed on

1. **Ventures are out of reach for a hand player (six of eight testers).** The bots buy them, but a person playing by
   hand makes $10–$28 a month. That makes $360–$600 down read as "never". Only the $40 almanac was ever bought by
   hand, first at month 9. Three of four cards say "NOT TO BE HAD" through month 40, with no progress shown. The bot
   balance numbers overstate what a player sees, because the bots play cleaner than people do.
2. **The paper's filler repeats (five testers).** "THE BRIDGE." ran in 13 of 14, and 17 of 17, editions. It was set as
   a dated item with a 1885–1890 window, and dated items print in every edition inside their window. The pedestal
   fund "passes ninety thousand dollars" for seven months. Seasonal items ignore the date ("Hot and close" beside
   "Sleet", oysters in June, baseball in January). The Wants column draws from a small set.
3. **Buying a venture makes the front page call trade dull (casual, careful).** The slump test counts the down
   payment as a bad month. It happened every time either tester bought one.
4. **Arrow keys leave the drawers (UX).** Tab is trapped inside a drawer, but the arrow keys are not, so they walk onto
   the dimmed bench behind it. With VENTURES open, Down ×6 and then Enter sent the paper to press. VENTURES also opens
   with focus on the purchase, and two Enters bought the almanac.
5. **The routine month is a chore (lapsed, casual).** The still forgets its batch size, and two stills mean marking,
   ordering and lighting twice. The right column reflows, so the WORKS buttons move by up to 70 px from month to
   month (lapsed, sim player), which caused misclicks.

## Findings by area

**Balance and ventures**

| Finding                                                                                  | Who                            |
| ---------------------------------------------------------------------------------------- | ------------------------------ |
| Ventures out of reach by hand; no progress shown toward "1500 bottles sold"              | tycoon, casual, all three cold |
| Wholesale is a must-buy (+45% to +100% for every line); the almanac is −$71 for adaptive | tycoon                         |
| About 90c is the dominant price with the contract (55% of shelf, capped at 50c)          | tycoon                         |
| The almanac's risk line ("read in ten thousand kitchens") reads as a boast or a warning  | casual                         |
| The laboratory card gives no payoff in words                                             | sim player                     |

**The paper**

| Finding                                                                                  | Who                               |
| ---------------------------------------------------------------------------------------- | --------------------------------- |
| THE BRIDGE in nearly every edition; dated items stick; Wants repeat                      | art, careful, casual, period, sim |
| Seasonal and weather items contradict the date and the weather box                       | art, careful, period, sim         |
| "TRADE DULL" in the month a venture is bought                                            | casual, careful                   |
| "TRADE HOLDS UP … SOLD AGAIN" when sales fell (round 4, not fixed)                       | casual                            |
| "BARE A SECOND MONTH" reprinted in the third month                                       | period                            |
| The Trade counts wholesale bottles as sold at the shop at the shop price                 | careful                           |
| The bribe reply always says "the verdict will be the one you paid for"                   | sim player                        |
| The teething-syrup buyer falls ill herself; historically the infants died                | period                            |
| The infant lead blames a claim that wasn't made                                          | art                               |
| Grant's death and funeral are missed in July and August 1885; the pedestal fund misdated | period, art                       |
| Given names cluster (three Salvatores in eight months)                                   | sim player, careful               |

**The desk**

| Finding                                                                                  | Who                |
| ---------------------------------------------------------------------------------------- | ------------------ |
| Arrow keys leave every drawer; VENTURES opens focused on a purchase, with no guard       | UX                 |
| The right column reflows month to month and the buttons move                             | lapsed, sim player |
| The still forgets its batch size; two stills double the chores                           | lapsed             |
| A fifth claim overflows the type case and hides SPACE IN THE GAZETTE                     | lapsed, period, UX |
| A second remedy can't be advertised (the type case only offers the first)                | sim player         |
| Counter tags: focus barely visible, and five more Tab stops (a minimal month is 32 keys) | UX                 |
| The prospectus is see-through between its cards; its last line overlaps the counter tags | art, UX, lapsed    |
| Death at the counter is unexplained until month 9, and is the same figure as decoration  | lapsed             |
| Ad-space buttons show no price until hover                                               | lapsed             |
| "THE SHOP: HEARD" doesn't say it's sound; its tooltip is Godot's default style           | UX, art            |

## Fixed before the round, confirmed by testers

- **The re-cut seals show on the letterheads** (art).
- **Hyphenation breaks correctly** (art, UX).
- **The price current has leader dots** (art).
- **Names are paired by origin** (art, careful).
- **No full name repeats** (careful, about 60 checked).
- **Escape returns to the drawer's button** (UX).
- **Focus after a reply stays on screen** (UX).
- **Inquest odds differ by reply** (UX).
- **The ventures do exactly what their terms say** (careful).

## Fixed after round 5

| Finding                                                   | Who                               | Now                                                                                                                                                                                                                                                                                                                                                                                                                              |
| --------------------------------------------------------- | --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Ventures out of reach by hand                             | tycoon, casual, all three cold    | Balance pass 5 (`balance-pass-5.md`). A `sloppy` bot plays like the testers did: $196 at month 12 and $326 at month 24 in the old game, against the hand reports' $160–220 and $150–300. A first rung was added, a druggist's standing order ($100, at 400 bottles sold). The wholesale contract is now $300, at 1,200 bottles. Sloppy's second venture comes at month 13 (was 35) and its third at month 26 (was 47)            |
| No progress toward a venture                              | tycoon, sim player, casual        | Each card prints what it still waits for: "So far: for the shop next door, 15 of 25 regulars and $148.95 of $200.00 put by; $148.95 of the $600.00 in hand."                                                                                                                                                                                                                                                                     |
| Wholesale a must-buy; 90c dominant; the almanac worthless | tycoon                            | Druggists order fewer bottles the dearer the shelf price, and are served after the regulars from at most half of what is left. Bought alone, the contract adds $1.1–3.1k, in line with the other ventures. Adaptive at 90c falls from 1.29× to 1.12×. The almanac's readers send for about twelve bottles a month by post, and it pays back in 7–9 months. Buying everything is not a win for a hand player                      |
| THE BRIDGE every edition; dated items stick; Wants repeat | art, careful, casual, period, sim | Dated items run once, in the first edition of their window. New items of the day include Grant's death and funeral, the pedestal fund opened and closed, Ellis Island, the cholera ships, the panic, the Maine, the subway and the muckrakers. 69 standing items read on six an edition, and 30 Wants in a per-game order                                                                                                        |
| Seasonal items ignore the season                          | art, careful, period, sim         | Oysters, skating, the heat, the ice, Coney Island, the holidays and the weather items carry their months                                                                                                                                                                                                                                                                                                                         |
| TRADE DULL in a venture month; SOLD AGAIN when sales fell | casual, careful                   | A venture's down payment is an outlay, not a slump. "Again" heads need sales that held                                                                                                                                                                                                                                                                                                                                           |
| Wholesale bottles counted as sold at the shop             | careful                           | Shop sales, wholesale and post orders are reported apart                                                                                                                                                                                                                                                                                                                                                                         |
| Arrow keys leave drawers; one Enter buys a venture        | UX                                | The arrows stay inside drawers and cuttings. VENTURES opens on its first notice, and a purchase takes a second press with a guard                                                                                                                                                                                                                                                                                                |
| Counter tags: weak focus, five Tab stops                  | UX                                | The counter is one Tab stop and the arrows walk it. The customer in hand is printed in reverse, with the hover text under AT THE COUNTER                                                                                                                                                                                                                                                                                         |
| The right column moves; the type case hides ad space      | lapsed, sim player, period, UX    | The post and the warnings have fixed depths. SPACE IN THE GAZETTE sits above the claims, with its rates printed                                                                                                                                                                                                                                                                                                                  |
| The routine month is a chore                              | lapsed                            | The still remembers its batch. With two stills free, one press orders for and lights both                                                                                                                                                                                                                                                                                                                                        |
| A second remedy can't be advertised                       | sim player                        | The type case has a tab for each remedy                                                                                                                                                                                                                                                                                                                                                                                          |
| The bribe message lies                                    | sim player                        | A bribe that comes out at once says so, with the suspicion and standing it cost                                                                                                                                                                                                                                                                                                                                                  |
| The teething-syrup victim                                 | period                            | The infant is the one the counter shows ill ("infant gravely ill; the mother asking for it by name")                                                                                                                                                                                                                                                                                                                             |
| Smaller copy                                              | art, careful, UX                  | The infant lead quotes the label only if it claimed teething. The testimonial's address matches its letter. The Clerk counts answered notices. An invented Newark wholesale house, and Elizabeth for Yonkers. "the money of" for "the dollar of". "STILL BARE" for "BARE A SECOND MONTH". The wholesale terms say they can be cancelled. The almanac's risk in plain words. "SOUND: THE SHOP HEARD". Printed tooltips everywhere |

Checks: TS 107/107 and C# 87/87 (parity on 28 fixture games, regenerated), typecheck, lint, format, Godot build,
`tools/check_cuts.py`.

## Balance after the fixes (80 runs, `--seed r5b`; full report `balance-pass-5.md`)

| Bot                          | Median final cash, before → after | Note                                       |
| ---------------------------- | --------------------------------- | ------------------------------------------ |
| cautious                     | $5,963 → $6,754                   |                                            |
| shopkeeper                   | $13,392 → $15,170                 |                                            |
| adaptive (moderate)          | $17,905 → $19,414                 |                                            |
| briber                       | $8,437 → $11,930                  | 51% end early                              |
| greedy (heavy narcotics)     | $14,844 → $14,844                 | p90 $27.3k (1.41× adaptive); 70% end early |
| sloppy (plays like a person) | $9,936 → $10,117                  | ventures at months 13, 26 and 40           |

The exploit hunter's best line is 0.94× adaptive's mean.

## Still open

- **The still's money lines use old prices** after the market moves, and the formula book's cost a bottle does too
  (careful, not fixed since round 4).
- **The counter cuts vary heads, not bodies**; the mothers hold one baby. The still's flames are still simple (art).
- **Death at the counter** is unexplained until the warning line, and is the same figure as the lead-story
  decoration (lapsed).
- **Given names cluster** within a stock (three Salvatores in eight months).
- **Escape after the last letter** lands on SEND TO PRESS (UX).
- None of the round-5 fixes has been played by hand yet: round 6 should test the ventures' new pacing with a
  first-time panel again.
