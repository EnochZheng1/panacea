# Panacea · round 6 · playtest by "Keyboard-only player with low vision"

I play with the keyboard alone (Tab, arrows, Enter, Escape, PageUp/PageDown). I need a focus indicator I can see, text large enough to read, and nothing that works only by hover or sound.

**Verdict:** I played eleven months without the mouse, apart from one click. The paper, the letters, the type case, the ad space and the formula book all work from the keyboard. Escape behaves well, and the dialogs say "(Click, or Escape.)". But four things keep it from being comfortable. The still's batch size cannot be changed from the keyboard. Focus is lost after about half the actions ("focus: none"). The VENTURES "TAKE IT UP" button can't be reached with Tab, and its "PRESS AGAIN" confirm moves focus away from itself. And on jars and clippings, focus shows only as a one-pixel-thicker border or a faint grey wash.

**Setup:** build `3e627bf` (frozen copy `build-r6`), port 47025, seed 6025, fresh game, no jump-ahead. I played about 33 minutes of active time (19:23–19:56 UTC), Jan → Dec 1885 (11 sends to press). For tooling, I wrote small shell wrappers around `tools/drive.py` in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r6/cold-keyboard-player/`: `k.sh` (press keys, then report focus and take a screenshot), `tabs.sh` / `tabto.sh` (press Tab N times, or until focus matches some text) and `txt.sh` (grep the visible text). Screenshots `01.png`–`81.png` are in the same folder. The game did not crash or restart. The driver log had only the expected ALSA "ERR_CANT_OPEN / dummy audio driver" lines at startup. There were no exceptions.

## Run log

| Seed | Months | What I tried | Result |
|---|---|---|---|
| 6025 | Jan–Mar 1885 | Read the first edition by Tab/Enter/PageDown. Built the first remedy in the formula book by keyboard (Water 3, Grain Alcohol 2, Sugar Syrup 1, Peppermint 0.5, Laudanum 0.5, Valerian 0.5 at first). Typed a name and set claims. Still: 24 bottles. Cut the price 60c → 55c in March | 20, 14, 25 bottles sold; cash $150 → $160 |
| 6025 | Apr–Jul | Added claims (Purifies the Blood). Letters: printed a testimonial, joined the St. Bridget's benefit society. Bought ¼ COL ad space in July | 27, 32, 37, 45 sold; a second still arrived; 12 → 18 regulars |
| 6025 | Aug–Sep | Feeney price-war letter ("Let him try"). First death (the Hale infant, under the teething claim). Inquest: testified, cleared. Took the laudanum out of the recipe | 71, 57 sold; 23 → 27 regulars |
| 6025 | Oct–Nov | Landlord letter: bought the lease ($60). VENTURES: took up **The Proprietor's Almanac** ($40). Jersey distiller letter | 78, 81 sold; cash $143 → $156; 31 regulars at Dec 1885 |

Key moments:
- **Jan:** TO THE PROOF puts focus on SEND TO PRESS. Two Enters from the paper would send an empty month (the proof does warn in small italics: "Nothing on the shelf and nothing on the still: the shop will sell nothing this month.").
- **Jan:** in the formula book, Enter pours 0.5 oz. I found by trial that Backspace takes 0.5 oz back. The on-screen help only mentions the mouse: "Click a jar to pour half an ounce … hold Shift for half as much. Right-click to take it back."
- **Jun:** the still's batch grid takes focus, but no key changes the number. I made my **one mouse click** on the bottle grid at (700,345) to go from 24 to 48 bottles.
- **Aug:** with two stills, my usual Right, Enter, Enter ordered for 48, then the second Enter landed on "ORDER AND LIGHT BOTH · 48 EACH" and lit both stills (96 brewing). I had meant one.
- **Aug/Sep:** the death and the inquest were both readable from the keyboard. The clippings are excellent ("IN MEMORIAM · The Infant of Mrs. Hale … No inquest has been called. Yet.").
- **Oct (month 10, about 30 min in):** took a venture. That took trial and error (see F3).

## Metrics

- **Whole month without the mouse:** yes, as long as the default 24- or 48-bottle batch is acceptable. Choosing a batch size needs the mouse. Every other action I needed had a keyboard path.
- **Typical keypresses per month** (proof → brew → press): about 25–35. Tab is a single ring around the whole desk, so reaching THE STILL from SEND TO PRESS took **11–16 Tabs**, and the number grows with claims and letters. Arrow keys were much shorter: Left from VENTURES, or Up, Up from SEND to OPEN THE LETTERS.
- **Focus lost to "none"**, from my focus log: after LIGHT THE FIRE (every month, 11/11), after the -5c price button, after POUR IT INTO THE BOTTLE (once out of two), after the last letter reply plus Escape (Jun, Sep, Oct, Nov), and after the first TAKE IT UP press (focus went to a card off-screen at y = −251). Recovery: Tab starts again at PUT IT DOWN (top right), or Escape closes the drawer.
- **Focus indicator strength** (measured from screenshots):
  - Buttons: a second inner rule about 2 px wide, light on black. Visible, but thin (`focuscmp.png`).
  - Clipping in the paper: grey wash (204,201,195) on paper (234,231,223) = **1.34:1**, plus a heavier left rule.
  - Drug jar: the border is **3 px when focused, 2 px when the jar has some in the glass, 1 px when empty**. The grey fill is identical (217,213,206) for focused and in-the-glass jars (`65z.png`). That is the hardest focus to see in the game.
  - Name field (LineEdit): no visible ring and no visible caret that I could see (`14z.png`, `15z.png`).
  - Counter customer: an inverted card, the clearest focus state in the game.
- **Text contrast (body):** good. Ink on paper measured **6.3:1 to 8.8:1** for italic notes, "Rates:", "the still is cold" and the counter status lines. Disabled ad-space buttons: **3.4:1**. "NOT TO BE HAD": **4.3:1**.
- **Text size:** the problem is size, not contrast. Many important lines are 13–14 px tall and set in italics. Their text-box heights from `read`:
  - "Rates: ¼ col $3.00 …": 14 px
  - jar costs and "0.5 oz in the glass": 14 px
  - customer status ("peaky · delighted"): 16 px
  - "suspicion: none to speak of (0 of 100)": 16 px
  - the letter-cost lines under each reply ("Standing in the ward +3 …"): about 14 px italic
- **Time to understand:** the core loop (paper → proof → brew → press) clicked in month 2. The keyboard "grammar" took longer. It clicked only around month 6, once I stopped using Tab and used arrows to jump between panels.

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | The batch size cannot be changed from the keyboard. The bottle grid takes focus, but Left/Right move focus away. Up/Down, +/−, Enter, Space, Home/End and PageUp/PageDown do nothing. "You have marked 24" stays. | THE STILL drawer, bottle grid (406,163,428×216) | Open THE STILL, Tab to the grid, press any key | Make arrows (or +/−) change the count by 1 or 12 while the grid has focus, or add "−12 / +12" buttons next to "You have marked N" |
| 2 | Major | Focus goes to **none** after common actions, so the next Enter does nothing and I can't see where I am. Tab then restarts at PUT IT DOWN. | LIGHT THE FIRE (every month); −5c; POUR IT INTO THE BOTTLE; after the last letter reply plus Escape | e.g. Still → ORDER ENOUGH → Enter → LIGHT THE FIRE → Enter → `focus: none` | After an action rebuilds a panel, move focus to the next sensible control (LIGHT → PUT IT DOWN or SEND TO PRESS; −5c → stay on −5c; last reply → SEND TO PRESS) |
| 3 | Major | **Ventures can't be taken with Tab.** Tab moves from card to card and never reaches "☞ TAKE IT UP · $40.00". Enter or Space on a card does nothing. Right arrow reaches the button, which I found by trial. After the first press, the button reads "PAY $40.00 DOWN? PRESS AGAIN", but focus has jumped to another card off-screen (y = −251), so pressing again does nothing. I had to press Up, then Right, to get back to the button. | VENTURES drawer (`68.png`–`76.png`) | Oct 1885, seed 6025: open VENTURES, Tab to the Almanac card, Enter; then Right, Enter, Enter | Put the TAKE IT UP button in the Tab order, or make Enter on a card press its button. Keep focus on the button after the first press. (The source shows that `Refresh()` rebuilds the list without refocusing it: `scripts/UI/Prospectus.cs` ~l.124.) |
| 4 | Major | Focus on drug jars is nearly invisible to low vision. A focused jar differs from a jar with drug in it by one pixel of border. Both have the same grey fill. | Formula book, drug shelf (`65z.png`) | Open FORMULA, arrow between Water, Peppermint and Valerian | Give focus its own mark: an inverted header, a pointing hand ☞, or a 3–4 px outline with a gap |
| 5 | Major | Instructions are mouse-only. The formula help says "Click … hold Shift … Right-click to take it back". There is no keyboard hint, and Backspace (which removes 0.5 oz) is never mentioned. The driver also can't send Shift, so I couldn't test the ⅛-oz pour. | Drug shelf header text | Open FORMULA and read the header | Add a line: "Enter pours ½ oz · Backspace takes it back · Shift for half" |
| 6 | Minor | With two stills, the keyboard default lands on the more expensive action. After ORDER ENOUGH FOR 48, focus moves to "ORDER AND LIGHT BOTH · 48 EACH", so Enter, Enter lit 96 bottles instead of 48. | THE STILL, Aug 1885 | With 2 stills: Right, Enter, Enter | After ORDER, focus LIGHT THE FIRE (one still), the same button as in the one-still flow |
| 7 | Minor | After a key or click skips the press animation, the footer keeps saying "☞ Click, or press any key, to see the edition." for the whole edition, in place of the date/cash line. | Footer, every month I skipped (Mar–Nov) | Send to press, press Escape about 3 s in, then read the footer | In `EndPress()` / the skip path, refresh the status line once `_pressing` is false (`scripts/Main.cs` ~l.284 and l.549) |
| 8 | Minor | Waiting out the press, then pressing Space "to see the edition", skips the whole edition: the edition had already appeared with focus on TO THE PROOF, so Space activated it. I recovered with LAST EDITION. | Feb → Mar 1885 | Send to press, wait about 9.5 s, press Space | Ignore Enter/Space for about 0.5 s after the edition appears, or put focus on the paper rather than TO THE PROOF |
| 9 | Minor | Focus goes to controls that are scrolled out of view. Claims 6–7 ("CURES CONSUMPTION !!!" and the long one) take focus below the visible part of THE TYPE CASE, which doesn't scroll to follow them. | Type case (`19.png`) | Tab through the claims in Jan | Scroll the type case to the focused claim (`ScrollContainer.EnsureControlVisible`) |
| 10 | Minor | TO THE PROOF lands focus on SEND TO PRESS. In month 1, Enter, Enter sends an empty month. The only warning is a 17 px italic line. | Proof, Jan 1885 | From the first edition, press Enter twice | Land focus on THE STILL or FORMULA while nothing is brewing, or ask once before sending a month that will sell nothing |
| 11 | Minor | The name field gives no visible focus and no visible caret, and nothing confirms that typing landed. | Formula book, name LineEdit (580,516) | Tab to the name field and type | Add an underline or inverted field when focused, and a visible caret |
| 12 | Minor | Small text carries key decisions: ad rates, letter consequences ("Free, and Feeney takes nothing, but suspicion +4 and standing −2."), "most the stock allows", the suspicion meter. 13–14 px italics. | Type case, letters, still | — | Raise the minimum to about 16 px roman for anything with a number in it, or add a text-size setting |
| 13 | Minor | The October headline reads "TRADE DULL ON MULBERRY STREET … ONLY SEVENTY-EIGHT BOTTLES SOLD; THE PROPRIETOR BLAMES THE WEATHER." That was my best month so far. The ledger shows the "dull" came from the lease ($60) and the Almanac ($40). The rent line also went from $3.00 to $7.00 with no explanation I could see. | Oct 1885 edition (`77.png`, `78.png`) | — | Key the headline to sales, not to cash after one-off outlays. Show what changed the rent in the ledger |
| 14 | Low | Proof text bleeds through between the drawer banner and its cards ("No deaths last month", "THE LEAD STORY", "SYRUP" peek out). The prospectus header overlaps the masthead when it scrolls. | VENTURES and letters drawers (`71.png`, `76.png`) | Take a venture or answer a letter | Make the drawer's background opaque behind the banner |
| 15 | Low | The Tab ring is one long loop over the whole desk (11–20 stops between SEND TO PRESS and THE STILL). Shift+Tab could not be tested (the driver sends no modifiers). | Proof | — | Make each panel (type case, letters, works, counter) one Tab stop with arrows inside it |

## What works

- **Escape is consistent**: it closes clippings, the ledger, letters and drawers, and focus comes back to what opened it (FORMULA, THE STILL, the clipping). Every dialog says so: "Put it down. (Click, or Escape.)"
- **Letters are a model for the rest of the game.** Opening the post puts focus on the first reply. Each reply moves focus to the next letter. Up/Down move between the replies. Each reply has its cost printed under it in plain words.
- **The paper reads well from the keyboard.** PageDown scrolls, Tab walks the clickable clippings, and THE MONTH IN BRIEF strip opens "The Month's Books" ledger by Enter. The clippings are big, high-contrast, and the best writing in the game.
- **Arrow keys move spatially** between panels (Up, Up from SEND TO PRESS reaches OPEN THE LETTERS; Left from VENTURES reaches THE STILL). That is quicker than Tab.
- **Counter customers invert when focused**: the clearest focus state in the game. Selected claims and the chosen ad space are inverted too. You can always see what is *chosen*, even when you can't see what is *focused*.
- **Nothing depends on sound**, and nothing depends on hover except tooltips that repeat what's on screen (the venture confirm tooltip "Press again to pay…" is the one exception). The press screen tells you, in text, how to skip it.
- **Contrast of body text is good** (6–9:1), and the woodcut style never puts text on a busy image.

## Disclosures

- **One mouse click** at (700,345) on the still's bottle grid in June, because no key changes the batch size. Everything else was done with keys.
- I used `focus` after every key, `read` to quote text and `targets` once to find the "PRESS AGAIN" button. A real player would have only the screen. That is how I could say "focus: none" with certainty where the screen just showed nothing highlighted.
- `state` was read three times (Apr, Aug, Dec) to cross-check cash and regulars.
- After play I read two source snippets, only to confirm bugs F3 and F7 (`scripts/UI/Prospectus.cs`, `scripts/Main.cs`). No jump-ahead and no second game.

## First hour

- **0:00** The first edition is a full newspaper. The masthead and headline are very readable. Focus starts on TO THE PROOF, shown by a thin inner rule. Tab walks four PERSONAL clippings (grey wash; hard to see), and Enter opens a big card. I understood "this is the result screen".
- **0:03** TO THE PROOF. The proof is busy: TYPE CASE, NO LETTERS, THE WORKS, the counter. Focus sits on SEND TO PRESS. I nearly sent an empty month. I read "No remedy yet: open the formula book."
- **0:05** FORMULA by keyboard. The jars are a 3×5 grid and arrows move between them. I couldn't tell which jar was focused without zooming. Enter pours. The help text speaks only of clicking. I guessed Backspace to remove, and it worked.
- **0:09** Typed the name "Mulberry Soothing Syrup". I couldn't see the field had focus, but it took the text. ENTER IT IN THE BOOK worked.
- **0:11** Claims appeared in the type case. I picked SOOTHES THE NERVES and COMFORT FOR TEETHING BABES. Focus wandered onto two claims hidden below the box.
- **0:12** THE STILL: "ORDER ENOUGH FOR 24 · $4.20" → "LIGHT THE FIRE · 24 BOTTLES", then focus vanished. SUPPLIES (+8/+32) is clear. VENTURES at month 1 lists offers I can't have yet, with the conditions spelled out. Good.
- **0:14** Sent to press. "Click, or press any key" is clear. February: 20 sold, books up $4.08. The loop clicked here.
- **0:16** I skipped the March edition by accident (F8).
- **0:20** By April I had a routine: brew 24, send. Clippings showed customers coming and going.
- **0:24** June letters: the best keyboard experience in the game. I needed the mouse once for a 48 batch.
- **0:27** August: the Hale infant died. Reading it right after picking "Comfort for teething babes" landed hard; the satire works. I took the laudanum out of the recipe in September.
- **0:30** October: VENTURES finally affordable. It took about 3 minutes of trial (Tab, Enter, Space, then arrows) to buy the Almanac. Worth it in-game? November sales rose to 81. The almanac's "orders by post" brought $7.15 in its first month, against a $40 cost, so it pays back in about 6 of its 12 months.
- **0:33** Stopped at Dec 1885 with $155.79, 31 regulars, 0 suspicion trouble.

## Score

**6/10.** It's a sharp, readable newspaper game. Nearly all of it works from the keyboard, which is rare, but the still and VENTURES break the keyboard flow, and the focus ring is too faint for low vision on jars and clippings. **One point up:** keyboard batch size, plus focus that never drops to "none" and never sits off-screen. That alone would make a full month mouse-free.
