# Panacea · round 1 · playtest by "Casual First-Timer"

A player who bought it on a whim from the Steam page because of the newspaper look. Plays management games now and then, reads only what's on screen, never opens a manual, and gives a game about 15 minutes to hook them.

**Verdict:** The look and the jokes hooked me within the first minute, and I did make and sell a remedy without help. But the "send to press" payoff never arrived. For ten straight months the lead story read **ALL QUIET IN THE WARD**, even in the issue where the coroner named my product in a baby's poisoning. The only time it changed was when I paid $20 for the front page. On top of that, the letter choices give no feedback, and my money crept down without the game ever telling me why. After an hour I'd tell a friend it is beautiful and funny, and to wait for an update.

**Setup:**
- Build `0ccdab7` (frozen copy `build-r1`), port 47011, seed `casual-1`, fresh game, **no jump-ahead**.
- Played January to December 1885: 11 sends to press, about 40 minutes of active play. I then restarted the same seed once, only to re-capture the first edition and the first proof (see Disclosures).
- Tooling: I only used `drive.py`, plus three small JSON click lists for my monthly supply-buying routine (`buy24.json`, `buy12s.json`, `buy24b.json` in the scratch folder). I wrote no scripts that make decisions.
- Screenshots: `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r1/casual-first-timer/`

## Run log

| Seed | Months | What I tried | Result |
|---|---|---|---|
| casual-1 | Jan–Mar 1885 | Accepted the starter recipe, added ½ oz laudanum, named it, brewed 16, claim "Soothes the Nerves", 60c. Then raised the price to 70c and added "Banishes Rheumatism" and "Cures Consumption" | 10, 19 and 15 bottles sold. Cash flat at about $150. Ads locked: "Not yet: the Gazette wants savings and a testimonial first." |
| casual-1 | Apr–Jun | Doubled the laudanum to 1.5 oz ("fatal in a large dose"; I just wanted more "Kick"). Answered "Something Stronger" (promised stronger). Printed "A Grateful Letter" as a testimonial | 20, 19 and 21 bottles sold. Customers turned "asks for it by name". Cash rose to $157.90 |
| casual-1 | Jul–Aug | Ads unlocked; bought ½ column ($6). Paid $50 for the mail-order diploma | 24 and 24 bottles sold, one new customer. The diploma took a third of my cash. Earned the "Doctor" capability |
| casual-1 | Sep–Oct | Halloran's infant died. Inquest letter: I chose "Testify to the purity". Paid the sanitary inspector $3. Second death (Whitlock). Cut laudanum back to 1 oz and bought the FRONT page ($20) | Coroner's verdict: "Died of poisoning by a proprietary medicine" and named my remedy. Reputation fell from 23 to 7. Cash $100 |
| casual-1 | Nov–Dec | Front page ran as "SOOTHES THE NERVES!". Back to ½ col. Third death (Widow Pruitt). Regulars drifting away | Cash $84.16, then $78.68. 9 regulars left of 13 |

**Timeline of key moments**
- **0:00.** The first edition is gorgeous: "A NEW CONCERN · On Mulberry Street · He Promises Much." I smiled at the weather box ("Snow, with general coughing."). The only clear thing to do was **TO THE PROOF ▸**, so I clicked it (`r01-first.png`).
- **~1:00.** On the proof, the Type Case said "No remedy yet: open the formula book." That hint is what got me moving (`r03-firstproof.png`).
- **~3:00.** In the Formula Book, the glass was already filled with a starter recipe. I clicked Laudanum and the Assay bars moved. The bars are clear, and "they'll come back shaking" made me laugh.
- **~4:00.** I clicked into the name box, which already held real text ("Dr. ——'s Celebrated Panacea"), and typed. The box is so narrow that I couldn't see what I'd done. My remedy was permanently entered as **"Dr. ——'s CelebraMulberry Soothing Syrupted Panacea"**. I never found a way to rename it, and that name is printed in every edition since.
- **~6:00.** THE STILL: "FIRE THE STILL: 0 BOTTLES", greyed out. It took me a while to realise I had to buy ingredients in SUPPLIES first. Nothing in the still says "buy supplies". After about 15 clicks of +8 / +32, it read 16 bottles. I fired the still and sent to press.
- **~8:00.** "THE PRESSES ARE RUNNING · 702 COPIES." Then the second edition: **ALL QUIET IN THE WARD / Nothing to Report.** The sale only shows up as a line at the bottom: "moved 10 bottles". It was anticlimactic.
- **Months 2–6.** The same headline, the same woodcut ("A RESIDENT.") and the same Wants & Notices every month. I kept sending without knowing what I was aiming for.
- **Month 6 (July).** The NOTICES column said "With savings in hand and a testimonial to print, the papers will take your money." The ad preview on the proof, with my green bottle, is the best moment in the hour (`59-ad.png`).
- **Month 9 (September).** DEATHS: "HALLORAN.—On Friday last, the infant child of Mrs. Bridget Halloran … aged 7 months." That chilled me. It was the humour working. But the headline was still **ALL QUIET IN THE WARD** (`71-ed9.png`).
- **Month 10 (October).** The Coroner's Court column named my remedy, and the headline was still "ALL QUIET" (`79-ed10.png`).
- **Month 11 (November).** I paid $20 for FRONT, and at last there was a real headline: "SOOTHES THE NERVES! … MR. JAN KOWALSKI … RESTORED", with the quote "'I cannot do without it'". That is very funny, but I had to buy it (`88-ed11.png`, `89-ed11b.png`).

