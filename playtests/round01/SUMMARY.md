# Panacea · review round 1 · summary

Five persona testers played the frozen build `0ccdab7` through the real Godot UI, using the new
scripted-input driver (`godot/scripts/Driver.cs`, `tools/drive_start.sh`, `tools/drive.py`). They
reached about 120 in-game months by hand and through jump-ahead games. The tycoon veteran also ran
the balance bots. Reports are in this folder.

| Persona                   | **R1**  | Headline                                                                                              |
| ------------------------- | ------- | ----------------------------------------------------------------------------------------------------- |
| Casual first-timer        | **5.5** | It looks great and it's funny, but the press never pays off and you can't tell whether you're winning |
| Careful newcomer          | **6**   | The books add up to the cent; the paper keeps contradicting the game                                  |
| Management-sim veteran    | **5**   | The tone and consequences are excellent; as a management game it isn't working yet                    |
| UX / accessibility critic | **6**   | Text on paper is legible (7–14:1); no keyboard path; the loop is unclear                              |
| Art and tone director     | **6**   | The paper is close to production level; the desk drawers look like a prototype                        |
| **Mean**                  | **5.7** |                                                                                                       |

All five raised the same top problem on their own: the lead story read "ALL QUIET IN THE WARD /
Nothing to Report." over deaths, inquests and seizures.

## Fixed after round 1

| Finding                                                                                 | Who                         | Now                                                                                                                                                                                                  |
| --------------------------------------------------------------------------------------- | --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The lead story never reacts                                                             | all five                    | The lead is the month's biggest fact: the coroner naming your remedy, then deaths (one, or "A SICKLY SEASON"), era news, brisk trade, and only then a quiet month                                    |
| Results are below the fold; money unexplained                                           | UX, casual                  | A "THE MONTH IN BRIEF" line under the dateline (sold, books ±, dead, inquests, new and lost customers), clickable for the ledger. The Trade itemises takings and outgoings                           |
| The still's batch buttons do nothing                                                    | careful, tycoon             | Fixed: drawer state survives refreshes of the proof. The still names the short ingredient and offers "BUY ENOUGH FOR 24 BOTTLES · $x"                                                                |
| The footer goes stale                                                                   | UX, careful, art, tycoon    | It refreshes after every action and shows the month's gain or loss while counting                                                                                                                    |
| No keyboard path                                                                        | UX                          | Each screen gives focus to its main action. Escape closes cuttings and drawers. Cuttings and jars are focusable (Enter pours ½ oz, Shift ¼, Backspace takes back)                                    |
| A click during the press skips the edition                                              | UX                          | The footer is locked during the press. Any click or key can only skip to the printed edition, and a hint says so. The sheet sweeps are slower (under 2 Hz)                                           |
| An infant's death is attributed to the mother                                           | casual, careful, art        | Death notices, the lead ("THE HALLORAN INFANT"), cuttings ("The Infant of Mrs. Halloran") and inquest letters all name the infant                                                                    |
| Inquests on unknown dead print a blank name                                             | art, tycoon                 | Now "a Person Unknown"                                                                                                                                                                               |
| The ad unlock is vague and gated at the starting cash                                   | careful, tycoon, casual, UX | The threshold dropped from $150 to $100 and is stated. A progress figure is shown. Bots now unlock ads in months 2–3, where the cautious bot's median was month 56                                   |
| Letters give no feedback; unanswered letters vanish                                     | casual, careful, tycoon     | Answering says what was sent and what it cost. The bench warns when letters, and especially inquests, are unanswered, and when the month has nothing to sell; an empty month asks for a second press |
| The name box got mangled                                                                | casual, tycoon, UX          | It selects all on focus and has a placeholder and a tooltip (renaming is still open)                                                                                                                 |
| The ½ oz pour is too coarse                                                             | tycoon                      | Shift pours ¼ oz                                                                                                                                                                                     |
| The assay overflows with Percolation; the proof overflows with inquests                 | art, tycoon                 | The method buttons wrap, and the proof lists two inquests and then counts the rest                                                                                                                   |
| Assay wording over-promises                                                             | careful                     | "hard to give up" / "most will come back"; the verdicts are hedged                                                                                                                                   |
| Hover looks the same as selected; weak focus ring; low-contrast hints and disabled text | UX                          | Hover thickens the rule with a wash; only set or pressed tickets are solid. A 2 px focus ring. Hints and disabled text are ≥ 3:1. The drawer shade is lighter                                        |
| Clickable cuttings look plain; own ad doesn't open                                      | UX, casual, careful         | A fine rule in the margin marks every item with a cutting behind it, and your own advertisement opens                                                                                                |
| Truncated names on the counter                                                          | UX, art, careful            | Names wrap, and figures have tooltips                                                                                                                                                                |
| Typography: ”wrong” quotes, "60c..", "1 bottles", misread "7"                           | careful, art                | A compositor's pass sets curly quotes and removes doubled stops. Plurals are fixed and small counts spelled out                                                                                      |
| Wrong pronouns; addresses change between notices                                        | casual, careful             | Widows are women, and a person's street stays the same in every notice                                                                                                                               |
| Repetitive ads and wants                                                                | casual, careful             | 7 rival advertisements (one stereotyped name was written and then dropped) and 13 classifieds                                                                                                        |
| Green overused on the drug shelf                                                        | art                         | Unpoured jars print in black ink only; green appears once a drug is poured                                                                                                                           |
| Two editions share a date                                                               | UX, careful, art            | The first edition is dated the first Saturday the shop opened                                                                                                                                        |
| Balance harness reuses one bot across runs                                              | tycoon                      | `runBatch` takes bot factories, and the CLI passes them: one fresh bot per run                                                                                                                       |
| Driver: kill leaves Godot holding the port                                              | casual, careful, tycoon     | `tools/drive_stop.sh PORT`; `drive_start.sh` stops any previous game first; a failed bind no longer spams                                                                                            |

