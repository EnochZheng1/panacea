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

## Next fixes, in order

1. **Bring the second act within reach of a hand player.**
   - Cheaper first steps (a smaller branch, a smaller wholesale order), with costs that scale with the shop.
   - Progress on each card ("1,120 of 1,500 bottles").
   - Make the almanac pay and make the wholesale contract a trade-off.
   - Tune against a "sloppy" bot that plays like the testers did, not the clean adaptive bot.
2. **The standing matter.**
   - Dated items run once.
   - Seasonal items carry their months, and weather items follow the weather box.
   - The rotation steps by the number of items actually printed.
   - A larger Wants pool, read in rotation.
   - Grant's death and funeral on their dates.
3. **The venture-month lead.** The slump test should ignore investment outlays, and "SOLD AGAIN" should need sales
   that held.
4. **Keyboard.** Trap the arrow keys in drawers. Open VENTURES on its first card, not the purchase, and guard the
   purchase. Show focus on counter tags clearly, or take them out of Tab and give the counter one stop.
5. **The routine month.** The still remembers its batch, and one order can serve both stills. Fix the WORKS box's
   height so the buttons stay put. Scroll or wrap the type case so ad space is always visible.
6. **Copy.**
   - The bribe outcome is stated truthfully.
   - Infants are the ones harmed by soothing syrup.
   - Wholesale bottles are reported apart.
   - The sound toggle says "SOUND".
   - The prospectus is set as one sheet.
