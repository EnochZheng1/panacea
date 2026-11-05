# Panacea · round 7 · playtest by "Non-gamer"

I rarely play video games. A friend who loves Victorian history sent me this one. I use the mouse, read slowly, and get anxious when I don't know what a button will do.

**Verdict:** I could play it without help. After the first month the loop was clear (brew in THE STILL, set claims, send to press, read the paper). Each letter prints the consequence of every reply before you choose, and that took most of my fear of buttons away. What still worried me: two customers died while my assay read "Danger: harmless" and nothing told me whether that was my fault, buying things in VENTURES jumps the page so the confirm button disappears, and the period words (peaky, bale, dele, standing, assay) are never explained.

**Setup:** build `5a1dbc9`, port 47045. Game 1: seed 7045, a fresh game with no jump-ahead, played by hand from the first edition (Jan 1885) through 20 sends to press, ending at the Sep 1886 edition. Game 2: seed 7046, `--play 10 --dose 1.5`, a jump-ahead with a heavy dose, played 3 months by hand (Nov 1885 to Feb 1886) to see inquests. About 33 minutes of active testing (22:01 to 22:34 UTC). My only tooling was a one-line wrapper around `tools/drive.py`. Screenshots are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r7/cold-non-gamer/`.

**Important disclosure up front:** screenshots 01.png to 60.png (game 1, Jan to Jul 1885) were captured, but my image viewer did not return them to me. For those first seven months I worked from the driver's `targets`, `read` and `state` text dumps rather than the screen. A real player would have had the screen. From 61.png (Aug 1885) onward I saw every screenshot. The First-hour section marks which observations are text-only.

## Run log

| Seed                        | Months                | What I tried                                                                                                                                                                                                                                     | Result                                                                                                                 |
| --------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| 7045                        | Jan–Jul 1885 (7)      | Kept the starter recipe and added 1 oz valerian, priced 60c and later 70c, claim "Soothes the Nerves" and later "Banishes Rheumatism", batches of 24, 36, then 48, a ¼ col ad and then ½ col. Took up the Proprietor's Almanac ($40) in month 4. | Sales went 14, 11, 12, 45, 50, 40, 43. A second still came at 200 bottles sold. Cash $150 → $166.54.                   |
| 7045                        | Aug 1885–Jan 1886 (6) | Used both stills. Letters: let the Sanitary Inspector look, bought the lease ($60), subscribed to St. Bridget's society. Opened the SUPPLIES order blank.                                                                                        | First death (Mr. Charles Taylor, Aug 1885). Sales 54 → 67. 27 regulars. Cash $234.56.                                  |
| 7045                        | Feb–Aug 1886 (7)      | VENTURES: The Shop Next Door (0c down, $5 a month) and A Larger Copper ($200). Front-page notice ($20) once. Price-war handbills ($10).                                                                                                          | Second death (Widow Honora O'Brien, Jun 1886). Best month: 73 sold. Finished at 36 regulars and $136.16 cash.          |
| 7046 `--play 10 --dose 1.5` | Nov 1885–Feb 1886 (3) | Heavy laudanum recipe inherited. Donated to the Temperance Ladies, kept my head down at two inquests, declined the society.                                                                                                                      | 3 deaths in a month and 2 inquests ("Died of the Vapours", "Natural Causes"). Suspicion fell from 74 to 61 out of 100. |

**Key moments (game 1):**

- **Month 1.** The formula book opens with a starter recipe already in the glass. I added valerian, chose ENTER IT IN THE BOOK, then went to THE STILL. Even free water had to be ordered first ("Not enough stock on the shelf: order first").
- **Month 4.** Ad space and the Almanac venture became available. Sales tripled (12 → 45).
- **Month 8.** The second still unlocked. The "BOTH STILLS · ORDER AND LIGHT 48 EACH" button made it a one-click job.
- **Month 8.** "DEATH OF MR. TAYLOR" ran as the lead story, with a skeleton-faced undertaker cut nearby. My recipe was harmless, so I felt guilty and confused.
- **Month 14.** The Shop Next Door appeared once I had 25 regulars.
- **Month 15.** I bought the Larger Copper. The next paper read "the books down $179.84" and gave me a fright.
- **Month 19.** The front-page notice became a lead story ("SOOTHES THE NERVES! A Remarkable Restoration."), and I sold 73 bottles.

## Metrics

- **Cash at the start of each month (game 1):** $150.00, 151.32, 150.84, 155.04, 129.92, 142.76, 153.60, 166.54, 179.02, 208.52, 159.90, 177.48, 213.48, 234.56, 249.94, 70.10, 86.70, 99.16, 117.74, 115.86, 136.16. The dips are the Almanac ($40), the lease ($60) and the Copper ($200).
- **Bottles sold per month:** 14, 11, 12, 45, 50, 40, 43, 54, 55, 51, 62, 65, 67, 66, 67, 53, 56, 56, 73, 64. Total about 1,000.
- **Regulars:** 12 → 36.
- **Deaths:** 2 in 20 months with a harmless recipe, and 0 inquests. Game 2 (heavy dose): 4 deaths and 2 inquests in 2 months.
- **Ventures:**
  - Almanac: first offered in month 4, by hand. It cost $40, and by Aug 1885 the paper credited "ten by the almanac" callers plus "By post, to readers of the almanac: ten bottles, $7.00" a month. It paid for itself in about 3 to 4 months, so it was worth it.
  - The Shop Next Door: month 14.
  - Larger Copper: month 15, at $200. It left me at $70 with little visible gain over the next 3 months, because sales stayed at 53 to 73 while 144 bottles a month were brewing.
- **Clicks for a routine month:** about 7 (TO THE PROOF, THE STILL, a bottle count, ORDER, LIGHT, PUT IT DOWN, SEND TO PRESS), plus 1 or 2 per letter.
- **Time to understand the loop:** about 1 month (one send to press).
- **Time to understand what my customers' health words mean:** never. I still don't know what "bale" means.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                | Where                                                                                         | How to reproduce                                                       | Suggested fix                                                                                                                                                       |
| --- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | Pressing "☞ TAKE IT UP" in the prospectus makes the list jump. The "PAY … DOWN? PRESS AGAIN" confirm button goes off-screen, and the top of the drawer shows another card ("YOURS · two months to run", then "A LABORATORY"). I couldn't tell whether I had bought something, and had to scroll up to find the confirm.   | VENTURES drawer (85.png after clicking The Shop Next Door; 90.png after A Larger Copper)      | Seed 7045, Feb 1886, VENTURES → TAKE IT UP · 0c. on The Shop Next Door | Keep the scroll position when the button changes into its confirm state, or confirm in a small pop-up next to the button.                                           |
| 2   | Major    | Customers die while my assay says "Danger: harmless", and nothing says whether I caused it. The memoriam card reads "Last seen at the counter: hale, delighted with it." I felt responsible and couldn't learn anything from it.                                                                                          | Editions of Aug 1885 (63.png, 66.png) and Jun 1886 (102.png, 103.png)                         | Seed 7045, harmless valerian recipe, play to Aug 1885                  | Add one line to the memoriam card or the deaths column ("the doctor gives it as old age; your bottle was not mentioned"), or show the inquest outcome as in game 2. |
| 3   | Minor    | With three stills the shortcut still says "BOTH STILLS · ORDER AND LIGHT 72 EACH" and lights only two, leaving the third cold. For four months I didn't notice I had a third still sitting idle.                                                                                                                          | THE STILL (105.png). Source `scripts/UI/StillBench.cs:198` hard-codes "BOTH" and two batches. | Own 3 stills (600 bottles sold), open THE STILL                        | Say "ALL THREE STILLS", light every free still, or name the count.                                                                                                  |
| 4   | Minor    | "THE MONTH IN BRIEF … the books down $179.84" after I bought the Larger Copper. It looked like a disaster, but it was my own $200 purchase.                                                                                                                                                                               | Edition of Mar 1886 (94.png)                                                                  | Take up A Larger Copper, send to press                                 | Add "(the larger copper, $200)" to the brief line.                                                                                                                  |
| 5   | Minor    | The outgoings list "branch rent, wages and upkeep $5.00", but I never took "A Branch in Another Ward", only The Shop Next Door. I thought I'd been charged for something I didn't buy.                                                                                                                                    | Books clipping, Jun 1886 edition                                                              | Take The Shop Next Door, read the books                                | Name it "the shop next door, $5.00".                                                                                                                                |
| 6   | Minor    | Words I didn't understand and couldn't hover for: "peaky", "bale", "poorly · shaking", "asks for it by name", "standing in the ward", "suspicion", "assay", "Kick", "Lingers", "steeping", "percolation", "the works", "type case", "Price current". "dele (strike out)" glosses itself, and that is the model to follow. | Counter cards, THE WORKS panel, formula book                                                  | Any proof screen                                                       | Add tooltips on the counter-card status words and the assay rows, in the same period voice.                                                                         |
| 7   | Minor    | After lighting a 72-bottle batch, the ORDER and LIGHT buttons are pushed below the bottom edge of the still panel and partly hidden.                                                                                                                                                                                      | THE STILL (105.png, 92.png)                                                                   | Larger Copper, 72 marked, light both                                   | Let the panel scroll, or keep the buttons pinned at the top.                                                                                                        |
| 8   | Low      | A caption "Cut No. 12, again." / "Cut No. 38, again." floats over the counter. It meant nothing to me and looked like a leftover label.                                                                                                                                                                                   | Proof counter (104.png, g2-02.png)                                                            | Any proof after many months                                            | Drop it, or explain it as a printer's note in a tooltip.                                                                                                            |
| 9   | Low      | A regular writes "the tonic dont do what it did … I have to have it" although my remedy reads "Habit: none".                                                                                                                                                                                                              | Letter "Something Stronger", Aug 1886 (107.png)                                               | Seed 7045, Aug 1886                                                    | Send this letter only when the recipe is habit-forming.                                                                                                             |
| 10  | Low      | A skeleton-faced man in a top hat stands among the customers at the counter and heads death stories. It's funny-grim, but on first sight I found it a little frightening.                                                                                                                                                 | Proof counter in game 2 (g2-02.png), lead cut (g2-08.png)                                     | Heavy dose, high suspicion                                             | Fine to keep. I'd just like it introduced once ("Fogg & Sons' man is in the shop").                                                                                 |

No exceptions in the driver log. The only messages were ALSA and dummy-audio warnings at start, and Godot leak messages at shutdown.

## What works

- **Letters print the consequence under each reply.** For example "$3, and he is gone by dinner. You become a little better known at the Department." and "Free, but the ward sees him at it: standing −2." This is the single best thing for an anxious player. Keep it.
- **Safety nets.**
  - "SEND IT ANYWAY" when inquests are unanswered (g2-m2.png).
  - "one letter unanswered: the usual reply will go out for you."
  - "Nothing on the shelf and nothing on the still: light THE STILL, or the shop will sell nothing this month."
  - Ventures and the formula book both ask "PRESS AGAIN" before taking money.
- **Progress hints.** "Next: a third still: 215 of 600 bottles sold" and "So far: 24 of 25 regulars." told me what to aim for.
- **The still.** Clicking a bottle to set the batch size is mouse-friendly. The one-click "ORDER ENOUGH FOR 48 · $6.72" removed all the arithmetic.
- **The formula book's glass.** It shows each layer by name, "IT OVERFLOWS" warns you plainly, and right-click takes an ingredient back (the instruction is printed at the top of the shelf).
- **The paper itself.** The jokes in the classifieds and quack adverts ("Twelve ladies of Brooklyn have vanished entirely", "Coffins, pine, $4.15. Lilies, firm.") made me want to read every edition. The woodcut look is lovely, and the green bottle is the only colour, so it stands out.
- **The front-page notice becomes the lead story.** That was a delightful payoff.

## Disclosures

- Game 2 was a jump-ahead (`--play 10 --dose 1.5`).
- I could not see screenshots 01–60, as explained under Setup. For game 1 months 1–7 I used the `targets`, `state` and `read` dumps (tooltips, button labels, cash) instead of the screen.
- I used `state` every month to log the cash, sold, regulars and deaths numbers above. Mostly the same numbers were on screen too, in the bottom bar and the brief line.
- I read the source only after both sessions, to confirm finding 3 (`StillBench.cs:198`) and finding 8 (`ProofView.cs:449`).
- Game 1 was played in one run, with no restarts.

## First hour

- **0:00–0:02 (text-only).** First edition, The Bowery Gazette. Only three things were clickable: TO THE PROOF, SOUND, and the line "The proprietor's books close the month at $150.00." Clicking that line opened a ledger. I understood it was a newspaper about my shop, and that the big button moves on.
- **0:02–0:06 (text-only).** On the proof I found four drawer tabs: THE STILL, FORMULA, SUPPLIES, VENTURES. In the formula book the tooltips were very clear ("Click or Enter: pour ½ oz (Shift: ¼ oz). Right-click or Backspace: take it back."). A starter recipe was already in the glass. I added valerian and chose ENTER IT IN THE BOOK. I also picked a claim, "Soothes the Nerves", the mildest one; I was scared of "Cures Consumption".
- **0:06–0:08 (text-only).** THE STILL wouldn't light until I ordered ingredients, even free water. The "ORDER ENOUGH FOR 24 · $3.36" button solved it. Then SEND TO PRESS.
- **About 0:09.** The second edition reported that I sold 14 bottles, and **it clicked**: brew, send, read.
- **0:10–0:14 (text-only).** I answered my first letter. Ad space opened (¼ col $3). VENTURES offered the Almanac for $40, and I took it, nervously. Sales jumped to 45.
- **About 0:15.** I first saw the counter cards ("peaky · content", "bale · delighted") and didn't understand them.
- **About 0:17.** The first death. I opened the memoriam card and didn't know whether I'd done it. That was the low point.
- **0:18–0:25.** Letters from the inspector, the landlord and the society. Each one clearly said what my reply would do, and I enjoyed these.
- **0:25–0:30.** Ventures: The Shop Next Door. The jumping prospectus confused me. Then the Copper, and the frightening "books down $179.84".
- **0:30–0:33.** Game 2 showed inquests and the skeleton. The coroner's letter gave odds ("about 9 in 10 for naming"), which helped.

## Score

**7 / 10.** A gentle, readable, genuinely funny game that a non-gamer can play by mouse alone. Its letters are a model of telling you what a button will do. The ventures page undoes that by hiding its confirm button, and the deaths arrive without any explanation.

**What would move it up one point:** make the VENTURES confirm button stay in view, and add one sentence to each death saying whether the remedy was involved.