## Metrics

| Month (edition) | Cash | Bottles sold (prior month) | Regulars | Deaths | Headline |
|---|---|---|---|---|---|
| Jan | $150.00 | — | 12 | 0 | A NEW CONCERN |
| Feb | $147.72 | 10 @60c | 12 | 0 | ALL QUIET IN THE WARD |
| Mar | $150.10 | 19 @70c | 12 | 0 | ALL QUIET |
| Apr | $149.68 | 15 | 12 | 0 | ALL QUIET |
| May | $155.00 | 20 | 12 | 0 | ALL QUIET |
| Jun | $157.90 | 19 | 12 | 0 | ALL QUIET |
| Jul | $162.20 | 21 | 12 | 0 | ALL QUIET |
| Aug | $158.48 | 24 | 13 | 0 | ALL QUIET |
| Sep | $106.84 | 24 | 12 | 1 (infant) | ALL QUIET |
| Oct | $100.11 | 21 | 11 | 1 + inquest | ALL QUIET |
| Nov | $84.16 | 19 | 10 | 1 | SOOTHES THE NERVES! (paid front) |
| Dec | $78.68 | 16 | 9 | 0 | ALL QUIET |

- **Time to first useful action:** about 1 minute (the Type Case hint). **Time to first bottle:** about 6 minutes, most of it spent working out that the Still needs Supplies.
- **Clicks to restock and brew a month's batch:** 14–17 (each ingredient is bought separately in +8 / +32 steps).
- **Lead stories that reacted to events:** 0 of 10 non-paid editions. One changed only because I paid for the front page.
- **Feedback after a letter choice:** none on the spot in 5 of 5 letters. The letter just disappears.
- **Profit per month:** never shown. I only saw the closing balance.

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | The lead story never reacts. "ALL QUIET IN THE WARD / Nothing to Report. / COUGHS STEADY." with the same "A RESIDENT." woodcut, for 10 months. That includes Sep (infant death) and Oct (coroner names my remedy). The SEND TO PRESS tooltip promises "The lead story is written by what happens." | Edition, centre column | Play seed casual-1 to Sep–Oct 1885 with 1.5 oz laudanum (`71-ed9.png`, `79-ed10.png`) | Make deaths, inquests, record sales and new customers produce a headline. A coroner's verdict naming the product should be the lead. |
| 2 | Major | The remedy name is easy to mangle and can't be changed. The default is real text in a narrow box; clicking in and typing inserts mid-string. I got "Dr. ——'s CelebraMulberry Soothing Syrupted Panacea", printed in every edition, with no rename afterwards. | Formula Book, name box at about (580,490) | Open the Formula Book in month 1, click the middle of the name, type. Caveat: my Ctrl+A was sent through the driver and may not have arrived. | Use placeholder text (or select all on focus), widen the box, add a rename option to the Formula Book. |
| 3 | Major | Letter choices give no feedback. After "Testify to the purity" or "Send the fifty dollars" the letter just vanishes. The $50 left with no confirmation, and I only learned the inquest result a month later. | Letters drawer | Answer any letter, e.g. "A Medical Education, by Post" (`66-letter3.png`, `67-diploma.png`) | Show a one-line result on the spot ("You are $50 poorer and a Doctor from next month"), and put costs on the buttons. |
| 4 | Major | I never knew whether I was making money. Cash went from $162 to $78 over five months and the paper never said why (ad spend, diploma, ingredients). | Edition "THE TRADE." and the Works panel | Play Jul–Dec with a ½ col ad | Add a short line to THE TRADE: "takings $X, costs $Y, advertising $Z". |
| 5 | Minor | The Still says "FIRE THE STILL: 0 BOTTLES (most you can: 0)" without saying which ingredient is short. I hit this twice (water, then alcohol). | The Still | Brew until water runs out (`85-still.png`) | Say "short of Water (need 3½ oz a bottle)", and add a "buy enough for N bottles" button. |
| 6 | Minor | Restocking takes 14–17 clicks every month on ±8/±32 buttons for five ingredients. It was tedious by month 4. | Supplies | Any month | Add "restock for this recipe × N". |
| 7 | Minor | The death is attributed to the wrong person. The DEATHS notice says the *infant* died. The clipping says "IN MEMORIAM Mrs. Bridget Halloran", the inquest is "into the Death of Mrs. Bridget Halloran", yet PERSONAL says she "is in mourning". Also "of Cherry street" in one place and "of Mulberry Bend" in another. | Sep edition clippings and Letters | Seed casual-1, Sep 1885 (`72-death.png`, `75-letters.png`) | Name the infant as the deceased and keep the address consistent. |
| 8 | Minor | Wrong pronoun: "PRUITT.—Peacefully, Widow Agnes Pruitt, retired, aged 69 years. He slept soundly to the last." | Nov edition, DEATHS | Seed casual-1, Nov 1885 (`88-ed11.png`) | Make the obituary template use the customer's gender. |
| 9 | Minor | The ad unlock is vague. "Save a little, and get yourself a testimonial." I had $157 and a printed testimonial in June, but it only unlocked in July. I never learned the target amount. | Proof, YOUR SPACE box | Months 1–6 | State the bar: "$150 saved and one testimonial (you have $X, 0/1)". |
| 10 | Minor | "Doctor" is earned ("A title for your name and your labels … yours to use from the next"), but I never found where to use it. My label stayed "Dr. ——'s". | Sep clipping, then the Formula Book | Pay for the diploma (`73-doctor.png`) | Offer it in the name/label box, or apply it automatically and say so. |
| 11 | Minor | The same ads (Whittle, Grimsby, Bloom, de la Rose) and the same Wants & Notices repeat word for word every month. The jokes were great once and stale by month 5. | Edition side columns | Any run past month 4 | Rotate more variants, and let some notices react to the player. |
| 12 | Low | My own ad in the edition is listed as clickable but nothing happens when I click it. | Aug edition, right column ad | Click the ad (`62-adclick.png`, `63-adclick2.png`) | Open a clipping, or make it non-clickable. |
| 13 | Low | SEND TO PRESS is fully live in month 1 with no remedy, so a new player can waste a month. | Proof | Month 1, before brewing | Ask for confirmation when nothing is brewing or on the shelf. |
| 14 | Low | The press sequence shows the same "ALL QUIET" headline flying past. With finding 1 fixed it would be a payoff; for now it's 9 seconds of waiting. I didn't test whether it can be skipped. | Press screen (`70-press.png`) | Any send | Consider tying it to the real headline first; a skip option is optional. |
| 15 | Low (tooling) | `kill $(cat /tmp/panacea-driver-47011.pid)` left the Godot process alive. The next `drive_start.sh` failed, and the log filled with `System.InvalidOperationException: Not listening` at `Driver.cs:line 52`. | Driver | Stop and restart on the same port | Kill the process group, and stop polling in `_Process` when `Start()` failed. |

