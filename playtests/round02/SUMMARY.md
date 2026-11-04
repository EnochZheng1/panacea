# Panacea · review round 2 · summary

Five persona testers played the frozen build `f90405e` through the real Godot UI with the
scripted-input driver. The build carried the balance pass and the drawer redesign: letters on
letterhead, the wholesaler's order form, and the still with its bottle shelf. Each tester had their
round-1 report and re-checked it. They played 13–17 months by hand in each main game and disclosed
every jump-ahead game. The tycoon veteran also ran the balance bots and variants of them. Reports
are in this folder.

| Persona                   | R1      | **R2**  | Headline                                                                                                   |
| ------------------------- | ------- | ------- | ---------------------------------------------------------------------------------------------------------- |
| Casual first-timer        | 5.5     | **7**   | The lead pays off the press now, but the footer and the paper give two different "how did I do" figures    |
| Careful newcomer          | 6       | **6.5** | The order form, the still and every ledger add up to the cent, but the new text contradicts itself         |
| Management-sim veteran    | 5       | **7**   | Honest play is viable and the dark route is a real gamble, but silence at inquests is the dominant line    |
| UX / accessibility critic | 6       | **7**   | A month can be done in 25 keys, but focus is lost inside drawers and the edition can't be read by keyboard |
| Art and tone director     | 6       | **7**   | The drawers are printed matter now, but finished below the paper; "BRISK TRADE" became the new "ALL QUIET" |
| **Mean**                  | **5.7** | **6.9** |                                                                                                            |

Four of the five reported the same new problem without prompting. The lead was stuck on one story:
"BRISK TRADE ON MULBERRY STREET … THE PROPRIETOR IS SATISFIED" ran in 8–13 editions in a row, over
losing months and arrears. All five found the footer's "this month" figure disagreeing with THE
MONTH IN BRIEF. Four found SEND TO PRESS pushed under the footer again. Four found the Coroner's
Court naming the mother when her infant had died.

## Fixed after round 2

