# Panacea · round 8 · playtest by "Keyboard-only player"

I have a repetitive strain injury and play with the keyboard alone: Tab, arrows, Enter, Space, Escape, PageUp/PageDown. I'm new to the game. I'm checking whether everything can be reached without a mouse, whether I can always see where focus is, and whether focus lands somewhere sensible after each action.

## Verdict

Panacea is one of the most keyboard-complete games I have tested. Every drawer, letter, venture, clipping, the still, the formula book, the save/quit menu and CONTINUE all worked with Tab, the arrows, Enter and Escape. After most actions focus went to the next sensible control: the lit still hands you PUT IT DOWN, and an answered letter hands you the next letter's first reply. The rough spots are small but they add up. The settings card moves focus onto THE WINDOW after you toggle SOUND, so a second Enter flips the game to full screen. The "At the counter" strip traps the Down arrow. Changing the price drops focus back onto the drug shelf. Every Escape on a title/menu card throws a NullReferenceException.

## Setup

- Build: frozen copy `build-r8`, commit `80e045d`. Port 47084, seed `keys-3`, `--player --profile keyboard`.
- One game, two launches. Session 1 ran Jan–Apr 1885 and ended with SAVE, AND QUIT. Session 2 relaunched with the same arguments, chose CONTINUE, played May–Nov 1885, then used SAVE, AND THE TITLE PAGE and CONTINUE again. No jump-ahead.
- 10 sends to press (the Jan through Oct editions). I answered every letter I received except the last one (coal, Nov).
- About 60 minutes of active testing.
- Input: only the driver's `key` and `type` actions, plus `wait`, `shot` and `[{"focus":true}]` to see where focus was. No `click`, `rclick`, `move` or `scroll` at any point.
- Tooling: `scratchpad/r8/keyboard/k.sh`, a small wrapper that sends a list of keys with a 350 ms wait after each, takes optional screenshots and prints the focus owner at the end.
- **Driver limitation (not the game):** the driver never refuses a key name. `"Bogus"` returned `{"ok":true}`. Modifier combinations (`Shift+Tab`, `Shift+Enter`, `Ctrl+A`) are accepted but do nothing. `Driver.cs` builds the key with `OS.FindKeycodeFromString` and sets no modifier flags. **So I could not test reverse-Tab, or the formula book's "hold Shift for half as much".** Every finding below is about forward Tab, the arrows, Enter, Space, Escape and PageUp/PageDown.

## Run log

| Seed                         | Months                   | What I tried                                                                                                                                                                                                 | Result                                                                                                    |
| ---------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| keys-3 (session 1)           | Jan–Apr 1885 (3 presses) | Title page, settings, new shop, first edition by keyboard, clippings, formula book, naming the remedy, still, supplies, ventures, first letter, ½-col ad, two claims, then save and quit from the pause menu | Everything reachable. Focus visible almost everywhere. 2 NREs in the log when I closed cards with Escape. |
| keys-3 (session 2, CONTINUE) | Apr–Nov 1885 (7 presses) | CONTINUE, reformulating (adding valerian), batches on both stills, 9 letters, The Shop Next Door venture, a third claim, the counter strip, the pause-menu settings, save to title page and CONTINUE         | Exact state restored both times. The settings focus-jump made me toggle full screen by accident. 3 NREs.  |

Timeline:

- **Title page:** focus starts on **A NEW SHOP**. Tab and Up/Down cycle the four buttons and wrap. Enter starts the game.
- **Jan 1885 (first edition):** focus starts on **TO THE PROOF ☞**. Tab walks the clippings, and each one gets a clear black frame. Enter opens the card and Escape closes it. PageDown scrolls the paper.
- **Jan proof:** with no remedy yet, focus lands on **FORMULA**, the right place to start. The shelf hint names the keys: _"By keyboard: the arrows walk the shelf, Enter pours, Backspace takes it back."_ The still's hint does too: _"◀▶ a bottle at a time, ▲▼ a dozen."_
- **Feb:** first letter, _A Grateful Letter_. I chose **"Print it as a testimonial."** It came back in the Feb 28 edition under NOTICES as "A TESTIMONIAL.—'Three physicians gave me up…'", and with it the half-column ad space opened.
- **Mar:** bought **½ COL** with Right/Enter. The ad appeared on the proof at once.
- **Apr:** SAVE, AND QUIT from the pause menu.
- **Relaunch:** focus starts on **☞ CONTINUE** (_"April 1885 · $150.59 · 18 regulars"_). Same date, cash, regulars, the ½-col ad with two claims, 48 brewing and the paper. Exact.
- **May:** the Death figure stands _"at the counter beside Widow Rebecca Levy"_. I added ½ oz of valerian with **CHANGE THE RECIPE**.
- **June:** the paper ran _"THE REMEDY RUNS DRY."_ The game bought a second still on its own (_"$13.00 of it laid out on the business"_).
- **Jul:** the still now offers **BOTH STILLS · ORDER AND LIGHT 48 EACH**, which is great for keypress count.
- **Aug:** 25 regulars. **THE SHOP NEXT DOOR** venture became available and I took it up by keyboard (it asks for a second press to confirm).
- **Sep:** turned down the diploma mill (_"Throw it in the stove."_). Toggled SOUND in settings and accidentally went full screen.
- **Nov:** SAVE, AND THE TITLE PAGE, then CONTINUE. State exact again.

