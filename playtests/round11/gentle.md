# Panacea · round 11 · playtest by "The Gentle Player"

A player of calm life-sim and shopkeeping games who rarely plays anything dark. A friend called this one "a shop game with a newspaper". I had never seen it, and I hate being punished without warning.

**Verdict:** I ran an honest, kindly shop for 25 months by hand, with no opium, one plain claim and the kind answer to every letter. It was viable and, in the end, it felt lovely: no deaths of my making, suspicion never above 1 of 100, cash from $150.00 to $327.50, and headlines like "THE BOTTLE THAT HURT NOBODY" and "A CLEAN BOTTLE ON MULBERRY STREET · The Undertaker Sighs" that rewarded me for being good. The first year is slow and a little anxious: one regular died in month 3, cash rose only $48 in twelve months, and the paper tells you more than once what you _could_ be selling. The letters are the best part, because every reply shows its price before you choose. I was never punished for something I couldn't see coming.

**Setup:** build `64dc5d8` (frozen copy), port 47133, seed 1133, `--player --profile gentle`. One game, played by hand from the first edition. No jump-ahead. About 45 minutes of active play across three launches: a menu save-and-quit, a hard `drive_stop.sh` part-way through a month, and CONTINUE both times. After the first few months I wrote a small shell helper (`/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r11/gentle/month.sh`). It lights the still, sends to press and screenshots the paper and the proof. I still read every edition and answered every letter by hand, from the screenshots. I read the source only once, after the first session, to confirm bug F1.

## Run log

| Seed | Months                    | What I tried                                                                                                                                                                                                                              | Result                                                                                                                                                                              |
| ---- | ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1133 | Jan–Apr 1885 (1–4)        | Started a new shop. Made a gentle recipe (water, valerian, syrup, peppermint, capsicum, a little spirit) named "Mrs. Kindly's Mulberry Cordial", 60c, cost 18c. Claim: SOOTHES THE NERVES only. Brewed 24 a month. Printed a testimonial. | 12, 21, 16, 24 bottles sold. One regular, Miss Krawczyk (consumption), died in March 1885: "She would have died the same without it, the physician says." Ad space opened in April. |
| 1133 | May–Dec 1885 (5–12)       | Took ¼ col, then ½ col ($6). Kind or honest answer to every letter. Put the slate on credit until March, bought the lease for $60, took the second still when offered.                                                                    | 23–55 bottles a month. Cash $159 → $197.94. Regulars 16 → 27. No deaths, suspicion 0.                                                                                               |
| 1133 | Jan–Jun 1886 (13–18)      | Took the venture THE SHOP NEXT DOOR (nothing down, $3/month). Went to 48-bottle batches on both stills.                                                                                                                                   | Jan 1886 was −$58.18 (the lease). After that +$2.66 to +$21.42 a month. Regulars 27 → 47. Third still "earned by six hundred bottles sold".                                         |
| 1133 | Jul 1886–Jan 1887 (19–25) | All three stills. Wholesale discount (15%) arrived. Signed the Temperance League petition. Declined the $200 Larger Copper after the game's own warning.                                                                                  | 87–100 bottles a month. Sold out once (Sep 1886, my under-brewing, and the game warned me). Cash $327.50, good name 46/100, 50 regulars, suspicion 0.                               |

**Key moments:**

- **Jan 1885, title page.** The content note is clear and honest (quoted below). A CONTINUE with someone else's shop was already there (see F7).
- **Jan 31 1885, first edition.** "Twelve bottles sold; the books up 36c." My heart sank a bit: is this how slow it is?
- **Feb 1885, proof.** Miss Agnieszka Krawczyk's card: "consumption … a little peaky, grumbling." Everybody else was "peaky" too.
- **Mar 28 1885.** Headline "DEATH OF MISS KRAWCZYK". I felt a jolt of guilt and worry. The article then gives the reason: "A bottle from the shop on Mulberry street stood by the bed, half finished … She would have died the same without it, the physician says". Suspicion did not move. I was relieved, but it was a sad note to start a gentle game on.
- **Apr 1885.** "Space in the Gazette is not for sale until you have $100 saved and a testimonial printed." Then the paper announced I could buy space. That was clear.
- **Oct 1885.** A break-in letter. I wrote off the till and put up iron bars. It was fair and labelled.
- **Jan 1886.** I took THE SHOP NEXT DOOR (month 13) and regulars nearly doubled over the next six months. It was worth it.
- **Apr 24 1886.** Headline "THE BOTTLE THAT HURT NOBODY · The Trade Is Mystified". This is where it clicked: the game _sees_ the honest shop and is wryly proud of it.
- **Jul 1886.** I answered the policeman's "Captain's Outing" letter with "Tell him your back room is open to the Captain any day", thinking it the friendly, non-bribing answer. The preview said "Free, but the precinct takes an interest: suspicion +2", and the next proof said "Rose 2: replies to the post." It was fair, and I had been told.
- **Dec 18 1886.** The Temperance League wrote again: "You answered our sheet honestly, which no other druggist in the ward did." A correspondent remembered me, and it felt wonderful.
- **Dec 25 1886.** "Nothing in the deaths column this twelvemonth has been laid at the door of Mrs. Kindly's Mulberry Cordial." The proprietor woodcut is captioned "Unremarked."

