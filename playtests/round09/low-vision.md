# Panacea · round 9 · playtest by "The Low-Vision Reader"

A player with moderate low vision who uses large text wherever a game offers it, relies on strong contrast, and tires quickly on dense small print. New to the game.

**Verdict:** The core of Panacea reads well. The ink-on-newsprint palette gives body text roughly 7:1 to 11:1 contrast, and headlines, cash, the letters and the pop-up clippings are large and bold. The trouble is the second tier of information. A customer's health, the suspicion level, jar prices, ad rates and the small print of the ventures are all set in 5–6 px x-height italic, with no text-size, zoom or contrast setting to fall back on. The fun survives for me, because the paper is funny and the loop is clear, but I played leaning in, and my eyes were tired by month eight.

**Setup:**

- Build: frozen copy at `scratchpad/build-r9`, commit `49d5c40`.
- Port: 47094.
- Game 1: seed 9094, `--player --profile low-vision`, played by hand from the first edition (Jan 1885) to the Nov 1885 proof. That is 11 sends to press, the last one made after a relaunch.
- Game 2: seed 9095, `--play 18 --dose 1.5`, a jump-ahead for coverage of deaths and alarming suspicion, played for 1 send.
- Time: about 50 minutes of active testing.
- Tooling, all in `scratchpad/r9/low-vision/`:
  - `crop.py`: crop and nearest-neighbour zoom.
  - `lum.py`: WCAG contrast between the 2nd-percentile ink and the 98th-percentile paper in a rectangle.
  - `rows.py` and `xh.py`: per-row ink counts, giving the ink height and x-height of a text line in pixels.
  - `d.sh` and `t.sh`: wrappers for the driver.