| Finding                                                                       | Who                          | Now                                                                                                                                                                                                                                                                                                                  |
| ----------------------------------------------------------------------------- | ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| One lead every quiet month, even over losses                                  | art, casual, careful, tycoon | The trade lead reads the books: a bought verdict ("THE CORONER IS SATISFIED"), arrears, nothing sold, a slump (sales down a third or the books down), new faces, lost custom, and a good month only when the books are up. Each has 2–4 bodies and its own cut                                                       |
| Footer "this month" contradicts the books                                     | all five                     | The footer prints "the books up/down $x" from the same opening-to-closing figure as the brief and the ledger. The till sounds only if the books are up                                                                                                                                                               |
| SEND TO PRESS and the counter nameplates under the footer                     | careful, UX, art, tycoon     | The proof's columns no longer push the counter. On the bench the type case scrolls, while the post, the works, the warnings and the lever stay put                                                                                                                                                                   |
| Coroner's Court names the mother; the infant's cutting calls it a wife        | casual, careful, art, UX     | Court notices, the bought-verdict lead and death cuttings name "the infant child of …". The mother's own cutting says she buys for her infant                                                                                                                                                                        |
| The paper says you "were not called to give evidence" after you testified     | casual, careful              | The coroner's lead no longer contradicts any reply                                                                                                                                                                                                                                                                   |
| Inquest replies don't matter; silence dominates                               | tycoon, careful              | Testifying gives the best odds (−0.15) and is never punished worse. Silence costs +28 talk if the jury names your bottle (testifying +20). Every verdict against the label raises the odds of the next. Each reply shows its risk                                                                                    |
| Coroner's fees in odd cents                                                   | tycoon, casual, art          | Asked in round five-dollar sums                                                                                                                                                                                                                                                                                      |
| Letters speak in the game's voice; dates shift; mixed coroner names           | art, careful                 | Each sender writes in their own voice: the coroner's summons, the college's circular, the inspector's hint, a pencilled note in a regular's spelling. A docket line says how each came. Each letter keeps its own date (never after the proof), the diploma comes from Cincinnati, and there is one coroner per game |
| Focus lost after every action in a drawer                                     | UX                           | Drawers and the bench give focus back to the same place after rebuilding (44 Tabs per order-form action becomes 0). Clicking the bottle shelf focuses it, so the arrow keys keep working                                                                                                                             |
| The edition can't be read by keyboard                                         | UX                           | Tab goes from the footer into every item on the paper in reading order, and the page follows focus. Enter opens a cutting. Arrow, Page and Home/End keys scroll the paper                                                                                                                                            |
| No visible focus on SEND TO PRESS and the footer                              | UX                           | A paper rule inset inside the border. Locked footer buttons are a dim outline, not lit                                                                                                                                                                                                                               |
| A second Enter flies past the edition                                         | UX                           | Input is ignored for 1.2 s after a skip                                                                                                                                                                                                                                                                              |
| The skip hint is covered by the sheets                                        | UX                           | It stays above them for the whole run                                                                                                                                                                                                                                                                                |
| A 48-bottle batch takes 13–21 order-form clicks                               | casual, tycoon               | The shelf accepts any batch up to its capacity, with bottles beyond the stock pencilled in. ORDER ENOUGH follows the batch you marked                                                                                                                                                                                |
| The still's cost line misleads; it orders when no still is free; it overflows | careful, tycoon, UX, art     | It says what lighting the fire costs (the glass) and what comes off the shelf. "Short of" appears only when it limits your batch. A busy still says so and offers nothing. The buttons wrap instead of running under the bench                                                                                       |
| Selling out is invisible                                                      | casual, careful, tycoon      | The Trade adds "The shelf was bare before the month was out, and callers went away empty-handed."                                                                                                                                                                                                                    |
| Outgoings don't add up                                                        | careful, tycoon              | Every outgoing is itemised                                                                                                                                                                                                                                                                                           |
| The month in brief reads as a dashboard                                       | art                          | A sentence with numbers spelled out: "Twenty-eight bottles sold; the books up $5.70; two new customers." Pointed with ☞ to the ledger                                                                                                                                                                                |
| "Talk" reads as good news; it has no number                                   | casual, tycoon               | "suspicion: a good deal (27 of 100)", with a tooltip giving the 100 mark. The ledger says "Suspicion in the ward rose …"                                                                                                                                                                                             |
| An unanswered inquest goes to press silently                                  | tycoon                       | The lever asks for a second press, as it does for an empty month                                                                                                                                                                                                                                                     |
| Your own ad opens only from its margin                                        | careful, casual              | The whole ad opens its cutting                                                                                                                                                                                                                                                                                       |
| The Doctor title has no visible use                                           | casual, careful              | Its notice names the claim it adds                                                                                                                                                                                                                                                                                   |
| The ad-unlock hint doesn't say what's missing                                 | casual, careful              | It shows your savings against the $100 and says to print a grateful letter. Once both are met, it says space opens next month                                                                                                                                                                                        |
| Typography                                                                    | art, careful, casual, UX     | "Dr. ——’s" takes an apostrophe. Letters and replies go through the compositor's pass. Debts print as "$4.19 owing". "Widow X, widow" is gone, and retirees are 61 or older. "in 1 month(s)" now reads "at the month's end"                                                                                           |
| A brown bottle contradicts the green product                                  | art                          | "A bottle, green, three-quarters empty"                                                                                                                                                                                                                                                                              |
| Stray period details                                                          | art                          | Antifebrin arrives in October 1886, not January. The paper's number advances by four an edition, since it is a weekly. The bottle cut is captioned "From the engraver's stock", not "Actual size". The order form is a "PRICE CURRENT AND ORDER BLANK"                                                               |
| Gauges print debug decimals                                                   | art                          | The figure is a hover note ("the chemist's figure")                                                                                                                                                                                                                                                                  |
| Developer wording on overflow                                                 | careful                      | "That is 8.5 oz, and the bottle holds 8. It is running over the lip: take some back."                                                                                                                                                                                                                                |
| Disabled tickets look ready; "—" for free stock; faint outlines; small seals  | tycoon, UX, careful          | Disabled is a faint outline on any ticket. Free stock reads "free". Bottle outlines and the clickable margin rule are ≥3:1. The drawer shade is lighter (0.30). Seal words are 12–13 px                                                                                                                              |
| Two "NOTICES" headings; a "since departed" neighbour with no deaths           | careful                      | "WANTS & SUNDRIES."; the referral line no longer implies a death                                                                                                                                                                                                                                                     |