## Metrics

**Cash and sales by month** (from the BOOKS drawer, `123_books_dec86.png`, `124_books_hist.png`):

| Month    | Sold          | Took in | Books           | Cash    | Regulars    |
| -------- | ------------- | ------- | --------------- | ------- | ----------- |
| Jan 1885 | 12            | $7.20   | +36c            | $150.36 | 14          |
| Mar 1885 | 16            | $9.60   | +$2.76          | $158.88 | 16 (1 died) |
| Jun 1885 | 23            | $13.80  | +$3.96          | $172.56 | 21          |
| Sep 1885 | 43            | $25.80  | +$4.62          | $190.68 | 23          |
| Nov 1885 | 39            | $23.40  | −$4.42          | $186.68 | 23          |
| Dec 1885 | 55            | $33.00  | +$11.26         | $197.94 | 27          |
| Jan 1886 | 56            | $33.60  | −$58.18 (lease) | $139.76 | 27          |
| Jun 1886 | 87            | $52.20  | +$15.42         | $191.12 | 47          |
| Aug 1886 | 100           | $60.00  | +$17.08         | $229.62 | 48          |
| Sep 1886 | 59 (sold out) | $35.40  | +$18.40         | $248.02 | 45          |
| Dec 1886 | 94            | $58.40  | +$46.90         | $327.50 | 50          |

