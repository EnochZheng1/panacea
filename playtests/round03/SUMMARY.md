# Panacea · review round 3 · summary

Five persona testers played the frozen build `94f10d8` through the real Godot UI with the driver.
This build was the first with the engraved still and the engraved seals. Each tester had their
round-1 and round-2 reports and re-checked the round-2 findings. They played 7–14 months by hand in
each main game and disclosed every jump-ahead game. The tycoon veteran also ran the bots and
variants of them. A second balance pass followed, delegated to a background agent; its reports are
`balance-after-fixes.md` and `variants-after-fixes.md`.

| Persona                   | R1      | R2      | **R3**  | Headline                                                                                                                           |
| ------------------------- | ------- | ------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Casual first-timer        | 5.5     | 7       | **7.5** | "The first round where I always knew whether I was making money", but one quiet-month lead ran 7 of 9 times                        |
| Careful newcomer          | 6       | 6.5     | **7**   | The footer, brief and ledger agree, but the still drawer collapses and the inquest risk lines miss the immediate verdict           |
| Management-sim veteran    | 5       | 7       | **7**   | The chores are fixed, but a strong-laudanum, high-price, bribing line makes about 5× the best honest shop                          |
| UX / accessibility critic | 6       | 7       | **7.5** | The edition reads by keyboard and focus is visible, but opening a drawer put focus on an ad button                                 |
| Art and tone director     | 6       | 7       | **7.5** | "The first build where every drawer looks like printed matter from the same shop"; the counter cuts are the last prototype surface |
| **Mean**                  | **5.7** | **6.9** | **7.3** |                                                                                                                                    |

Every tester whose game had no free still found the still drawer's card collapsing into a column
one letter wide. Two found one quiet-month lead repeating: "NEW FACES" ran 7 of 9 times for one,
and "TRADE DULL" ran four in a row for another, over months that had sold out. The new art landed:
the still "is now a proper cut in the house woodcut style", and the seals "carry real emblems with
good jokes".

## Fixed after round 3

| Finding                                                                            | Who                  | Now                                                                                                                                                                                                                                                                                                                                                                                           |
| ---------------------------------------------------------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The still card collapses when it has no buttons                                    | casual, careful, UX  | The batch card keeps a fixed minimum width                                                                                                                                                                                                                                                                                                                                                    |
| One quiet-month lead repeats; a sold-out month reads as a slump                    | casual, art, careful | The paper never runs the same lead two editions in a row when the month offers another. Pressing leads take a follow-up head ("TRADE STILL DULL", "STILL NO BOTTLES TO BE HAD"). New: "THE SHELF BARE" for a sell-out, "A RECORD MONTH", "TOO DEAR FOR THE WARD". A slump counts at a third                                                                                                   |
| The dark line dominates                                                            | tycoon               | Balance pass 2 (below): hooked regulars run out of means and are ruined, and a bribe can come to light. The dominant line falls from a $26.5k median with 17% closures to $10.8k with 68%                                                                                                                                                                                                     |
| Price gives no feedback                                                            | tycoon               | The sim counts callers who ask the price and leave. The Trade prints the count, and "TOO DEAR FOR THE WARD" leads when they are many                                                                                                                                                                                                                                                          |
| The honest mid game is a routine                                                   | tycoon               | Four new events with real effects: a rival's price war (Feeney, 65c, 4 months), the landlord's rent (buy the lease, move, or pay), a distillers' ring (spirit doubles), and a benefit society (five callers a month for a subscription). Each has its own letterhead and voice                                                                                                                |
| Opening a drawer puts focus on an ad button                                        | UX                   | Focus goes to the drawer's first control: the first reply, the shelf, the first jar. Focus is restored by position in the tree, so a button coming alive can't shift it. Disabled tickets leave the Tab order                                                                                                                                                                                 |
| The letters drawer doesn't follow focus                                            | UX                   | Drawer frames scroll to the focused control                                                                                                                                                                                                                                                                                                                                                   |
| Inquest risk lines ignore the immediate verdict                                    | careful, tycoon, UX  | Each reply shows the jury's odds as things stand ("about 7 in 10 would name your bottle"). The bribe shows its chance of coming out. The Sent note says what the jury did at once. A short input guard follows each reply                                                                                                                                                                     |
| A quick double Enter answers two letters                                           | UX                   | 0.8 s guard after a reply                                                                                                                                                                                                                                                                                                                                                                     |
| The press hint is hidden by the sheets                                             | UX                   | It is in the footer during the press                                                                                                                                                                                                                                                                                                                                                          |
| The footer is unreachable by keyboard from the proof                               | UX                   | The footer is in the proof's Tab cycle                                                                                                                                                                                                                                                                                                                                                        |
| A bought FRONT notice outranks the coroner                                         | UX                   | The coroner's verdict takes the lead. The notice still runs in the advertising column                                                                                                                                                                                                                                                                                                         |
| Counter cuts: clown nose, bug eyes, the proprietor as a customer                   | art                  | The longshoreman and clerk are redrawn in the widow's register. Repeats are labelled "Cut No. N, again." The well-to-do gentleman uses the old-gentleman cut                                                                                                                                                                                                                                  |
| Seals read as rubber stamps                                                        | art                  | The lower legends are set upright, with a beaded border, an engine-turned inner field and hatched emblems. The pump's drip is black                                                                                                                                                                                                                                                           |
| The still's motion is in a different register; the cut floats; no worm             | art                  | The drawer prints the framed card. A cutaway shows the worm, and the copper hatching follows the belly. Flames are uneven and curled, with a glow cut into the brick. Vapour curls at the arm's joint, a drop falls into the receiver, and the green is ruled                                                                                                                                 |
| Repeated copy                                                                      | art, careful         | Three infant leads, unknown-dead leads, three obituary bodies (pronouns fixed), six new-customer lines, five unknown-death notices, and a run-in head ("THE MULBERRY STREET BOTTLES.—") over three or more inquests                                                                                                                                                                           |
| Anonymous infants described three ways                                             | casual               | One description everywhere                                                                                                                                                                                                                                                                                                                                                                    |
| "Widow X, widow"; "Van Wyck" cut to "Wyck"; infant wording; no comma before quotes | careful              | Fixed in the decks, cuttings and short names. "who buys at the shop for a teething infant"                                                                                                                                                                                                                                                                                                    |
| Unknown dead are indistinguishable in letters                                      | careful              | "Inquest into the Death of an Unknown Longshoreman" and so on                                                                                                                                                                                                                                                                                                                                 |
| Second-person unlock notices                                                       | art                  | Written as trade gossip: "It is said in Centre street that Coroner Fogarty is a practical man…"                                                                                                                                                                                                                                                                                               |
| Notes set like printed letters                                                     | art                  | Pencilled notes are italic, crooked, and dated by weekday                                                                                                                                                                                                                                                                                                                                     |
| Smaller copy and consistency fixes                                                 | art, careful, tycoon | American spelling. A coroner whose name no customer shares. "the books even". "Dr. ——" never breaks across a line. The Sent note uses curly quotes. The type case and ad slip agree. A warning when the notice can't be paid for. "could not be paid for" rather than "in arrears". "free" stock. "Letters answered" in place of "Events". The still's lines and tooltips. "Still No. 1 of 2" |

