# Panacea · round 11 · playtest by "The Low-Vision Player"

A player with moderate low vision who turns on the largest text a game offers, depends on strong contrast, and tires fast with small print. First time seeing Panacea.

**Verdict:** Panacea is one of the more readable "printed matter" games I have played. The ink-on-newsprint palette gives 11:1 to 16:1 contrast almost everywhere, and at TEXT SIZE: LARGER the letters, the books, the notes and the regulars list are comfortable. The weak spots are the controls that do not grow with the setting (drawer tabs, claim slugs, order buttons, price buttons, all 5–8 px), and LARGER drops information from the proof instead of reflowing it. The fun survived for me: the paper's reply to my choices, the deaths I caused and the inquest that followed held me for ten months.

**Setup**

- Build: frozen copy at `build-r11`, commit `64dc5d8`. Port 47131.
- One game, seed `1131`, `--player --profile low-vision`, no jump-ahead. Played Jan 1885 → Oct 1885 proof (9 sends to press), about 35 minutes of active play from 23:06 to 23:41 UTC.
- Tooling (scratch folder only): `d.sh` (driver wrapper), `t.sh` (targets list), `brew48.sh` (a macro for the same order-and-brew clicks I had already done by hand), `crop.py` (crop and nearest-neighbour zoom), `prof.py` (counts ink per pixel row to get x-height and cap-height, and computes WCAG contrast between the 95th-percentile paper and the 2nd-percentile ink in a box). All screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r11/low-vision/`.
- Text size: LARGER for the whole game, except one round trip through REGULAR and LARGE in the August 1885 proof to compare them (shots 90, 91, 92).

## Run log

| Seed | Months       | What I tried                                                                                                                                                                                                                                               | Result                                                                                                                                                                                                |
| ---- | ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1131 | Jan–Mar 1885 | Honest valerian tonic (water 3, alcohol 2, syrup 1, peppermint ½, valerian 1), 24 a batch, "Soothes the Nerves", 60c. Save-and-quit in Feb. Took up the almanac venture in Mar, bought ½ col ad space, then killed the game with `drive_stop.sh` mid-month | Cash $150 → $154. 12 → 14 regulars. No deaths. Both resumes were exact                                                                                                                                |
| 1131 | Apr–Jun 1885 | Price 60c → 70c, 48 a batch, answered letters (Kehoe, Inspector Quinlan paid $3, Sunday school)                                                                                                                                                            | 52–57 bottles a month, cash $168. Nobody left over the price                                                                                                                                          |
| 1131 | Jun–Jul 1885 | Dishonest turn: ½ oz laudanum added to the recipe, and "Comfort for Teething Babes" set                                                                                                                                                                    | **4 dead** in the June edition (O'Brien 78, Sullivan 67, an unknown young mother, an unknown old man). Suspicion 0 → 21. Inquest: "keep your head down", bottle named, suspicion 45, good name 16 → 4 |
| 1131 | Jul–Oct 1885 | Back to honest: laudanum out, teething claim struck. Second still (earned at 200 bottles). Both stills at 48                                                                                                                                               | 24 → 49 → 53 bottles. Suspicion back down to 34. Cash $190.18. Ventures drawer now offers the shop next door (19 of 25 regulars), a larger copper ($200) and a fourth still                           |

Key moments:

- **Jan edition.** I read it as a newspaper first and only understood it was a game screen when I found "TO THE PROOF ☞" at the bottom right.
- **First proof.** The empty formula book and the notes told me what to do. The game clicked at the still screen (shot 19), when "Click a bottle to mark the batch" turned the cost into something I could see.
- **Mar.** VENTURES tooltip: "New: a venture is on offer." Ad space became available the same month.
- **Jul edition** "THE UNDERTAKER IS BUSY." This was the strongest moment of the game, and also the one that felt least fair (see F3).
- **Aug edition** "THE JURY NAMES THE LABEL." **Sep edition** "MULBERRY STREET KEEPS ITS DISTANCE." The paper remembered.

## Metrics

**Smallest text I had to read to play** (1280×800 screenshots; x-height is lowercase height in px; caps are cap-height)

| Element                                                           | REGULAR                         | LARGE          | LARGER     | Contrast    | Shot         |
| ----------------------------------------------------------------- | ------------------------------- | -------------- | ---------- | ----------- | ------------ |
| NOTES FOR THE PROPRIETOR body (italic)                            | x 6                             | x 6            | **x 8**    | 12.7:1      | 91 / 92 / 90 |
| Counter card symptom line ("neuralgia")                           | x 6                             | x 8            | x 9        | 12.8:1      | 91 / 92 / 90 |
| Counter card risk word "POORLY" / "WEANING OFF IT"                | caps 8                          | caps 10        | caps 12    | 13.6–15.3:1 | 91 / 77      |
| THE WORKS "good name: 5 of 100"                                   | x 6                             | x 7            | x 8        | 12.6:1      | 91 / 92 / 90 |
| Suspicion column "Rose 24: …"                                     | x 6                             | x 9            | x 9        | 13.9:1      | 91 / 90      |
| Edition body text (clippings, Personal)                           | x ≈ 9 (did not appear to scale) |                |            | 15.9:1      | 05, 07       |
| Letters body (italic and roman)                                   |                                 |                | x 9–10     | 13–15:1     | 57, 63, 79   |
| **Drawer tabs** THE STILL / FORMULA / SUPPLIES / VENTURES / BOOKS | caps 8                          | caps 8         | **caps 8** | 15.1:1      | 10, 91       |
| **ALL THE REGULARS ▸** button                                     | caps 8                          | caps 8         | caps 8     | 15.4:1      | 90           |
| **Claim slugs** (SOOTHES THE NERVES …)                            | caps ≈7                         | caps ≈7        | caps ≈7    | 15:1        | 15b zoom, 90 |
| **MORE CLAIMS BELOW ▼ (scroll)**, white on black                  | caps 7                          |                | caps 7     | 13.7:1      | 90           |
| **Supplies +8 / +32** buttons                                     |                                 |                | **≈6**     | 11.4:1      | 86           |
| **Formula −5c / +5c**                                             |                                 |                | ≈6         | 11.5:1      | 11           |
| Price under the ad bottle "70c. the bottle."                      | x 5                             | gone at LARGER |            | 11.2:1      | 91           |
| "▼ more below (scroll)"                                           | x 5                             |                | x 7        | 11.0:1      | 91 / 90      |

**Contrast of grey and faint text.** Ink on paper runs 11:1 to 16:1 almost everywhere. Exceptions:

- Greyed "THE LEAD STORY" heading on the proof: 7.2:1, fine.
- Price figure on the formula's −5c/+5c strip: 7.2:1.
- "THE ALMANAC" pill: 6.9:1 ink to paper. It passes, but a counter figure overlaps it (shot 77).
- Disabled "−" buttons on the Hobart's order blank: **1.70:1** (shot 86). The disabled SEND THE ORDER is 8.7:1 and still reads as disabled by its outline.
- Scheele's green is used only for the bottle and the glass. Nothing I needed was carried by green alone: the green layers in THE GLASS are also labelled in words ("Laudanum ½ oz").

**Information carried by tint or marks:**

- "Gravely ill" is shown in white-on-black type and is also spelled out in the regulars legend: "GRAVELY ILL (white on black): near the end."
- Suspicion is a thin bar under THE WORKS and also printed in words: "SUSPICION 45 OF 100 · ALARMING".
- The still's batch is filled black bottles against outlined ones, with the number printed on the button ("LIGHT THE FIRE · 48").
- The one tint-only cue I found is the faint ghost figure at the right of the counter. It reads as "more regulars", but "and sixteen more regulars" is printed beside it, so nothing is lost.

**Economy and play:**

- Cash at each month's open: $150.00 → 150.24 → 154.44 → 128.08 → 150.82 → 168.66 → 177.30 → 171.66 → 180.40 → 190.18.
- Bottles sold: 19, 12, 52, 57, 50, 50, 24, 49, 53.
- Regulars: 12 → 21 at peak → 19.
- Deaths: 4, all in the one laudanum month. Suspicion peaked at 45 and decayed about 4 a month.
- Clicks to brew a month by hand: about 6 for SUPPLIES → FILL IN FOR 24 → SEND → STILL → LIGHT. With the second still, the "BOTH STILLS · ORDER AND LIGHT 48 EACH" button made it 2.
- Time to understand the loop: about 6 minutes, to the first brew.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Where                                 | How to reproduce                                                                                         | Suggested fix                                                                                                                              |
| --- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Major    | TEXT SIZE does not reach the controls. At LARGER the drawer tabs are still 8 px caps, ALL THE REGULARS is 8 px, the claim slugs about 7 px, MORE CLAIMS BELOW 7 px white-on-black, the Hobart's +8/+32 buttons about 6 px, and the formula −5c/+5c about 6 px. These are the smallest things I have to read to play, and they are smaller than the body text the setting does enlarge (x 8–9). The settings line itself says LARGER is for "the counter cards, the works, the drawers, the jars", but the drawer tabs do not change | Proof right column; Supplies; Formula | Set LARGER, open the proof (shot 90), compare the tab row at y≈636–643 with REGULAR (shot 91): identical | Scale button and tab labels with the setting, or give LARGER a minimum of about 11 px caps for every control                               |
| 2   | Major    | LARGER removes information instead of reflowing it. Compared with REGULAR, the proof at LARGER loses: THE WORKS line "Last month: took $16.80, lost $5.64; rent and charges $9.00."; the "YOUR SPACE · ½ COL · $6.00" ad preview; the type-case lines "Rates: ¼ col $3.00 · ½ col $6.00 …" and "Bolder type sells more, and the coroner reads it too."; and two of the five counter cards (5 → 4 at LARGE → 3). The player who needs large print gets less of the "where the money went" story                                      | Proof, Aug 1885                       | Shots 91 (REGULAR) vs 92 (LARGE) vs 90 (LARGER)                                                          | Let the right column scroll, or move the dropped lines into the drawers. Never hide a line only because of text size                       |
| 3   | Major    | Deaths came with no risk shown on the cards. In June I added ½ oz laudanum. O'Brien and Sullivan were on the counter as "peaky", which the legend defines as "a little pale and off colour, **not yet in danger**". Next edition both were dead, with two more. The only warning was the assay's short Danger bar and "a risk to the frail". After the deaths the notes did warn: "Death waits beside Widow Kathleen Hennessy …". Through the text, the game reads as fair afterwards and unfair before                             | Proof cards, Jun → Jul 1885           | Seed 1131: add ½ oz laudanum in June 1885 and send                                                       | When a recipe change raises danger, put a note in NOTES FOR THE PROPRIETOR naming who the new bottle endangers, and flag them on the cards |
| 4   | Minor    | The suspicion tooltip at LARGER runs off the right edge of the screen and is cut mid-word: "…Rose 1: bold claims on th". Reading the reason suspicion moved depends on it, though the SUSPICION column also has it                                                                                                                                                                                                                                                                                                                  | THE WORKS suspicion strip, hover      | Hover "☞ SUSPICION 45 OF 100 · ALARMING" at LARGER (shot 82)                                             | Wrap tooltips to the window width                                                                                                          |
| 5   | Minor    | The venture text contradicts the books. A LARGER COPPER says "Last month the shop sold twenty-four bottles". The notes the same month say "last month sold forty-nine", and the books say Aug 1885: 49                                                                                                                                                                                                                                                                                                                              | Ventures drawer, Sep 1885 proof       | Shot 101 vs 103                                                                                          | Use the same last-month figure as the books                                                                                                |
| 6   | Minor    | The inquest letter and the verdict disagree on who died. The coroner's letter is "Inquest into the Death of an Unknown Young Mother". The edition's Coroner's Court says "The jury sitting upon the infant child of an unknown young mother"                                                                                                                                                                                                                                                                                        | Letters Jul 1885 / Aug edition        | Shots 79, 89                                                                                             | Make the verdict name the same deceased                                                                                                    |
| 7   | Minor    | At LARGER the NOTES box shows about 3 lines, so "who is at risk" is often below the fold behind "▼ more below (scroll)". On the July proof the risk note was the second item and half hidden                                                                                                                                                                                                                                                                                                                                        | Proof, centre top                     | Shots 77, 88                                                                                             | Let NOTES take the empty LEAD STORY space at LARGER, or put risk notes first                                                               |
| 8   | Low      | Disabled "−" buttons on the order blank are 1.70:1, almost invisible. I could not tell at a glance which rows had an order except from the ORDERED column                                                                                                                                                                                                                                                                                                                                                                           | Supplies                              | Shot 86                                                                                                  | Raise the disabled ink to about 3:1                                                                                                        |
| 9   | Low      | A hovered reply button grows and shifts left by about 10 px, so text under my magnifier moves                                                                                                                                                                                                                                                                                                                                                                                                                                       | Morning's Post                        | Hover the second reply (shot 100)                                                                        | Hover by inverting the fill only, not by resizing                                                                                          |
| 10  | Low      | The "THE ALMANAC" sign on the counter is overlapped by the first figure (the undertaker, once deaths begin)                                                                                                                                                                                                                                                                                                                                                                                                                         | Counter, left                         | Shots 77, 88                                                                                             | Keep figures clear of the sign                                                                                                             |
| 11  | Low      | The type case keeps an old scroll position: after I scrolled the claims, the NONE / ¼ COL row was left half cut off at the top on later proofs                                                                                                                                                                                                                                                                                                                                                                                      | Type case                             | Shots 63, 77                                                                                             | Reset the scroll at the start of each month, or pin the space row                                                                          |

## What works

- **Contrast.** 11–16:1 nearly everywhere, black ink on warm paper, and it never relies on Scheele's green. Keep this.
- **States are words, not tints:** "POORLY", "ASKS BY NAME", "WEANING OFF IT", "1 POORLY · 5 CRAVING", "SUSPICION 41 OF 100 · ALARMING". The regulars legend explains even the one inverse-type cue in words.
- **The letters and THE BOOKS at LARGER** are the best-set screens: x 9–10, and each reply carries its exact odds ("Pencilled in the margin: about 9 in 10 for naming"). WHERE AUGUST 1885'S MONEY WENT answered every money question I had.
- **The paper remembered.** "THE UNDERTAKER IS BUSY" → "THE JURY NAMES THE LABEL" → "MULBERRY STREET KEEPS ITS DISTANCE" → "TRADE GOOD ON MULBERRY STREET". A customer who left "will not take another spoonful". The Personal column credits "the testimonials in the window".
- **The still's "BOTH STILLS · ORDER AND LIGHT 48 EACH"** saves a tired player a dozen small clicks.
- **The front door.** SAVE, AND QUIT → relaunch → CONTINUE put me on the Feb 1885 proof with the same cash ($150.24), shelf and price. After `drive_stop.sh` mid-month in March, CONTINUE restored even the mid-month work: venture taken, ½ col ad set, stock ordered, still lit, cash $105.88. Both resumes landed on the proof. The title page shows "May 1885 · $150.82 · 19 regulars" under CONTINUE. "A NEW SHOP? PRESS AGAIN / The shop on the books will be lost." is a kind guard.
- **Honest and dishonest shops are both real choices.** Honest: 53c. clear a bottle, slow growth. Laudanum: "a good, steady seller" with Habit "most will come back", and the bill came due in bodies, an inquest and good name 16 → 4.

## Disclosures

- No jump-ahead. One game, seed 1131, played by hand. `brew48.sh` replays the same order-and-brew clicks I had already made by hand twice.
- I used `targets` and `read` to find click coordinates and to quote long note text exactly (the July notes, the suspicion tooltip, the ventures text). I used `state` to record cash, suspicion and deaths for the metrics table. Every finding is from screenshots.
- The first time I opened SETTINGS on this profile it already showed "TEXT SIZE: LARGER". I don't know whether that is the default or left over from earlier.
- I cannot hear: the "SOUND: THE SHOP HEARD" toggle was left on. The driver log showed only audio-driver failures and Godot leak warnings when I stopped the game. There were no exceptions in play.
- I did not see a holiday marked in the parts of the editions I read (Jan–Sep 1885). I did not read every column, so this is not a verified absence.
- Rector Garvey wrote twice (Sunday school excursion in June, parish bazaar in September). The second letter did not mention my first gift.

## First hour

- **0:00 Title page.** Big, high-contrast buttons. SETTINGS → TEXT SIZE cycles REGULAR / LARGE / LARGER with the line "Press for larger small print: the counter cards, the works, the drawers, the jars." I kept LARGER.
- **0:02 A NEW SHOP.** The first edition opens. I read the Gazette as flavour and thought the game was "read the paper". Clicking a clipping ("AMONG THE CALLERS.—…") opened it on its own (shot 09).
- **0:05 TO THE PROOF.** A wall of panels. NOTES FOR THE PROPRIETOR told me the shelf was empty. The drawer tabs (THE STILL … BOOKS) were the hardest thing on the screen to read, at 8 px caps.
- **0:08 FORMULA.** Clicked jars to pour, and THE ASSAY's word labels ("a warm wallop", "they sleep") are friendlier than numbers. ENTER IT IN THE BOOK.
- **0:10 SUPPLIES.** FILL IN FOR 24 BOTTLES is the thing a tired eye wants. Then THE STILL. **Clicked here**: the bottles-as-batch picture.
- **0:12 First send to press.** Seven seconds of press, then "Strangers at the counter".
- **0:14 Save-and-quit through the menu.** Relaunched, CONTINUE, back on the same proof.
- **0:17 March.** VENTURES lit up ("New: a venture is on offer"), took the almanac, bought ½ col. Killed the game; CONTINUE restored everything mid-month.
- **0:22 June.** Got greedy: laudanum and teething babies. **0:24** "THE UNDERTAKER IS BUSY": four dead, and only then did the cards and notes speak of death.
- **0:27 Inquest.** The odds are printed on each reply, which is exactly what a low-vision player needs instead of hunting for a hidden rule.
- **0:30** Switched sizes to compare, found the dropped lines at LARGER (F2). **0:35** Back to honest; suspicion decays; Oct proof; stopped.

## Score

**7 / 10.** A readable, high-contrast paper game whose story of consequences survives large print. It loses points because the controls stay small at LARGER and because LARGER hides some of the money-and-risk lines. One point up: make every button and tab scale with TEXT SIZE, and never drop a line at LARGER, which fixes F1 and F2 together.
