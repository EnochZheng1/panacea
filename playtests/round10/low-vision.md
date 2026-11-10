# Panacea · round 10 · playtest by "The Low-Vision Player"

A player with moderate low vision who turns large text on wherever it exists, depends on strong contrast, and tires fast on small print. First time seeing the game.

**Verdict:** At LARGER, the reading surfaces (the Gazette, the letters, the Books, the IN MEMORIAM clippings) are good: about 9 px x-height, black ink on newsprint at about 13:1. I could read them all evening, and the fun is in them. The setting does not reach the parts of the proof that tell you who is about to die and why. The regulars' cards, NOTES FOR THE PROPRIETOR and the deaths column stay at 5–6 px x-height at every size, and the addiction state ("shaking", "ruined by it", "craving") is the smallest print in the game. Suspicion is well handled: once it matters it grows into a full-width inverted banner. A customer's danger reaches me only through the skeleton figure at the counter.

**Setup:**

- Build: frozen copy at `scratchpad/build-r10`, commit `f988c58`. Port 47111.
- Game 1 (first session, by hand): seed 1910, `--player --profile low-vision`. Text size LARGER from the title page on; REGULAR checked for comparison. 12 months, Jan 1885 to Jan 1886.
- Game 2 (jump-ahead, not saved): seed 2207, `--play 24 --dose 1.5`. Two months at REGULAR, then LARGER.
- About 50 minutes of active testing.
- Tooling, all in `scratchpad/r10/low-vision/tools/`:
  - `lv.py`: nearest-neighbour crop and zoom; x-height estimate (rows where the dark-pixel count is at least 40% of the peak row); WCAG contrast, ink against median background.
  - `tg.sh`: lists targets.
  - `jar.sh`: clicks a jar by name, because the shelf moves after each pour.
  - `press.sh`: sends to press and dumps the state.
- Screenshots: `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r10/low-vision/NN_*.png`. Zoomed crops are `z_*.png`.

## Run log

| Seed                               | Months            | What I tried                                                                                                                                                                | Result                                                                                                                                                                                                |
| ---------------------------------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1910 (player)                      | Jan–Mar 1885      | Mild herbal recipe (valerian, gentian, peppermint, syrup, spirit). "Soothes the Nerves". Price 60c, then 50c. 24 bottles a month.                                           | 14, 6, 17 bottles sold. Cash $150.00 → $146.52. One death (Widow Fontana, natural). Printed a testimonial.                                                                                            |
| 1910                               | Apr–Jun 1885      | Ad space unlocked; bought ½ col ($6). Price 40c. Gilt sign. Brewed 36–48. Save-and-quit through the menu, then CONTINUE.                                                    | 24, 44, 33 sold. Regulars 13 → 18. Cash drifting down ($133.62).                                                                                                                                      |
| 1910                               | Jul–Sep 1885      | Second remedy "Quieting Cordial" with 1 oz laudanum ("hard to give up", "dangerous to anyone weak"). Claims: Soothes the Nerves and Comfort for Teething Babes. 24 bottles. | Mr. Wieczorek (81) "Died of an overdose… The register blames the bottle." Inquest; I kept my head down, and the jury found nothing. Second still at 200 bottles sold. Never brewed the cordial again. |
| 1910                               | Oct 1885–Jan 1886 | Both stills at 48. Scale fee, physician's commission, Jersey spirit. **Took THE SHOP NEXT DOOR** in Nov 1885 (month 11). Landlord raised the rent the next month.           | 38, 54, 64, 62 sold. Regulars 29. Cash $110.92. Two deaths: Kelly (consumption, not blamed) and Ten Eyck (inquest, I testified, cleared).                                                             |
| 2207 (jump `--play 24 --dose 1.5`) | Jan–Feb 1887      | Looked at how danger reads in a heavy-laudanum shop, at REGULAR and at LARGER.                                                                                              | Suspicion 37 → 46 ("ALARMING"). Fontana and Shea died; "The register blames the bottle."                                                                                                              |

**Timeline of key moments**

