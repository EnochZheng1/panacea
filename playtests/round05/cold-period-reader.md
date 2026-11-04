# Panacea · round 5 · playtest by "Gilded Age reader"

A first-time tester who reads social history of 1880s–1900s New York (tenements, the patent-medicine trade, the muckrakers, the 1906 Act) and plays a few story-rich games a year; judges period truth, the aim of the satire, and whether a non-strategy player enjoys it.

**Verdict:** *The Bowery Gazette* is the best thing here. It is printed, dated and set with real care, it knows its Sixth Ward, and nearly every joke lands on the proprietor, the trade, the doctors and the aldermen, not on the people buying the bottles. Two things wear it down. By the fourth month the paper reruns its own standing matter: THE BRIDGE item ran in all ten editions I read. And the shop grows so slowly that, apart from the almanac, the ventures stay something I read about rather than something I bought. The infant-soothing-syrup thread, the darkest and most historically loaded thing in the game, shows the mother sickening rather than the baby.

**Setup:**
- Build `ea3dd20` (frozen copy `build-r5`), port 47017.
- **Game 1** (first session, no jump-ahead): seed 1885, played by hand January 1885 to November 1885, 10 sends to press. The machine restarted during the 11th press, which killed the game.
- **Game 2:** seed 1885 `--play 24 --dose 0.5`. One letter answered and the ventures drawer inspected, no press.
- **Game 3:** seed 1885 `--play 40 --dose 1.5`. One letter answered, one press, to see the heavy route.
- About 40 minutes of active play before the restart and about 12 after it.
- Tooling: small shell wrappers around `drive.py` in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r5/cold-period-reader/bin/` (targets, read, brew, press). Screenshots and edition text dumps are in `/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/r5/cold-period-reader/`.

## Run log

| Game | Seed / args | Months | What I tried | Result |
|---|---|---|---|---|
| 1 | 1885, none | Jan–Nov 1885 (10 presses) | Starter recipe plus ½ oz laudanum and ½ oz gentian ("Mulberry Soothing Cordial", 60c, later 70c). Claims: Soothes the Nerves and Banishes Rheumatism, plus Comfort for Teething Babes from May. Printed the first testimonial; ½ col ad from March; Almanac taken up in April; bought the Ohio "diploma" in June ($50); second copper (July), percolator (Aug) and the shop knocked through (Oct) all came as unlocks. Reformulated without laudanum in November when Death stood beside Mrs. Abrams. | Cash $150 → $219.26; 12 → 29 regulars; 1 death (Mr. Luigi Greco, Oct). Branch ($600) and Laboratory ($400) offered but never affordable. |
| 2 | 1885 `--play 24 --dose 0.5` | Jan 1887 (inspection) | Price-war letter from Feeney; ventures drawer. | $159.85, 16 regulars, 30 bottles a month. Wholesale contract ($360) and Lab ($400) both "NOT TO BE HAD · Not enough cash". |
| 3 | 1885 `--play 40 --dose 1.5` | May–Jun 1888 (1 press) | Temperance League letter (donated $5); one press. | $56.20 → $45.15, 10 regulars, suspicion "alarming (46 of 100)". Lead story "FOUND DEAD IN BAXTER STREET". No venture reachable. |

**Timeline of key moments (game 1)**
- **Jan 3, 1885:** first edition, "A NEW CONCERN · On Mulberry Street". I understood at once that the paper is the game's voice; it was less clear what I was meant to do.
- **Jan (proof):** the hints ("No remedy yet: open the formula book", "Nothing on the shelf and nothing on the still") walked me through. I had brewed and pressed within about 10 minutes.
- **Jan 31:** "NEW FACES AT THE COUNTER". The loop clicked: what I do on the proof comes back as news.
- **Feb:** first letters. "Re: A Grateful Letter" (Mrs. Lombardi) and "Re: Something Stronger" ("Doc, the tonic dont do what it did…"). The habit letter came after one month on ½ oz laudanum per bottle.
- **Feb 28:** the testimonial prints as "Mrs. Teresa Lomabrdi … who asks that her name be spelled correctly". The best joke in the game.
- **Apr:** took up the Almanac ($40). From here, "Callers at the counter: … ten by the almanac".
- **May:** the skeleton appears at the counter beside Mr. Greco ("poorly · asks for it by name"). A clear, chilling signal.
- **Jun:** "THE ECLECTIC MEDICAL COLLEGE OF OHIO · Diplomas Conferred by Post"; I bought it. "ENDORSED BY EMINENT PHYSICIANS !!" was added to the type case.
- **Oct 31:** "DEATH OF MR. GRECO". "The physician who signed the certificate did not ask what it was." Below it is the undertakers' ad: "Fogg & Sons … We Thank Our Friends in the Medicine Trade for Their Continued Patronage."
- **Nov:** "Death waits at the counter beside Mrs. Sarah Abrams" ("for a teething infant … gravely ill"). I reformulated: "Reformulated. The next batch will be the new recipe; the customers will not be told."
- **Nov (press):** the machine restarted mid-press.

## Metrics

**Game 1, by edition**

| Edition | Sold | Books | Cash after | Regulars |
|---|---|---|---|---|
| Jan 31 1885 | 22 | +$4.92 | $154.92 | 15 |
| Feb 28 | 26 | +$4.68 | $159.60 | 17 |
| Mar 28 | 36 | +$4.68 | $164.28 | 18 |
| Apr 25 | 60 (sold out) | −$23.56 (almanac) | $140.72 | 22 |
| May 30 | 48 (sold out) | +$14.04 | $154.76 | 24 |
| Jun 27 | 48 (sold out) | −$35.96 (diploma $50) | $118.80 | 25 |
| Jul 25 | 66 | +$18.72 | $137.52 | 30 |
| Aug 29 | 73 | +$26.26 | $163.78 | 30 |
| Sep 26 | 78 | +$29.76 | $193.54 | 29 |
| Oct 31 | 76 | +$25.72 | $219.26 | 29 |

- **Profit rate:** at the best rate I reached (about $27 a month), the Branch ($600) would take about 15 more months and the Laboratory ($400) about 7. The Wholesale contract also needs 1,500 bottles sold; I had about 530 after 10 months.
- **Deaths:** 1 in game 1, in month 10. Game 3 (heavy) had 1 in its first live month.
- **Time to understand the loop:** about 10 minutes, at the first edition after a press. Time to see that Death at the counter is a warning: immediate, at month 5.
- **Repetition:**
  - THE BRIDGE ("Twenty thousand crossed the Brooklyn Bridge on Sunday…") ran in 10 of 10 game-1 editions and in both jumped games.
  - THE PEDESTAL FUND ran in at least 4 editions (Mar 28, Apr 25, Jul 25, Oct 31 1885).
  - First-edition items (THE COURTS bitters suit, SHIPPING *Germanic*, POLICE COURT Hester St.) were back in full by the Mar 28 edition.
- **Personal-column templates:**
  - "has tried every remedy for X but one, and as of this month has tried that one too": 5 times in 7 editions.
  - "on the recommendation of a neighbor, who has not been seen about lately": 4 times.
- **Dates checked:** every edition date I read falls on a Saturday (Jan 3 1885, Jan 31, Feb 28 … Dec 25 1886, Apr 28 1888, May 26 1888), and the issue numbers step by 4 a month. VOL. XV in 1885 and VOL. XVIII in 1888 fit "ESTABLISHED 1871". All correct.
- **Driver log:** no exceptions. Only the expected ALSA/audio `ERR_CANT_OPEN` lines.

## Findings

| # | Severity | What I saw | Where | How to reproduce | Suggested fix |
|---|---|---|---|---|---|
| 1 | Major | **The paper reruns itself.** THE BRIDGE appeared in all 10 editions I read. Edition 1's items (bitters suit, *Germanic*, Hester St. soothing syrup) were all back by edition 4. The Personal column cycles a handful of templates ("…tried every remedy … but one…", "…a neighbor, who has not been seen about lately"). For a reader, the paper is the reward, and by month 4 it reads as reprinted. | Edition: BRIEFLY, CITY ITEMS, PERSONAL | Play any 4 consecutive months and compare `read` dumps (e.g. `63.txt`, `65.txt` in my scratch folder). | Keep a cooldown on each standing item (not again within 6–8 editions), never print the same item two editions running, and add more Personal templates or vary them per complaint. |
| 2 | Major | **Ventures are effectively out of reach for a hand player.** After 10 months of good trade I had $219, earning about $25 a month. Branch $600, Laboratory $400 ("NOT TO BE HAD · Not enough cash"), Wholesale needs 1,500 bottles. The jump-ahead proprietor was poorer still: $159.85 at month 24 and $56.20 at month 40 on the heavy route. Only the $40 almanac was ever affordable. For a non-strategy player this is the main hook to keep playing, and it stays a picture in a prospectus. | VENTURES drawer (`17.png`, `62.png`, `j04.png`) | Seed 1885, play as in the run log; open VENTURES each month. | Bring the first big venture within about 12–18 months of decent play (cheaper down payment or terms in instalments), or let the Gazette offer credit ("Terms: $100 down, the rest from takings"). |
| 3 | Major | **The teething-syrup victim is the wrong person.** "Mrs. Sarah Abrams · for a teething infant" is a regular for 10 months. The tag then reads "gravely ill · asks for it by name" and "Death waits at the counter beside Mrs. Sarah Abrams". Historically the soothing-syrup dead were the infants (Mrs. Winslow's was the scandal the AMA and Adams's *Great American Fraud* pressed). The game makes the mother the habituated patient. With "COMFORT FOR TEETHING BABES" and laudanum on the label for 6 months, nothing happened to any baby, except the free-floating FOUND item (basket on church steps). This is the sharpest historical point the game could make, and it misses its mark. | Counter tags and hover tooltip (`67.png`, `68.png`) | Put laudanum in the formula, buy for 10 months; watch Mrs. Abrams. | Track the infant as the dose-taker for "for a teething infant" customers ("Mrs. Abrams's baby: sleeps very soundly"), and let the death notice read as such. The satire then lands on the trade, as the rest of the game does. |
| 4 | Minor | The lead story repeats verbatim with a wrong count. "STILL NO BOTTLES TO BE HAD … BARE A SECOND MONTH" printed in both May 30 and June 27, 1885; by June it was the third month bare. | Lead story | Sell out three months running. | Count consecutive sold-out months ("A THIRD MONTH"), or vary the headline. |
| 5 | Minor | The edition contradicts itself. The Sept 26 1885 weather box says "Cooler. Catarrh returns with the children to school", while a BRIEFLY item in the same paper reads "THE WEATHER.—Hot and close. The Health Department urges the poor to keep out of the sun…". | Edition Sep 26 1885 (`63.txt`) | Seed 1885, reach the Sept edition. | Tag standing weather items by season, or drop them when they clash with the weather box. |
| 6 | Minor | **The Pedestal Fund item is misdated (about 80% confident).** "The World's subscription for the Statue's pedestal passes ninety thousand dollars" appears on Mar 28, 1885. Pulitzer opened the *World*'s drive on about March 16, 1885 and passed $100,000 only on about August 11, 1885. The item is right in late July, too early in March and April, and stale on Oct 31, after the drive closed. | BRIEFLY, Mar–Oct 1885 (`36.png`, `66.png`) | Read the Mar, Apr, Jul and Oct 1885 editions. | Give dated items a window: run it only in Jul–early Aug 1885, with a "pedestal fund closes" item after. |
| 7 | Minor | Large period beats are missing. Grant died at Mount McGregor on July 23, 1885, and his New York funeral (Aug 8) was the city's event of the year, but the July 25 and Aug 29 1885 editions say nothing. The Dec 25, 1886 edition has no Christmas. The game clearly has dated items (the Blizzard is in the Apr 28 1888 paper), so the gaps stand out. | Editions Jul 25 and Aug 29 1885; Dec 25 1886 | As above. | Add a few marquee dated items (Grant; the Statue's dedication, Oct 28 1886; the Blizzard, Mar 1888; Christmas). Each is a natural joke for the trade ("A remedy for grief, at all druggists"). |
| 8 | Minor | The type case overflows. Once the diploma adds "ENDORSED BY EMINENT PHYSICIANS !!", SPACE IN THE GAZETTE (NONE / ¼ COL / ½ COL / FULL COL / FRONT) is clipped at the panel bottom, half under the letters panel. | Proof, right column (`53.png`, `54.png`) | Buy the diploma; open the next proof. | Let the panel grow and push LETTERS and THE WORKS down, or wrap claims in two columns. |
| 9 | Low | **"Bowery Theatre" is anachronistic in 1885 (about 80% confident).** The old Bowery Theatre was renamed the Thalia in 1879. | AMUSEMENTS item | Appears from Feb 1885. | Call it the Thalia, the People's Theatre or the Windsor, or "a Bowery theatre". |
| 10 | Low | **The Cincinnati diploma mill is aimed at the wrong school (about 85% confident).** The notorious mail-order diploma mill was Buchanan's Eclectic Medical College of *Pennsylvania* (Philadelphia, exposed 1880). Cincinnati's Eclectic Medical Institute was a real, working (sectarian) school. "The Eclectic Medical College of Ohio · Diplomas Conferred by Post · Cincinnati" reads as a jab at the latter. | Letter "Re: A Medical Education, by Post" (`48.png`) | Month 6, game 1. | Move it to Philadelphia, or use an obviously invented name. The joke itself is excellent. |
| 11 | Low | **The baseball item mixes names (about 50% confident).** "The Metropolitans beat the Brooklyns by eleven runs to four. The Atlantics' pitcher had taken a tonic…" The Brooklyn AA club was sometimes called the Atlantics in 1884, but using both names in one item reads as two different clubs. | SPORTING | May and Jul 1885 editions. | Use one name for the club. |
| 12 | Low | Surnames and given names recur often within a game. Hoffmann ×3 (Widow Louisa, August, Heinrich); Whitlock ×2 (Agnes, Prudence); Abernathy ×2 (Henry, Agnes); Prudence ×2 (Whittle, Whitlock); Luigi ×2; Heinrich ×2; Agnes ×2. Full names are unique, but the ward feels small. Pairing given names with same-origin surnames works well (Lombardi, Dabrowski, Zielinski, Becker). | Personal column, counter | Game 1, Jan–Oct 1885. | Lower the weight of surnames already used, or make relations explicit ("Mr. August Hoffmann, brother of the widow"). |
| 13 | Low | The ventures drawer shows the proof underneath. The underlying proof text shows through the gaps between prospectus cards ("customer. And one more. dele (strike", "YOUR SPACE · NONE"). After scrolling, the top card is clipped under the proof header. | VENTURES (`16.png`, `17.png`) | Open VENTURES and scroll. | Give the drawer an opaque backing and clip its scroll area below its own header. |
| 14 | Low | An unexplained caption floats in the counter area: "Cut No. 18, again." | Proof, AT THE COUNTER (`50.png`, `53.png`) | Game 1, from June 1885. | If it's a printer's joke about the stock woodcut, set it as a caption under a figure; otherwise remove it. |
| 15 | Low | Water is "From the pump in the yard. Costs nothing", yet the still says "The shelf is short of water: 3 oz a bottle, 0 oz on hand" and the water has to be ordered from Hobart & Co., Pearl Street. | THE STILL, SUPPLIES (`10.png`, `15.png`) | Any first brew. | Treat water as always on hand. |
| 16 | Low | The batch size resets to 24 each month (and once with two stills) instead of remembering last month's 48. | THE STILL (`54.png`) | Brew 48, then reopen next month. | Remember the last batch per still. |
| 17 | Low | Habit sets in very fast. "Re: Something Stronger" ("I take two spoons and then four and its like water") arrived after the first month on ½ oz laudanum in an 8 oz bottle. That's plausible for a heavy user, but early for the ward's first buyers. | Letters, Feb 1885 (`22.png`) | Game 1 recipe. | Delay that letter a month or two on mild doses. |
| 18 | Low | The second copper arrived, and the shop was knocked through with "The landlord has put the rent up five dollars a month", without my being asked. Not wrong, but I'd have liked to say yes. | Edition notices, Jul and Oct 1885 | Reach 200 bottles / 25 regulars and $200. | Offer them as letters ("The landlord writes…") with accept/decline. |
| 19 | Low | Customers hold green bottles at the counter in January 1885, before a single bottle was sold. | Proof, first month (`05.png`) | New game, TO THE PROOF. | Show empty hands until the first sale. |
| 20 | Low | A stale tooltip appeared once: hovering Valerian Root showed the Dilute Sulphuric Acid tooltip. I couldn't reproduce it after moving away and back. | Formula book (`69.png` vs `70.png`) | Right-click Laudanum, then click Valerian. | Hide the tooltip on any click or right-click. |

## What works

- **The Gazette's period ear.** These are true to the 1880s and I was delighted to find them:
  - "VOL. XV.—NO. 600", "NEW-YORK" with the hyphen, every date a Saturday, and the price current from "J. & W. HOBART & CO. … 81 Pearl Street".
  - Mulberry Bend, Baxter, Mott, Elizabeth, Hester, Orchard, Ludlow and Cherry Streets, the Sixth Ward, the Second and Third Avenue Els, and Harry Hill's.
  - Cooper Union, the pedestrian match at the Garden, the dime museum's Living Skeleton, St. Bartholomew's charity fair, the *Germanic* and *Wyoming*, and the Blizzard.
  - A ward of Lombardis, Grecos, Dabrowskis, Beckers and Donnellys, which is right for Mulberry Street.
- **The patent-medicine parodies, each recognisable:**
  - "Mrs. Prudence Whittle's Vegetable Compound … Mrs. Whittle's correspondence continues brisk, notwithstanding her funeral in March" (Lydia Pinkham, who died in 1883, to the letter).
  - Grimsby's Galvanic Belt ("Wearers are requested not to stand in puddles"), the Anti-Fat ("Twelve ladies of Brooklyn have vanished entirely") and Kline's Electric Liver Pad.
  - Dr. St. John's Consumption Cure, the arsenical complexion wafers ("the envy of all who attended the viewing"), and Dilute Sulphuric Acid "that tells the customer the microbes are being killed" (Radam's Microbe Killer).
- **The satire aims up.** The jokes land on the proprietor, the druggists, the clergymen-for-hire, the diploma mill, the aldermen ("referred to the dinner"), the coroner's certificate and the undertakers. The customers are named, given a trade and a complaint, and treated with sympathy: "She came out with a bottle and a look of hope." The pauper's death is the strongest line in game 3: "the proprietor of the remedy has not come forward either."
- **Death at the counter.** The skeleton standing beside a named regular, with the tag sliding from "peaky · delighted" to "poorly · asks for it by name" to "gravely ill", is the clearest moral feedback I've seen in a game like this, and it got me to reformulate without any tutorial.
- **"Reformulated. The next batch will be the new recipe; the customers will not be told."** One line that carries the whole trade.
- **The proof sheet as the desk:** proofreader's "dele (strike out)" on last month's names, "set after press", and the ad preview updating as claims are set.
- **THE TRADE column**, which explains the economy honestly: "Callers at the counter: thirteen from the ward, nineteen by the Gazette, ten by the almanac", "TRADE TALK.—The proprietor is said to be halfway to the cobbler's shop…".
- **The Almanac** venture is exactly right for the period (Ayer's and Hostetter's almanacs) and its notice is funny: "The pickles are said to be reliable."

## Disclosures

- The machine restarted during my 11th press in game 1. Per the coordinator, I continued with two jump-ahead games, which a real player would not have: `--play 24 --dose 0.5` and `--play 40 --dose 1.5`, both seed 1885. Neither reached a venture beyond the almanac.
- I used `targets` to find button coordinates and `read` to quote text exactly and check repetition across editions. All judgements of look and layout are from screenshots.
- I used `state` once in each jumped game to see cash and regulars after the jump.
- I did not read the source, the docs or other playtests.
- Historical claims above carry my confidence. I'm sure of the Saturday dates, the volume numbering, the Sixth Ward, the Els and the Pinkham parallel. The Thalia, Pedestal Fund and diploma-mill points are about 80–85%; the baseball point is about 50%.

## First hour

- **0–3 min.** First edition. Gorgeous, and I read it as a newspaper, not a menu: "AMONG THE CALLERS.—Mrs. Sarah Abrams, wife of a working man, troubled by a teething infant…". I took the four callers' complaints as the game's brief and the market prices as a hint at the drug shelf. I clicked the one clickable clipping ("The proprietor's books…") and got a Ledger card with a skeleton clerk. I wasn't sure there was more to click.
- **3–8 min.** TO THE PROOF. At first I misread the proof as *the next paper I'd be writing*, and wondered why last month's customers were struck out ("dele"). It soon read as a compositor's proof. The right-hand hints walked me into the FORMULA book. A starter recipe was pre-poured and the Assay bars were legible at once ("Danger: a risk to the frail", "In an assay: any chemist would find it"). I added laudanum because that is what 1885 would do, and named it.
- **8–12 min.** THE STILL and its "ORDER ENOUGH FOR 24" button were easy. I was puzzled that pump water must be ordered. The TYPE CASE was clear: "Bolder type sells more, and the coroner reads it too." That one line explained the risk model to me. Ad space was locked: "the Gazette wants $100 in savings and a printed testimonial first". I had $150, so "savings" confused me for a moment.
- **12–15 min.** First press. "THE PRESSES ARE RUNNING · 908 COPIES", then "NEW FACES AT THE COUNTER". **This is when it clicked**: my choices come back as news, and the Personal column is my customer list.
- **15–25 min.** Letters. Printing the testimonial unlocked ad space. "Something Stronger" put the moral question in front of me in month 2. I chose "Recommend rest and fresh air. *Good advice, and nothing changes.*" and admired the bluntness. Bought ½ col; the ad preview on the proof is lovely.
- **25–35 min.** Almanac, diploma and price changes. I began skimming BRIEFLY because I'd read it before. The Death figure appeared beside Mr. Greco in May, and five months later he died, which made the hint feel earned.
- **35–40 min.** Death beside Mrs. Abrams; I reformulated. By now I fully understood the loop and was enjoying the paper. I also saw that I was months or years away from the branch shop, and wondered what the long game held for me.

## Score

**7 / 10.** For a reader of this period, the writing, the dating and the aim of the satire are unusually good, and the Death-at-the-counter mechanic does real moral work. Repetition in the paper and the slow road to ventures blunt the second half of the first hour. **What would move it up one point:** stop the standing matter rerunning (no item twice within about 6 editions) and bring one real venture (wholesale or branch) within reach in the first year of decent play. Making the teething-syrup consequence fall on the infant would lift it further for a reader like me.
