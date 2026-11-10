# Panacea · round 9 fix · THE WORKS, the drawers and the proof

Godot only. No sim or content change, so no parity fixtures. Files: `godot/scripts/UI/ProofView.cs`,
`FormulaBench.cs`, `StillBench.cs`, `Prospectus.cs`, `OrderForm.cs`, and two new ones, `MonthBook.cs` (the month's
day-book and the books by month, read from the save) and `BooksDrawer.cs` (a fifth drawer, THE BOOKS). Checks: TS
140/140, typecheck, lint, `content:lint` (0/0), `check_cuts.py`, `dotnet test` 117/117, Godot build with 0 warnings.

Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r9fix/works/`.
The `base_*.png` files are before the fix. To check the round-9 cases exactly, I copied the cozy and lapsed testers'
saves to a profile of my own, cut each save's log back to the month in question, and deleted that profile afterwards.
The testers' own saves were not touched.

## 1. Danger that looks the same at every level (low-vision #2, #3)

> "Suspicion uses the same 6 px italic line whether it reads 'none to speak of (0 of 100)' or 'alarming (47 of 100)'."

- **Suspicion** now has its own line in THE WORKS, set heavier at each step:
  - under 3: pencilled italic;
  - under 15: roman, one size up;
  - under 40: capitals in a ruled box ("SUSPICION: A GOOD DEAL · 22 OF 100");
  - 40 and over: a reversed slug, white on black, the width of the slip ("☞ SUSPICION 47 OF 100 · ALARMING");
  - 70 and over: the same slug, larger.

  A ruled gauge filled with ink to the figure sits beside or under it. See `p1.png`/`h3.png` (47), `h1c.png` (22),
  `p4.png` (3) and `d1.png` (1).

- **Customer health** on the counter cards: "hale" in italic, "peaky" in roman, "POORLY" in capitals, and "GRAVELY
  ILL" reversed, white on black (`z_cards.png`, `h1b.png`). The card's tooltip now gives the key to the four words.
  THE WORKS' health line puts "N GRAVELY ILL" in a reversed slug and has the full width of the slip.
- A drawer now dims the bench to 0.8 rather than 0.6, so cash and suspicion stay readable while a letter asks for
  money (low-vision #6).

## 2. "Habit: none" for spirits (cozy #1)

`FormulaBench.Words`: habit reads "none" only for 0. Anything above 0 and under 1 reads **"slight: the spirit in
it"** when the habit comes only from spirits, and "slight" otherwise. The verdict adds: "The spirit in it makes a
slight habit: a regular who takes it month after month may come to ask for it by name." The Habit gauge's tooltip
says the same (`f3.png`: the starter mixture, 2 oz of grain alcohol, habit 0.8). **Assumed value: none.** The label
is true for any habit above zero, so it holds whatever threshold the sim agent sets for "craving". THE WORKS'
"craving" tooltip names the cause too.

## 3. Deaths you can read (satire reader #5, cozy #2)

- **The warning covers every regular at risk.** "Death waits beside X: …" now goes on: "Also at risk: A and B." (or
  "Four more are poorly or gravely ill, and at risk as well."). A shop with no opiate is told: "Any regular may die
  of age or illness, whatever the bottle; the frail most of all." Death's own tooltips say the same. When nobody is
  frail, the far-end tooltip no longer says "Nobody is near enough for him": it says anyone may die, the peaky
  sooner than the hale.
- **A dead regular's line on the proof says whether the bottle played a part.** Under each of last month's deaths in
  the proof's DEATHS column, there is now:
  - the register's cause;
  - the bottle's part: "the register blames the bottle" (an overdose or wasting); "The remedy is harmless by the
    assay; the bottle is not blamed" (assay danger under 0.8, nothing building up, and the dead regular was not
    craving); "A strong bottle may have hastened it; nobody can say"; or "Nothing had been bought at the shop in six
    months";
  - the inquest: the verdict, "An inquest sits", or **"No inquest was called."**, with no "Yet.".

  This is checked on the cozy tester's own Mr. Bruno (cozy save cut to 20 presses; `bruno.png`): "Of a fever going
  about, the register says. The remedy is harmless by the assay; the bottle is not blamed. No inquest was called."

- **Still open, in a file I don't own** (see the end): the IN MEMORIAM clipping in the paper itself still ends "No
  inquest has been called. Yet." (`EditionWriter.cs:104`). `Scrutiny.RecordDeath` decides the inquest when the death
  happens, so no later inquest is possible, and the "Yet." is false.

## 4. "This month so far" (lapsed #1)

`MonthBook` reads the game's own save (the seed and every action since the last "press"), so it is the same after a
relaunch or a hard kill. A **THE MONTH SO FAR** list now sits in the lead story's box on the proof. It shows:

- money laid out ("Laid out so far: $44.00 ($163.38 when the month opened)");
- letters answered;
- ventures taken up;
- remedies entered, recipes changed, and prices changed ("Price of X: 45c., now 50c.");
- claims reset;
- space bought;
- stock ordered;
- fires lit.

The post keeps a trace of answered letters: "THE POST IS ANSWERED · One letter answered this month; nothing more in
the post", or "At Two in the Morning (one answered)." I checked this before and after a `drive_stop` kill and
CONTINUE (`cz2.png`, `cz3c.png`: identical).

## 5. Why the till is flat (cozy #3; the tycoon's old "no history view")

- THE WORKS gains a line: "Last month: took $19.80, lost $1.72; rent and charges $11.00."
- A new **BOOKS** drawer (fifth tab) has three parts:
  - **Where last month's money went**, in words: every cash line by name and amount.
  - **What a bottle earns**: price, cost and margin for each remedy, then the break-even count. On the cozy save
    (`cozy_books.png`) it reads: "The rent, the ventures' upkeep, the subscriptions and the Gazette came to $11.00
    last month, sold or not. At the best margin above, the first forty-three bottles a month only pay for them;
    last month sold forty-four…" That is the cozy tester's year two, explained.
  - **By the month**: sold, took in, the books, cash, regulars (and deaths), each month with a rule of ink for the
    cash. It is rebuilt by replaying the save in a private copy of the game, and only new actions are replayed
    after the first opening.

## 6. Low stock (cozy #4)

A new note: "Low stock: thirty-five bottles on the shelf and nothing on the fire, and last month sold thirty-eight.
Light THE STILL, or the shelf will be bare before the month is out." It shows when the shelf plus the still holds
fewer than last month's sales plus a tenth (at least two more) (`dz_all.png`, third panel). With 17 on the shelf
after a 17-bottle month (the cozy April), it fires.

## 7. Jargon (cozy #5, #7; older rounds)

- "dele (strike out)" on every struck line is replaced by one note per section, "last month's, struck out by the
  printer". Its tooltip: "Nothing for you to do."
- "standing in the ward: 1" becomes "good name in the ward: 1 of 100". The tooltip says this is the "standing" a
  letter's "standing +1" moves.
- Assay rows: "Lingers: passes through" becomes "Builds up: passes through / slowly / in the body". "In an assay: a
  chemist might find it" becomes "A chemist: would not / might / would find it". Every gauge, Kick included, has a
  one-line tooltip with the chemist's figure under it.
- SIMPLE MIXTURE, STEEPING and PERCOLATION have tooltips with their real effects (from `processes.json`) and their
  cost per bottle.
- "Peaky" is explained in the card key.
- **"Better known" is left open.** It is `Observation.Notoriety`, and the Godot `DeskModel` doesn't carry it (see the
  end).

## 8. Layout

- **The drawer tabs are pinned.** The bench is now the type case (which takes the slack), then the post (fixed at
  104 px), THE WORKS (a spacer keeps the tabs at its foot) and the lever, stacked from the bottom. The ☞ notes moved
  off the bench into the proof's lead-story box, as **NOTES FOR THE PROPRIETOR**, set in ink on paper. The tabs sat
  at y = 618–640 in every state I checked: no letters, one, two, answered, alarming suspicion and a grown works slip
  (`p3`, `d1`, `m5`, `m5a`, `h3`, `h1c`).
- **The type case**: claims that run below the frame show a "MORE CLAIMS BELOW ▼ (scroll)" slug at the foot. There is
  padding, so the last claim scrolls clear of it. The longest claim wraps inside 200 px (`targets`: 200×50).
- **The drug shelf** ends inside the drawer. The FORMULA bench now fits the drawer's height (`f2.png`), and "more jars
  below ▼" tells you the shelf scrolls. The jar sub-labels are roman, at 13 rather than italic 11.
- **The ±5c buttons** are a fixed 52×28 around the price figure. The remedy's name is on its own line above them.
- **Stale tooltips**: every drawer frame, the shelf and the type case "move" the pointer when they scroll, so a
  tooltip follows whatever is now under it. The venture notices no longer carry a tooltip of their own name, which
  is what floated before.
- **THE STILL**: the "On the fire" lines are now under the buttons. Lighting a still left LIGHT THE FIRE where it was
  (`cz_still.png` → `cz_still2.png`). "Three stills are free" is capitalised.

## 9. The still

- **Marked bottles**: solid ink with a paper label band (and a green rule for the product). Bottles wanted beyond the
  stock are hatched, and unmarked ones are bare outlines (`s2.png`, `s3.png`). This reads in monochrome.
- **Prices**: both buttons now name stock and glass apart. Single: "ORDER FOR 24 · $1.86" and "LIGHT THE FIRE · 12 ·
  36c. GLASS". Both stills: "BOTH STILLS · ORDER AND LIGHT 12 EACH · $3.72 + 72c. GLASS".
- **Water**: free ingredients count as always on hand. Lighting the fire draws them from "the pump" first, and they
  never block the batch or show as "short". SUPPLIES shows "the pump" in place of "0 oz". Checked in a new game:
  order and light worked with 0 oz of water (`s4.png`).

## 10. Ventures

- "Taken up: The Shop Next Door, for 0c." now reads "Taken up: The Shop Next Door, nothing down, and $5.00 a month
  after. The next edition will report it." (`lp3.png`, the lapsed save cut before month 15).
- The "So far" lines are split into what the venture waits on and its price. They read "Waits on a third still, 566
  of 600 bottles sold." / "So far: 566 of 2,000 bottles sold." / "Its price: $250.27 of the $300.00 in hand." The
  price no longer reads as a condition of the still. The branch's "the shop next door taken (in VENTURES)" reads
  "the shop next door, which is not yet taken up (it is above)".
- **Arrivals are announced.** The month a still, the percolator, newspaper space, the wholesale supplier or quality
  control arrives, a note says what it is, what earned it and where it is. For example: "New: a glass percolator,
  earned by running ten batches. PERCOLATION is now in the FORMULA book… Nothing was spent." (`pc_all.png`, month
  10). FORMULA is printed in reverse until opened when a new method appears, as THE STILL and VENTURES already were.

## 11. Price feedback (low-vision #9)

Under the price in the formula book: "At 60c. last month, six asked the price and went away." (or "nobody went
away"). This is the sim's `TurnedAwayByPrice`, which is shop-wide, so with two remedies the price is left out.
THE BOOKS repeats it.

## Left open, and changes needed in files I don't own

1. **`godot/scripts/Paper/EditionWriter.cs:104` (paper agent).** Replace `"No inquest has been called. Yet."` with
   `"No inquest was called."`. Add the bottle's part from the same facts, as `ProofView.BottleNote` does. For a death
   whose `Cause` is old_age/ailment/natural, the product's `Danger < 0.8`, `Accumulation < 0.3` and `!Craving`, use
   "The remedy is harmless by the assay; the bottle is not blamed." When `Product == null`, use "Nothing had been
   bought at the shop in six months." Otherwise use "A strong bottle may have hastened it; nobody can say."
2. **"Better known" meter (`IGame.cs`/`SimGame.cs`, whoever owns them).** Add `public double Notoriety;` to
   `DeskModel` and set it from `o.Notoriety` in `SimGame.Desk()`. THE WORKS can then print "known in the trade: N",
   and LettersDesk's "you become better known" can say which figure it moves and that it makes a paid coroner likelier
   to be found out (`Scrutiny.BribeExposureChance`).
3. **Large print (Print.cs agent).** My text goes through `Print.Label/Body/Button`. Text drawn directly with
   `DrawString` won't scale: the bottle shelf and the still card in `StillBench.cs`, and `MeasuringGlass.cs` (glass
   labels, sizes 12–15). The five tabs in THE WORKS fit 352 px only with the side margins I tightened. At 125%+ they
   would want an `HFlowContainer`, or the shorter labels "STILL", "BOOKS".
4. **The footer's two cash figures after a mid-month resume (lapsed #2)** are in `Main.cs`. THE MONTH SO FAR now
   gives the opening cash and the amount laid out, which explains the gap, but the footer itself is unchanged.
5. **Ledger labels (cozy #10)**: jury duty and the Jersey distiller book under `cause.event` "Letters answered". THE
   BOOKS shows that label as "what the letters cost". A finer cause needs a sim/content change.
