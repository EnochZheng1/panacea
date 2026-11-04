# Panacea · round 5 · playtest by "UX / accessibility critic"

A UX and accessibility reviewer working from Nielsen's heuristics and Celia Hodent's game-UX framework. I measure contrast, type size, focus, keyboard use, feedback and error prevention, and I check whether the screen alone tells the player what happened.

**Verdict:** Most of my round-4 list is fixed. Escape now returns focus to the drawer's own button. After a reply, focus moves to the next letter's first reply and stays on screen. The press hint shows from the first frame, and the inquest odds differ by reply. The new VENTURES drawer reads well and reports clearly what was taken up, but it opens with focus on **TAKE IT UP**, so two quick Enters bought a $40 venture with no confirmation. I also found that the arrow keys walk straight out of every drawer's focus trap onto the dimmed bench. With the VENTURES drawer open, Down ×6 then Enter sent the paper to press. Counter tags are now in the Tab order, but the focused tag shows almost no change, and the tags add five stops to every trip round the proof.

**Setup:**

- Build `ea3dd20` (frozen copy `build-r5`), port 47014, 1280×800 window.
- **Game 1, seed `ux-5`:** a fresh game played by hand from January to April 1885, with 4 sends to press. January to March were mostly by keyboard; April was a mix. I bought ¼ COL in April and took up the Proprietor's Almanac by keyboard. The machine restarted during the April→May press, which stopped this game.
- **Game 2, jump-ahead, `ux-5 --play 30 --dose 0.25`** (it starts in July 1887): I opened VENTURES by keyboard. No press. Only the almanac was on offer, so I restarted with a longer jump.
- **Game 3, jump-ahead, `ux-5b --play 48 --dose 0.5`** (it starts in January 1889): VENTURES with a disabled offer (the Laboratory), the almanac bought by a double Enter, and January to March 1889 played with 3 sends.
- Total: 7 sends to press, about 45 minutes of active testing.
- **Tooling**, carried over from round 4 with paths updated, in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r5/ux-accessibility-critic/tools/`: `k.py` (press a key N times and log focus), `act.py` (click by text), `contrast.py`, `crop.py`, `d` and `shot`. New: `month.py`, which plays a routine month (proof, letters, still, press) by mouse.

Below, `D/` is `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r5/ux-accessibility-critic/`.

## Run log

| Seed / game                   | Months                        | What I tried                                                                                                                                                                                                                                                                                                               | Result                                                                                                                                                 |
| ----------------------------- | ----------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ux-5`                        | Jan–Apr 1885 (4 presses)      | First edition and a cutting by keyboard. Counter tags by Tab and hover. VENTURES with nothing on offer. The sound toggle. A letter by keyboard. Formula book (+½ oz laudanum) and still by keyboard. Supplies. Arrow keys inside drawers. Two letters (inquest, testimonial) by keyboard. ¼ COL by keyboard. Almanac taken up by keyboard. Paper sent to press from behind the open VENTURES drawer | Cash $150.00 → $131.96, after $40 on the almanac. 5 deaths in the first two months; one inquest, which found nothing. Leads: A NEW CONCERN, A SICKLY SEASON, NEW FACES AT THE COUNTER, THE SHELF BARE ON MULBERRY STREET |
| `ux-5 --play 30 --dose 0.25`  | Jul 1887 (no press)           | Proof Tab cycle with 8 claims and 5 ad sizes. VENTURES by keyboard                                                                                                                                                                                                                                                         | Only the almanac on offer; restarted                                                                                                                   |
| `ux-5b --play 48 --dose 0.5`  | Jan–Mar 1889 (3 presses)      | VENTURES with a disabled offer (Laboratory: "NOT TO BE HAD / Not enough cash."). Enter, Enter on VENTURES. Three routine months. A counter tag beside Death                                                                                                                                                                  | Almanac bought by the double Enter. Cash $324.69 → $300.02. The paper reported "IN THE TRADE.—Ten thousand copies of the proprietor's almanac…"        |

**Timeline**