Checks: TS 86/86, C# 71/71 (parity on 24 fixture games with 5 shutdowns, regenerated), typecheck,
lint, format, Godot build.

## Balance after the fixes (fresh bots, 80 runs each, `--seed bal2`)

| Bot                      | Bankrupt | Shut down | Median final cash | p10    | p90     | Cash at month 12 |
| ------------------------ | -------- | --------- | ----------------- | ------ | ------- | ---------------- |
| idle                     | 100%     | 0%        | −$9               | −$9    | −$9     | $114             |
| cautious                 | 0%       | 0%        | $2,815            | $1,738 | $3,756  | $187             |
| shopkeeper               | 0%       | 8%        | $7,419            | $5,277 | $9,061  | $232             |
| adaptive (moderate)      | 0%       | 3%        | $11,148           | $9,778 | $12,712 | $255             |
| briber                   | 4%       | 36%       | $6,043            | $101   | $15,770 | $335             |
| greedy (heavy narcotics) | 8%       | 93%       | $253              | $54    | $1,017  | $388             |

The tycoon's variants (`r3tv`, 60 runs):

| Line                                   | Median, before → after | Shut down, before → after |
| -------------------------------------- | ---------------------- | ------------------------- |
| Dark line with bribes (`tvdark-bribe`) | $26.5k → $10.8k        | 17% → 68%                 |
| Dark line with testimony               | $28.0k → $16.7k        | 50% → 65%                 |
| Dark line with silence                 | $23.4k → $13.0k        | 55% → 67%                 |

The dark lines keep the highest ceiling (p90 about $21–24k) and lead at month 12, but two in three
of those shops are closed. Adaptive play is now the strong, safe line.

## Still open

**Major**

- **A low-dose morphine line still medians about 1.7× adaptive** ($19.3k, 5% early endings). The
  balance agent's exploit hunter found it at $1.13 with bribes. Its hooked regulars buy two bottles,
  which stays under the means limit. The coca line is about 1.5×.
- **Heavy narcotics are now almost always fatal to the business.** Greedy shuts down in 93% of
  runs. That may be too harsh: the design wants the heavy route "high-variance", not near-certain
  ruin.
- **The counter's stock cuts still repeat.** Each trade has one engraving, so the next art task is
  two or three variants per trade.

**Minor**

- Focus isn't trapped inside cuttings and drawers; Tab still reaches the bench (UX 8).
- Old-style figures hide the decimal point in money at 15 px (UX 16).
- Justified rivers in narrow columns.
- Unbalanced column feet.
- Morphine at 40c. an ounce, unchecked against a period price current.
- The printed testimonial is never printed.
- Danger wording doesn't scale with dose.
- "The books" are cash flow, so investment reads as a bad month (tycoon 6).
- `contentVersion` is still 0.2.0 after two rule changes.

## Lessons for round 4

- **Every new rule needs a dead-option check.** Round 2's fix made testifying strictly better than
  silence. The tycoon found it within an hour. Check that each reply still has a reason to be
  chosen.
- **A collapsed container is a missing minimum, not a missing button.** The still card was given
  its width by its own contents. Any slip whose contents can vanish needs a stated width.
- **Restore focus by identity, not count.** Counting focusable controls broke as soon as a button
  turned on or off. A path through the tree survives a rebuild.
- **Delegate the rules, keep the words.** The balance agent changed both sims and the fixtures. The
  paper copy, letterheads and odds lines that make the rules legible stayed in the main thread,
  where the voice is kept consistent.