Checks: TS 79/79, C# 71/71 (parity with 24 fixture games, regenerated for the $100 threshold, seed
`hunt-3` added to cover a shutdown), lint, format, content lint.

## Balance after the fixes (fresh bots, 40 runs each, `--seed r1-after`)

| Bot                                 | Bankrupt | Shut down | Median final cash  |
| ----------------------------------- | -------- | --------- | ------------------ |
| idle                                | 100%     | 0%        | −$15               |
| cautious (no narcotics to speak of) | 5%       | 0%        | $313               |
| shopkeeper                          | 0%       | 0%        | $6,381             |
| adaptive (moderate dose)            | 0%       | 10%       | $11,428            |
| briber                              | 30%      | 23%       | $472 (mean $1,888) |
| greedy (heavy narcotics)            | 98%      | 3%        | −$13               |

The tycoon veteran's main critique still stands. The dark path isn't the "high-variance" route the
design calls for; heavy narcotics are a near-certain collapse. Honest play barely beats rent, and a
moderate dose dominates.

## Still open

**Major**

- **Economy balance.** Give honest play a real but slow growth path. Make the heavy route high-risk,
  high-reward instead of a guaranteed collapse. Add the price, ad and production return readouts a
  tycoon player needs (the tycoon report, findings 2, 3, 5 and 8).
- **The drawers are stock forms, not printed matter.** The still (a stepper), supplies (a
  spreadsheet) and letters (button rows) need the treatment the formula bench got (the art report,
  finding 6).
- **A solved monthly routine.** Few letters, events or market movements once a formula works (the
  tycoon report, finding 9).

**Minor**

- Rename a remedy after it is entered; the Doctor title has no visible use.
- Unmet demand is invisible ("the shelf was bare by the 20th").
- Unfilled column space in the edition.
- Repeated identical stock cuts at the counter.
- Small ad cuts.
- Water must be bought.
- Period names and prices: community-paired names, the 25c/50c/$1 price convention, round bribes.
- Talk from claims without an ad isn't explained.
- Notices that don't react to the player.

**Low**

- The press sequence is generic (no platen or ink, and the pile doesn't grow).
- Modern glyphs (▸ ✕).
- Greeked bars on the proof.

## Lessons for round 2

- **A shared driver made real-UI testing possible** without a mouse or window. Keep it, and give
  testers `drive_stop.sh` up front.
- **Five testers found the same headline problem independently.** When one fix appears across
  every report, fix it first, and check that the fix doesn't reintroduce a finding. The new lead's
  first draft named the mother for an infant's death, the very misattribution three testers had
  reported.
- **Bots must be fresh per run.** Treat any balance figure from before this fix as unreliable.