- Jan 1885: found TEXT SIZE in SETTINGS on the title page within a minute and set LARGER. It applies at once and persisted across relaunches.
- Mar 1885: after a price cut only 6 bottles sold, and I could not see why. The answer was in the Books, which I hadn't opened yet.
- Apr 1885: "TO ADVERTISERS.—The Gazette is pleased to announce that the proprietor … is now in a position to buy space". The ad-space buttons, greyed at 2.5:1 until then, went live.
- Jul 1885: the laudanum cordial killed a regular in its first month. The clipping said so plainly.
- Nov 1885: first venture, the shop next door (nothing down, $3 a month), about 35 minutes in.
- Dec 1885: the landlord wrote, "I see the trade is good, and I am glad of it. From this month the rent is a dollar more". He remembered.

## Metrics

**Smallest text you must read to play.** x-height in px from 1280×800 screenshots, measured at LARGER unless marked.

| Surface                                                                                       | x-height LARGER   | x-height REGULAR | Contrast   | Notes                                                                   |
| --------------------------------------------------------------------------------------------- | ----------------- | ---------------- | ---------- | ----------------------------------------------------------------------- |
| Gazette body (edition)                                                                        | 9                 | 7                | 13.2:1     | `09_ed1_c.png`, `200_jump_ed.png`                                       |
| Letters body / reply consequences (italic)                                                    | 9 / 8             | –                | ~13:1      | `203_jump_letter.png`                                                   |
| Books, clippings (IN MEMORIAM)                                                                | 9                 | –                | ~13:1      | `96_books8.png`, `206_clip_fontana.png`                                 |
| THE WORKS main lines / "Last month: took…"                                                    | ~10 / 6           | –                | 8:1        | `95_inquest_sent.png`                                                   |
| Drawer buttons (THE STILL … BOOKS)                                                            | 8 (caps)          | 8                | high       | Only 22 px tall                                                         |
| **NOTES FOR THE PROPRIETOR** ("Death waits beside Mrs. Angela Fontana…")                      | **6**             | **6**            | ~10:1      | Same at both sizes: `201` vs `202_jump_proof_larger.png`                |
| **Regulars' cards**: name / ailment / health / habit italic                                   | **7 / 6 / 6 / 5** | same             | 6.9–7.9:1  | Only 361 changed pixels between REGULAR and LARGER (`z_jump_cards.png`) |
| **Deaths column on proof** ("Found insensible after a dose: the register blames the bottle.") | **5**             | 5                | 7.5:1      | `207_jump_proof2.png`                                                   |
| "set after press", "No deaths last month." (grey italic)                                      | 5–6               | 5–6              | **4.67:1** | `10_proof1.png`                                                         |
| Struck-out Personal names (strike line through 4–6 px text)                                   | 4–6               | –                | 6.6:1      | `z_deaths_col.png`                                                      |
| Small print in rival ads (edition)                                                            | –                 | 5                | 4.83:1     | `200_jump_ed.png`                                                       |
| Disabled ad-space buttons, Jan–Mar                                                            | 8                 | –                | **2.51:1** | `20_entered.png`                                                        |
| Disabled "TAKE IT UP · $200.00"                                                               | ~8                | –                | 4.0:1      | `102_ventures9.png`                                                     |
| Footer status line (inverted)                                                                 | 8                 | –                | 16.4:1     | –                                                                       |

**Colour**

- Green glass in the formula drawer against paper: 1.4:1 (light) and 2.2:1 (dark). The layers are also hatched or dotted, and every layer is labelled in text, so nothing depends on the tint.
- Assay bars: black fill against a grey track, about 14:1, with text labels ("a warm wallop", "would find it").
- The green bottle is never the only carrier of information.

**Danger at a glance**

| Signal              | Low state                                                                           | High state                                                                                                                                  | Readable at a glance?                      |
| ------------------- | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------ |
| Suspicion           | "suspicion: none to speak of (2 of 100)", lowercase 6 px, with a 2 px sliver of bar | "SUSPICION: A GOOD DEAL · 37 OF 100" boxed; "☞ SUSPICION 46 OF 100 · ALARMING" as a full-width inverted banner (`207`), with a good tooltip | Yes, once it matters                       |
| A customer's health | "peaky", lowercase 6 px                                                             | "POORLY" caps; "GRAVELY ILL" inverted chip (10.7:1, 8 px caps); a skeleton figure beside that customer at the counter                       | Partly: the skeleton yes, the card text no |
| Habit / addiction   | –                                                                                   | "· craving", "· shaking", "· ruined by it", "· asks by name": 5 px lowercase italic; "shak…" truncated                                      | No                                         |

