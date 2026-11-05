# Panacea · round 6 · playtest by "Keyboard-only player with low vision"

(DRAFT, in progress.) Plays with Tab / Shift+Tab / arrows / Enter / Escape only; needs a visible focus, readable text, nothing hover- or sound-only.

## Notes as I go
- Edition 1 (Jan 1885), seed 6025, port 47025. Focus starts on TO THE PROOF. Focus ring on buttons = a thin inner second rule (1-2 px). Tab order: TO THE PROOF -> SOUND -> 4 PERSONAL clippings -> THE TRADE line -> back. Shift+Tab not yet tested.
- Focused clipping: grey wash (204,201,195 on 234,231,223 paper, about 1.3:1) plus a heavier left rule. Enter opens the clipping card; Escape closes and focus returns to the clipping. Good.
- PageDown scrolls the paper. Body type is small (~14 px).
- Jan: TO THE PROOF -> focus lands on SEND TO PRESS (two Enters from the paper would send an empty month). Proof Tab order: SOUND, LAST EDITION, counter (arrows pick customer; focused card inverts: good), STILL, FORMULA, SUPPLIES, VENTURES, SEND. Type case claims join the Tab ring once a recipe exists.
- Formula: jars by arrows/Tab, Enter pours 0.5 oz, Backspace takes back 0.5 oz (found by trying; on-screen text only says click / right-click / Shift). Focused jar looks the same as a jar with drug in the glass (thick border + grey). LineEdit focus has no visible ring. ENTER IT IN THE BOOK sends focus back to the last jar.
- Type case: claims 6 and 7 (CURES CONSUMPTION...) take focus while scrolled out of view below the box: invisible focus.
- Still: batch grid takes focus but no key changes the count (arrows move focus away; +/-/Enter/Space/Home/End nothing). After LIGHT THE FIRE focus goes to "none".
- Supplies: +8/+32 focus ring is a clear double frame. Ventures: focused card has a heavy black border, list scrolls with focus: good.
- Press screen: "Click, or press any key, to see the edition." good. Feb edition: THE MONTH IN BRIEF strip opens The Month's Books by Enter. 20 bottles sold, books up $4.08.
- Feb->Mar: pressed Space ~9.5 s after SEND; the edition had already appeared on its own with focus on TO THE PROOF, so Space skipped the whole March edition straight to the proof (recovered via LAST EDITION).
- Pressing a key during the press animation works, but afterwards the footer keeps saying "Click, or press any key, to see the edition." over the edition (no cash/date line) — seen Mar and Apr.
- -5c and POUR IT INTO THE BOTTLE drop focus to none (second Enter does nothing). Recovery by Tab restarts at PUT IT DOWN: 16 Tabs back to -5c. Shift+Tab could not be tested (driver sends no modifiers).
- Months: Jan 20 sold +$4.08 12 reg; Feb 14 sold +$0.48 12 reg; Mar (55c) 25 sold +$5.83 14 reg; Apr 27 sold +$6.93, 1 new 2 gone.
- May 32 sold (+$9.68); Jun 2 letters: printed testimonial (Bridget Sullivan), joined St. Bridget's benefit society ($1.50/mo). Letters drawer: focus starts on first reply, moves to next letter after a reply: good. Focus to none after the last reply; Escape then puts focus on SEND TO PRESS.
- Jun: batch size cannot be changed by keyboard; ONE mouse click on the bottle grid (700,345) set 48. Disclosed.
- Jul: bought 1/4 COL by keyboard (selected = inverted + frame). 45 sold, a second still arrived ("IN THE TRADE.—A second copper still...").
