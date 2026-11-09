# Panacea · events pass 2 · the everyday letters

Content 0.8.0 → **0.9.0**. Both sims changed together (the TypeScript reference and the C# port); the parity
fixtures were regenerated (32 runs) and `dotnet test` is green (110). TS 132/132, typecheck, lint, prettier,
`content:lint` (0 errors, 0 warnings), the Godot build and `check_cuts.py` pass.

Pass 1 ([events-pass-1.md](events-pass-1.md)) reached 0.57 letters a month for an honest shop and could not get the
1.1 oz laudanum line back under 1.15× adaptive. This pass does both.

## What changed, in short

- **79 new letters** (§1): 27 of the shop's ordinary life and 19 of the seasons (both any decade, coming back every
  four or five years), 26 of their decade (3 early, 16 of the 1890s, 7 of 1900–06), and 7 in chains: the clerk
  (3), the lodge (3), and the apprentice who passes the College (extending pass 1's `apprentice`). 196 replies, each
  with its `Risks` line.
- **A season condition** (§2): `{ "type": "season", "months": [12, 1] }`, so Christmas comes in December, Lent in
  February and moving day on the first of May.
- **Frequency** (§3): from 0.57 to **1.14** letters a month for an honest shop (1.29 for a dark one), **140
  different letters** in a run instead of 70, repeats still years apart (median gap 67 months).
- **The ward's trust** (§4): callers sent by a letter, and the standing a reply earns, are scaled by the ward's
  memory of harm and by suspicion. The 1.1 oz laudanum line goes from **1.25× to 1.12×** adaptive.
- **Balance** (§5) keeps its shape: adaptive $21.1k → $20.9k, shopkeeper $15.8k → $15.7k; heavy and greedy still
  gamble. The exploit hunter's best line is lower than before on both searches (1.28× → 1.24×; on a fresh seed,
  1.46× → 1.41×, a pre-existing honest bold-claims line).

## 1. The new letters

The rules are pass 1's. Every letter has a correspondent with a name and a reason to write, 2–5 sentences in their
own register (`event.<id>.body`; LettersDesk `Voice` empty), and one-line replies written as what the proprietor
does. The jokes are on the trade and on whoever is selling something: the coal roads that "have not consulted us,
or you", the Street Cleaning Department that "has not yet cleared the streets", the Board of Rapid Transit
Commissioners "which has no funds for the purpose", the Leader's Christmas list "also kept", the bureau of awards
whose medals "are not to be confused with the Exposition's own", the Estate that "keeps a lawyer for the purpose",
the printer's compositor who set CURSES for CURES. The sufferers (a laid-off man's wife, a bedridden father and his
nurse, a mother whose child had croup at 2 a.m., the tenants burned out next door) write plainly and are never the
joke.

Sensitivity (§11): no dialect (the pencilled notes are terse, not phonetic); immigrant correspondents are
tradesmen and committee secretaries with business of their own (a rag-and-bottle dealer, the Columbian committee of
the Mulberry district, a stamp company's agent, a picture-show manager); deaths stay off the page (the fire next
door: "every soul got down the fire escape"; the croup: "She is better now"; the Board's summer circular gives
infant deaths as a figure); no real trademarks (the Paris perfume and the arsenic wafers are unnamed); no real
person writes. Real people appear only as era context: Mr. Bryan (the gold clause), President Hayes (when the
shopfront was last painted).