- **Jan 1885, first edition.** TO THE PROOF has focus. Tab: TO THE PROOF → THE SHOP: HEARD → 4 callers → trade line (7 stops). Follow-focus now leaves a margin: the first caller lands at y=40, and the trade line at y=672–696, about 40 px above the footer (`D/02_tab_caller1_after_cycle.png`, `D/03_trade_line.png`). Hyphenation is now right: "rheuma-tism", "an-swer", "neighbor-hood", "sin-gle", "cook-ing", "noth-ing". The rivers after caps run-in heads remain (`D/01_first.png`).
- **Jan, cutting.** Enter opens Mrs. Wilhelmina Mueller's cutting, Tab can't leave it, and Escape returns focus to the item (`D/04_cutting.png`).
- **Jan, proof.** The Tab cycle is SEND TO PRESS → THE SHOP: HEARD → ☜ LAST EDITION → **5 counter tags** → OPEN THE LETTERS → THE STILL → FORMULA → SUPPLIES → VENTURES: 13 stops. The focused tag *loses* its inner rule, which is the only change I could find (`D/c06_tags.png`, `D/c07_tags.png`). Its description is printed under AT THE COUNTER, at the top right of the counter: "Mrs. Wilhelmina Mueller, a regular for no months; hale, content." It runs over Miss Schneider's head (`D/c07_desc.png`). Hovering a figure shows a different text in a separate tooltip: "A regular for 0 months" (`D/c09.png`). The proof's strike-outs now read "Mrs. Wilhelmina Mueller, a new customer. _dele (strike out)_" and "…And one more."
- **Jan, VENTURES (none on offer).** Focus opens on PUT IT DOWN. Tab ×6 stays on it, so the trap holds for Tab. Escape → VENTURES. Each card says what it waits for ("Offered once the Gazette will sell you space."). The fourth card, A LABORATORY, is below the fold. With only one focusable control, the keyboard can't scroll to it: PageDown does nothing (`D/11_ventures_open.png`, `D/15_v_scrolled.png`, mouse wheel). **Down ×3 moved focus out of the drawer to OPEN THE LETTERS, SUPPLIES and then SEND TO PRESS** behind the dimmed bench. Enter there produced "SEND IT ANYWAY" (`D/13_v_down.png`, `D/14_enter_behind.png`).
- **Jan, sound toggle.** "THE SHOP: HEARD" becomes "THE SHOP: HUSHED". The only mention of sound is a hover tooltip, "Sound is on. Press to silence it.", in Godot's default grey box. That tooltip was still showing "Sound is on…" after the button already read HUSHED (`D/c17.png`, `D/c18.png`).
- **Jan, letter.** Opens on "☞ Promise a stronger preparation." with its effect line. After the reply, the Sent strip reads "Sent: "Promise a stronger preparation." At once: your standing rises 1." Focus first reported the removed button, then went to PUT IT DOWN on Tab. Escape → SEND TO PRESS, because the opener (OPEN THE LETTERS) had gone (`D/21_replied.png`, `D/22_after_letters_esc.png`).
- **Jan, formula book.** Opens on the Water jar, and the trap has 21 stops. After ENTER IT IN THE BOOK, focus went to the Grain Alcohol jar, the last jar touched. **Right ×4 left the book for THE STILL**. Enter there closed the book and opened the still (`D/27_entered.png`, `D/28_still_from_behind.png`).
- **Jan, still.** ORDER ENOUGH → LIGHT THE FIRE → focus `none`. The card now says it once: "Every still is on the fire (24 bottles…). The bottles will be on the shelf by press time; send the paper to press when you are ready." (`D/31_lit.png`). Escape → THE STILL.
- **Jan, supplies.** Leader dots on the price current. The +8 focus ring is clear. Escape → SUPPLIES (`D/32_supplies.png`).
- **Jan → Feb, press.** "☞ Click, or press any key, to see the edition." is in the footer from the first frame (`D/press/footers.png`). The brief now has one net figure, "the books up $4.44", with a "☞ the books" ledger cutting (`D/34_books.png`).
- **Mar, letters.** The inquest replies give different odds: "about 7 in 10 for naming" against "about 6 in 10" (`D/35_mar_letters.png`). After my reply, focus went to the next letter's "☞ Print it as a testimonial." at y=419, in view with its effect line (`D/36_after_reply.png`). After the last reply, focus was `none`.
- **Apr, proof.** The proof cycle is now 25 stops, and THE STILL is 21 Tabs from SEND TO PRESS. ¼ COL by keyboard shows "YOUR SPACE · ¼ COL · $3.00" at once (`D/39_quarter.png`). Death stands beside "poorly" Widow Greco. The focused tag's description doesn't mention him (`D/c40.png`).
- **Apr, VENTURES.** It opens with focus on **"☞ TAKE IT UP · $40.00"** (`D/43_v_apr.png`). Down/Up reached the claims in the type case behind the drawer, and Enter set SOOTHES THE NERVES (`D/44_claim_behind.png`). Enter on TAKE IT UP took the almanac at once. Feedback: "Taken up: The Proprietor's Almanac. It will be in the next edition." The card's button became "YOURS · twelve months to run", and the Works and the footer dropped to $121.76. Focus went to `none` (`D/46_takeup.png`).
- **Apr → May.** With the VENTURES drawer still open, Down ×6 → SEND TO PRESS, then Enter: "THE PRESSES ARE RUNNING". The unanswered Sanitary Inspector letter got the default reply (`D/48_send_behind_before.png`, `D/49_send_behind_after.png`). The May edition reports the almanac under NOTICES (`D/50_apr_ed.png`).
- **Jump game, Jan 1889.** The Laboratory card shows a disabled "NOT TO BE HAD" with the reason printed under it, "Not enough cash.", and Tab skips it (`D/k02_ventures_scrolled.png`). The Works lists "☞ Death waits at the counter beside Miss Abigail Wilson." **Enter on VENTURES, then Enter 250 ms later, bought the almanac**: $324.69 → $284.69 (`D/k03_double_enter.png`).
- **Jump game, Apr 1889.** The first tag, focused, shows the same weak cue and a shorter tag (64 px against 78 px). The description "…poorly, shaking for want of a dose." wraps onto Mr. Goldberg's cap (`D/k04_tag_ill.png`).