- All screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r9/low-vision/`, and the numbered paths below refer to that folder.

## Run log

| Seed        | Months       | What I tried                                                                                                                                                                                       | Result                                                               |
| ----------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| 9094        | Jan–Mar 1885 | Read edition 1. Opened the formula and entered the starting recipe at 60c. Set claims SOOTHES THE NERVES and PURIFIES THE BLOOD. Ordered and brewed 24. Printed Mrs. Kelly's testimonial.          | Sold 17 a month. Cash went $150.00 → $149.76.                        |
| 9094        | Mar 1885     | Used SAVE, AND QUIT from the menu, relaunched, pressed CONTINUE.                                                                                                                                   | Same shop: $144.84, 12 regulars, 24 brewing, letter answered.        |
| 9094        | Apr–May 1885 | Bought ¼ col, then ½ col. Removed the Gentian from the recipe. Brewed 48. Killed the game with `drive_stop.sh` mid-May, after ordering and lighting the still.                                     | CONTINUE restored it exactly: $153.84, ½ col, still lit.             |
| 9094        | Jun–Aug 1885 | Answered the Board of Pharmacy, the Captain's Outing, the Pedestal Fund and the Benefit Society. Took up **The Proprietor's Almanac** ($40, June). A second still arrived in Aug, and I brewed 96. | Sold 46 → 58 → 62. The shelf ran bare in July. 24 regulars.          |
| 9094        | Sep–Nov 1885 | Took up **The Shop Next Door** at 25 regulars. Ordered the gilt sign. Raised the price from 60c to 70c. Agreed to Dr. Fenn's arrangement.                                                          | Sold 68 → 70 → 71. Cash $199.34, 32 regulars, no deaths.             |
| 9095 (jump) | Jul–Aug 1886 | Looked at a shop the script had run heavy (1.5 oz) for 18 months. Answered 2 letters and brewed 48.                                                                                                | "DEATH OF MRS. KELLY". suspicion "alarming (47 of 100)", standing 1. |

**Timeline of key moments:**

- Mar 1885: first letter.
- Apr: the first "Death waits beside Widow Honora Callahan" hint, and the skeleton appears beside her at the counter (42).
- Apr: the ad space unlocks.
- Jun: first venture taken.
- Aug: the second still is mine (79–81).
- Sep: The Shop Next Door taken (90).
- No deaths in game 1.

## Metrics

**Text size.** Measured from 1280×800 screenshots with `xh.py`. Ink height is ascender to descender. x-height is the height of the dense lowercase band.

| Text                                                           | Where (screenshot)        | Ink height                       | x-height |
| -------------------------------------------------------------- | ------------------------- | -------------------------------- | -------- |
| Customer card status, "peaky · delighted" / "poorly · shaking" | counter cards, y≈718 (79) | 12 px                            | **5 px** |
| "set after press", "dele (strike out)"                         | proof, left column (79)   | 11 px                            | **5 px** |
| Ad rates, "Rates: ¼ col $3.00 · …"                             | type case, y≈125 (79)     | 12 px                            | **5 px** |
| Jar amounts and prices, "3 oz in the glass", "3c./oz"          | drug shelf (47)           | 11 px                            | **5 px** |
| Glass labels, "Valerian Root ½ oz"                             | the glass (47)            | 11 px                            | **5 px** |
| Ad footers, "Her complexion was the envy…"                     | edition ads (07, 76)      | 11 px                            | **5 px** |
| "60c. the bottle" under my own ad                              | proof ad (66)             | 8 px                             | **5 px** |
| suspicion / standing lines                                     | THE WORKS (102)           | 13 px                            | 6 px     |
| "the still is cold", "2 poorly · 5 craving"                    | THE WORKS (79)            | 10 px                            | 6 px     |
| Venture "So far: …" lines                                      | ventures (69)             | 13 px                            | 6 px     |
| Edition body (Personal, City Items)                            | edition (05)              | ~13 px                           | 6 px     |
| THE TRADE body                                                 | edition (78)              | 15 px                            | 7 px     |
| Footer status bar                                              | footer (06)               | 17 px                            | 7 px     |
| Price "on the shelf at 70c."                                   | formula (91)              | 15 px                            | 7 px     |
| Cash in THE WORKS, "$241.64"                                   | (102)                     | cap ≈ 18 px, bold lining figures | —        |

At least 7 kinds of information sit at a 5 px x-height, which is about a 10–11 px font. Two of them, the card health line and the jar prices, are things I needed every month.

**Contrast.** WCAG ratios from `lum.py`:

| Pair                                                                    | Ratio         |
| ----------------------------------------------------------------------- | ------------- |
| Edition body ink on newsprint, (49,47,46) on (239,236,228)              | **11.3:1**    |
| Footer status, light on black                                           | 15.8:1        |
| Hints under THE WORKS, light italic on black                            | 10.7–11.9:1   |
| THE WORKS small italics                                                 | 9.8–10.2:1    |
| Proof masthead italic and card names                                    | 8.8:1         |
| Ad italic footers, grey ink (79,77,75)                                  | 6.9–7.3:1     |
| Customer card status line                                               | 6.2–7.1:1     |
| Disabled LIGHT THE FIRE (grey)                                          | 4.5:1         |
| Disabled ad-space buttons in month 1 (grey)                             | **3.6:1**     |
| Side panels behind an open drawer (type case, THE WORKS)                | **2.4–3.2:1** |
| Marked bottle (pale green fill) against an unmarked bottle in the still | **1.15:1**    |

**Settings.** The settings page has two options and nothing else: THE WINDOW and SOUND (02, 03). There is no text size, zoom, UI scale, high contrast, plain (non-italic) font or reduced-texture option.

**Clicks and time.**

- It took about 4 minutes to realise THE STILL had to be ordered _and_ lit as well as the formula entered.
- Reaching a venture by hand took 6 sends, to June 1885 (the Almanac at $40).
- The Shop Next Door took 9 sends, at 25 regulars.
- Cash over the first game: Jan $150.00 → Mar $144.84 → May $161.04 → Jul $141.04 (after the $40 Almanac) → Sep $175.44 → Nov $199.34. Zero deaths.
- Price test: raising 60c → 70c in Sep did not dent sales (68 → 70 → 71) or arrivals (+5, +4 customers). Within that range, I could not see that price mattered.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                    | Where                                                                                  | How to reproduce                                   | Suggested fix                                                                                                                                                                                                              |
| --- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | No text-size, zoom or contrast option anywhere. SETTINGS has only THE WINDOW and SOUND, and the window is a fixed 1280×800 layout. For a low-vision player this is the single biggest barrier.                                                                                                                                                | Title → SETTINGS (02), pause menu → SETTINGS                                           | Open SETTINGS.                                     | Add a "TYPE: SMALL PICA / LONG PRIMER / GREAT PRIMER" (text scale) setting that scales at least the proof side panels, the cards and the drawers by 125–150%. Add a "PLAIN INK" option that drops italics and paper grain. |
| 2   | Major    | A customer's health, the thing that predicts a death, is shown only as a 5 px x-height italic word on each card ("peaky · delighted", "poorly · shaking", "peaky · ruined by it"), at 6–7:1 contrast. Most regulars are hidden behind "and nineteen more regulars", in even smaller italic.                                                   | Counter cards, y≈718 (54, 79, 102)                                                     | Any proof screen.                                  | Give health a large glyph or bar on each card, such as a filled / half / empty heart or a tally, in solid black. Make the card status line at least 8 px x-height and roman, not italic.                                   |
| 3   | Major    | Suspicion uses the same 6 px italic line whether it reads "none to speak of (0 of 100)" or "alarming (47 of 100)". Nothing grows, bolds, inverts or changes shape as danger rises. On a monochrome screen, size and weight are the only channels left, and they are not used.                                                                 | THE WORKS, right of the cash (79 vs 102; zoom `z-suspicion.png`)                       | Jump-ahead `--play 18 --dose 1.5`, open the proof. | Above a threshold, set suspicion as a black inverted slug ("SUSPICION · ALARMING") at cash size, or add a gauge beside the cash.                                                                                           |
| 4   | Major    | The prices and amounts that make up a recipe (e.g. "3c./oz", "0.5 oz in the glass", "$2.50/oz · by the ⅛") are 5 px x-height italic under each jar, and the glass labels are just as small. I had to hover each jar to read its tooltip, which is a comfortable size (11, 46).                                                                | FORMULA → THE DRUG SHELF and THE GLASS (10, 47)                                        | Open FORMULA.                                      | Raise the jar sub-labels to body size. Give the glass a plain list of ingredients and ounces in the assay column.                                                                                                          |
| 5   | Minor    | The old-style (hanging) figures make "1" look like a small-cap "I" and "0" look like "o". Examples: "standing in the ward: 1" reads as "I", "$3.00" reads "$3.oo" and "$19.80" reads "$I9.8o". This happens in the ledger, the trade report, the footer and the letters. Only the big cash in THE WORKS uses lining figures.                  | THE WORKS (zoom `z-suspicion.png`), ledger (85, zoom `z-oldstyle.png`), THE TRADE (78) | Read any number in body text.                      | Use lining figures for every number the player acts on (cash, prices, counts, standing, suspicion). Keep old-style figures for flavour text.                                                                               |
| 6   | Minor    | An open drawer dims the side panels to 2.4–3.2:1, so I could not check cash or suspicion while reading a letter that costs money. Example: the letter asks for "$50", and THE WORKS behind it is grey on grey.                                                                                                                                | Any drawer (33, 63, 81)                                                                | Open THE MORNING'S POST.                           | Dim less, to about 4.5:1, or repeat the cash and suspicion in the drawer header ("cash in hand $199.34"). The footer does carry cash, but in 7 px italic.                                                                  |
| 7   | Minor    | In THE STILL, marked and unmarked bottles differ only by a pale green tint, 1.15:1 against the paper (zoom `z-bottles.png`). The sentence "You have marked 48" saves it, but the grid alone is unreadable for me.                                                                                                                             | THE STILL, batch grid (16, 80)                                                         | Open THE STILL and mark fewer than 48.             | Give marked bottles a solid black fill or a black label band, and leave unmarked ones as outlines.                                                                                                                         |
| 8   | Minor    | The drawer tabs move. THE STILL / FORMULA / SUPPLIES / VENTURES sit at y=531, y=524 or y=509 depending on how tall the letters box is that month. The type-case claim list is clipped, with BANISHES RHEUMATISM !! half cut off (102, 107), and the only scrollbar is a thin grey line at x≈1253. I rely on things staying in the same place. | Proof, right column (79 vs 102 vs 107)                                                 | Compare a month with no letters to one with two.   | Fix the panel heights so the drawer tabs never move. Make the type case's overflow obvious, with "MORE CLAIMS ▼" or a thicker rule.                                                                                        |
| 9   | Minor    | The price did not seem to matter. Going 60c → 70c cost nothing visible, and the paper said only "up to 70c. from 60c." in THE MARKETS.                                                                                                                                                                                                        | Formula ± 5c (91), editions 92–96                                                      | Raise the price 10c.                               | Have THE TRADE say whether callers balked at the price, so its effect can be read.                                                                                                                                         |
| 10  | Low      | Disabled controls are grey on newsprint (3.6:1 for the ad buttons in month 1, 4.5:1 for LIGHT THE FIRE). They are readable, but at a glance they look much like enabled ones.                                                                                                                                                                 | Type case (14), THE STILL (80)                                                         | Month 1 type case.                                 | Strike through disabled buttons or hatch them, as well as greying them.                                                                                                                                                    |
| 11  | Low      | "New: another still is yours" exists only as a hover tooltip on THE STILL. On screen, the only cue was a slightly bolder tab border.                                                                                                                                                                                                          | Proof, Aug 1885 (79)                                                                   | Sell 200 bottles.                                  | Put it in the hint list under THE WORKS, where the other hints go.                                                                                                                                                         |
| 12  | Low      | The Board of Health letter's subtitle says "A printed circular, the same every June" but is dated "New-York, July 22nd, 1886".                                                                                                                                                                                                                | Morning's Post, game 2 (103)                                                           | Jump-ahead to Jul 1886.                            | Say "every July", or date it June.                                                                                                                                                                                         |
| 13  | Low      | THE MARKETS strip is word for word the same every month ("Morphine, $2.50 the ounce … Coffins, pine, $4.00. Lilies, plentiful."). The display ads (Mrs. Whittle, Madame de la Rose, Grimsby's belt) also repeat. Personals, weather and city items were fresh each month.                                                                     | Foot of every edition (31, 41, 53, 61, 72, 78)                                         | Scroll to the bottom of any two editions.          | Vary one or two market lines a month.                                                                                                                                                                                      |
| 14  | Low      | The drug-shelf panel runs past the drawer frame: "Dilute Sulphuric …" is cut off at the bottom edge.                                                                                                                                                                                                                                          | FORMULA (47, 91)                                                                       | Open FORMULA.                                      | Shorten the shelf grid or let the panel scroll.                                                                                                                                                                            |

No exceptions or script errors appeared in `/tmp/panacea-driver-47094.log` in either game. The only noise was ALSA audio-driver warnings.

## What works

- **The essentials are large.** Masthead, headlines ("NEVER SO MANY BOTTLES SOLD", "DEATH OF MRS. KELLY"), SEND TO PRESS, the drawer headers, the cash in THE WORKS and the letter reply buttons are all bold and black. I never misread a headline.
- **Body contrast.** Black ink on warm newsprint gives about 11:1. The paper grain is faint enough not to break up letterforms.
- **Pop-ups are the right size.** The clipping cards (84, "GONE FROM THE SHOP · Mr. Jacob Rosen … Got better on their own") and the ledger (85) use type about 50% larger than the paper, with short lines. Please keep this, and use the same size for more.
- **Letters read comfortably.** Roman body at about 7–8 px x-height, each choice in a black-ruled box, and the consequence printed under each choice ("$5; standing +1: a framed certificate behind the counter."). I could always tell what a reply would do.
- **Death is visible at the counter.** The skeleton in the top hat standing beside a frail regular (42, 102) is unmistakable at any size. It is the one danger signal that works for me, so extend that approach to suspicion.
- **Selected states.** Selected claims and the chosen ad size are solid black with reverse type (`z-adsize.png`), and the keyboard focus gets a double black border. These are clear without colour.
- **Hints in the dark gutter.** Lines such as "Death waits beside …" and "Nothing on the shelf and nothing on the still: light THE STILL" are high contrast (about 11:1). They are small italic, though.
- **Saving.** Both SAVE, AND QUIT and a hard kill mid-month restored exactly the shop I left. The title page shows "March 1885 · $144.84 · 12 regulars" under CONTINUE, which is a good, readable reassurance.
- **The paper agreed with my books every month.** Example: Jul 1885 "Sixty-two bottles sold" = "50 sold over the counter" + "By post … twelve bottles". The cash in THE TRADE always matched the footer.
- **Ventures were worth it.** The Almanac ($40) showed up next month as "ten by the almanac" callers and "twelve bottles, $7.20" by post, so it paid for itself in about 4 months.
- **Period details.** The Pedestal Fund letter in July 1885, and the joke in the paper's spelling of the testimonial: Mrs. Kelly asked "with my name, which I should like spelled correctly", and it was printed "Mrs. Bridget Klely".

## Disclosures

- Game 2 was a jump-ahead (`--play 18 --dose 1.5`, seed 9095), not saved and not played from the start.
- I used `targets` to find click coordinates and button tooltips. I used `state` to confirm that the cash and stills after CONTINUE matched before and after the kill. A real player would compare the screen, which also matched.
- All size and contrast numbers come from my own pixel scripts on the screenshots, not from the source. I did not read the source, `docs/` or `playtests/`.
- I cannot hear, so I make no claims about sound.

## First hour

- **0:00 Title page (01).** "PANACEA" is huge and the content note is readable. I wanted a text-size option first, and SETTINGS has only window and sound (02). Disappointing.
- **0:03 Edition 1 (05–07).** The headline "A NEW CONCERN / On Mulberry Street" is easy to read. The columns of Wants and Personals are tiring at 6 px x-height, so I read the left column and skimmed the rest. I took the AMONG THE CALLERS items to be my first customers (they were).
- **0:06 Proof (08).** The page is busy. I found SEND TO PRESS at once, but the four drawer tabs (THE STILL / FORMULA / SUPPLIES / VENTURES) are small 22 px-high buttons. I thought the lead-story box was something I had to fill in. Its grey "THE LEAD STORY … The editor sets it when the month is out" looks like an input.
- **0:09 FORMULA (10–13).** I could not read the jar prices without hovering, but the tooltips are readable. The assay bars are clear black-on-grey with a word at the right ("harmless", "a warm wallop"). I entered the starter recipe.
- **0:12 Type case (14).** I understood claims as "bolder type sells more, and the coroner reads it too." The ad sizes were disabled, and I only learned why from the small italic "Not yet: the Gazette wants $100 in savings and a printed testimonial first."
- **0:14 THE STILL (16–18).** **The click:** order, then LIGHT THE FIRE. I would not have known the 24 bottles were marked without the sentence below the grid.
- **0:18 Edition 2/3 (22–31).** THE TRADE section is where my books are. Its numbers are body-size old-style figures, and "$149.76" vs "$I49.76" made me squint.
- **0:21 First letter (33–34).** Mrs. Kelly's grateful letter. The reply consequences printed under each choice are excellent. Printing it as a testimonial unlocked advertising, and I understood the chain.
- **0:23** SAVE, AND QUIT, relaunch, CONTINUE: exactly where I left off (36–38).
- **0:27 April proof (42).** A skeleton is standing beside Widow Callahan. That was the most legible warning in the game, and it made me cut the Gentian.
- **0:33** Hard kill mid-May and CONTINUE: identical (57–58).
- **0:36** Board of Pharmacy and the Captain (63–64): real dilemmas with their costs stated. Took up the Almanac (67–69).
- **0:45** Second still, BOTH STILLS · ORDER AND LIGHT (80–81). The game had started to flow, but I was reading each proof with my face close to the screen.

## Score

**6 / 10.** It is a witty, coherent game whose headline layer and letters are readable, but too much of what I act on is 5–6 px italic, and danger is shown by words rather than by size or weight. Adding a text-scale setting that enlarges the cards, THE WORKS and the drawer sub-labels by about 1.5× would move it a point. Making suspicion and health visually escalate (bold, inverted, glyphs) would move it another.