**Other measures**

- Cash, game 1, month by month: $150.00, 150.96, 146.52, 152.02, 148.36, 138.30, 133.62, 130.68, 132.00, 120.44, 132.04, 120.88, 110.92.
- Bottles sold: 14, 6, 17, 24, 44, 33, 45, 48, 38, 54, 64, 62.
- Regulars: 12 → 29. Deaths: 4, one blamed on my bottle.
- Time to first venture: month 11, about 35 minutes. "A LARGER COPPER" ($200) was never affordable.
- Clicks to brew once both stills existed: 2 (THE STILL, then "BOTH STILLS · ORDER AND LIGHT 48 EACH"). Good.
- Driver log: only ALSA noise and resource-leak lines at exit. No exceptions.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                                                                        | Where                                                 | How to reproduce                                                                                             | Suggested fix                                                                                                                                                                 |
| --- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | TEXT SIZE LARGER leaves the danger text at 5–6 px x-height: NOTES FOR THE PROPRIETOR ("Death waits beside Mrs. Angela Fontana: gravely ill, in want of the bottle. Also at risk: …"), the regulars' cards, and the deaths column ("Found insensible after a dose: the register blames the bottle."). A diff between REGULAR and LARGER shows almost no change on the cards. These are exactly the "who is at risk and why" lines. | Proof page: centre column, counter cards, left column | Jump game, `201_jump_proof.png` (REGULAR) vs `202_jump_proof_larger.png` (LARGER). Crop x 80–785, y 660–735. | Scale these blocks with the setting. Let the cards grow to 3–4 lines. Move NOTES to a drawer or panel with room to grow instead of shrink-fitting it into the lead-story box. |
| 2   | Major    | The addiction state appears only as a lowercase italic word after a dot ("peaky · shaking", "POORLY · ruined by it", "· asks by name"), 5 px x-height, and is the smallest print on screen. "GRAVELY ILL · shak…" is truncated on Mrs. Fontana's card at both sizes. Health escalates through caps and inversion; habit has no escalation.                                                                                        | Counter cards                                         | `z_jump_cards.png`, `207_jump_proof2.png`                                                                    | Treat habit like health: caps or an inverted chip for "SHAKING" or "RUINED", on its own line, never truncated.                                                                |
| 3   | Minor    | At LARGER, THE TYPE CASE shrinks to a ~150 px-tall scroll box. The claims (the main dishonest lever) sit under "MORE CLAIMS BELOW ▼ (scroll)", and at first I saw only the rates. In the formula drawer, the price −5c/+5c buttons and ENTER/CHANGE THIS RECIPE are below the fold of the assay column.                                                                                                                           | Proof, right column; FORMULA drawer                   | `21_proof_after_formula.png`, `40_price.png`                                                                 | Let the type case take the height it needs, or open the claims as a drawer. Keep the price on screen.                                                                         |
| 4   | Minor    | The regulars' card strip draws on top of open drawers. At the bottom edge of the letters and ventures drawers, card text ("GRAVELY ILL · shak…", "POORLY · asks by name", "peaky · delighted") overlaps "YOUR REPLY".                                                                                                                                                                                                             | Letters and VENTURES drawers                          | `203_jump_letter.png` (y ≈ 712–720), `68_ventures6.png`                                                      | Fix the z-order, or hide the strip while a drawer is open.                                                                                                                    |
| 5   | Minor    | On the drug shelf, jars move after every pour or take-back. The rects jumped about 95 px (Water went from y −25 to y 70 between two clicks) and jars change size. For someone using magnification or tracking a target, the jar moves out from under the eye.                                                                                                                                                                     | FORMULA, the drug shelf                               | Pour Valerian, then right-click Bitter Aloes; compare targets                                                | Keep the shelf's scroll position and jar size fixed when the glass changes.                                                                                                   |
| 6   | Minor    | Low-contrast greys. "set after press" and "No deaths last month." are grey italic at 5–6 px, 4.67:1. The disabled ad-space buttons in months 1–3 are 2.51:1, with the reason only in a tooltip.                                                                                                                                                                                                                                   | Proof                                                 | `10_proof1.png`, `20_entered.png`                                                                            | At least 4.5:1 and 8 px or more, and a printed reason ("Space opens once the Gazette knows you").                                                                             |
| 7   | Minor    | At LARGER, the footer status line silently drops its last item ("· 11 regulars") when letters are waiting.                                                                                                                                                                                                                                                                                                                        | Footer                                                | `201` vs `202_jump_proof_larger.png`                                                                         | Wrap to two lines, or abbreviate rather than drop.                                                                                                                            |
| 8   | Low      | Narrow justified Gazette columns at LARGER make big word gaps ("A NEW PATRON.—Mrs.", "WIECZOREK.—Mr. Piotr"). The rivers make tracking harder.                                                                                                                                                                                                                                                                                    | Edition, left column                                  | `88_ed8.png`, `205_jump_ed2.png`                                                                             | Ragged-right or tighter hyphenation at the larger sizes.                                                                                                                      |
| 9   | Low      | I found no way to see the other regulars ("and twenty-four more regulars"). Cards don't respond to clicks; figures give a good tooltip on hover only ("Mr. Henry Abernathy: neuralgia. A regular for 1 month; a little peaky, content.").                                                                                                                                                                                         | Counter                                               | Click or hover the cards and figures                                                                         | A REGULARS list in BOOKS, sorted by risk, in the same large print as the Books.                                                                                               |
| 10  | Low      | CONTINUE after `drive_stop` mid-month opened on the edition, not on the proof I was working. Otherwise the shop was exact: $146.52, still lit, price 50c, same for the menu save-and-quit ($129.70, letter answered, price 40c).                                                                                                                                                                                                  | Title, CONTINUE                                       | Stop mid-proof, relaunch, CONTINUE                                                                           | Reopen on the screen you left.                                                                                                                                                |

