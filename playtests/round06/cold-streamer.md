# Panacea · round 6 · playtest by "Streamer"
I play new indie games live and judge the first twenty minutes hard: is there a hook, what's worth clipping, where does the stream stall, and can people who are only watching follow it.

**Verdict:** This is very streamable. The newspaper is a clip machine: it prints your own ad claim ("COMFORT FOR TEETHING BABES!") as the lead story, in the same issue as the obituary of the baby it killed, and chat would lose it. The first two or three minutes are the weak spot. The game opens on a dense broadsheet with a "PROOF" screen behind it and nothing tells you what to do, and the main loop of ordering, lighting and sending to press works but gets repetitive by month 8. Headlines repeat too ("BRISK TRADE ON MULBERRY STREET" ran four times in eleven editions).

**Setup:** build `3e627bf` (frozen copy build-r6), port 47024.
- Game 1: seed `1906`, fresh, no jump-ahead. Jan 1885 to Apr 1886: 15 editions read, 15 sends to press, about 27 minutes of wall-clock time.
- Game 2: seed `77 --play 20 --dose 1.5` (jump-ahead). It loaded straight onto the game-over edition, so I couldn't act. I used it to look at the ending screen.
- Game 3: seed `77 --play 24 --dose 0.25` (jump-ahead). Jan 1887: took up a second venture and sent one month to press.
- About 32 minutes of active play plus writing up. No tooling beyond `drive.py` one-liners. Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r6/cold-streamer/`.

## Run log

| Game | Seed / args | Months | What I tried | Result |
|---|---|---|---|---|
| 1 | 1906, fresh | Jan 1885 to Apr 1886 (15 sends) | Named the remedy "Chat's Miracle Syrup" with 1 oz laudanum on top of the starter glass. Half-column ad from Apr 1885 claiming TEETHING BABES and SOOTHES NERVES. Almanac venture in month 6. Second remedy "Chat's Knockout Drops" (coca + morphine) in month 7. Paid the coroner twice, kept quiet twice, testified once. Bought a mail-order MD. One FRONT ad. | Cash $150 → $46. 12 deaths in all, 6 inquests. Suspicion peaked at 96 ("the Board of Health is asking"). Never went under. |
| 2 | 77 --play 20 --dose 1.5 | arrived at May 1886, already ended | Looked at the game-over edition | "A PROPRIETOR FAILS / Creditors at the Door", then BEGIN AGAIN |
| 3 | 77 --play 24 --dose 0.25 | Jan 1887 → Feb 1887 | VENTURES: took A DRUGGIST'S STANDING ORDER ($100). Lit both stills. | 70 bottles. "A RECORD MONTH" headline. A Brooklyn druggist notice appears in the paper. |

**Timeline of key moments (game 1)**
- **0:00, Jan 1885 edition.** Gorgeous woodcut front page, but it's a wall of text. Funniest lines: "Twelve ladies of Brooklyn have vanished entirely", "Several of the testimonials were received posthumously", and from THE MARKETS: "Coffins, pine, $4.00. Lilies, plentiful."
- **~2:00, proof screen.** I found the formula book. The glass is already filled, and the assay reads out ("they sleep", "hard to give up"). Naming the remedy is a chat moment: "Chat's Miracle Syrup".
- **~4:00.** The type case offers "COMFORT FOR TEETHING BABES !" while Mrs. Carmela Bruno, "for a teething infant", stands at the counter. That's a deliberate, very dark setup.
- **Mar 1885.** A grateful letter from Mrs. Bruno: "my husband says he hardly knows me". The Gazette prints her testimonial and misspells her "Burno" after she asked to be spelled right.
- **Jun 1885 (month 6).** Took up THE PROPRIETOR'S ALMANAC ($40).
- **Jul 1885.** Lead story "THE BRUNO INFANT". The undertaker's ad thanks "Our Friends in the Medicine Trade for Their Continued Patronage". **Clip #1.**
- **Aug 1885.** "A SICKLY SEASON", with a skeleton cut captioned "A GENTLEMAN SEEN IN THE NEIGHBORHOOD." Three deaths. Two coroner's letters with odds pencilled in the margin.
- **Sep 1885.** "THE CORONER NAMES A REMEDY". In the same column, the paid inquest comes back "Died by the Visitation of God" ("A green bottle entered in evidence could not afterward be found") and the unpaid one comes back "Died of poisoning... papers sent to the District Attorney". **Clip #2: the bribe A/B side by side.**
- **Sep 1885.** Bought a mail-order MD for $50. The paper then reads "is to be addressed henceforth as Doctor... the ink on it is very nearly dry", and letters start "Dear Doctor".
- **Nov to Dec 1885.** A FRONT ad ($20) sets my claim as the lead: "COMFORT FOR TEETHING BABES! A Remarkable Restoration." The Deaths column directly beside it: "WILSON.—On Wednesday last, the infant child of Mrs. Ada Wilson... aged 11 months." **Clip #3, the strongest in the game.**
- **Jan 1886.** "DEATH OF WIDOW WEBER". Death had stood beside her at the counter since July. That six-month wait pays off. Then the "Something Stronger" letter: "Doc, the tonic dont do what it did... I have to have it."
- **Mar 1886.** "THE CORONER IS SATISFIED": "Coroner Hanlon, lately seen in a new overcoat". Wants & Sundries: "Will the lady who bought a bottle of Elixir on Thursday and left her baby please return for the baby. The Elixir is paid for."

## Metrics

| Measure | Value |
|---|---|
| Time to first understood action (open the formula book) | ~2 min; nothing on the first proof screen points you there |
| Time to first "oh no" moment (baby claim next to a teething customer) | ~4 min |
| Time to first clip-grade payoff (the BRUNO INFANT lead) | month 7, ~13 min wall-clock |
| Sends to press | 15 (game 1) + 1 (game 3) |
| Press sequence | ~7 s, with a clean "THE PRESSES ARE RUNNING / 1,116 COPIES" card that already shows the headline (good stream tease) |
| Clicks per routine month (open still, fill, light, close, send) | 5–6, plus 2–4 for letters |
| Cash, game 1 | $150.00 (Jan 85), $142 (Feb), $153 (May), $127 (Jul), $115 (Sep), $73 (Oct), $83 (Dec), $63 (Jan 86), $46 (Apr 86) |
| Bottles sold per month, game 1 | 9, 10, 17, 50 (first ad), 43, 60, 39, 59, 61, 64, 56, 48, 71, 49, 48 |
| Deaths / inquests, game 1 | 12 / 6 (bribes $20, $25, $35 on three; quiet named the bottle; testify named the bottle, suspicion 87) |
| Suspicion, game 1 | 0 → 12.7 (Jul) → 51 (Aug, after "keep quiet") → 87 (Jan, after "testify") → 96 → 76 |
| Time to reach a venture | Almanac offered and taken in month 6 (~9 min). It was worth it: callers "by the almanac" showed up in THE TRADE at once (ten in Aug). |
| Repeated lead headlines | "BRISK TRADE ON MULBERRY STREET / The Remedy Moves" 4× in 11 editions; "THE CORONER NAMES A REMEDY" 2×; verdict "Died of an Excess of Spite" 2× |
| Moments chat would get bored | Every month from about month 8 on is the same 6 clicks when there's no letter. The first broadsheet is a lot to read aloud. |

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | The first minutes don't tell a new player what to do. Edition 1 is a full broadsheet. "TO THE PROOF" opens a proof with an empty lead, five customers and four small drawer buttons (THE STILL / FORMULA / SUPPLIES / VENTURES, each 82×22 px), and nothing points to FORMULA first. Viewers have no idea what the goal is. | Proof screen, Jan 1885 (`04.png`) | New game, click TO THE PROOF | Show a one-line printer's note on the first proof ("First, write a formula in the book ☞"), or open the formula book automatically the first time |
| 2 | Major | Repetition kills the late stream. "BRISK TRADE ON MULBERRY STREET / The Remedy Moves", with the same bottle cut, ran in 4 of 11 editions. Paid verdicts reuse "Died of an Excess of Spite". Several BRIEFLY/CITY ITEMS items repeated word for word from edition 1 to 2 (Fire in Baxter street; A Card from Mrs. E. Dooley). | Lead story, months 4–12 | Play 12 months with no deaths in most of them | Rotate more headline variants for quiet months and don't repeat a filler item for N editions |
| 3 | Minor | The headline contradicts the numbers. "A RECORD MONTH ON MULBERRY STREET... THE BOOKS WELL UP" ran directly under "THE MONTH IN BRIEF... the books down $2.88" (Apr 1885) and "the books down $84.45" (Jan 1887). A viewer reading both lines will call it a bug. (Source check: the `record` story in `data/paper.en.json` always prints "THE BOOKS WELL UP".) | Edition lead, `30.png`, `g3-11.png` | Have a high-sales month in which you also buy ad space or a venture | Choose that deck only when the books are actually up, or say "THE TILL FULL" instead |
| 4 | Minor | "ORDER AND LIGHT BOTH · 48 EACH" with two remedies in the book lit both stills with the *current tab's* remedy. I read "both" as both remedies: "Every still is on the fire (48 bottles of Chat's Miracle Syrup; 48 bottles of Chat's Miracle Syrup)". The tooltip explains it, but on stream nobody sees tooltips. | The Still, Aug 1885 (`75.png`, `77.png`) | Have 2 remedies and 2 stills; on the first tab click ORDER AND LIGHT BOTH | Name the remedy in the label: "LIGHT BOTH STILLS WITH CHAT'S MIRACLE SYRUP · 48 EACH" |
| 5 | Minor | In VENTURES, a hover tooltip ("The Proprietor's Almanac") stays floating over the Wholesale Contract text after a scroll. The next Escape only clears the tooltip, so the clicks that follow fall into a drawer that is still open (my SEND TO PRESS didn't fire once). | Ventures drawer (`90.png`, `g3-03.png`); Still drawer (`g3-08.png`) | Open VENTURES, hover the Almanac, scroll down 8 | Hide tooltips on scroll, and let one Escape close the drawer |
| 6 | Minor | The confirm step jumps. After "TAKE IT UP · $100.00" the list re-scrolls about 60 px, so "PAY $100.00 DOWN? PRESS AGAIN" isn't under the cursor any more. Pressing again in the same spot misses. | Ventures (`g3-04.png`) | Click TAKE IT UP on the Druggist's Standing Order | Keep the scroll position when the button changes to its confirm state |
| 7 | Minor | Death (the skeleton) shows up in the counter line from month 6 with no explanation, until a sidebar line in July: "Death waits at the counter beside Widow Lena Weber." Great idea, but on first sight chat asks "what's the skeleton?" | At the Counter (`57.png`) | Brew a laudanum-heavy remedy for a few months | Add a hover card or a one-time note when Death first appears |
| 8 | Minor | Type-case claim buttons sit in a scroll box. A scroll moved them under the cursor, and one click struck a different claim from the one I meant. The bottom claims are hidden below the fold. | Type case, right column (`66.png`, `67.png`) | Scroll the type case, then click a claim | Make the type case taller or let it wrap without scrolling; or lock clicks for 200 ms after a scroll |
| 9 | Minor | Nothing signals new things. The second still (Aug 1885) and new ventures became available with no highlight on THE STILL or VENTURES buttons. I found the Almanac only by clicking around. | Works panel | Reach month 6 / the second still | Ink a small "NEW" or a pointing fist on the drawer button when it has something new |
| 10 | Low | Text is small and dense for viewers. Customer cards (status line ~11 px italic), ventures terms and the in-paper small print are unreadable on a 720p stream. | Counter cards, ventures fine print | n/a | Bump the minimum italic size to about 14 px |
| 11 | Low | Engine leak errors at shutdown only: "58 RID allocations of type 'PN13RichTextLabel4ItemE' were leaked at exit", plus PagedAllocator warnings. No runtime exceptions in any session. | `/tmp/panacea-driver-47024.log` | Run drive_stop.sh | Free RichTextLabels on exit (cosmetic) |

## What works
- **The newspaper as the feedback screen** is the star of the game. Your own ad copy, the deaths column, the coroner's court and the undertaker's thank-you ad all on one page is the best "consequences" design I've streamed. The Nov/Dec 1885 page (lead "COMFORT FOR TEETHING BABES!", the Wilson infant in Deaths) is the clip of the session.
- **"THE PRESSES ARE RUNNING / 1,116 COPIES"** with the headline on the falling sheet is a perfect three-second tease before the reveal. Keep it short like this.
- **Inquest letters with the odds pencilled in the margin by the clerk** ("about 1 in 10 that it comes out. —J.D.") are clear, funny, and make a real decision viewers can vote on. The rising "consulting fee" ($20, $25, $30, $35) is a nice escalation.
- **The paid vs unpaid verdicts printed side by side** ("Visitation of God" with the missing green bottle, against "papers sent to the District Attorney").
- **Death standing at the counter for months** before the "DEATH OF WIDOW WEBER" headline. Long-arc payoffs like this keep viewers around.
- **Small print gags**: "Coroner Hanlon, lately seen in a new overcoat"; "the ink on it is very nearly dry"; "Will the lady who... left her baby please return for the baby. The Elixir is paid for."; the Gazette misspelling "Burno".
- Scheele's green kept to the bottle and the customers' bottles reads well even on a compressed stream.
- Ventures: the Almanac ($40, month 6) gave visible callers right away. The Standing Order gave a druggist notice in the next paper. Both felt worth it and read in-world, not as menus.

## Disclosures
- Games 2 and 3 used jump-ahead (`--play 20 --dose 1.5` and `--play 24 --dose 0.25`). Game 2 started already ended.
- I used `targets` to get button coordinates throughout, and `state` to log cash, suspicion and sales numbers each month. On screen, cash and suspicion are visible in THE WORKS panel, so a player has those too.
- After play I read `godot/scripts/UI/StillBench.cs` (the BOTH button) and `godot/data/paper.en.json` (the "record" story) to check findings 3 and 4.
- I can't hear, so I make no claims about sound.

## First hour
- **0:00.** Edition 1. "This is pretty." I scrolled the whole page and still didn't know my goal. I took the PERSONAL notices about new customers as flavour.
- **1:00.** TO THE PROOF. A proof sheet with greyed "set after press" sections. I guessed it meant "this is next month's paper". I didn't understand the four small drawer buttons were the game.
- **2:00.** I opened FORMULA. The glass was pre-filled, which helped. Added laudanum, and the assay bars jumped: "they sleep very soundly", "dangerous to anyone weak". **Click moment #1: the game is brewing, and the assay tells you what you're doing to people.**
- **3:30.** The still: "ORDER ENOUGH FOR 36" and then "LIGHT THE FIRE". After ordering, the button moved, so my first light-click missed.
- **4:00.** Sent to press. The presses card is good.
- **5:00.** Feb edition, "TRADE DULL". The ledger pop-up joke: "The silent partner keeps a second set of books. They are not shown."
- **8:00, Apr.** Ad space opens. The ad renders live on the proof as I pick claims, which is very legible. Sales jumped 17 → 50. **Click moment #2: the ad claims drive sales, and the paper will hold you to them.**
- **9:00, Jun.** VENTURES had something. Almanac taken.
- **13:00, Jul.** "THE BRUNO INFANT". This is where I'd go live with a "chat, we did this" moment. Everything after is me steering between bribes and claims.
- **16 to 25:00.** Inquests, the MD diploma, the FRONT ad, the Wilson infant. Peak stream.
- **25 to 27:00.** Routine months. Fewer surprises, and repeated headlines.

## Score
**7.5 / 10.** It has the darkest, most clippable consequence loop I've seen in a management game, and the newspaper makes it legible for people who only watch. It's held back by a cold, wordless opening and by repetition after month 8. To move it up a point: a one-line pointer on the first proof to the formula book, and twice the variety in headlines for quiet months, so the paper still surprises in hour two.