- **Deaths:** 1 in 25 months (March 1885, consumption, not laid at my door). Inquests: 0.
- **Suspicion:** 0 of 100 almost the whole game. It peaked at 1, after the Captain's Outing reply.
- **Good name:** 10 → 46 of 100. "Better known" went 0 → 8.
- **Year 1 profit:** +$47.94. **Year 2:** +$129.56. The honest shop is viable, but the first year is a slow walk.
- **Margin:** "60c. and costs 18c. to make: 42c. clear on every bottle … the first twenty-eight bottles a month only pay for them."
- **Ventures:** second still around month 9, THE SHOP NEXT DOOR taken in month 13, third still in month 18 (600 bottles), wholesale supplier in month 21. I declined A LARGER COPPER ($200) because the game itself said my stills weren't full.
- **Clicks per month, once I had learned it:** about 6–10. Still 2, letters 2–6, press 1.
- **Time to understand the loop:** about 4 months, roughly 15 minutes. It clicked at the April 1886 headline.
- **Driver log:** no exceptions in 25 months. Only ALSA/audio-device noise.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Where                                                                                                                  | How to reproduce                                                                                              | Suggested fix                                                                                                                                                                      |
| --- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F1  | Minor    | The Prospectus says "Mark well: your stills are not yet full. Last month the shop sold fifty-nine bottles". But the edition I had just read (Oct 30 1886), the BOOKS and THE WORKS all say 89. The 59 was the month before (Sep 1886). It contradicts my books. Source confirms: `SimGame.cs:169` sets `Facts.SoldLastMonth = before` (the month before the one just reported, used for the paper's "sales up" comparison), and `Prospectus.cs:173` reads it as "last month". | VENTURES drawer, A LARGER COPPER, Nov 1886 proof (`120_ventures4.png`)                                                 | Play a month whose sales differ from the month before, then open VENTURES while the Larger Copper is offered. | Use the reported month's sales (the figure in THE MONTH IN BRIEF) in the Prospectus.                                                                                               |
| F2  | Minor    | The first death came in month 3 to a careful, opium-free shop. The only warning on her card was "consumption … a little peaky, grumbling", the same "peaky" every other regular wore for two years and lived. The paper did give the reason ("She would have died the same without it") and nothing was punished. But to a gentle player, a front-page death with no frailty signal beforehand felt like a jump-scare.                                                        | Regular card and tooltip (Feb 1885 proof, `31_clickfig.png`); Gazette of Mar 28 1885 (`45_press3.png`, `46_apr_b.png`) | New game, seed 1133. Look at Miss Krawczyk in Feb 1885, press.                                                | Give a dying regular a distinct state ("failing", "abed") a month before, and say on the card that it is the illness, not the remedy.                                              |
| F3  | Minor    | The BOOKS drawer lists "$2.00 what the letters cost" as part of what the shop _took in_ ("The shop took in $58.40: $43.20 from the regulars, $13.20 from callers off the street, $2.00 what the letters cost."). That month the letters _saved_ me money (the rag dealer's bottles, "About $2 saved on new glass"). "What the letters cost" reads as an expense placed in the income list, and I couldn't tell what it meant.                                                 | BOOKS, "WHERE DECEMBER 1886'S MONEY WENT" (`123_books_dec86.png`)                                                      | Answer I. Goldman's "Your Bottles" letter with "Buy them back, washed", press, open BOOKS.                    | Word it "$2.00 saved on glass, by the letters", or list it among the savings.                                                                                                      |
| F4  | Minor    | At TEXT SIZE: LARGER (which I used), the proof needs scrolling to read what matters. NOTES FOR THE PROPRIETOR cut a new note mid-sentence ("New: a third still is yours, earned by six hundred bottles sold. THE STILL…", then a scroll bar). The PERSONAL column shows "last month's, struck out by the printer" clipped under "▼ more below (scroll)". The counter shows 3 regulars, against 5 at REGULAR. Nothing overlapped, but I often missed notes until I scrolled.   | Proof, June 1886 (`m17_proof.png`); compare REGULAR (`98_cont2.png`)                                                   | Set LARGER, reach a month with 3+ notes.                                                                      | Let the notes box grow at LARGER, or put a count ("3 notes ▼") at the top of it.                                                                                                   |
| F5  | Minor    | Early profit for an honest shop is very thin: +36c, +42c and −$4.42 months in year one, and $48 for the whole year. Meanwhile the shelf shows Laudanum, Morphine and Fowler's Solution, and the paper's ads boast. I worried for a long while that kindness wasn't viable. It is, but it only became satisfying around month 15.                                                                                                                                              | BOOKS history (`124_books_hist.png`)                                                                                   | Play honest from month 1.                                                                                     | A short note in year one (Notes or Books), such as "An honest shop grows by word of mouth: the regulars send their neighbours", so the slow start reads as a path, not as failing. |
| F6  | Low      | Words and terms I didn't know, with no in-game gloss: _Fowler's Solution_ (the tooltip helps), _Calomel_, _steeping_/_simple mixture_ (I wasn't sure it mattered), _better known_ vs _good name_ (two reputations; which one is which?), _peaky_, _hale_, _the Tombs_, _wardman_, _the consideration_ (in the break-in letter: a ransom?), _on the slate_, _the grip_, _the Leader's young men_, _Target Excursion_, _set after press_.                                       | Letters, cards, formula book                                                                                           | Read the letters.                                                                                             | Tooltips on _good name_ / _better known_ and on card states, since they are mechanical. The period words are part of the charm and can stay.                                       |
| F7  | Low      | On first launch, the title page already offered "☞ CONTINUE" over a stranger's paper ("MULBERRY COMFORT TONIC … NOLAN.—On Monday, Widow Honora Nolan, aged 65 … one dead"). TEXT SIZE was already LARGER. This is a leftover `gentle` profile from earlier testing, not a game bug, but a real first-time player would never see it. The "A NEW SHOP? PRESS AGAIN" confirmation is good.                                                                                      | Title page (`01_title.png`)                                                                                            | Launch with a reused profile.                                                                                 | Clear the test profiles between rounds.                                                                                                                                            |
| F8  | Low      | The paper repeats its proprietor woodcut and some ads often: the same smiling "THE PROPRIETOR" cut in June and Dec 1886, and Dr. Grimsby's Galvanic Belt, Prof. Kline's Liver Pad and the Anti-Fat Cordial in many months. The copy under them changes, so it isn't tiresome, just noticeable over two years.                                                                                                                                                                 | Editions Jun/Dec 1886                                                                                                  | Play 12+ months.                                                                                              | More cuts and ads, or rotate them less often.                                                                                                                                      |

I found nothing Critical or Major. Nothing came "without a reason I could see". Each suspicion change had a line ("Rose 2: replies to the post", "Fell 1: the talk dying down, as it does in a quiet month"). Each customer lost had a PERSONAL line ("Mr. Solomon Katz … the complaint having gone of its own accord"; "Mr. Fritz Vogel, who stopped coming"). Money matched the books, apart from F1 and F3. The one sell-out (Sep 1886) was warned in advance: "Low stock: fifteen bottles on the shelf and nothing on the fire, and last month sold ninety-two. Light THE STILL".

## How the dark subject landed

The title-page note is exactly right for me, and I read it before I chose: "Panacea is a satire of the patent-medicine trade. It shows drug use and addiction, deaths (among them the deaths of infants) and the frauds the trade practised. The joke is on the sellers and the men who let them sell, never on the people they sold to." It warned me without lecturing me.

In play, the darkness is mostly _available_ rather than imposed. Laudanum and Morphine sit on the shelf. A mother "troubled by a teething infant" comes to the counter in the very first edition, which made my stomach drop: I knew what an 1885 druggist would sell her. Letters offer temptations (a $50 medical diploma, arsenic complexion wafers for a girl with a freckle, a gentleman's morphia "by messenger, in a plain wrapper").

Because the game let me say no, and then _noticed_ that I had ("Fifty-three bottles sold and not one mentioned at an inquest; the coroner is unacquainted with the label"), the satire felt aimed at the trade, not at me. The one death (consumption, month 3) was the only moment it felt heavy. The undertaker ad "FOGG & SONS … We Thank Our Friends in the Medicine Trade for Their Continued Patronage" made me laugh. I could play this.

## The letters, for a kind player

They were fair every time, because each reply shows its cost and effect in italics before you click ("$5; good name +1: a framed certificate behind the counter."; "Free, but the Board marks you down: suspicion +5."). The kind answer was never secretly punished. Often it cost a few dollars and paid back in good name or callers.

- **Liked:** the Easter freckle letter. Of the three replies, "Mix her a cold cream at cost, and write that the freckle is becoming" is kind, cheap and funny.
- **Liked:** the orphans' appeal: "Send a dollar all the same: it reads beautifully."
- **Liked:** the wedding letter's "Send a bottle of good port as a wedding present, and nothing for sale."
- **Liked:** the Ladies' Temperance League. I filled in the line truthfully in September; in December Mrs. Stowe wrote back, "You answered our sheet honestly, which no other druggist in the ward did." That is a correspondent who remembered.
- **Liked:** the Gentleman's morphia letter, which offers "Send the messenger back with the name of a doctor who treats the habit. — Good name +1." A gentle way out.
- **Liked less:** the Captain's Outing. Both replies are compromised. "Give him five dollars" is a bribe, and the friendly-sounding "Tell him your back room is open to the Captain any day" costs suspicion. It is fair and labelled, and true to the period, but it was the only letter with no clean kind answer, and I wished for one.
- **Liked less:** the counterfeit note, where the kind-sounding "Stand the loss" and the civic "Give the Secret Service man a description" sat side by side. I gave the description; it came back in April as a summons to attend court, and the follow-up was consistent and fair.
- Dates and holidays fit their days. "CHRISTMAS.—Santa Claus was seen on Grand street…" (Dec 1885); "CHRISTMAS.—The churches are full this morning and the shops are shut, all but the druggists'" (Dec 25 1886); St. Patrick's Day; Decoration Day; the President's wedding (June 1886); the Statue of Liberty unveiled "in a fog so thick that the guns could not find her" (Oct 30 1886); the 1886 mayoral election with Hewitt, George and Roosevelt in the right order.

## What works

- **Cost previews on every letter reply.** This is the single biggest reason I trusted the game.
- **NOTES FOR THE PROPRIETOR** ("Low stock: … Light THE STILL, or the shelf will be bare before the month is out"; "Two letters unanswered: the usual reply will go out for you") and **THE MONTH SO FAR** ("Laid out so far: $3.84 … A fire lit: 24 bottles"). I always knew what I had already done.
- **SUSPICION column reasons** ("Rose 2: replies to the post"; "Fell 1: the talk dying down").
- **BOOKS drawer.** "WHAT A BOTTLE EARNS … the first twenty-eight bottles a month only pay for them … Nobody went away over the price last month." It is clear and kind, and the month-by-month table with ink bars is lovely.
- **The ventures Prospectus warns you off a bad buy** ("this pays only once they are kept busy"), apart from the wrong number in F1.
- **"SEND IT ANYWAY"** when you press with nothing on the shelf or the fire. It is a gentle safety net.
- **The paper praising the honest shop:** "THE BOTTLE THAT HURT NOBODY", "A CLEAN BOTTLE ON MULBERRY STREET", "The Undertaker Sighs", "Unremarked by the Coroner."
- **"A NEW SHOP? PRESS AGAIN"** confirmation on the title page.
- **Saving.** SAVE, AND QUIT (April 1885) and SAVE, AND THE TITLE PAGE (April 1886) both brought me back to the shop I left. The title card read "April 1886 · $159.08 · 33 regulars", and CONTINUE matched it (`96_proof_regular.png`, `98_cont2.png`). After a hard stop part-way through May 1885 (still lit, 24 brewing, ¼ col bought), CONTINUE restored _exactly_ that: "$159.60 · 24 bottles brewing", THE MONTH SO FAR intact (`56_before_stop.png` vs `59_after_stop_proof.png`). CONTINUE opens on the current edition, and TO THE PROOF takes you back to your desk.
- **The one-click "ALL THREE STILLS · ORDER AND LIGHT 48 EACH"** button once I had several stills.

## Disclosures

- No jump-ahead. I played one game by hand from the first edition.
- After month 4 I used a shell helper to batch "light still, press, screenshot". Every decision (recipe, claims, ad size, all 30-odd letters, ventures) was made by hand from screenshots.
- I used `state` occasionally to log cash, regulars and bottles sold for the metrics, and `targets` to find button coordinates. All judgements are from screenshots.
- I read the source once (`Prospectus.cs`, `SimGame.cs`) after my first session, to confirm F1.
- The `gentle` profile already held a save and LARGER text from earlier testing (F7). I started A NEW SHOP over it.

## First hour

- **0:00, title page.** Read the content note. Saw a CONTINUE I hadn't made, chose A NEW SHOP, and got "PRESS AGAIN". Good.
- **0:02, first edition (Jan 1885).** "AMONG THE CALLERS" notices for four people with their complaints (dyspepsia, nerves, rheumatism, a teething infant). I opened a clipping. I thought the game wanted me to _read the paper_ to learn who my customers were, and that was right.
- **0:05, proof.** It looked busy at first: type case, works, letters, five drawer buttons. NOTES FOR THE PROPRIETOR told me what to do next, and that was my tutorial.
- **0:08, formula book.** The ingredient shelf has wonderful tooltips ("Water: free, from the pump … Costs nothing, cures nothing, fills a bottle"). I poured a gentle recipe, renamed it and ENTERED IT IN THE BOOK. I didn't touch the price. I wasn't sure STEEPING vs SIMPLE MIXTURE mattered.
- **0:10, type case.** I set only SOOTHES THE NERVES. I understood that bolder claims ("CURES CONSUMPTION, CHOLERA, MELANCHOLY AND BAD TEMPER !!!") would be read by the coroner: "CLAIMS, UP TO FOUR: THE CORONER READS THEM". I felt safe.
- **0:12, still.** I thought "LIGHT THE FIRE" would just work. Its tooltip said "Not enough stock on the shelf: order first." So I clicked ORDER FOR 24 · $3.12, then LIGHT THE FIRE. A two-step, but explained.
- **0:14, first press.** Twelve bottles sold, books up 36c. This was the first anxious moment: so little!
- **0:18, Feb 1885.** No letters, then a testimonial letter in March. I printed it. I noticed Miss Krawczyk had consumption.
- **0:22, March edition.** DEATH OF MISS KRAWCZYK. Worried, then reassured by the physician's line.
- **0:25, April 1885.** Ad space unlocked. I chose ¼ col for $3. I quit through the menu and came back. Fine.
- **0:30, May 1885.** I stopped the game mid-month. CONTINUE brought everything back exactly. Relief.
- **0:35–0:45, Jun–Dec 1885.** Letters every month. I learned to read the italic cost lines and stopped being nervous. Clicked: the game will tell me the price of every kindness.

## Score

**8 / 10.** A gentle player can run a kind shop here and be rewarded with some of the wittiest writing I've read in a game, and it never punished me without telling me why. One point more: a frailty signal before regulars die (F2), the honest-shop encouragement in year one (F5), and fixes for the two books/prospectus numbers (F1, F3), so the first year feels as safe as the second.