## Metrics

**Keypresses for common tasks** (counted from where focus actually landed):

| Task                                          | Keys                          | Path                                                                                                                                                                 |
| --------------------------------------------- | ----------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Start a new shop                              | **1** (+1 to reach the proof) | Enter on A NEW SHOP (focused by default), then Enter on TO THE PROOF                                                                                                 |
| Write the first remedy and enter it           | ~**16** + one per pour        | Enter on FORMULA; arrows + Enter to pour; Tab ×8–9 to the name field; type; Tab ×3 to ENTER IT IN THE BOOK; Enter; Escape                                            |
| Brew a batch (later months)                   | **5**                         | Enter (THE STILL is focused by default when the shelf is low), Tab, Enter (order), Enter (focus has moved to LIGHT THE FIRE), Enter (focus has moved to PUT IT DOWN) |
| Brew on both stills                           | **5**                         | Enter, Tab, Tab, Enter on BOTH STILLS, Enter on PUT IT DOWN                                                                                                          |
| Answer one letter                             | **5–6**                       | From SEND TO PRESS: Up, Up (OPEN THE LETTERS), Enter, [Down], Enter on the reply, Enter on PUT IT DOWN                                                               |
| Each further letter                           | **+1–2**                      | Focus moves to the next letter's first reply on its own                                                                                                              |
| Buy ad space (½ col)                          | **7**                         | From SEND TO PRESS: Tab ×4 to NONE, Right ×2, Enter                                                                                                                  |
| Add a claim                                   | **3–4**                       | Up/Down within the type case, Enter                                                                                                                                  |
| Take up a venture                             | **7**                         | Right ×3 to VENTURES, Enter, Tab to TAKE IT UP, Enter, Enter (PRESS AGAIN)                                                                                           |
| Send to press                                 | **1–5**                       | 1 if focus is already on SEND TO PRESS (it often is); 5 from THE STILL (Tab ×4, Enter); **17** from the counter strip                                                |
| Open a clipping in the paper                  | **2–5**                       | Tab ×n, Enter; Escape to close                                                                                                                                       |
| Save and quit                                 | **5**                         | Escape, Down ×3, Enter                                                                                                                                               |
| Continue                                      | **1** (+1 to the proof)       | Enter on ☞ CONTINUE (focused by default)                                                                                                                             |
| A whole routine month (letter + brew + press) | **~13–16**                    |                                                                                                                                                                      |

**Game numbers (seed keys-3):**

| Month    | Jan     | Feb    | Mar    | Apr    | May    | Jun    | Jul    | Aug    | Sep    | Oct    | Nov    |
| -------- | ------- | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ | ------ |
| Cash     | $150.00 | 146.22 | 149.30 | 163.43 | 167.44 | 178.74 | 175.94 | 174.54 | 172.49 | 186.54 | 193.29 |
| Regulars | 12      | 13     | 16     | 18     | 22     | 23     | 23     | 25     | 27     | 29     | 32     |

There were no deaths. Suspicion stayed at 1 of 100.

**Exceptions:** 5 `NullReferenceException`s in `FrontCard._Input` (`FrontDoor.cs:154`), one for each Escape on a title/menu/settings card.

## Findings