## What works

- **TEXT SIZE is easy to find and immediate.** SETTINGS is on the title page and in the menu, the option is one press per step, and it persists. LARGER makes the paper, letters, Books, clippings and drawers comfortable: 9 px x-height at 13:1, black on warm newsprint. Most of the time I didn't tire.
- **Suspicion escalates typographically.** It goes from lowercase "none to speak of" to boxed caps "A GOOD DEAL · 37 OF 100" to a full-width inverted "☞ SUSPICION 46 OF 100 · ALARMING", with a plain tooltip: "at 100 the Board of Health moves to close the shop." Keep this; do the same for health and habit.
- **The skeleton at the counter** stands beside the customer who is about to die. It is a big pictorial cue that needs no reading. Best accessibility feature in the game.
- **IN MEMORIAM clippings answer "was it me?" in large plain sentences:** "Died of an overdose, the register says. The register blames the bottle. An inquest sits: see your letters." For Kelly: "The remedy is harmless by the assay; the bottle is not blamed."
- **Letters print the cost of each reply under it** ("$5; standing +1: a framed certificate behind the counter."), and the next screen confirms it: "Sent: 'Keep the box on the counter, and put in the dime.' It cost 10c." Inquest odds are given in the margin: "Pencilled in the margin: about 8 in 10 for naming."
- **THE BOOKS → WHAT A BOTTLE EARNS** finally explained my slow bleed: "At the best margin above, the first forty-four bottles a month only pay for them; last month sold forty-five, so the rest, about one, were the profit."
- **Patterns, not tint:** the glass layers are hatched or dotted and labelled, selected buttons are inverted, and the still's bottles are hatched versus empty outlines. Nothing I found was shown by colour alone.
- **Two-press confirmation** on A NEW SHOP and ventures ("TAKE IT UP? PRESS AGAIN").
- **Writing I liked:**
  - "the half-ounce weight to be a quarter-ounce weight with ambitions" (Weights and Measures)
  - "The Coroner's time is valuable, as is his good opinion."
  - "Sir and Brother (we trust),—" (the Golden Lamp lodge)
  - "Verdict: 'Died of an Excess of Spite.'"
  - Weather: "Heat unbroken. Pine coffins advance."