No game exceptions during play. Startup prints the usual ALSA/audio `ERR_CANT_OPEN` lines only.

## What works

- **The first impression.** The masthead, "VOL. XV.—NO. 600.", the weather box and the woodcut proprietor in the first edition sold me instantly. The look matches the Steam-page promise.
- **The hints that exist.** "No remedy yet: open the formula book." and "Nothing to advertise. Enter a recipe in the formula book." are exactly the right size. More of this, please.
- **The Formula Book.** The pre-filled glass, coloured layers and live Assay bars with flavour text ("they'll come back shaking", "fatal in a large dose", "Customers will return to this bottle whether they like it or not."). I understood the trade-off without a manual.
- **The ad preview on the proof** (`59-ad.png`, `84-front.png`). My bottle in Scheele's green appears in the paper before I commit. FRONT shows "YOUR NOTICE, SET AS NEWS". It's the most satisfying moment.
- **The humour lands when it's new.** Favourite lines:
  - "WANTED.—A PHYSICIAN willing to endorse anything. Diploma not required, but helpful."
  - "Mrs. Whittle's correspondence continues brisk, notwithstanding her funeral in March."
  - "Coffins, pine, $4.00. Lilies, plentiful."
  - "after a short and remarkably cheerful illness"
  - "Weather: Raw. Coughs ripening nicely."
  - Fogg & Sons thanking "Our Friends in the Medicine Trade"
  - The front-page testimonial: "'I cannot do without it,' says Mr. Jan Kowalski."
- **The counter of regulars.** Named woodcut customers with status tags ("peaky · delighted", then "gravely ill · asks for it by name", then "hale · shaking") quietly told the addiction story better than any number. The skeleton creeping in at the left edge is a great touch.
- **The clippings.** The new-customer card, In Memoriam card and capability card are clear and in-world.

## Disclosures

- No jump-ahead and no `--fake`, `--autopress`, `--pour` or `--clip`.
- I used `targets` to get button coordinates (a real player would just see them). I used `state` after each press to log cash, sales and deaths accurately; every number above was also visible on screen.
- Some early screenshots from my main run could not be viewed because of a tool limit. I restarted seed `casual-1` once, only to re-capture the first edition and first proof (`r01-first.png`, `r03-firstproof.png`). My early narration matches those screens.
- I used `read` to quote some text exactly (the first-edition Personal column and the November obituary).
- Ctrl+A on the name box was sent as a driver key event. If the driver didn't deliver it, finding 2 is "insert at caret" rather than "select-all failed". The narrow box and the missing rename stand either way.
- I can't hear, so nothing here is about audio.

## Score

**5.5 / 10.** It looks and reads like nothing else, and making a remedy is genuinely fun. But the paper never reports what I did unless I pay it to, my choices give no feedback, and I couldn't tell whether I was winning, so the hour didn't hook me.