| #   | Severity  | What I saw                                                                                                                                                                                                                                                                                                                                            | Where                                                         | How to reproduce                                                                                                           | Suggested fix                                                                           |
| --- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| 1   | **Major** | Toggling **SOUND** moves focus up to **THE WINDOW** button. Press Enter again thinking you're still on SOUND, and the game goes **full screen**. I did exactly this. Toggling THE WINDOW keeps focus where it is.                                                                                                                                     | Settings card (title page and pause menu), SOUND at (400,407) | Escape, Down, Enter (SETTINGS), Down to SOUND: ON, Enter. Focus is now on THE WINDOW. Enter again switches to FULL SCREEN. | Keep focus on the button that was pressed after it rebuilds its label.                  |
| 2   | Minor     | Every Escape that closes a front card (pause menu, settings) logs `System.NullReferenceException … FrontCard._Input … FrontDoor.cs:line 154`. The card is freed in `OnEscape`, then `GetViewport().SetInputAsHandled()` runs on a node that is no longer in the tree. No visible effect yet.                                                          | Pause menu, settings card                                     | Escape (opens menu), Escape (closes it). Check `/tmp/panacea-driver-PORT.log`.                                             | Call `SetInputAsHandled()` before `OnEscape?.Invoke()`, or cache the viewport first.    |
| 3   | Minor     | Arrows on the "AT THE COUNTER" strip: Left/Right walk the five visible customers, Up leaves to the type case, but **Down does nothing**, so it is a dead end. When it first takes focus there is no ring on it at all; only the chosen customer card gets a heavier border once you press an arrow. Tab from here to SEND TO PRESS is **17** presses. | Proof, counter area (22,430,844,292)                          | From the type case press Down past the last claim; then press Down repeatedly                                              | Let Down go on to the WORKS / SEND TO PRESS column. Draw a focus frame round the strip. |
| 4   | Minor     | The counter's caption is overprinted by the customers' woodcut heads, e.g. "Mr. Luigi Costa: a regular for eight months; a little peaky, asking … by name." Only the 5 front customers can be walked; "and twenty-two more regulars" cannot be reached.                                                                                               | Proof, counter caption (575–850, 460–500)                     | Focus the counter, press Right. See `84crop.png`.                                                                          | Put the caption on a paper panel above the figures.                                     |
| 5   | Minor     | Pressing **−5c / +5c** rebuilds the formula panel and sends focus back to the drug shelf (the Laudanum jar at 228,398). Each further 5c step means tabbing back through the shelf.                                                                                                                                                                    | Formula book, price buttons                                   | Tab to −5c, press Space. Focus is now on a shelf jar.                                                                      | Restore focus to the same price button after the rebuild.                               |
| 6   | Minor     | Two "highlighted" looks. The **☜ BACK** button on settings is always solid black with a pointing hand, which looks focused, while the real focus is a thin double outline on another button. On the title page and pause menu the focused default button has both styles, so you learn "black = here" and then it misleads you on settings.           | Settings card                                                 | Open SETTINGS and look: BACK is black while focus is on THE WINDOW (`86_settings_pause.png`, `87_settings_sound.png`)      | Use one focus style everywhere (the double outline, or move the black fill with focus). |
| 7   | Minor     | Arrow navigation on the proof jumps strangely. Up from THE STILL goes to the counter strip on the far left, not to the letters panel just above. Down from "CURES CONSUMPTION !!!" skips the last, half-scrolled claim and lands on the counter.                                                                                                      | Proof, right column                                           | Focus THE STILL, press Up                                                                                                  | Set `FocusNeighborTop/Bottom` explicitly in the right-hand column.                      |
| 8   | Minor     | Paper headline contradicts the brief: _"FIFTY-SEVEN BOTTLES SOLD, AND THE BOOKS WELL UP; THE PROPRIETOR ORDERS A NEW HAT"_ while THE MONTH IN BRIEF says _"the books down $2.05, $5.00 of it laid out on the business."_                                                                                                                              | Gazette of Aug 29, 1885                                       | Take up a venture that costs money in a good month                                                                         | Choose the headline from the net change, or say "trade up" rather than "books up".      |
| 9   | Low       | **☰ MENU** is never in the Tab order (the cycle is SEND TO PRESS → SOUND → LAST EDITION → counter → …). Escape does open the menu, but nothing on screen says so.                                                                                                                                                                                    | Footer, every screen                                          | Tab round the proof or paper                                                                                               | Add MENU to the Tab ring, or label it "☰ MENU (Esc)".                                  |
| 10  | Low       | PageDown on the paper scrolls the focused clipping off screen (focus rect at y = −341) with no visible focus until the next Tab.                                                                                                                                                                                                                      | Gazette                                                       | Tab to the first clipping, press PageDown                                                                                  | Move focus to the first clipping in view after a page scroll.                           |
| 11  | Low       | On relaunch the title card sits over a stale footer: _"The Gazette of January 3 · now January 1885 · cash in hand $150.00 · 12 regulars"_, while the card itself says April 1885 · $150.59 · 18 regulars.                                                                                                                                             | Title page after a save                                       | Save and quit mid-game, relaunch with `--player`                                                                           | Hide the footer or render the saved edition behind the card.                            |
| 12  | Low       | Season slip: in an October letter the landlord says the roof "has let in the rain all winter" and he'll mend it "when the spring rents are in."                                                                                                                                                                                                       | Letter "Re: The Roof Over the Still", Oct 1885                | Seed keys-3, Oct 1885                                                                                                      | Gate the letter to Feb–Apr, or reword it for the season.                                |
| 13  | Low       | Shop-floor items in the Tab order have only internal names (`@PanelContainer@453`). Fine for sighted keyboard users, but they would be blank to a screen reader.                                                                                                                                                                                      | Paper clippings, drug-shelf jars, ventures                    | `[{"focus":true}]` on any clipping                                                                                         | Give focusable panels an accessible name or tooltip.                                    |