- **Memory:** the printed testimonial ran the next month ("So writes Mrs. Gussie Lownethal"). The Books counted "one with a cent for the pedestal, two drawn by the gilt sign". The landlord raised the rent once the trade grew.
- **Period details I checked were right:** Grant dead at Mount McGregor in the July 25, 1885 edition; the Pedestal Fund in 1885; the Guion line's Arizona.

## Disclosures

- I used `targets`, `state` and `read` to find click centres, quote text exactly and confirm cash and claims after clicks. My judgments come from the screenshots.
- `state` showed `stillsFree` and `claims`, which I double-checked against the screen.
- Game 2 was a jump-ahead (`--play 24 --dose 1.5`, not saved).
- On the first launch of my fresh `low-vision` profile, the title page already offered CONTINUE, behind an old edition ("Sixty-four bottles sold"). It is probably a leftover save from an earlier round's tester with the same slug. I started A NEW SHOP.
- I didn't read the source, `docs/` or `playtests/`.

## First hour

| Time      | What happened, and what I understood                                                                                                                                                                                                                                                |
| --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0–2 min   | Title page laid over a Gazette. Found SETTINGS, then TEXT SIZE: REGULAR → LARGE → LARGER. The title buttons grew at once. "A NEW SHOP? PRESS AGAIN" confirmed.                                                                                                                      |
| 2–5 min   | First edition: a readable paper at 9 px. I took it as flavour and wasn't sure it was mine until I saw "THE MONTH IN BRIEF". TO THE PROOF.                                                                                                                                           |
| 5–8 min   | The proof is a mock-up of next month's paper. I misread "set after press" as an instruction. Saw five small drawer buttons (22 px tall) and a big SEND TO PRESS. The small print here was the first thing that tired me.                                                            |
| 8–14 min  | FORMULA: the glass came pre-filled (water, spirit, syrup, peppermint). I added valerian and gentian. The assay labels ("Habit: passes through") are clear. The jars kept jumping, so I wrote `jar.sh`. Entered at 60c. Set a claim in THE TYPE CASE, which I had to scroll to find. |
| 14–17 min | THE STILL: you must ORDER before LIGHT THE FIRE (it said so: "Not enough stock on the shelf: order first."). Pressed. A death in the next paper, Widow Fontana, plainly not my bottle.                                                                                              |
| 17–25 min | Months 2–3: cutting the price to 50c sold fewer. Confused. Stopped the game mid-month, CONTINUE: same shop. First letter (a testimonial), printed. Ad space opened in April; sales doubled.                                                                                         |
| 25–30 min | It **clicked** around month 5, opening THE BOOKS: rent and ads set a break-even of about 44 bottles. I was a busy, honest, slowly shrinking shop. Save-and-quit through the menu, CONTINUE: exact.                                                                                  |
| 30–40 min | The laudanum cordial and a dead 81-year-old in its first month. I read it on the IN MEMORIAM card, not on the proof, where the deaths column was 5 px. Inquest letter, readable and tense. I quit the cordial.                                                                      |
| 40–50 min | Second still, a run of letters, and the shop next door in month 11. Then the jump game to watch real danger: suspicion's banner read at a glance; the cards' "shaking" did not.                                                                                                     |

**Does the fun survive for me?** Yes, mostly, because the fun lives in the paper and the letters, and those are well set at LARGER. The tension (who will die, is it my bottle) lives on the proof's cards and notes, and I had to lean in or zoom for every one. The skeleton figure carried me.

## Score

**6 / 10.** The writing and the paper are a pleasure at LARGER, and suspicion is well signalled, but the danger and risk print ignores the size setting. Scaling the cards, NOTES FOR THE PROPRIETOR and the deaths column with TEXT SIZE, and giving "shaking" or "ruined" the same caps or inverted treatment as GRAVELY ILL, would move it to 7.