## Metrics

**Contrast (WCAG 2.x, from screenshots).** Body text needs 4.5:1, and UI components need 3:1.

| Screen · element                                                  | Ratio      | Pass?                    |
| ----------------------------------------------------------------- | ---------- | ------------------------ |
| Counter description ("Mrs. Wilhelmina Mueller, a regular…")       | 11.6       | ✅                       |
| "AT THE COUNTER" (dimmed behind a drawer)                         | 8.8        | ✅                       |
| Tag status ("hale · new", ≈10 px italic)                          | 6.8        | ✅ contrast, small size  |
| Works suspicion line                                              | 6.6        | ✅                       |
| Manicule warnings (paper on black)                                | 10.0       | ✅                       |
| VENTURES: "It brings about ten more callers a month."             | 9.2        | ✅                       |
| VENTURES: "Offered after fifteen hundred bottles sold…"           | 6.8        | ✅                       |
| VENTURES: disabled "NOT TO BE HAD"                                | 4.0        | ✅ (disabled is exempt)  |
| VENTURES: reason "Not enough cash."                               | 5.8        | ✅                       |
| **Counter-tag focus cue** (focused against unfocused tag)         | the inner rule is removed (≈1–2 px); no added ring | ❌ (#3) |

**Keyboard, steps per task:**

| Task                                                   | R4                | R5                                                                      |
| ------------------------------------------------------ | ----------------- | ----------------------------------------------------------------------- |
| Proof Tab cycle, January                               | 6 stops           | **13** (+5 counter tags, +VENTURES, +letters)                           |
| Proof Tab cycle with ad space and 7 claims             | —                 | **25** (26 with 8 claims)                                               |
| SEND TO PRESS → THE STILL (ad space open)              | 15 Tabs           | **21 Tabs**                                                             |
| Minimal month (proof → still → order → light → press) | 21 keys           | **32** with ad space open; **20** in January                            |
| Escape from a drawer: focus lands on                   | SEND TO PRESS     | the opener: THE STILL, SUPPLIES, VENTURES ✅; letters after the last reply → SEND TO PRESS |
| Open VENTURES and buy the first offer                  | —                 | **2 keys** (Enter, Enter, 250 ms apart), no confirmation                |
| Arrow keys out of a drawer to SEND TO PRESS            | not tested        | **6 Downs** (VENTURES), **3 Downs** (January), then Enter sends         |

**Focus after actions:**

| Action                              | Focus lands on                                                       | OK?         |
| ----------------------------------- | -------------------------------------------------------------------- | ----------- |
| Open VENTURES with an offer         | ☞ TAKE IT UP · $40.00                                                | ❌ (#2)     |
| Open VENTURES with no offer         | PUT IT DOWN                                                          | ✅          |
| TAKE IT UP                          | `none`                                                               | ❌ (#5)     |
| Reply with another letter left      | the next letter's first reply, in view, with its effect line         | ✅ (fixed)  |
| Reply to the last letter            | `none` (once, the removed button)                                    | ❌          |
| LIGHT THE FIRE                      | `none`                                                               | ❌          |
| ENTER IT IN THE BOOK                | the last jar touched (Grain Alcohol)                                 | ⚠           |
| Escape from a cutting               | the same item                                                        | ✅          |

**Feedback for TAKE IT UP:** a Sent-style strip, a change of button text ("YOURS · twelve months to run"), the cash drops in the Works and the footer, and an item in the next edition. The feedback is good. The only fault is that it came too easily.

## Findings

| #   | Severity | What I saw | Where | How to reproduce | Suggested fix |
| --- | -------- | ---------- | ----- | ---------------- | ------------- |
| 1   | Major    | **Arrow keys leave every drawer's focus trap and work the dimmed bench behind it.** Tab is trapped, but Up/Down/Left/Right are not. With VENTURES open, Down ×6 reached SEND TO PRESS, and Enter sent the paper to press. An unanswered letter went out with the default reply. Down/Up also reached the claims, and Enter set SOOTHES THE NERVES on the label behind the drawer. In the formula book, Right ×4 reached THE STILL. The bench looks dimmed and inert, and the focus ring on it is hard to see. | All drawers (VENTURES, formula book, letters, still) | `ux-5`, April 1885: open VENTURES, Down ×6, Enter (`D/48_send_behind_before.png`, `D/49_send_behind_after.png`). Claim: Down/Up, Enter (`D/44_claim_behind.png`). Formula: Right ×4 (`D/28_still_from_behind.png`). | While a drawer is open, set the bench controls' `FocusMode` to None (as the mouse filter already does for clicks). Or set `focus_neighbor_*` on the drawer's edge controls to themselves, and consume `ui_up/down/left/right` that would leave the drawer. |
| 2   | Major    | **VENTURES opens with focus on the purchase, and one Enter buys with no confirmation.** Enter, then Enter 250 ms later, on the VENTURES button took up the almanac: $40, 12% of cash. There is no undo. The letters' 0.8 s guard doesn't apply here. Round 3's "Enter buys an ad" accident has come back in a new drawer, and the branch ($600) and laboratory ($400) buttons work the same way. | VENTURES drawer | `ux-5b --play 48 --dose 0.5`, January 1889: focus VENTURES, Enter, Enter (`D/k03_double_enter.png`; cash 32469 → 28469). Also April 1885 (`D/43_v_apr.png`). | Open the drawer on PUT IT DOWN or the first card's heading. Make TAKE IT UP two-step ("TAKE IT UP · $40.00" → "SIGN FOR IT · $40.00 ✓" / "NOT NOW"). Apply the 0.8 s guard. |
| 3   | Minor    | **The focused counter tag is barely distinguishable.** The focused tag *drops* its inner rule and lifts by about a pixel. There is no ring, wash or underline. At 1× I couldn't tell which tag had focus without zooming. The description appears 250–500 px away, under AT THE COUNTER, and runs over the figures' heads ("Mueller, a regular" over Miss Schneider; "shaking for want of a dose" over Mr. Goldberg's cap). Focused tags can also be shorter (64 px against 78 px). | Proof, counter | Any proof: Tab to the tags (`D/c06_tags.png`, `D/c07_tags.png`, `D/c07_desc.png`, `D/k04_tag_ill.png`). | Give a focused tag the slips' double-rule ring, or invert it like a selected claim. Also mark its figure (a rule under the feet). Print the description on a paper slip just above the focused tag, clear of the heads. |
| 4   | Minor    | **The counter tags make the proof's Tab cycle noisy.** Five information-only stops sit between the footer and the type case on every trip round. The cycle went from 6 to 13 stops in January, and to 25 once ad space opens. THE STILL is 21 Tabs from SEND TO PRESS, up from 15, so a minimal month went from 21 to 32 keys. | Proof | `ux-5`, April 1885: Tab from SEND TO PRESS (`k.py` log in this report) | Make the counter a single Tab stop, with ←/→ moving between tags (a roving tabindex). Put it after the Works, not before the type case. |
| 5   | Minor    | **Focus is still lost after several actions.** It goes to `none` after TAKE IT UP, LIGHT THE FIRE and the last reply. Once, after the last reply, the focus query reported the removed reply button. ENTER IT IN THE BOOK still sends focus to the last jar touched. | VENTURES, still, letters, formula book | `D/46_takeup.png`, `D/31_lit.png`, `D/37_last_reply.png`, `D/27_entered.png` | Fall back to the card's next control or to PUT IT DOWN. After TAKE IT UP, focus the card's new "YOURS" line or the next offer. |
| 6   | Minor    | **The VENTURES drawer can't be scrolled by keyboard.** In January only PUT IT DOWN is focusable, and the fourth card (A LABORATORY) is below the fold. PageDown does nothing, and Down leaves the drawer (#1). | VENTURES | `ux-5`, January 1885 (`D/11_ventures_open.png`, `D/12_v_pgdn.png`). Mouse wheel works (`D/15_v_scrolled.png`). | Make each card focusable (follow-focus scrolls it into view), or bind PageUp/PageDown and the arrows to the drawer's scroll. |
| 7   | Minor    | **"THE SHOP: HEARD" doesn't say it is a sound control.** The word "sound" appears only in a hover tooltip, Godot's default grey box, which is out of style and clipped by the footer rule. That tooltip went stale: after the switch to HUSHED it still said "Sound is on. Press to silence it." A keyboard player never sees a tooltip. "HEARD" also reads like a status ("the shop is well known"). | Footer | Edition or proof: focus or hover THE SHOP: HEARD, press Enter (`D/c17.png`, `D/c18.png`) | Keep the period phrase but add a cue: "THE SHOP: HEARD ♪" / "HUSHED", or a small "sound" caption under it. Refresh the tooltip on toggle, and style it as a paper slip. |
| 8   | Minor    | **Drawers are see-through between and below their cards.** Proof text and counter tags show through the gaps ("Mr. Luigi Lombardi, a new", "COMPOUND", "MUELLER.—The infant of Mrs.", "THE LEAD STORY"). The proof column behind a drawer is barely dimmed, so the eye picks up stray words mid-reading. | VENTURES, letters drawers | `D/11_ventures_open.png`, `D/21_replied.png`, `D/36_after_reply.png` | Put the cards on an opaque backing panel. Dim the proof as much as the right-hand column. |
| 9   | Minor    | **Escape from the letters drawer after the last reply lands on SEND TO PRESS**, because the opener (OPEN THE LETTERS) has gone. "Escape, Enter" then sends the paper. | Letters drawer | `ux-5`, January and March 1885 (`D/22_after_letters_esc.png`) | When the opener is gone, fall back to THE STILL, or to the Letters box heading, never to SEND TO PRESS. |
| 10  | Low      | TAKE IT UP's terms note, "Paid at once; it starts this month.", is a hover-only tooltip. "Offered after fifteen hundred bottles sold" gives no progress figure. | VENTURES | `targets` tooltip, `D/k02_ventures_scrolled.png` | Print the note under the button. Add "(you have sold 1,212)". |
| 11  | Low      | A new customer is "a regular for no months" (focus) or "A regular for 0 months" (hover). A hover tooltip and a focus description can show two different customers at once. | Counter | `D/c07_desc.png`, `D/c09.png` | "New this month." Hide the focus description while a hover tooltip is up. |
| 12  | Low      | The hyphenation is fixed, but rivers remain after caps run-in heads: "AMONG THE CALLERS.—Mrs.", "POLICE COURT.—Patrick", "NOTICE.--The Ladies' Temperance", "A TESTIMONIAL.—"Three physicians", "WANTED.—Bottles, half-pint, round,". The em dash still prints as "--" in "NOTICE.--The". | Edition, 270 px columns | `D/01_first.png`, `D/03_trade_line.png`, `D/c_dash2.png`, `D/mar_ed.png` | Let the first line after a run-in head set ragged, or allow letter-spacing on caps-only lines. Check the em-dash glyph in the regular weight. |
| 13  | Low      | A jar's focus is still only about 1 px thicker than its in-glass border. The drug shelf still cuts off the bottom row ("Dilute Sulphuric Acid"). | Formula book | `D/c24.png`, `D/c25.png`, `D/23_formula.png` | Use the double-rule ring. Let the shelf scroll, or tighten the cards. |
| 14  | Low      | Death stands beside "poorly" customers (Widow Greco, Miss Hale) with no Works warning and nothing in the tag description. The warning appears only for some cases ("Death waits at the counter beside Miss Abigail Wilson."). | Counter, Works | `D/c40.png`, `D/k04_tag_ill.png` against `D/k02_ventures_scrolled.png` | Whenever the figure is drawn, add "Death waits beside her" to the tag description and the Works. |

## What works

- **Escape restores the opener.** THE STILL, SUPPLIES, VENTURES and the cuttings all hand focus back to what opened them. Round 4's "Escape, Enter sends the paper" is gone for every drawer except letters after the last reply.
- **Focus after a reply** moves to the next letter's first reply and scrolls it into view with its effect line.
- **The press hint** is in the footer from the first frame of the run.
- **The edition's follow-focus** leaves about 40 px of margin top and bottom.
- **The inquest odds differ by reply** ("about 7 in 10" / "about 6 in 10"). That makes it a real decision.
- **The VENTURES prospectus** is clear. Each card gives terms, what it brings, a "Mark well" risk in italic, and when it will be offered. An offer that can't be had shows a disabled button with its reason printed under it ("Not enough cash."), and Tab skips it. TAKE IT UP gives four kinds of feedback: the Taken-up strip, "YOURS · twelve months to run", the cash figure, and a paper item. None of it depends on sound.
- **The ledger cutting** ("☞ the books") and a single net figure in the brief ("the books up $4.44").
- **The still card** says one thing once, and the type case prints "up to four".
- **Hyphenation** breaks at the right places ("neighbor-hood", "rheuma-tism", "an-swer").
- **"Death waits at the counter beside Miss Abigail Wilson."** puts the skeleton's meaning into text in the Works.
- **Contrast** is good throughout: everything I measured is at 5.8:1 or better except the exempt disabled label.

## Disclosures

- **Jump-ahead:** two jump-ahead games, `ux-5 --play 30 --dose 0.25` and `ux-5b --play 48 --dose 0.5`. The machine restarted during my first game (`ux-5`, April→May 1885 press), which stopped it. I continued in jump-ahead games, as the coordinator asked, to reach later ventures. None of my games reached the branch, the wholesale contract or an affordable laboratory, so I took up only the almanac (twice, in two games).
- **State reads:** I used `targets`, `read`, `state` and the driver's `focus` query to find coordinates, quote text and confirm cash changes. The `focus` query showed where the arrow keys took focus behind the drawer (#1), and that focus was `none` (#5). On screen those show only as a faint ring on the dimmed bench, or no ring at all.
- **Scripted months:** `month.py` played January to March 1889 by mouse (first reply to each letter, brew, press). Those months were for coverage, not for judging the keyboard.
- **Shift could not be tested:** the driver sends keys without modifiers, so Shift+Tab was not tested.
- **The arrow-key escape (#1) may predate round 5:** I didn't test arrows in round 4.
- **Not re-tested:** a fifth claim, and FRONT against a ten-death month. The mouse behind a drawer was not re-tested either, though the summary says the bench now ignores the mouse.
- **Driver log:** across all three games, only the ALSA/dummy-audio warnings and one audio `ERR_CANT_OPEN`. No exceptions.
- **Code:** I didn't read the source.

## Round-4 findings re-checked

| R4 # | Severity | Status now | Evidence |
| ---- | -------- | ---------- | -------- |
| 1    | Major    | fixed      | After a reply, focus is on "☞ Print it as a testimonial." at y=419, in view with its effect line (`D/36_after_reply.png`). |
| 2    | Major    | partly     | Escape → THE STILL / SUPPLIES / VENTURES / FORMULA. After the last letter is answered, Escape → SEND TO PRESS (`D/22_after_letters_esc.png`, #9). |
| 3    | Minor    | partly     | The type case says "Claims for Dr. —'s Celebrated Panacea, up to four." A fifth click was not re-tested. |
| 4    | Minor    | fixed      | The hint is in the footer from the first frame (`D/press/footers.png`). |
| 5    | Minor    | fixed (as seen) | The next letter's slip and effect line are in view after a reply. I didn't Tab through a 5-letter drawer this round. |
| 6    | Minor    | not fixed  | `none` after LIGHT THE FIRE and the last reply; the last jar touched after ENTER IT IN THE BOOK (#5). |
| 7    | Minor    | partly     | The breaks are correct now. Rivers and "--" remain (#12). |
| 8    | Minor    | partly     | The infant is now right ("MUELLER.—The infant of Mrs."), and "And one more." covers the rest. The strike-out wording ("a new customer") still isn't the edition's ("AMONG THE CALLERS.—… wife of a working man"). |
| 9    | Minor    | partly     | "YOUR SPACE · ¼ COL · $3.00" shows on the proof. With 8 claims the ad row sits below the type case's visible area (`D/j02_proof.png`), but Tab focus reaches it. |
| 10   | Minor    | fixed      | y=40 top margin; trade line at 672–696 (`D/02_tab_caller1_after_cycle.png`, `D/03_trade_line.png`). |
| 11   | Minor    | not re-tested | — |
| 12   | Minor    | fixed      | "about 7 in 10" against "about 6 in 10" (`D/35_mar_letters.png`). |
| 13   | Low      | not re-tested (mouse) | The keyboard can now reach the bench by arrows (#1). |
| 14   | Low      | partly     | The brief has a net figure and a ledger cutting (`D/34_books.png`). I didn't check column balance. |
| 15   | Low      | not fixed  | `D/c25.png`, `D/23_formula.png` (#13). |
| 16   | Low      | fixed      | One line on the lit card (`D/31_lit.png`). |
| 17   | Low      | partly     | "_dele_ (strike out)" now explains itself. The state words are still ≈10 px italic. |
| 18   | Low      | partly     | The counter tags are in the Tab order, and "Death waits at the counter beside…" is in the Works. The focus cue is weak (#3), and Death is unexplained beside "poorly" customers (#14). |
| 19   | Low      | partly     | The dash still prints "--" in "NOTICE.--The" (`D/c_dash2.png`). |

## Score

**7.5 / 10** (round 1: 6, round 2: 7, round 3: 7.5, round 4: 8). The round-4 focus fixes landed cleanly: Escape restores the opener, focus after a reply stays visible, the press hint shows, and the edition scrolls with a margin. If only those had changed, this would be an 8.5. The new drawer brings back the round-3 class of accident. It opens on a money button, and Enter, Enter spends $40 without asking. The arrow-key leak lets a keyboard player send the paper to press, or change the label, from behind a drawer that looks modal. The counter tags make keyboard play slower (32 keys for a minimal month, up from 21), and their focus cue is very hard to see.
