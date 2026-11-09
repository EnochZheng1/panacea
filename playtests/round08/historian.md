# Panacea · round 8 · playtest by "Gilded Age historian"

I teach American social history (New York 1870–1910: Tammany, the Lower East Side, the patent-medicine trade, the Pure Food and Drug Act) and play history-themed strategy games now and then. I was new to this game. I judged it on whether the period feels right, whether the satire is fair to the people it shows, and whether it gets dates, prices, institutions, laws and idioms right.

## Verdict

This is the best-researched satire of the patent-medicine trade I have seen in a game. The Pinkham-style "Mrs. Whittle answers every letter", the subscription "mug book", Coroner Messemer, the Proprietary Association's "red clause", Collier's "Great American Fraud" in October 1905, and the Act passing at the end of June 1906 are all accurate. They are aimed at the sellers and the men who let them sell, as the content note promises. The main weakness is the pool of filler items the paper repeats. Several of them are fixed to the 1880s (the Metropolitans, Harry Hill's, the Signal Service, the Guion steamer _Wyoming_), and they keep running into the 1900s. The money scale is also far below real Bowery prices.

## Setup

- Build `80e045d`, the frozen copy at `build-r8`. Port 47082.
- **Game 1:** seed `hist-1`, `--player --profile historian`. I played by hand from the first edition (January 1885) to February 1886: 13 sends to press, one menu SAVE AND QUIT, a relaunch and CONTINUE, then SAVE AND THE TITLE PAGE.
- **Jump-ahead games** (said so here; they open on the paper and are never saved), all on seed `hist-1` with `--dose 0.5`:
  - `--play 100` (May 1893): 2 months by hand.
  - `--play 200` (September 1901): 2 months by hand.
  - `--play 240` (January 1905): played to the end of the game in December 1906, about 24 months. I answered letters by hand for the first month or two. After that a small script opened the letters, brewed and pressed, and I read what it captured.
- Tooling, all in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r8/historian/`:
  - `month.sh` (brew, press, screenshot and dump the paper text)
  - `fullread.sh` (scrolls the whole paper and dumps its text)
  - `loop.sh` (dumps the letters, brews, presses, dumps the paper)
- About 45 minutes of active play in game 1, plus about 40 minutes of jump-ahead reading.

## Run log

| Game | Seed / args         | Months                           | What I tried                                                                                                                                                                                                                                    | Result                                                                                                |
| ---- | ------------------- | -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| 1    | hist-1 `--player`   | Jan 1885 – Feb 1886 (13 presses) | First recipe: ½ oz laudanum, peppermint, syrup, spirit and water at 50c ("Soothing Cordial"). In May I took the laudanum out and put in valerian. Bought ¼ col, then ½ col. Answered every letter. Took up THE SHOP NEXT DOOR in December 1885. | Cash $150 → $176.86. Regulars 12 → 27 → 24. 5 deaths, 1 inquest ("Visitation of God").                |
| 2    | hist-1 `--play 100` | May–June 1893                    | Paid the $25 Internal Revenue special tax. Brewed 36.                                                                                                                                                                                           | Read two papers and one letter.                                                                       |
| 3    | hist-1 `--play 200` | Sept–Oct 1901                    | Answered the "Wrong Bottle" and "Pen-Wiper" letters.                                                                                                                                                                                            | Read two papers and two letters.                                                                      |
| 4    | hist-1 `--play 240` | Jan 1905 – Dec 1906              | Read every edition and letter up to the Act and the closing edition.                                                                                                                                                                            | The game ends 29 December 1906: "The business is closed after 22 years: 7,672 bottles sold, 14 dead." |

Timeline of key moments in game 1:

- **January 1885:** Title page with its content note. The first edition gives the lead "A NEW CONCERN … On Mulberry Street."
- **February:** The first letter is a testimonial. I printed it, and it ran in March as "A TESTIMONIAL … Mrs. Eliza Clrake … asks that her name be spelled correctly".
- **March:** "TO ADVERTISERS.—The Gazette is pleased to announce…": ad space opens.
- **April:** A landlord letter about the roof over the still. The paper carries the Pedestal Fund item.
- **May:** 4 deaths. "Physicians attribute the trouble to the weather, the water and the foreign element, in that order. Nobody has thought to look at the bottles." Then a Coroner's notice.
- **June:** The inquest verdict: "Coroner Messemer … pronounced 'perfectly harmless, in moderation.' Verdict: 'Died by the Visitation of God.'"
- **June:** I saved and quit through the menu, relaunched and CONTINUED.
- **August:** Grant's funeral appears ("The General's funeral went up Broadway for five hours").
- **September:** The Pedestal Fund passes $100,000.
- **December:** Tammany (the "Hogan Association") Christmas dinner letter, and a diploma-mill letter. I took up THE SHOP NEXT DOOR.
- **January 1886:** "DEATH OF MR. LOMBARDI."

## Metrics

- **Cash, game 1 (start of each month):**

  | Month (1885 unless marked) | Cash    |
  | -------------------------- | ------- |
  | Jan                        | $150.00 |
  | Feb                        | 151.08  |
  | Mar                        | 150.20  |
  | Apr                        | 156.28  |
  | May                        | 158.86  |
  | Jun                        | 165.46  |
  | Jul                        | 169.56  |
  | Aug                        | 168.86  |
  | Sep                        | 171.16  |
  | Oct                        | 171.96  |
  | Nov                        | 180.76  |
  | Dec                        | 179.06  |
  | Jan 1886                   | 172.66  |
  | Feb 1886                   | 176.86  |

  The net gain over 13 months was $26.86.

- **Bottles sold per month:** 18, 19, 34, 37, 36, 36, 40, 42, 43, 57, 56, 57, 51.
- **Regulars:** 12 → 16 (April) → 20 (September) → 27 (December) → 24 (February 1886).
- **Letters received in game 1:** 17 over 13 months. Each one offered 2–3 replies, with the cost and effect printed under each.
- **Time to first venture:** 12 months (the shop next door needs 25 regulars). Its effect in the next two months was −$5 a month upkeep, and regulars went from 27 to 24. I could not tell yet whether it was worth it.
- **Repetition of filler items in the paper:** across the 24 editions of 1905–06 I counted about 70 distinct "City Items / Briefly" paragraphs. About 60 of them were ones I had already read in the 1885 papers.
- **Period accuracy:** 19 problems listed below, 2 of them internal continuity errors. 7 of the 19 I am sure of, against roughly 40 items the game gets clearly right.

## Findings

| #   | Severity | What I saw                                                                                                                                                                                                                                                                                                                                                                     | Where                                                                   | How to reproduce                                                   | Suggested fix                                                                                                          |
| --- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| 1   | Major    | 1880s-specific filler keeps running in the 1890s and 1900s: "The Metropolitans beat the Brooklyns", "At Harry Hill's on Saturday, a sparring exhibition", "The Signal Service predicts", "the steamer Wyoming, from Liverpool", "The pedestrian match at the Garden", "New Year's.—Gentlemen made their calls". It undoes the paper's otherwise careful sense of time passing. | City Items / Briefly. Seen in May 1893, Sept 1901, Jan 1905 – Dec 1906. | `--play 240`, read any 1905 edition (e.g. `j241.txt`, `j247.txt`). | Give every filler item an earliest and latest year, the way the dated items (Pedestal, Grant, Collier's) already have. |
| 2   | Major    | Filler repeats heavily: the same ~60 paragraphs make up most of every edition across 22 years ("Patrick Hagan, 40, was fined…", the Baxter street fire and others). By 1905 the paper reads like a rerun.                                                                                                                                                                      | All editions.                                                           | Compare `m04.txt` (April 1885) with `j247.txt` (July 1905).        | More era-tagged pools, and fewer repeats of the same item within a few years.                                          |
| 3   | Minor    | Money is on a scale about a tenth of the period's: shop rent $3 a month, a boy to mind the fire at $2 a month, a College of Pharmacy apprentice at $4 a month, monthly takings of $9–28. A Mulberry street store would rent for tens of dollars a month, and an errand boy earned about $2–3 a week. Bottle prices (50c) and ad rates are period-right, so the mismatch shows. | Ledger in THE TRADE; letters (Escher 1905, ventures).                   | Any "books" clipping.                                              | Either scale the economy up, or have the paper make a joke of the shop's smallness.                                    |
| 4   | Minor    | Letter continuity: Feeney's handbill (Nov 1885) is signed "J. FEENEY, Druggist, two doors down". Two months later "A Rival on the Corner" announces "Feeney has opened two doors down."                                                                                                                                                                                        | Morning's Post, Nov 1885 → Jan 1886.                                    | Game 1, seed hist-1.                                               | Gate the price-war letter on the rival-opens letter, or the reverse.                                                   |
| 5   | Minor    | "Your customer these four years, P. HAGGERTY" in January 1886, though the paper opened the shop as "A NEW CONCERN" in January 1885.                                                                                                                                                                                                                                            | Morning's Post, Jan 1886.                                               | Game 1.                                                            | Write "your customer, and your predecessor's" or scale the years to time in trade.                                     |
| 6   | Low      | The 1885 title page shows a sample "Among the callers" (Russo, Kowalski, Abrams, Zielinski) that differs from the first edition's (Pettibone, Rosen, Donnelly, Clarke). The footer behind the CONTINUE card shows "now January 1885 · $150.00 · 12 regulars" while the card says "June 1885 · $159.06 · 16 regulars".                                                          | Title page.                                                             | Relaunch with a save. See `34_title2.png`.                         | Hide the footer behind the title card, or fill it from the save.                                                       |
| 7   | Low      | The copper pot "still" is a game-ism. Proprietary cordials were mixed and percolated, not distilled. The game does later offer a "glass percolator", which is the right tool.                                                                                                                                                                                                  | THE STILL drawer.                                                       | n/a                                                                | Keep it, but a line in the still text ("we call it the still; nothing is distilled") would please the pedants.         |
| 8   | Low      | No Chinese residents or businesses appear among callers or items, although the paper names Mott and Pell street repeatedly and imports opium from Smyrna. I think leaving them out is a defensible choice (it avoids the period's own caricature), but historians will notice it.                                                                                              | Callers, Personal.                                                      | n/a                                                                | Optional: one respectful item, or leave as is.                                                                         |

I found no exceptions in the driver log, only the usual ALSA and audio fallback lines.

## Period accuracy

| #   | What the game says                                                                                                    | Where (month, screen)                                       | Problem                                                                                                                                                                                                                                    | Confidence                            | Correction                                                                                                                    |
| --- | --------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| 1   | "THE WEATHER.—The Signal Service predicts fair and colder."                                                           | Jan 1905 and Feb 1905 editions, City Items                  | Forecasting passed from the Army Signal Service to the Weather Bureau (Dept. of Agriculture) in 1891.                                                                                                                                      | sure                                  | After mid-1891: "The Weather Bureau predicts…"                                                                                |
| 2   | "SPORTING.—The Metropolitans beat the Brooklyns by eleven runs to four. The Atlantics' pitcher…"                      | June 1893, Sept 1901, July 1905 editions                    | The Metropolitans (American Association) folded after 1887. "Brooklyns" and "Atlantics" are the same club in one sentence.                                                                                                                 | sure (post-1887 use)                  | Retire it after 1887. In the 1900s use the Giants v. Brooklyn ("Superbas") or the Highlanders (1903+).                        |
| 3   | "At Koster & Bial's the vitascope shows a train coming into a station."                                               | Dec 1904 edition                                            | The Vitascope's debut at Koster & Bial's was April 1896. The train-arriving film is Lumière's, shown with the Cinématographe. Koster & Bial's 34th St hall was gone by about 1901 (Macy's site).                                           | likely                                | Date it to 1896–97, or for 1904 use "the nickel show" or the bioscope at a vaudeville house.                                  |
| 4   | "At Harry Hill's on Saturday, a sparring exhibition…"                                                                 | May 1893, Sept 1901, 1905 editions                          | Harry Hill's Houston St resort lost its licence and closed in the mid-1880s.                                                                                                                                                               | likely                                | Limit it to 1885–86. Later, use an athletic club bout (boxing was only semi-legal in NY until the Horton Law, 1896–1900).     |
| 5   | "SHIPPING.—Arrived, the steamer Wyoming, from Liverpool, with 612 steerage…"                                          | 1885, June 1893, 1905 editions                              | Right for 1885 (Guion Line). The Guion Line stopped sailing in the mid-1890s, so it is wrong for 1905.                                                                                                                                     | likely                                | Rotate ships by era (e.g. Cunard _Umbria_/_Etruria_ 1885–1900; _Lucania_, _Campania_; NDL _Kaiser Wilhelm der Grosse_ 1897+). |
| 6   | "THE TROLLEY.—The new electric cars on Third avenue have killed another dog…"                                         | May 1893 edition                                            | Manhattan barred overhead trolley wires. Third Avenue went from horse cars to cable in 1893, and to underground-conduit electric only around 1899. Brooklyn had the trolleys ("trolley dodgers").                                          | likely                                | 1893: "The new cable cars on Third avenue…" or "The trolley cars in Brooklyn…".                                               |
| 7   | "Dear Sir,—We are preparing a History of the Lower East Side of New-York…"                                            | Sept 1885 letter (Historical & Biographical Publishing Co.) | "Lower East Side" as a place name belongs to the 1890s–1900s. In 1885 one said "the East Side" or named the ward. Mulberry street is also west of the Bowery, in the Sixth Ward, not the East Side as then understood.                     | likely                                | "A History of the Sixth Ward and the Lower Wards of New-York…"                                                                |
| 8   | "THE PULPIT.—The Rev. Dr. Parkhurst preached on the sins of the city, naming forty."                                  | April 1885 edition (repeats later)                          | Parkhurst was at Madison Square Presbyterian in 1885, but his famous anti-vice crusade against Tammany and the police began in February 1892.                                                                                              | likely                                | Start it in 1892. Before then use a generic "a clergyman of Fifth Avenue".                                                    |
| 9   | "THE MUCKRAKERS.—Collier's prints 'The Great American Fraud'…"                                                        | October 1905 edition (end-of-month issue)                   | Collier's series timing is right (October 1905). But "muckraker" was Roosevelt's coinage of spring 1906.                                                                                                                                   | sure (term), sure (series date right) | Headline "THE MAGAZINES." in 1905. Use "MUCKRAKERS" from April 1906.                                                          |
| 10  | "THE CITY.—The Health Department has seized forty barrels of swill milk in Brooklyn."                                 | April 1885 edition                                          | Before 1898 consolidation Brooklyn was a separate city with its own Board of Health. New York's Health Department had no reach there.                                                                                                      | likely                                | 1885: "The Brooklyn Board of Health has seized…" (or move the seizure to Manhattan).                                          |
| 11  | "TWO CENTS. Cheaper than the World."                                                                                  | Masthead, May 1893                                          | The _World_ was itself two cents until it cut to one cent in 1896 in the war with Hearst's _Journal_.                                                                                                                                      | unsure                                | "TWO CENTS. As Cheap as the World." The 1901 "One Cent, like the Journal" is right.                                           |
| 12  | "NEW YEAR'S.—Gentlemen made their calls yesterday, two hundred houses apiece…"                                        | Feb 1886 (fine), Jan 1906 (repeat)                          | The Knickerbocker New Year's calling custom had largely died out by the 1880s–90s. It reads oddly in 1906.                                                                                                                                 | likely (for 1906)                     | Limit it to the 1880s. For 1906 use a Times Square ball-drop crowd (first held 1907; Times Square revels from 1904).          |
| 13  | "SPORTING.—The pedestrian match at the Garden enters its fifth day."                                                  | 1885 (fine), 1905 (repeat)                                  | The six-day pedestrian craze peaked around 1879–1888. By 1905 the Garden's six-day races were on bicycles.                                                                                                                                 | likely (for 1905)                     | 1900s: "The six-day bicycle race at the Garden enters its fifth day…"                                                         |
| 14  | "TAMMANY.—The Hall's new sachem has promised clean streets…"                                                          | Sept 1901 and 1905 editions                                 | Sachems were officers of the Society of St. Tammany. The political chief was "the Leader" or "the Boss" (Croker until 1902, then Murphy).                                                                                                  | unsure                                | "The Hall's new Leader…"                                                                                                      |
| 15  | "the new building at Madison square is twenty stories high … 'twenty-three skidoo.'"                                  | 1905 edition                                                | The Flatiron was finished in 1902 at about 20–22 floors, so "new" in 1905 is a stretch but tolerable. The skirt-and-wind story is genuine lore.                                                                                            | unsure                                | Date it to 1902–03.                                                                                                           |
| 16  | Morphine $2.50, coca 40c, laudanum 15c an ounce (1885); heroin 28c, aspirin 10c an ounce (1901)                       | THE MARKETS, Formula tooltips                               | 1885 prices look right. Aspirin was under a US Bayer patent (1900–1917) and was notoriously dear in America, so 10c an ounce seems far too cheap.                                                                                          | unsure (aspirin)                      | Raise aspirin, or leave it off THE MARKETS line until later.                                                                  |
| 17  | Rent $3 a month for the shop; boy at $2 a month; apprentice at $4 a month                                             | Ledger, ventures, letters (1885–1905)                       | All wages and rents are about a tenth of period figures. See Finding 3.                                                                                                                                                                    | likely                                | Scale them up, or make the smallness a running joke.                                                                          |
| 18  | "The new federal law … must now declare upon the label any alcohol, opium, morphine, cocaine, chloroform or cannabis" | Closing edition, Dec 29 1906                                | The Act's list also included heroin, chloral hydrate, acetanilide and the eucaines. Heroin matters here, since the game sells it in the 1900s.                                                                                             | sure                                  | Add "heroin, chloral hydrate, acetanilide".                                                                                   |
| 19  | THE ECLECTIC MEDICAL COLLEGE OF OHIO, "Diplomas Conferred by Post · Cincinnati"                                       | Dec 1885 letter                                             | The Cincinnati Eclectic Medical Institute was a real, chartered (if sectarian) school. The notorious mail-order diploma mill was Buchanan's in Philadelphia (exposed 1880). Pinning the mill on a near-real school's name is unfair to it. | likely                                | Use an invented name ("The American Eclectic University of Medicine, Philadelphia").                                          |

**What the game gets right.** This is a long list, and the list is the point:

- **Calendar and dates.** The Saturday dates check out (Jan 3 1885, Aug 31 1901, Dec 29 1906 are all Saturdays). Volume and issue numbers advance correctly for a weekly established in 1871.
- **1885 news.**
  - The World's Pedestal Fund appeal in spring 1885, passing $100,000 "chiefly in dimes from schoolchildren" in August.
  - Grant's funeral up Broadway in August 1885, with "a throat lozenge … advertised in his memory": a sly and correct nod to his throat cancer.
- **Coroner's inquests.** Coroner Messemer is a real name from the period, and the jury's "Died by the Visitation of God" verdict is exactly right.
- **Rival nostrums.**
  - Mrs. Whittle's Compound, "correspondence continues brisk, notwithstanding her funeral", is Lydia Pinkham to the life (died 1883, letters answered by staff, exposed in 1905).
  - The galvanic belts, liver pads, anti-fat and radium tonics of 1904 are all real types of the period.
  - The microbe-killing dilute sulphuric acid recalls Radam's Microbe Killer.
- **Places and institutions.**
  - Pearl street and Maiden Lane for the wholesale druggists.
  - The coroner sitting on Centre street.
  - Tony Pastor's and the Bowery dime museum.
  - The red ball up for skating in Central Park.
  - Oysters out until September.
  - The Children's Aid Society and the newsboys.
- **Political, legal and trade letters.**
  - The subscription "History … with Biographical Sketches of its Leading Men", $3 for a sketch and $8 for a steel engraving, is the mug-book racket exactly.
  - The district leader's Christmas dinner and pound of coffee, with "the list is also kept".
  - The $25 federal special tax for retail liquor dealers on drinkable "medicines" (1893).
  - The Secret Service chasing counterfeit notes.
  - Rural Free Delivery and a two-cent stamp (1905), and drops for a drinking husband slipped into his coffee.
- **The road to the 1906 Act.**
  - The Proprietary Association's assessment and Cheney's "red clause" in advertising contracts.
  - The Bureau of Chemistry asking for bottles (Wiley).
  - Collier's in October 1905, and the Act passed in June 1906 with labels required from 1 January 1907.
- **1900s city life.**
  - A nickelodeon showing _The Great Train Robbery_ with illustrated songs (1906).
  - A shirtwaist contractor on Walker street.
  - The motorist buying benzine from a druggist.
  - Luna Park's lamps.
- **Newspaper price history.** Five cents, then two, then one cent "like the Journal", and "NOW WITH HALF-TONES" as an 1890s boast.

**Fairness of the satire.**

- Mostly the satire hits the right targets. The physicians who blame "the foreign element" are the joke. So are the St. Bartholomew's ladies whose fair for the poor did not admit the poor, the trade that regards a woman who "has got better on her own" as "a breach of faith", and the editor's paragraph on the girls at the machines.
- The immigrant names (Irish, Italian, Russian-Jewish, Polish, German, Scandinavian) and occupations (seamstress, longshoreman, "wife of a working man") are plausible for the lower wards. They are never the butt of the joke.
- The letters from the poor (Mrs. Quaid's pen-wiper, Mrs. Burke's boys, Mrs. Carrow's egg money) are written with dignity.
- Two items lean on the victims rather than the trade:
  - "PERSONAL.—Will the lady who bought a bottle of Elixir on Thursday and left her baby please return for the baby."
  - "The poor of the Sixth Ward will burn the furniture, as usual".

  Each works once. Repeated every year, they start to sound like the paper sneering at the poor rather than the game sneering at the paper.

## Letters

- **Worth reading:** yes. They are the best writing in the game.
- **My favourites:**
  - The coroner's notice ("The Coroner's time is valuable, as is his good opinion").
  - The mug-book letter ("Gentlemen who do not subscribe are not, as a rule, leading men").
  - Kessler the printer ("He points out that CURSES is the stronger word, and that nobody reads a label").
  - Mrs. Carrow from Licking County.
  - H. Kemp's piece-work girls ("I would put it in the water pail").
  - The Member's letter ("The two are not connected, the Member asks me to say").
- **Real choices:** yes. Every reply shows its cost and effect ("$8; standing +1, you become better known, and about one more caller a month for a year"), so I could always tell what a reply would do. Sometimes it is too explicit: "suspicion +2" on a letter takes some of the period voice away.
- **Memory:** good. The benefit society, the pedestal subscription, the CURSES label and the gilt sign each turned up afterwards as named sources of callers. The printed testimonial ran the next month, and the inquest verdict was printed after it sat.
- **Seen twice:** not within one game. Kessler, Feeney and the Hogan dinner each appeared in both game 1 (1885) and the 1905 jump-ahead game. "As every year" makes the Hogan repeat fine.
- **Wrong for its year:** "Lower East Side" (1885), and the diploma mill named for a real Ohio school (see the Period accuracy table).

## What works

- The newspaper as the whole interface. The month's consequences arrive as lead story, deaths, inquest and personals in period voice, and it really feels like reading a weekly.
- The masthead changes over time: price, slogan, "NOW WITH HALF-TONES", "Foe of Fraud".
- The cut-out obituary for Mr. Lombardi, longshoreman, "of Mulberry Bend, aged 40 years. Funeral private."
- The closing edition. "THE LAW ARRIVES … The Trade Is Thoughtful" and the career-in-review column ("Fourteen of the shop's customers, or the infants they bought for, died while it traded") is a strong, sober ending.
- The content note on the title page is well judged and short.
- Save and CONTINUE worked exactly: back to the June 1885 paper with $159.06, 16 regulars, 36 bottles brewing, the ¼-column ad, the answered letters and the society subscription intact.
- The ventures prospectus writes its conditions in period terms ("The coppersmith on Elm street…").

## Disclosures

- Three jump-ahead games (`--play 100`, `--play 200`, `--play 240`, dose 0.5), none saved. After the first hand-played months of 1905 I used a script to press through most of 1905–06, and read the dumped text rather than every screenshot.
- I used `read` dumps to quote text exactly and to search whole papers (e.g. for McKinley in September 1901; none found, which is a missed chance).
- I used `state` for cash and regulars figures.
- I did not read the source, docs or other playtests.
- My one attempt to pour ¼ oz with Shift+Enter did nothing through the driver. That may be my key syntax, so I don't report it as a bug.

## First hour

- **0:00** — Title page. The content note frames it well, and A NEW SHOP was obvious.
- **0:02** — First edition, January 3 1885. I understood at once that I am "the new concern" on Mulberry street. I enjoyed the briefs, and caught that the weather and the rival ads are jokes.
- **0:05** — TO THE PROOF. At first I thought the proof was the paper I had to lay out. Then I saw that the drawers (THE STILL, FORMULA, SUPPLIES, VENTURES) are the shop.
- **0:08** — The formula book clicked. Ingredients carry period-accurate tooltips (Fowler's Solution as "a respectable arsenical tonic"; calomel "lingers in the body"). I made a ½ oz laudanum cordial at 50c.
- **0:10** — The still: ORDER FOR 24, LIGHT THE FIRE. That was clear.
- **0:12** — Setting a claim in type. Ad space stays disabled until "both conditions are met", which the still screen later explained.
- **0:15** — Second edition. "EIGHTEEN BOTTLES SOLD" and the books clipping made the loop click: proof, press, paper, consequences.
- **0:20** — March. Ads opened. I bought ¼ col and set "Comfort for Teething Babes", knowing exactly what that claim meant historically.
- **0:25** — May. Four deaths and an inquest letter. That was the moment the satire landed. I took the laudanum out.
- **0:30** — June. The inquest verdict. I quit through the menu and CONTINUED, and everything was intact.
- **0:35 – 0:45** — July to December. The letters carried me. I took up the shop next door at 27 regulars.

## Score

**8 / 10.** The research and voice are excellent, and the paper and letters do real historical work while staying funny and mostly fair. To move it up a point, give the filler items an era range so the 1900s papers stop reprinting 1885, and bring wages and rents nearer the period.