Stakes are small: cents to a few dollars, standing ±1, suspicion +1 to +4, the ward's memory ±1 or 2. Promotional
replies cost money for a few callers (the fans, the calendar, the phonograph parlour, trading stamps), never
callers for nothing. The first draft gave standing +1 for many cheap kindnesses (a cold cream at cost, a bottle on
the house); in twelve letters those no longer raise standing (most soften the ward's memory instead), because
standing turned out to be what a strong bottle turns into habits (§4).

"When" is the window the letter can come in (a season is the months of the year); "Draw" is its monthly chance once
it can, and whether it comes once or may come back after a cooldown. Replies are in file order; the default, sent
if the month ends unanswered, is _in italics_.

### The chains (7)

| Letter            | Theme                           | When                        | Draw                   | What it is                                                                           | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                                              |
| ----------------- | ------------------------------- | --------------------------- | ---------------------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `clerk_applies`   | the clerk (chain)               | 1889-01 – 1904-12           | once, 0.05/mo          | A business-college graduate asks to keep the books, for $3 a month and a stool.      | Engage her, stool and all. / _Write that the back room is no place for a young lady._                                                                                               |
| `clerk_books`     | the clerk (chain)               | after clerk_engaged         | once, 0.1/mo           | Miss Pruitt finds Hobart's double charges, and the Alderman's cousin's debt.         | Have her write to Mr. Dunphy, in her best hand. / _Let Mr. Dunphy be._                                                                                                              |
| `clerk_leaves`    | the clerk (chain)               | after clerk_books_done      | once, 0.04/mo          | Miss Pruitt marries, and recommends her cousin.                                      | Write her a handsome character, send a wedding present, and engage Miss Agnes. / _Write her a handsome character, and send a wedding present._                                      |
| `lodge_invite`    | the lodge (chain)               | from 1886-01                | every 60+ mo, 0.035/mo | The Golden Lamp proposes you for membership: $5, dues, and the brothers' custom.     | Be balloted for, and buy the regalia. / _Decline: you are not a joiner._                                                                                                            |
| `lodge_brother`   | the lodge (chain)               | after lodge_member          | every 48+ mo, 0.06/mo  | A brother out of work, and the obligation of the fourth degree.                      | Give Mrs. Tierney what she needs, on the slate, until he is working. / Give him one bottle of the liniment, and the grip. / _Tell him the fourth degree did not mention the slate._ |
| `lodge_supper`    | the lodge (chain)               | after lodge_member, Jan–Feb | every 54+ mo, 0.2/mo   | The lodge supper: a toast to answer, and the punch (not medicinal).                  | Make the punch, and the speech. / Buy a ticket, and leave the punch to the caterer. / _Send regrets, and a dollar to the band._                                                     |
| `apprentice_exam` | the apprentice (extends pass 1) | after apprentice_served     | every 24+ mo, 0.05/mo  | The apprentice passes the College, and would stay at $6 with his name on the window. | Keep him at six, and put his name on the window. / _Give him a dinner and a good character, and let him go._                                                                        |

### The seasons (any decade) (19)

| Letter              | Theme                 | When                       | Draw                  | What it is                                                                           | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                                           |
| ------------------- | --------------------- | -------------------------- | --------------------- | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `carrier_address`   | New Year              | from 1885-12, Dec–Jan      | every 54+ mo, 0.2/mo  | The Gazette's carrier sends his verses, and expects a tip.                           | Give him a dollar, and pin the verses in the window. / Give him a quarter. / _Tell him you pay the Gazette already._                                                             |
| `holiday_goods`     | Christmas             | from 1886-10, Oct–Nov      | every 54+ mo, 0.14/mo | Hobart's offer fancy goods for the window, and Kiss of the Riviera.                  | Order the case for the window. / _Decline: the shop sells medicine, not the Riviera._                                                                                            |
| `ward_turkeys`      | Christmas             | 1885-11 – 1905-12, Nov–Dec | every 54+ mo, 0.2/mo  | The district leader's Christmas dinner: the subscribers' list is read out, and kept. | Subscribe five dollars, and be read out. / Send a case of cough syrup for the families. / _Give to the parish instead._                                                          |
| `lent_pledge`       | Lent                  | from 1886-02, Feb–Mar      | every 54+ mo, 0.2/mo  | A customer off the drink for Lent asks whether the tonic counts.                     | Write that it is a medicine, and sign it. / Write that it counts, and sign it. / _Keep out of it._                                                                               |
| `easter_complexion` | Easter                | from 1885-03, Mar–Apr      | every 54+ mo, 0.2/mo  | A box-factory girl wants arsenic wafers before Easter Sunday.                        | Mix her a cold cream at cost, and write that the freckle is becoming. / Sell her the arsenic wafers. / _Write that lemon juice is cheaper, and the young man will not mind._     |
| `moving_day`        | moving day            | from 1885-04, Apr          | every 54+ mo, 0.2/mo  | A family moving to Harlem on May 1st asks for the tonic by express.                  | Send it up by the express each month. / _Write her the name of a good druggist in Harlem._                                                                                       |
| `moving_debts`      | moving day            | from 1885-05, May          | every 54+ mo, 0.16/mo | A collection agency offers to find the debtors May 1st took away.                    | Hand them the book. / _Write off the eleven dollars._                                                                                                                            |
| `summer_fans`       | summer                | from 1886-05, May–Jun      | every 54+ mo, 0.14/mo | Palm-leaf fans with your name, for the churches in August.                           | Order a gross of fans. / _Decline: the ladies can fan themselves with the undertaker._                                                                                           |
| `summer_syrups`     | the heat              | 1886-06 – 1897-12, Jun–Jul | every 54+ mo, 0.14/mo | The Board's summer circular on infant deaths and soothing syrups.                    | Put the syrups behind the counter until September. / _Leave them where the mothers can reach them._                                                                              |
| `fourth_of_july`    | the Fourth            | from 1885-06, Jun          | every 54+ mo, 0.2/mo  | The street committee asks for the fireworks, and for the door open that night.       | Give two dollars, for the band and the rockets. / Keep the door open on the night, with lint and carron oil. / _Put up the shutters early, as usual._                            |
| `excursion`         | the church            | from 1885-06, Jun–Jul      | every 54+ mo, 0.14/mo | St. Ursula's Sunday-school excursion up the Hudson.                                  | Send the court-plaster, the stomach drops, and a dollar for the lemonade. / Send a dozen of the tonic for the teachers, with your card for the programme. / _Send your regrets._ |
| `snow_sidewalk`     | the weather           | from 1885-12, Dec–Feb      | every 54+ mo, 0.12/mo | A lady fell on your unswept sidewalk; the Department hasn't swept the streets.       | Pay a boy to keep it clear until March. / Clear it yourself, and send the lady a bottle of liniment with your apologies. / _Leave it to the thaw._                               |
| `coal_dealer`       | the weather           | from 1885-10, Oct–Nov      | every 54+ mo, 0.14/mo | The coal roads raise the price: buy now, or by the bucket.                           | Take the winter's coal now, for cash. / _Buy it by the bucket, as the weather goes._                                                                                             |
| `calendar_printer`  | the new year          | from 1886-09, Sep–Nov      | every 54+ mo, 0.1/mo  | Calendars with a little girl and a kitten (the same as the undertaker's).            | Order five hundred, without the moon. / _Decline: the kitchen has the undertaker's._                                                                                             |
| `cloudburst`        | the weather           | from 1885-07, Jul–Aug      | every 60+ mo, 0.08/mo | A summer storm backs the sewer into the cellar.                                      | Have him pump it out and fit the trap. / _Wait for the Department._                                                                                                              |
| `school_prize`      | the school            | from 1887-06, Jun          | every 54+ mo, 0.2/mo  | A prize for Physiology and Hygiene, which includes the effects of alcohol.           | Give a dictionary for Physiology and Hygiene. / Offer a prize for penmanship instead. / _Send your regrets._                                                                     |
| `choir_lozenges`    | the church            | from 1885-11, Nov–Dec      | every 54+ mo, 0.12/mo | The tenors are hoarse before the Messiah.                                            | Send a box of plain horehound lozenges, with your compliments. / Send your strongest throat pastilles. / _Send your regrets: the tenors will have to manage._                    |
| `target_excursion`  | the street (1885–95)  | 1885-07 – 1895-12, Jul–Sep | every 54+ mo, 0.1/mo  | The Flynn Guard's target excursion: prizes, or the windows.                          | Give a silver cup, four dollars, engraved. / Give a case of the tonic as a prize. / _Put up the shutters on the fourteenth._                                                     |
| `labor_day`         | Labor Day (from 1887) | from 1887-08, Aug          | every 54+ mo, 0.1/mo  | The trades ask the shops to close for the parade.                                    | Put up the card and close for the day. / _Stay open, and sell lemonade to the parade._                                                                                           |

### The shop's ordinary life (any decade) (27)

| Letter           | Theme                | When         | Draw                   | What it is                                                                            | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                                                          |
| ---------------- | -------------------- | ------------ | ---------------------- | ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `slate_credit`   | credit               | from 1885-04 | every 54+ mo, 0.035/mo | A laid-off man's wife asks for the syrup on the slate until March.                    | Put it all on the slate until March. / The syrup on the slate, the liniment for cash. / _Keep to cash, with regret._                                                                            |
| `wrong_bottle`   | a small accident     | from 1885-06 | every 54+ mo, 0.035/mo | The boy delivered the liniment for the cough syrup.                                   | Send the syrup, an apology, and a pound of tea. / Send the syrup, and have the liniment labels printed POISON in red. / _Send the syrup._                                                       |
| `horse_order`    | an odd order         | from 1885-06 | every 60+ mo, 0.035/mo | A gallon of tonic for the funeral-coach mare.                                         | Sell him the gallon. / _Write that the shop does not treat horses, and name a veterinary._                                                                                                      |
| `thanks_note`    | thanks               | from 1885-04 | every 54+ mo, 0.035/mo | A mother sends a pen-wiper, and her sister.                                           | Send her sister a sample bottle, with your card. / _Send your thanks, and put the pen-wiper on the desk._                                                                                       |
| `window_ball`    | children             | from 1885-04 | every 60+ mo, 0.035/mo | A baseball through the window; the father offers a nickel a week.                     | Take the nickels, and give Michael the ball. / _Send Mr. Cullen the glazier's bill, and keep the ball._                                                                                         |
| `bad_note`       | the bank             | from 1885-04 | every 60+ mo, 0.035/mo | The bank returns a counterfeit note from your deposit.                                | Give the Secret Service man a description of the customer. / _Stand the loss, and look harder at notes._                                                                                        |
| `bottle_boys`    | the street           | from 1886-01 | every 60+ mo, 0.035/mo | A rag-and-bottle dealer sells your empties back (another druggist wants them).        | _Buy them back, washed._ / Buy them back unwashed, and rinse them yourself.                                                                                                                     |
| `ship_chest`     | an odd order         | from 1885-06 | every 60+ mo, 0.035/mo | A barque's master wants the medicine chest filled with your tonic.                    | Fill it with two dozen of the tonic, and a book of directions in your own hand. / _Fill it from the Dispensatory, at the Dispensatory's prices._                                                |
| `theatre_order`  | an odd order         | from 1886-01 | every 60+ mo, 0.035/mo | A Bowery melodrama wants a poison the actress can drink eight times a week.           | Send coloured water in a blue POISON bottle, and take the passes. / Send your tonic in both bottles, with the label to the gallery. / _Decline: the shop is not in the theatrical line._        |
| `history_book`   | a small swindle      | from 1885-06 | every 60+ mo, 0.035/mo | A subscription history of Leading Men: a sketch for $3, an engraving for $8.          | Subscribe for the engraving. / Subscribe for the sketch, and supply the particulars generously. / _Decline to be a leading man._                                                                |
| `fire_insurance` | the insurer          | from 1885-06 | every 60+ mo, 0.035/mo | The insurer finds a still four feet from the spirit barrel.                           | Pay the double premium. / Move the barrel, and ask the surveyor back with a cigar. / _Let the policy lapse._                                                                                    |
| `charity_appeal` | a small swindle      | from 1885-06 | every 60+ mo, 0.035/mo | An appeal for seamen's orphans from a Reverend known to the Tombs.                    | Send a dollar all the same: it reads beautifully. / Send the appeal to the sergeant. / _Put it in the stove._                                                                                   |
| `doctor_counter` | a doctor             | from 1886-01 | every 60+ mo, 0.035/mo | A dispensary doctor complains of counter-prescribing.                                 | Promise to send him the cases you do not understand. / Reply that your customers cannot afford his profession. / _Let him write to the Board._                                                  |
| `tonic_taste`    | a complaint          | from 1885-09 | every 54+ mo, 0.035/mo | A regular says the tonic never tastes the same twice.                                 | Send him a bottle from a good batch, on the house. / Taste every batch before it is bottled, for three months, and pay the boy to mind the counter. / _Say nothing: a man's taste changes too._ |
| `child_errand`   | children             | from 1886-01 | every 60+ mo, 0.035/mo | A girl of eight is sent with a jug for "the strong tonic".                            | Fill the jug, and put it on the slate. / _Send the girl home with the jug empty, and a peppermint._                                                                                             |
| `carboy`         | a tradesman          | from 1885-06 | every 60+ mo, 0.035/mo | The truckman dropped your carboy; whose was it in the street?                         | Have a lawyer write to both of them. / _Pay for the carboy yourself, and change truckmen._                                                                                                      |
| `label_printer`  | the printer          | from 1885-06 | every 60+ mo, 0.035/mo | The new compositor set CURSES for CURES.                                              | Take the reprint, and have the boy soak off the five hundred. / _Take the reprint, and sell the five hundred as they are._                                                                      |
| `jobber_rival`   | the supplier         | from 1886-01 | every 60+ mo, 0.035/mo | Another wholesale house undercuts Hobart's, on a trial order.                         | Send the trial order. / _Stay with Hobart's._                                                                                                                                                   |
| `neighbour_fire` | the neighbours       | from 1885-06 | every 60+ mo, 0.035/mo | The front house next door burned (everyone got out); the tenants need syrup and lint. | Send the syrup and lint, and something for the coughs, on the house. / Send them at cost. / _Send your sympathy._                                                                               |
| `agent_paint`    | the landlord's agent | from 1888-01 | every 60+ mo, 0.035/mo | Clause nine: the shopfront must be painted, in the Estate's green.                    | Have it done in the Estate's green. / Have the sign painter do it in your own colours, for six. / _Ignore it: the Estate's lawyer is busy._                                                     |
| `agent_noxious`  | the landlord's agent | from 1890-01 | every 60+ mo, 0.035/mo | Clause four forbids noxious trades; the agent would like a bottle a quarter.          | Say so in writing, and send the agent his quarterly bottle. / Write that clause four does not mention druggists, and let the Estate sue. / _Ignore it._                                         |
| `boy_measles`    | the boy              | from 1886-01 | every 60+ mo, 0.035/mo | Tommy Burke has the measles; his brother Joseph cannot read the labels.               | Keep Tommy's wages going, and pay Joseph too. / Take Joseph, at Joseph's wages. / _Do without a boy for three weeks._                                                                           |
| `wedding_punch`  | an odd order         | from 1885-06 | every 60+ mo, 0.035/mo | Three gallons of "medicinal" spirit for a Sunday wedding.                             | Sell him three gallons of Wedding Cordial, medicinal. / Send a bottle of good port as a wedding present, and nothing for sale. / _Decline, and wish the couple well._                           |
| `perfume_copy`   | an odd order         | from 1888-01 | every 60+ mo, 0.035/mo | A milliner wants an imitation of the Paris perfume.                                   | Make her a gallon of Essence of Paris, from bergamot and spirit. / _Make it, but label it plainly: Mulberry Street Violet._                                                                     |
| `sealer`         | the city             | from 1885-06 | every 60+ mo, 0.035/mo | The sealer finds the scale an ounce in the pound in favour of the house.              | Have the scale adjusted and sealed. / Give the deputy a quarter to look again. / _Write that the scale was your predecessor's._                                                                 |
| `night_bell`     | the neighbours       | from 1885-06 | every 60+ mo, 0.035/mo | A mother whose child had croup at 2 a.m. asks for a night bell.                       | Put in a night bell, and sleep in the back room. / _Write that the shop keeps hours, and name the Grand Street man._                                                                            |
| `jury_duty`      | the city             | from 1885-06 | every 60+ mo, 0.035/mo | A jury summons for two weeks; the clerk is sympathetic, at a dollar.                  | See the Commissioner's clerk. / _Serve your two weeks, and let the boy mind the counter._                                                                                                       |

### The early shop (1885–1892) (3)

| Letter            | Theme                 | When              | Draw                   | What it is                                                                  | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                             |
| ----------------- | --------------------- | ----------------- | ---------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `pedestal_fund`   | the Statue of Liberty | 1885-06 – 1885-08 | once, 0.5/mo           | Summer 1885: a box on the counter for the pedestal fund.                    | Keep the box on the counter, and put in a dollar. / Keep the box on the counter, and put in the dime. / _Decline: the French gave it, the French can stand it up._ |
| `centennial_1889` | the Centennial        | 1889-03 – 1889-04 | once, 0.5/mo           | April 1889: the ward committee sells bunting for the Washington Centennial. | Buy twenty yards and dress the shop. / Put a portrait of Washington in the window, with a bottle at his elbow. / _Leave the shop as it is._                        |
| `trade_cards`     | advertising           | 1885-03 – 1892-12 | every 54+ mo, 0.035/mo | Chromo trade cards for the children's albums.                               | Order a thousand cards. / _Decline: you have seen the albums._                                                                                                     |

### The 1890s (16)

| Letter               | Theme                     | When              | Draw                   | What it is                                                                     | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                               |
| -------------------- | ------------------------- | ----------------- | ---------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `census_1890`        | the census                | 1890-06 – 1890-07 | once, 0.5/mo           | June 1890: the schedule of manufactures; what kind of product?                 | Return it truthfully: patent medicines, so many gallons of spirit. / _Return yourself as a manufacturing chemist, with modest figures._              |
| `columbus_1892`      | the Columbian celebration | 1892-09 – 1892-10 | once, 0.5/mo           | October 1892: the district marches to the Circle; lanterns at ten cents.       | Light the window with the Committee's lanterns. / Shut the shop and march with the druggists. / _Light the window with your own lamp._               |
| `fair_medal_1893`    | the World's Fair          | 1893-02 – 1893-10 | once, 0.1/mo           | A bureau sells a Chicago "gold medal" for $8.                                  | Send the eight dollars: GOLD MEDAL, CHICAGO 1893. / _Decline: you were not in Chicago, and neither was your remedy._                                 |
| `phonograph`         | the phonograph parlour    | 1891-01 – 1899-12 | every 54+ mo, 0.035/mo | An elocutionist speaks your advertisement between the songs.                   | Have the elocutionist speak for you, for six months. / _Decline: the shop speaks for itself._                                                        |
| `settlement_nurse`   | the visiting nurse        | from 1893-09      | every 54+ mo, 0.035/mo | A visiting nurse asks what is in the bottle a bedridden father lives on.       | Tell her exactly what is in it, and how much. / Sell him only the small bottle, and tell the nurse so. / _Write that the receipt is a trade secret._ |
| `antitoxin_1895`     | diphtheria antitoxin      | 1895-01 – 1899-12 | once, 0.08/mo          | The Board makes antitoxin, and asks for the "throat cures" out of the windows. | Take the throat cure out of the window, and send the cases to the doctors. / _Keep the card in the window: SURE CURE FOR SORE THROAT._               |
| `white_wings`        | street cleaning           | 1895-06 – 1898-12 | once, 0.08/mo          | The Department's new rules: ashes, garbage and rubbish apart.                  | Buy two more barrels, and sort it. / Give the cartman a quarter to take it as it is. / _Leave it for the rats, as before._                           |
| `bicycle_boy`        | the bicycle               | 1895-04 – 1901-12 | every 54+ mo, 0.035/mo | The errand boy wants a wheel: $8 from the store, $5 from Canal Street.         | Buy the wheel from the store. / Buy the wheel on Canal Street. / _Tell him to walk faster._                                                          |
| `x_rays_1896`        | the X rays                | 1896-03 – 1897-12 | once, 0.1/mo           | An "electrician" will show customers their bones from your window.             | Let him set up in the window. / _Decline: the ward can keep its bones to itself._                                                                    |
| `gold_clause_1896`   | the 1896 election         | 1896-08 – 1896-10 | once, 0.4/mo           | Hobart's want accounts payable in gold, against the risk of Mr. Bryan.         | Sign the gold agreement. / Refuse, and put a silver card in the window. / _Put it with the bills._                                                   |
| `raines_law`         | the Raines law            | 1896-06 – 1900-12 | every 60+ mo, 0.035/mo | The saloon next door is now a hotel, and would sell your tonic on Sundays.     | Agree: half of every nickel. / _Decline: the tonic is not a beverage, whatever the Collector says._                                                  |
| `school_inspector`   | school inspection         | from 1897-06      | every 54+ mo, 0.035/mo | The medical inspector's lice lotion costs 4c; will the mothers pay 40?         | Make the Department's lotion, and sell it at cost. / Sell your own Nursery Lotion at forty cents. / _Make the lotion, at the usual profit._          |
| `consolidation_1898` | the Greater City          | 1897-12 – 1898-03 | once, 0.35/mo          | January 1898: every licence renewed on the new city's forms.                   | Fill in the forms and pay the dollar. / Send it through the district leader's office, for two. / _Wait: the new city cannot find its own forms._     |
| `war_stamps_1898`    | the war tax               | 1898-06 – 1898-08 | once, 0.5/mo           | July 1898: every proprietary bottle must bear a revenue stamp.                 | Buy the stamps, and stamp every bottle. / Buy one sheet, and steam the stamps off the empties. / _Sell without stamps, and wait for the Deputy._     |
| `telephone`          | the telephone             | from 1896-01      | every 54+ mo, 0.035/mo | A telephone on the counter, for physicians' prescriptions.                     | Have the instrument installed. / _Decline: the physicians can walk._                                                                                 |
| `liquid_air`         | a promotion               | 1899-04 – 1901-06 | once, 0.08/mo          | A liquid-air company wants a druggist to say it cures consumption.             | Write the letter, for the three dollars. / _Decline: you have your own preparation that cures consumption._                                          |

### 1900–1906 (7)

| Letter           | Theme              | When              | Draw                   | What it is                                                           | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                          |
| ---------------- | ------------------ | ----------------- | ---------------------- | -------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `subway_digging` | the subway works   | 1900-06 – 1903-12 | once, 0.1/mo           | The rapid transit works open Elm Street; a plank bridge for $2.      | Pay for the plank bridge. / _Apply to the Commissioners._                                                                                       |
| `smallpox_1901`  | smallpox           | 1901-12 – 1902-03 | once, 0.4/mo           | Winter 1901–02: free vaccination, and the "preventive" by the spoon. | Post the notice in the window. / Sell the preventive, by the spoon or the bottle. / _Do neither._                                               |
| `trading_stamps` | trading stamps     | from 1901-01      | every 54+ mo, 0.035/mo | Stamps with every dime, for a Morris chair.                          | Give stamps, for a year. / _Decline: your customers want the tonic, not the chair._                                                             |
| `motorist`       | the motor-car      | from 1902-04      | every 54+ mo, 0.035/mo | A stranded motorist buys benzine at a dollar a gallon.               | Sell him the benzine. / Sell him the benzine, and a bottle of tonic for the road. / _Tell him the benzine is for gloves, and send for a horse._ |
| `postcards`      | picture post cards | from 1903-01      | every 54+ mo, 0.035/mo | A thousand post cards of the shop, with you in the door.             | Order a thousand cards, and stand in the door. / _Decline: the relations in Ohio buy by post already._                                          |
| `electric_sign`  | the electric sign  | from 1902-06      | every 54+ mo, 0.035/mo | A sign of sixty lamps that spells your remedy.                       | Put up the sign. / _Decline: the shop is not on Broadway._                                                                                      |
| `nickelodeon`    | the picture show   | from 1906-01      | once, 0.12/mo          | A slide between the reels, and an audience with headaches.           | Take a slide, with the headache powder on it. / _Decline: the street has headaches enough._                                                     |

## 2. Chains, and the season condition

Most of the new letters stand alone. Three short chains, each a letter that stands alone too:

1. **The clerk (1889–1904).** `clerk_applies`: Miss Pruitt, from a business college on Fourteenth Street, keeps
   books by double entry and "should like to judge for myself" whether a back room is a place for a young lady
   ($3 a month; every ingredient 5% cheaper while she checks Pearl street's bills) → `clerk_books`: Hobart's
   charged the same carboy three times, and Mr. Dunphy, the Alderman's cousin, owes four dollars → `clerk_leaves`:
   she marries Mr. Hutchins of the insurance office, "who says a married woman does not keep books, though his
   mother keeps his", and recommends her cousin.
2. **The lodge.** `lodge_invite`: the Independent Order of the Golden Lamp balloted on Wednesday (a doctor in
   sickness, a band at your funeral, and the custom of 140 brothers) → while a member, `lodge_brother` (a brother
   out of work, and the obligation of the fourth degree) and `lodge_supper` (January–February: answer the toast to
   'Our Professional Brethren' and supply the punch, which the committee would not wish to be medicinal).
3. **The apprentice passes.** Pass 1's `apprentice.take_him_on` now sets `apprentice_served`; `apprentice_exam`:
   W. Escher, Ph.G., would stay at $6 a month with his name on the window. Tommy Burke, pass 1's errand boy, comes
   back too (`boy_measles`, `bicycle_boy`), as do the Estate's landlord Mr. O'Rourke, Hobart's, the Board's
   Mr. Kemble, the grammar school's principal and St. Ursula's.

**`season` (new requirement type).** `{ "type": "season", "months": [12, 1] }` holds when the month of the year is
one of these. Both sims (`requirements.ts`, C# `SeasonRequirement` in `Requirements.Evaluate`/`Progress`, its JSON
reader and writer); the linter refuses it in an unlock's or a venture's requirement, where a condition that comes
round every year would only hold back progress. Nineteen letters use it: New Year (the carrier's address), Lent,
Easter, moving day (April for the May 1st letters), the Fourth, the Sunday-school excursion, summer fans, the
Board's summer circular, a cloudburst, the coal dealer before winter, calendars and Christmas goods in the autumn,
the Leader's Christmas dinner, the choir's Messiah, snow on the sidewalk, Labor Day, the target excursion.

## 3. Frequency and repeats

Measured as in pass 1 (40 runs a bot, seeds `ev1m-*`; random letters drawn, the coroner's notices not counted).
Before is pass 1's final measurement.

| Bot        | Letters a month, before → after | Months with no letter | Different letters a run, before → after | Repeat gap, median (p10) | Repeats within 3 years a run, before → after |
| ---------- | ------------------------------- | --------------------- | --------------------------------------- | ------------------------ | -------------------------------------------- |
| adaptive   | 0.57 → **1.14**                 | 55% → 25%             | 70 of 92 → **140 of 171**               | 56 (42) → **67 (49)**    | 3 → **2**                                    |
| shopkeeper | 0.57 → **1.13**                 | 55% → 26%             | 71 → 139                                | 57 (43) → 67 (49)        | 3 → 2                                        |
| sloppy     | 0.59 → **1.16**                 | 54% → 25%             | 74 → 144                                | 56 (37) → 67 (48)        | 6 → 5                                        |
| greedy     | 0.73 → **1.29**                 | 45% → 20%             | 72 → 145                                | 49 (8) → 61 (12)         | 36 → 30                                      |
| heavy      | 0.75 → **1.29**                 | 45% → 20%             | 62 → 116                                | 49 (9) → 61 (12)         | 31 → 25                                      |

By decade (adaptive, median a run): 1885–89 **71 letters, 70 different** (1.18 a month); 1890–99 **139, 100
different** (1.16); 1900–06 **91, 79 different** (1.08). Before: 38/35, 70/44, 45/34. The new letters are about
157 of an honest shop's 302 a run; the old ones come about as often as before (150 → 145).

How it was tuned: the first draft (any-decade letters 0.05 a month after a 48-month cooldown, seasonal letters 0.3
a month in season after 36) gave 1.28 a month for an honest shop, with the seasonal letters five times a run. Now
any-decade letters are drawn at 0.035 a month after **54** months, seasonal ones at 0.2 a month in season (some at
0.08–0.16) after **54**; the decade letters at 0.035 (recurring) or 0.08–0.5 (once, in a short window).
`maxEventsPerMonth` stays at 2. The remaining repeats within three years are pass 1's deliberate ones
(`customer_wants_stronger`, and for dark shops `temperance_picket` and `board_of_health`).

Chain reach (letters a run): default-takers see `clerk_applies` and `lodge_invite`; the greedy bot (first reply)
gets the clerk's whole story (0.95 / 0.85), the lodge's brother and supper (2.5 / 2.0) and the apprentice's
diploma (2.5); the sloppy bot reaches each in some runs.

## 4. Callers, standing and the laudanum line

Pass 1 left one line above the 1.15× limit: candidate 72 of the `ev1` search (1.1 oz laudanum escalating, cuts at
heat 52, 155¢, four bold claims, silent at inquests, every venture, always the first reply), at 1.25× adaptive.

**What the numbers showed.** The ward's memory alone does nothing for this line: it averages 1.5 (p90 3.9), because
its deaths are few and mostly look natural. Where its letter callers arrive, though, suspicion is 60, against 7
for the adaptive shop. And the first draft of the new letters pushed it to **1.47×**: stripping the new letters'
standing (`STRIP=rep`) brought it back to 1.17×. Standing raises how many strangers walk in and how many become
regulars, and a strong bottle turns regulars into habits.

**The fix (both sims, identically).** One notion, the ward's trust in a letter's word:

```
letterTrust = (1 − ward.letterCallerLoss × memory/100) × max(0, 1 − scrutiny.letterCallerLoss × heat/shutdownHeat)
```

with `ward.letterCallerLoss` 1 and `scrutiny.letterCallerLoss` **1.5** (new config, `config.json`): nobody vouches
for a shop two-thirds of the way to being shut.

- **Callers sent by a letter** (`callers_<source>`) are `floor(n × letterTrust + 0.5)` a month (TS `market.ts`
  `letterCallers` / `letterTrust`, C# `Market.LetterCallers` / `Market.LetterTrust`). The rector, the club captain,
  the doctor send fewer people to a shop the ward remembers harm of or the Board suspects.
- **Standing a reply earns** is credited at `delta × letterTrust` (TS `progression.ts` `creditedEffects`, C#
  `Progression.CreditedEffects`); standing lost is lost in full. Only random letters; the coroner's notice is not a
  letter of the ward's.
- **Content**: in twelve of the new letters a cheap kindness no longer raises standing (most soften the ward's
  memory instead).
- **The game**: when suspicion is 14 or more, a letter whose replies promise standing or callers carries a note in
  the proprietor's hand ("with the shop suspected as it is, the ward credits at most seven-tenths of the standing a
  reply says, and at most seven-tenths as many come on a letter's word"), so the risk lines stay true. Rendered and
  checked by eye (May 1888, suspicion 17, `customer_wants_stronger`).

Tests: TS `letters.test.ts` and C# `LettersTests.cs` each gain "sends fewer of a letter's callers when the ward
remembers harm or the Board suspects" (memory 50 → half; with suspicion 20, 0.35: 6/4/1 callers → 2/1/0; memory 100
or suspicion 70 → none), "credits the standing a reply earns as far as the ward trusts the shop, and takes it in
full", and "brings a seasonal letter only in its months". The existing callers test sets suspicion 0.

**Candidate 72, step by step** (40 seeds, `top-0…39`, against adaptive on the same seeds):

| Content and sims                                                                | Candidate 72 |
| ------------------------------------------------------------------------------- | ------------ |
| Pass 1 (0.8.0), before this pass                                                | 1.25×        |
| + the first draft of the new letters, no fix                                    | 1.47×        |
| + callers scaled by the ward's memory only                                      | 1.44×        |
| + and by suspicion (weight 1)                                                   | 1.41×        |
| cheap kindnesses in twelve letters no longer raise standing (no fix)            | 1.32×        |
| … with callers scaled by memory and suspicion (weight 1)                        | 1.23×        |
| + standing credited by the same trust                                           | 1.17×        |
| + suspicion weight 1.5 (**final**)                                              | **1.12×**    |
| (for scale: every letter effect stripped; weight 2 instead of 1.5, on 60 seeds) | 1.17×; 1.03× |

Letter callers a month, before → after: candidate 72 **7.7 → 0.6**; candidate 14 (honest bottle, bold claims,
first reply, suspicion 40) 7.7 → 3.9; adaptive 0.07 → 0.09.

## 5. Balance

`pnpm sim:batch --bots shopkeeper,cautious,greedy,briber,adaptive,sloppy,heavy --runs 80 --seed ev2`, before
(pass 1, content 0.8.0) and after:

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Ended early | Cash m12 / m24 / m48 after | Product deaths | Peak heat |
| ---------- | ---------------------------- | ----------------- | ----------------- | ----------- | -------------------------- | -------------- | --------- |
| shopkeeper | $15,788 → **$15,666**        | $15,725 → $15,573 | $14,832 / $16,081 | 0% → 0%     | $239 / $344 / $276         | 3 → 3          | 30 → 31   |
| cautious   | $7,082 → **$6,769**          | $6,934 → $6,762   | $6,118 / $7,454   | 1% → 0%     | $192 / $227 / $270         | 6 → 7          | 33 → 40   |
| greedy     | $27,137 → **$19,396**        | $22,077 → $18,487 | $4,962 / $29,235  | 39% → 59%   | $262 / $547 / $2,670       | 5 → 5          | 100 → 100 |
| briber     | $15,515 → **$15,476**        | $15,595 → $15,005 | $12,440 / $17,791 | 0% → 3%     | $218 / $383 / $714         | 30 → 30.5      | 100 → 100 |
| adaptive   | $21,140 → **$20,907**        | $21,035 → $20,800 | $20,091 / $21,371 | 0% → 0%     | $216 / $251 / $576         | 1 → 2          | 18 → 21   |
| sloppy     | $15,952 → **$15,377**        | $15,790 → $15,576 | $12,495 / $19,027 | 0% → 1%     | $215 / $277 / $404         | 8 → 8.5        | 62 → 67   |
| heavy      | $7,673 → **$9,289**          | $9,541 → $9,698   | $614 / $18,146    | 60% → 54%   | $324 / $589 / $877         | 73 → 75        | 100 → 100 |

The dark and noisy bots at 240 runs (`--seed ev2h`), before → after:

| Bot    | Ended early   | Median            | Mean              | p10 / p90                             |
| ------ | ------------- | ----------------- | ----------------- | ------------------------------------- |
| heavy  | 56% → **56%** | $9,696 → $7,139   | $9,650 → $9,456   | $541 / $18,661 → $533 / $18,499       |
| greedy | 52% → **50%** | $25,410 → $25,077 | $21,833 → $20,717 | $7,674 / $30,247 → $6,476 / $29,033   |
| briber | 3% → 6%       | $15,677 → $15,545 | $15,232 → $14,572 | $12,805 / $18,299 → $11,295 / $17,950 |
| sloppy | 0% → 0%       | $15,811 → $15,514 | $15,857 → $15,430 | $11,529 / $19,513 → $11,345 / $18,799 |

- **Honest play keeps its shape**: adaptive −1% ($20.9k, in the $15–21k band), shopkeeper −1%, cautious −4% (it
  answers every letter with the default, and some new defaults to officials cost a point or two of suspicion:
  the sealer, the dispensary doctor, the Estate's clause four, the war stamp).
- **Greedy's 80-run jump (39% → 59% ended early) is noise**: at 240 runs it is 52% → 50%, median $25.4k → $25.1k.
  Heavy's median sits between its two humps (half its games end early) and moves with them; its mean is $9.7k →
  $9.5k. Briber ends early a little more often (3% → 6%; probably the trust fix, which credits its kindnesses
  less while suspicion is high).
- Wealth and product deaths correlate at −0.06 (−0.05 before).

**The exploit hunter.** `pnpm sim:optimize --candidates 120 --seeds 4`, the top lines re-scored on 40 seeds against
adaptive on the same seeds, before (0.8.0) and after. On pass 1's search seed `ev1`:

| Line (candidate)                                                                                                              | Before | After     |
| ----------------------------------------------------------------------------------------------------------------------------- | ------ | --------- |
| No narcotic, 2.5 oz spirit, valerian, 117¢, no ads, four bold claims, testify, branch + almanac + next door, first reply (14) | 1.28×  | **1.24×** |
| 1.1 oz laudanum escalating, cuts at heat 52, 155¢, four bold claims, silent, every venture, first reply (72)                  | 1.25×  | **1.12×** |
| 0.57 oz laudanum, 176¢, bold claims, silent, every venture, first reply (22)                                                  | 1.03×  | 0.55×     |
| 0.13 oz morphine, 148¢, bold claims, testify, default reply (10)                                                              | 0.96×  | 0.92×     |

And on a fresh seed, `ev2` (its top four by the 4-seed score):

| Line (candidate)                                                                                              | Before | After     |
| ------------------------------------------------------------------------------------------------------------- | ------ | --------- |
| No narcotic, 3 oz spirit, valerian, 118¢, $8 ads, four bold claims, silent, every venture, first reply (32)   | 1.46×  | **1.41×** |
| 0.64 oz laudanum, 153¢, no ads, four bold claims, bribe, cuts at heat 58, every venture, first reply (33)     | 1.31×  | 1.19×     |
| No narcotic, 2.7 oz spirit, capsicum + gentian, 112¢, $8 ads, bold claims, silent, three ventures, first (37) | 1.21×  | 1.18×     |
| 0.08 oz morphine, 107¢, bold claims, silent, cuts at heat 21, every venture, default reply (56)               | 1.09×  | 1.12×     |

The best line on each search is the same as before and lower. **The fresh search found a stronger pre-existing
line than pass 1's:** candidate 32 of `ev2`, an honest 3 oz spirit bottle at $1.18 with every bold claim and every
venture, at 1.46× before this pass and 1.41× after. Like pass 1's candidate 14 it is a price-and-claims line, not a
letters one (it takes the first reply, but letters move it by −0.05), and it is the next thing to look at.

## 6. The game's letters drawer

`godot/scripts/UI/LettersDesk.cs`: a `Heads` entry for all 79 letters (sender, address, sign-off, docket,
salutation, `Voice` empty; seals only where pass 1's are fitting: `seal_hobart` for Hobart's circulars,
`seal_health` for the Board's), a `Risks` line for each of the 196 replies, `Places` for the Chicago medal bureau,
and the trust note of §4. `godot/data/paper.en.json` names the 24 new caller sources ("by the night bell", "to see
the label that CURSES", "in goggles", "from the picture show"). Rendered and checked by eye: October 1887 (the
sealer's letter under the coroner's notice, with its three replies and risk lines) and May 1888 at suspicion 17
(the trust note).

## 7. History I would like checked

Prices are the game's (a month's rent is $3), not the period's. Facts the letters rest on:

- **The Pedestal Fund (1885)**: the statue in its crates on Bedloe's Island from June 1885; a newspaper printing
  every giver's name, March–August 1885.
- **The Washington Centennial**: the parades of April 30 (army and navy) and May 1 (the trades), 1889.
- **The 1890 census**: a schedule of manufactures filled in by the proprietor, with a pledge of confidentiality,
  collected by special agents in June 1890.
- **The Columbian celebration, October 1892**: the societies' parade and the Columbus monument at the Circle,
  paid for by subscription among the city's Italians and unveiled that week.
- **The World's Columbian Exposition (1893)**: its official award medals were struck years later; private
  "bureaus" sold medals meanwhile.
- **Phonograph parlours** (a nickel, ear-tubes) in the early 1890s.
- **Visiting nurses' settlements** from 1893 (the letter invents a house on Eldridge Street).
- **Diphtheria antitoxin** made by the city's health authorities from about 1895 and given free to the poor.
- **The Street Cleaning Department** (from 1881) and its rules for separating ashes, garbage and rubbish (from
  about 1896), and its men in white; the sidewalk snow ordinance.
- **X-ray exhibitions** in 1896.
- **Gold clauses** in merchants' accounts before the November 1896 election.
- **The Raines law (1896)**: Sunday drink in "hotels" of ten bedrooms, with a meal; the sandwich.
- **Medical inspection of schools** by the Health Department from 1897, with exclusions for pediculosis.
- **Consolidation**: the greater city from 1 January 1898 (Manhattan, Brooklyn, Queens, Richmond, the Bronx);
  "Bureau of Licenses" in the Mayor's office is my guess at the name.
- **The War Revenue Act of 1898**: stamps on proprietary medicines from 1 July 1898, graded by price (the letter
  gives no rates), repealed a few years later (the risk line says two years).
- **Telephones in drug stores** and physicians telephoning prescriptions (from 1896 in the letter).
- **Liquid-air promotions**, 1899–1901.
- **The first subway's construction under Elm Street** (now Lafayette), 1900–04, by sections; the Board of Rapid
  Transit Railroad Commissioners.
- **Smallpox in New York, winter 1901–02**, and the Health Department's vaccinators in the tenements.
- **Trading stamps** around 1900–06; druggists selling **benzine to motorists** (and "the Club");
  **picture post cards** (private cards headed "Post Card" from 1901; the Flatiron Building, 1902);
  **flashing electric signs** on Broadway; **nickelodeons** in New York by 1906 showing _The Great Train Robbery_
  (1903).
- **Labor Day** a legal holiday in New York from 1887; **scientific temperance instruction** in the schools (the
  letter's "Physiology and Hygiene, which includes the effects of alcohol"; a New York law of 1884, I believe).
- **Carron oil** for burns; **target excursions** that collected prizes from merchants (1880s–90s); **the law
  requiring a medicine chest** aboard American ships; **Moving Day** on May 1st; district leaders' **Christmas
  dinners**; the **carrier's New Year's address**; **arsenic complexion wafers**; the **anthracite roads**
  agreeing prices; the **Inspector of Weights and Measures** and the **Commissioner of Jurors** (Court of General
  Sessions).
- **The Board's summer circular**: that infant deaths peaked in July is well attested; that the Board issued a
  circular about soothing syrups is my invention of a plausible one.
- **The counterfeit note**: "the Merchants' Bank of a town in Ohio, which has not had a Merchants' Bank since 1873"
  (a broken bank's note passed in 1885; worth a check that such notes still turned up).

## Plumbing

- **Content**: `events.json` (+79 events; `apprentice.take_him_on` sets `apprentice_served`), `en.json` (+354
  strings), `config.json` (`contentVersion` 0.9.0; `ward.letterCallerLoss` 1, `scrutiny.letterCallerLoss` 1.5),
  `schemas.ts` (the `season` requirement; the two config fields), `lint.ts` (no season in unlocks or ventures).
- **TS sim**: `requirements.ts` (`season`), `market.ts` (`letterTrust`, `letterCallers(state, content)`),
  `progression.ts` (`creditedEffects`).
- **C# sim**: `ContentTypes.cs` (`SeasonRequirement` and its JSON; the config fields), `Progression.cs`
  (`Requirements.Evaluate`/`Progress`; `CreditedEffects`), `Market.cs` (`LetterTrust`, `LetterCallers`).
- **Tests**: `packages/sim/test/letters.test.ts`, `sim-cs/Panacea.Sim.Tests/LettersTests.cs` (three new each);
  `GameTests` counts (172 events, 914 strings); fixtures regenerated.
- **Game**: `LettersDesk.cs` (`Heads`, `Risks`, `Places`, the trust note), `paper.en.json` (`callerSources`).