Driver: `{"focus":true}` reports where the keyboard is.

Checks: TS 79/79, C# 71/71 (parity on 24 fixture games, regenerated for the inquest and timeline
changes), typecheck, lint, format. The parity replay test now has a 30 s budget, because it timed
out at 5 s while testers loaded the machine.

## Balance after the fixes (fresh bots, 80 runs each, `--seed r2fix`)

Full report: `balance-after-fixes.md`.

| Bot                      | Bankrupt | Shut down | Median final cash | p10    | p90     | Cash at month 12 |
| ------------------------ | -------- | --------- | ----------------- | ------ | ------- | ---------------- |
| idle                     | 100%     | 0%        | −$9               | −$9    | −$9     | $114             |
| cautious                 | 0%       | 0%        | $3,044            | $1,646 | $4,128  | $190             |
| shopkeeper               | 0%       | 4%        | $7,816            | $5,122 | $8,807  | $228             |
| adaptive (moderate)      | 0%       | 0%        | $11,630           | $9,304 | $13,160 | $270             |
| briber                   | 9%       | 14%       | $11,182           | −$10   | $17,088 | $353             |
| greedy (heavy narcotics) | 3%       | 57%       | $1,387            | $227   | $25,684 | $452             |

The tycoon's variants were re-run after the inquest change (60 runs, `r2tv`). Briber-with-silence
fell from the top line ($18.7k median, 18% shut down) to $15.6k with 52% shut down.
Briber-with-testimony is now the best of the three at $17.9k, with 38% shut down. Adaptive beats the
shipped briber on median and p10. Honest play is viable and slower. The heavy route is a jackpot or
ruin.

## Still open

**Major**

- **The dark line still pays best in expectation.** Strong laudanum, ads and testimony gives a
  median of about $18k, but closes the shop in about 4 runs of 10. That may be the right shape for
  the design ("tempting and risky"), but the design should decide it. A District Attorney track
  would make repeat verdicts end a run rather than only raise the odds (tycoon 3).
- **The honest month is still a solved routine.** It has few letters, no market movements, and no
  rival who cuts prices (tycoon 5).
- **Price has no feedback.** There's no "walk-ins who looked and left: too dear" line, and the
  assay doesn't say who will pay what. The slump lead now catches a fall of a third (tycoon 1).
- **The new drawers are finished below the paper.** The still is flat vector art, not an
  engraving. The seals are discs rather than engraved vignettes. The order form has no leader dots
  and uses web steppers (art 5–7).

**Minor**

- Money replies are one click with no undo before press (UX 14).
- The printed testimonial never appears in the paper (careful 17).
- The FRONT notice quotes a customer whose complaint doesn't match the headline (careful 18).
- Danger wording doesn't predict mass-death months (careful 5).
- Unfilled column feet (art 8).
- Repeated identical counter cuts, mirrored only (art 10).
- Small grey state text under the drawer shade is still 3.5–4.3:1 (UX 9).
- Justified rivers in narrow columns (UX 19).

**Low**

- Morphine at 40c. an ounce is far below the 1880s wholesale price. It is load-bearing for balance,
  so check it against a price current before changing it.
- Modern glyphs (▸ ✕).
- Greeked bars on the proof.
- Water must be ordered.
- Remedies can't be renamed.
- The edition and footer show different months.

## Lessons for round 3

- **A fix can become the next finding.** "BRISK TRADE" was the round-1 fix for "ALL QUIET", and it
  became round 2's top complaint because it had one body and ignored the books. Any generated text
  needs variety and a condition that can be false.
- **One figure, one definition.** The footer and the brief measured "this month" from different
  baselines. Every tester noticed, and every report flagged it.
- **Test the layout at its worst month.** Many claims, 7 letters and two warnings broke a bench
  that looked fine in month 1. Capture the busiest proof in the suite, not only the first.
- **Keyboard support needs walking, not wiring.** Focusable items in a separate CanvasLayer were
  unreachable by Tab. The driver's new `focus` query makes this checkable.