## What works

- **Default focus is nearly always right.** A NEW SHOP or ☞ CONTINUE on the title page. TO THE PROOF on each edition. FORMULA when you have no remedy. THE STILL when the shelf is low. SEND TO PRESS otherwise. Most months I pressed Enter far more than Tab.
- **Focus moves forward on its own in the still.** After ORDER, focus moves to LIGHT THE FIRE. After lighting, it moves to PUT IT DOWN. After closing the drawer, it returns to THE STILL. Brewing takes five keys. The batch size is remembered between months. BOTH STILLS · ORDER AND LIGHT is excellent.
- **Letters chain well by keyboard.** After a reply, focus jumps to the next letter's first reply; after the last, to PUT IT DOWN. Each reply has its consequence printed beneath it (_"$3, and he is gone by dinner…"_, _"Free, but the ward sees him at it: standing −2."_), so I could choose without hovering for a tooltip.
- **The keyboard is taught on screen**, in the shelf and still hints. That's rare and welcome.
- **Venture confirmation:** "TAKE IT UP? PRESS AGAIN" protects against a stray Enter.
- **Escape is consistent.** It closes any drawer or card and puts focus back on the button that opened it. On the proof or paper it opens the pause menu, and closing that menu returns focus to where it was.
- **The focus frame is clear** on drawers, clippings, jars, venture panels and letter replies: a thick black double rule that fits the woodcut style.
- **Save/continue is exact.** Both round trips restored the date, cash, regulars, ad size and claims, the brewing batch, the letters waiting and the paper.
- The letters are good reading. The Pedestal Fund (June 1885, Bedloe's Island) is right for its year. The Board of Pharmacy, the Eclectic Medical College of Ohio ("examination is waived in cases of evident merit, and all cases are evident") and the barque _Mary Ellen_ wanting a five-month medicine chest were highlights. The testimonial came back in print the next edition.

## Disclosures

- No jump-ahead. Only `key` and `type` were used for input.
- I used `[{"focus":true}]` as my eyes for focus, and checked it against screenshots. I used `read` a few times to quote text exactly and once (`targets`) to read the type-case tooltips. I read `Driver.cs` (key parsing only) during play to understand why Shift+Tab did nothing. After the session I read `FrontDoor.cs` lines 140–165 to confirm finding #2.
- The driver could not send modifier combinations, so reverse-Tab and Shift-pour are untested (see Setup).
- I can't hear, so I make no claims about sound.

## First hour

- **0:00** Title page. The content note is clear. Focus is on A NEW SHOP and it's obvious where I am.
- **0:03** Settings from the title page. Tab cycles, Escape backs out (the first NRE shows up in the log).
- **0:05** First edition. I Tabbed through clippings and saw an obvious frame on each, and learned Enter opens a person's card. The paper is a joy to read.
- **0:08** Proof. Focus is already on FORMULA, and the hint _"Begin with the FORMULA book: pour a remedy and enter it. Then light THE STILL…"_ tells me the loop. **Clicked here:** this is a month-by-month newspaper turn.
- **0:12** Formula book. The keys are printed on the shelf. Naming the remedy was fiddly: about 9 Tabs to the name field, and the price button threw me back to the shelf.
- **0:16** Still. The arrows set the batch and focus follows the action. Lovely.
- **0:20** Supplies (the order form's +8/+32 buttons work with Tab and the arrows) and VENTURES (a long prospectus; Tab walks each panel).
- **0:22** First press: _"TRADE BRISK IN THE SIXTH WARD."_ Opened the ledger with Enter on "the books".
- **0:28** Letters. The consequences are printed under each choice, so the replies are real choices.
- **0:32** Ad space. I couldn't buy it until _"The Gazette wants $100 in savings and a printed testimonial."_ The testimonial letter supplied the second half, and the requirement made sense.
- **0:38** Save and quit, relaunch, CONTINUE. Exact.
- **0:45–0:60** Routine months at ~15 keys each, a second still, the venture, settings. Then the full-screen accident.

## Score

**7 / 10.** As a keyboard-only player I could do everything in the game without a mouse, and the default focus and the way it moves forward show real care. What costs points is the handful of places where focus jumps somewhere I didn't ask: the settings toggle, the price buttons, the counter dead end. **One point up:** keep focus on the control just pressed (settings, ±5c), give the counter strip a frame and a Down exit, and fix the Escape NRE.
