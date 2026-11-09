# Panacea · events pass 1 · the letters of the long middle

Content 0.7.0 → **0.8.0**. Both sims changed together (the TypeScript reference and the C# port); the parity
fixtures were regenerated (32 runs) and `dotnet test` is green (107). TS 129/129, typecheck, lint, prettier,
`content:lint` (0 errors, 0 warnings) and the Godot build pass.

This pass answers the completionist's round-7 finding 2 ([round07/cold-completionist.md](round07/cold-completionist.md),
[SUMMARY.md](round07/SUMMARY.md) item 5): "the same quack ads, the same filler items and the same three letters turn
up in 1885, 1895 and 1906". There were 12 letters (11 drawn at random). There are now **93** (92 random): **81 new
letters**, written for their decade, and the twelve old ones with longer cooldowns.

## What changed, in short

- **81 new letters** (§1), each with its own correspondent, letterhead, docket and register, two or three replies,
  and a plain line under every reply saying what it does.
- **Ten chains** (§2) of two to four letters, linked by flags.
- **A fair draw** (§3): a seeded shuffle of the eligible letters each month, identical in both sims.
- **Two small effects** (§4): `wardMemory`, and callers sent by a letter (`callers_<source>`).
- **Frequency** (§5): from 0.20 to **0.57** letters a month for an honest shop (0.75 for a dark one), **70 different
  letters** in a run instead of 9, repeats at least three or four years apart.
- **Balance** (§6) keeps its shape: adaptive $20.8k → **$21.1k**, greedy and heavy still gambles. The exploit
  hunter's best line is the same line as before (1.28×, a pre-existing line); one strong-laudanum line climbed from
  1.16× to 1.25× (see §6, "What I could not fix").

## 1. The new letters

Every letter is a period letter from someone with a name and a reason to write: a ward heeler's scrawl, a lawyer's
paragraphs, a mother's careful hand, a reporter's breeziness, a circular from Pearl street. The body is 2–5
sentences in the correspondent's voice (`event.<id>.body` is the letter itself, so LettersDesk `Voice` is empty for
all of them). The replies are one line each, written as what the proprietor does. The jokes are on the trade, the
officials and the profiteers (the Captain's steamboat outing, the doctor who invented Bicycle Face, the radium
company that cannot afford radium, the lawyers' six-point label, the ice trust's regret for the poor); the
sufferers write plainly and are never the joke.

Sensitivity (§11): no stereotyped depictions or dialect (the ward heeler and the wardman write a scrawl, not an
accent); immigrant correspondents (a pushcart man, a sign painter, a milliner, a relief committee's secretary) are
tradesmen with business of their own; the infant in the Kehoe letters is never shown dying (the dispensary doctor
walks her all night and she lives); no real trademarks; no real living person; real institutions (the Board of
Health, the Lexow committee, the war inquiry, the Bureau of Chemistry, the AMA's Council, the Proprietary
Association, the Tenement House Department) appear only as letterheads of a clerk or secretary saying what the
institution historically did.

"When" is the window the letter can come in; "Draw" is its monthly chance once it can, and whether it comes once
or may come back after a cooldown.

### The early shop (from 1885) (26)

| Letter                  | Theme                        | When              | Draw                  | What it is                                                                            | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                                         |
| ----------------------- | ---------------------------- | ----------------- | --------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `landlord_roof`         | the landlord                 | 1885-04 – 1889-12 | every 48+ mo, 0.06/mo | The landlord won't mend the roof that drips into the copper.                          | Pay the slate man five dollars. / Take the landlord's offer: fifty cents more on the rent for a year. / _Put a pan under the drip._                                            |
| `doctor_arrangement`    | a doctor                     | 1885-06 – 1891-12 | every 48+ mo, 0.06/mo | A Grand Street physician offers to trade patients, for a commission.                  | Agree, and send him the dollar each month. / _Decline: your customers may go to whom they please._                                                                             |
| `parish_bazaar`         | the parish                   | 1885-09 – 1892-12 | every 48+ mo, 0.06/mo | The rector asks for raffle bottles for the roof fund.                                 | Send a dozen bottles for the raffle, three dollars' worth, with your card. / Send a dollar, quietly. / _Send your regrets._                                                    |
| `police_collection`     | the police                   | 1885-05 – 1894-02 | every 36+ mo, 0.06/mo | The precinct's wardman collects for the Captain's steamboat outing.                   | Give him five dollars on Friday. / _Tell him your back room is open to the Captain any day._                                                                                   |
| `gazette_canvasser`     | a newspaper canvasser        | 1885-03 – 1889-12 | once, 0.07/mo         | The Gazette's canvasser sells an advertisement dressed as news.                       | Take the reading notice for four dollars. / _Wait until you can afford the honest kind._                                                                                       |
| `board_of_pharmacy`     | the licence                  | 1885-04 – 1887-12 | once, 0.06/mo         | The Board of Pharmacy wants the shop on its register, for five dollars.               | Go before the Board and pay the five dollars. / Buy a certificate from a man who knows the Board's clerk. / _File it with the other circulars._                                |
| `blizzard_1888`         | the Blizzard of '88          | 1888-03 – 1888-04 | once, 0.8/mo          | March 1888: the ward is snowed in, and the Relief Committee asks for credit.          | Give credit until the thaw. / Put the chest rub up to a dollar while the snow lasts. / _Keep to cash, as always._                                                              |
| `grippe_1889`           | the Russian influenza        | 1889-12 – 1890-02 | once, 0.7/mo          | Winter 1889–90: the grippe is everywhere, and the jobber sells 'preventive' placards. | Order the placards and put one in the window. / _Put up your own card: Go to bed, and send for the doctor._                                                                    |
| `jobber_exclusive`      | the supplier                 | 1886-03 – 1895-12 | every 48+ mo, 0.06/mo | The wholesaler offers a tenth off for a year of exclusive dealing.                    | Sign for the year, and pay the deposit. / _Stay free to buy where you like._                                                                                                   |
| `medicine_show_offer`   | the medicine show            | 1886-04 – 1895-12 | every 48+ mo, 0.06/mo | A travelling show will sell your remedy from the wagon all summer.                    | Send the fifteen dollars and a crate of labels. / _Decline: you have seen the strong man._                                                                                     |
| `medicine_show_returns` | the medicine show            | any time          | every 12+ mo, 0.15/mo | The showman sends your third of the summer, less breakages.                           | _Take the twenty-one dollars, with thanks._ / Write that you would like to see the books, and the breakages.                                                                   |
| `kehoe_thanks`          | a family (chain)             | 1885-04 – 1894-12 | once, 0.05/mo         | A grateful mother asks whether you keep something for a teething baby.                | Send up a soothing syrup for the baby, with your compliments. / _Write that she should rub the gums with a cold spoon and take the baby to the dispensary._                    |
| `kehoe_more`            | a family (chain)             | any time          | once, 0.12/mo         | The syrup works too well, and she asks for a stronger one.                            | Send a stronger bottle, at the same price. / Write that the baby must have no more of it, and fetch the dispensary doctor yourself. / _Send the same bottle, and say nothing._ |
| `kehoe_last`            | a family (chain)             | any time          | once, 0.12/mo         | The baby could not be woken; the dispensary doctor has the bottle.                    | Send ten dollars for the doctor, and no more syrup. / Call on the doctor, and ask him what twenty-five dollars would make it. / _Send nothing._                                |
| `kehoe_well`            | a family (chain)             | any time          | once, 0.08/mo         | The baby is well, and the mother sends her neighbours.                                | Ask whether you may print her letter. / _Send your thanks, and a stick of candy for Nora._                                                                                     |
| `stowe_tract`           | the temperance woman (chain) | 1886-01 – 1895-12 | once, 0.05/mo         | The temperance League asks how much spirit is in your remedy.                         | Fill in the line truthfully. / Write PURELY VEGETABLE across the sheet. / _Use the sheet to wrap a bottle._                                                                    |
| `ward_boss_favor`       | the ward boss (chain)        | 1886-06 – 1893-12 | once, 0.05/mo         | The Alderman's man offers protection for a picture in the window.                     | Put the Alderman's picture in the window. / _Thank him, and keep the window for your own bottles._                                                                             |
| `ward_boss_due`         | the ward boss (chain)        | 1887-01 – 1897-12 | once, 0.1/mo          | The Alderman's favours come due on election day.                                      | Send the bottles and the dollars. / Send the bottles, but not the dollars. / _Tell him you vote as you please._                                                                |
| `feeney_boy`            | the rival (chain)            | 1886-06 – 1894-12 | once, 0.05/mo         | The rival druggist tries to hire away your boy, and your receipt.                     | Give him the fifty cents a week. / _Let him go to Feeney._                                                                                                                     |
| `feeney_copy`           | the rival (chain)            | any time          | once, 0.15/mo         | Feeney bottles your receipt and sells it at half your price.                          | Put the matter in a lawyer's hands. / Let it be known that his copy is a dangerous imitation. / _Let him: his copy is only a copy._                                            |
| `feeney_inquest`        | the rival (chain)            | any time          | once, 0.06/mo         | A man dies of Feeney's copy, and Feeney says the receipt was yours.                   | Testify that Feeney cannot read your writing, or anything else. / Buy Feeney's shop and his bottles, and close it. / _Say nothing._                                            |
| `pawn_ticket`           | a regular (chain)            | any time          | once, 0.06/mo         | A regular who wanted something stronger has pawned her ring to pay for it.            | Redeem the ring and send it back to her, the account cleared. / Sell the ticket back to the pawnbroker. / _Put the ticket in the drawer with the others._                      |
| `widow_account`         | the bereaved                 | from 1885-06      | every 48+ mo, 0.06/mo | A widow writes about her late husband's account for the tonic.                        | Write that the account is closed. / Ask for the watch. / _Ask for ten cents a week._                                                                                           |
| `soda_fountain_offer`   | the soda fountain (chain)    | 1888-05 – 1899-12 | every 48+ mo, 0.06/mo | A soda fountain, sixty dollars installed.                                             | Install the fountain. / _Decline: the shop is a pharmacy, not a parlour._                                                                                                      |
| `undertaker`            | the undertaker               | from 1886-01      | every 48+ mo, 0.06/mo | The undertaker proposes an exchange of recommendations.                               | Shake hands on it. / _Show him the door._                                                                                                                                      |
| `electric_light`        | the electric light           | 1889-06 – 1899-12 | every 48+ mo, 0.06/mo | The electric company will light the window.                                           | Have the window wired. / _Keep the gas._                                                                                                                                       |

### The 1890s (26)

| Letter               | Theme                               | When              | Draw                  | What it is                                                                      | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                                           |
| -------------------- | ----------------------------------- | ----------------- | --------------------- | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `reporter_calls`     | the reporter (chain)                | 1890-01 – 1899-12 | once, 0.05/mo         | A penny-press reporter wants to see the back room.                              | Show him the back room and answer the questions. / Give him five dollars, and a bottle for his editor. / _Show him the door._                                                    |
| `reporter_proofs`    | the reporter (chain)                | any time          | once, 0.15/mo         | The reporter sends proofs: POISON ON MULBERRY STREET?                           | Send the twenty-five dollars. / Write to his editor that your advertising will go elsewhere. / _Let it run._                                                                     |
| `reporter_kind`      | the reporter (chain)                | any time          | once, 0.2/mo          | The reporter's piece was kind; he wants patients for a follow-up.               | Send him three regulars who swear by it. / _Thank him, and leave it there._                                                                                                      |
| `stowe_chemist`      | the temperance woman (chain)        | from 1888-01      | once, 0.08/mo         | The League's chemist finds your 'purely vegetable' remedy is not.               | Offer to pay for the League's hall for a year. / Engage a chemist of your own to find it vegetable. / _Let them print it._                                                       |
| `stowe_petition`     | the temperance woman (chain)        | from 1890-01      | once, 0.08/mo         | The League asks you to head a petition for honest labels.                       | Put your name at the head of the petition. / _Decline, with regret._                                                                                                             |
| `proprietary_invite` | the Proprietary Association (chain) | 1889-01 – 1904-12 | every 48+ mo, 0.06/mo | The trade association invites you to join.                                      | Join the Association. / _Decline: your property can look after itself._                                                                                                          |
| `red_clause`         | the Proprietary Association (chain) | until 1905-05     | once, 0.08/mo         | The Association advises the red clause in every advertising contract.           | Add the red clause to your contracts. / _Keep your contracts plain._                                                                                                             |
| `cholera_1892`       | the cholera scare                   | 1892-08 – 1892-11 | once, 0.7/mo          | Autumn 1892: cholera at Quarantine, and the Board forbids 'cholera cures'.      | Put a card in the window: A SURE PREVENTIVE OF CHOLERA. / _Post the Board's notice on boiling the water._                                                                        |
| `magazine_household` | the magazines                       | until 1899-12     | every 36+ mo, 0.06/mo | A magazine that refuses medicine ads will mention a 'household cordial'.        | Become a household cordial for twenty dollars. / _Decline: your remedy is medicine or it is nothing._                                                                            |
| `panic_relief`       | the panic of 1893                   | 1893-08 – 1894-12 | once, 0.12/mo         | Winter 1893: the relief committee asks druggists to take its tickets.           | Honour the tickets at cost through the winter. / _Decline: the shop is not a charity._                                                                                           |
| `panic_credit`       | the panic of 1893                   | 1893-06 – 1894-06 | once, 0.15/mo         | The panic of 1893: the wholesaler stops credit.                                 | Settle the account in cash. / _Carry on, at a tenth over list._                                                                                                                  |
| `lexow_subpoena`     | the ward boss (chain)               | 1894-03 – 1894-12 | once, 0.35/mo         | 1894: the Lexow committee has the wardman's book, with your name in it.         | Attend, and say what you paid and to whom. / _Attend, and remember nothing._ / Take a month's rest in the Catskills.                                                             |
| `sunday_closing`     | the reform police board             | 1895-06 – 1897-04 | once, 0.15/mo         | 1895: the reform police board shuts the saloons on Sundays.                     | Sell bitters by the glass on Sundays. / Sell by the bottle only, as the law allows. / _Keep the Sabbath, and the shutters._                                                      |
| `wheelmen`           | the bicycle                         | 1894-04 – 1899-12 | every 48+ mo, 0.06/mo | A cycling club wants a club price, and will wear your name.                     | Agree to the club price. / _Decline: the shop does not ride._                                                                                                                    |
| `trolley_cards`      | the cable cars                      | 1895-01 – 1901-12 | every 48+ mo, 0.06/mo | Advertising cards in the cable cars, four dollars a month.                      | Take a year of cards. / _Decline._                                                                                                                                               |
| `bicycle_face`       | the bicycle                         | 1895-04 – 1897-12 | once, 0.06/mo         | A doctor has invented 'Bicycle Face', and will sell the cure's endorsement.     | Pay for the endorsement: CURES THE BICYCLE FACE. / _Write that the cure is to get off the bicycle, and charge nothing._                                                          |
| `state_chemist`      | the state chemist                   | 1890-06 – 1899-12 | every 48+ mo, 0.06/mo | The state analyst has assayed your bottle, and will print it.                   | Send courteous observations, and something for the analyst's trouble. / _Let it go to the printer._                                                                              |
| `lodging_house`      | the Bowery                          | 1890-01 – 1901-12 | every 60+ mo, 0.06/mo | A Bowery lodging-house keeper wants a sedative for his lodgers' coffee.         | Sell it to him by the gallon. / _Tell him to hire the watchman._                                                                                                                 |
| `tammany_back`       | the ward boss (chain)               | 1898-01 – 1899-12 | once, 0.15/mo         | 1898: Tammany is back, and remembers who testified.                             | Pay the district club. / Send his letter to the Gazette. / _Let the Bureau come._                                                                                                |
| `volunteers_1898`    | the war of 1898                     | 1898-04 – 1898-08 | once, 0.6/mo          | 1898: the ward's volunteers want tonic for the voyage to Tampa.                 | Send a crate as a gift, with a flag on it. / Let them have it at cost. / _Write that the shop cannot spare it._                                                                  |
| `army_contract`      | the war of 1898 (chain)             | 1898-05 – 1898-11 | once, 0.3/mo          | A contractor will bottle your tonic for the army, on your certificate.          | Send the receipt and sign the certificate. / _Decline: you do not know what prevents camp fever._                                                                                |
| `army_inquiry`       | the war of 1898 (chain)             | 1898-10 – 1899-12 | once, 0.25/mo         | The war inquiry asks about the certificate you signed.                          | Go to Washington and lay it at the contractor's door. / Write that a druggist is not a physician, and meant no more than a hope. / _Say nothing, and let the contractor answer._ |
| `farm_wife`          | the mail (rural delivery)           | until 1906-06     | every 48+ mo, 0.06/mo | A farm wife wants secret drops to cure her husband's drinking.                  | Send her the drops, and keep the egg money. / _Write back that your remedy is mostly spirit, and keep the dollar out of it._                                                     |
| `testimonial_broker` | testimonials                        | from 1890-01      | every 48+ mo, 0.06/mo | A bureau sells signed testimonials by the hundred.                              | Buy a hundred, with the clergyman. / _Decline: your customers write their own._                                                                                                  |
| `heroin_traveller`   | the drug houses                     | 1898-10 – 1903-12 | every 36+ mo, 0.06/mo | A drug-house traveller offers cheap opiates if you'll try the new cough remedy. | Agree to the fair trial. / _Decline: you have heard 'not habit-forming' before._                                                                                                 |
| `letter_broker`      | the letter trade                    | from 1895-01      | every 48+ mo, 0.06/mo | A broker buys sufferers' letters to sell to other proprietors.                  | Sell him the letters. / _Burn them._                                                                                                                                             |

### 1900–1906 (17)

| Letter              | Theme                               | When              | Draw                  | What it is                                                                       | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                              |
| ------------------- | ----------------------------------- | ----------------- | --------------------- | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ice_trust`         | the soda fountain (chain)           | 1900-05 – 1900-09 | once, 0.4/mo          | 1900: the Ice Trust doubles the price of ice for the fountain.                   | Pay the new price, and keep the fountain running. / Buy from a Jersey iceman who cuts it from the Hackensack. / _Shut the fountain for the summer._ |
| `cut_rate`          | the chain stores                    | from 1900-01      | every 36+ mo, 0.06/mo | Cut-rate chain stores sell your bottle under your price.                         | Sign the plan, and pay the dues. / _Let them: a sale is a sale._                                                                                    |
| `grimsby_belt`      | electric cures                      | 1900-01 – 1906-06 | every 48+ mo, 0.06/mo | The galvanic-belt man proposes to recommend each other.                          | Shake hands with the Doctor. / _Decline, and return the Belt._                                                                                      |
| `tenement_dept`     | the Tenement House Department       | from 1902-03      | every 48+ mo, 0.06/mo | The new Tenement House Department orders the still fireproofed.                  | Have the rear room lined with brick and tin. / Ask the inspector whether thirty days could be sixty, and sixty never. / _Show cause, at length._    |
| `bureau_sample`     | the Bureau of Chemistry             | until 1906-11     | once, 0.05/mo         | The Bureau of Chemistry asks for two bottles 'as sold'.                          | Send two bottles from a special batch, mostly water. / Send two bottles from the shelf. / _Mislay the letter._                                      |
| `radium_water`      | radium                              | from 1903-06      | every 36+ mo, 0.06/mo | A radium company sells the right to print RADIO-ACTIVE on your label.            | Buy the licence: RADIO-ACTIVE on the label. / _Decline: your tonic gives off quite enough._                                                         |
| `motor_car`         | the motor-car                       | from 1903-01      | every 48+ mo, 0.06/mo | A motor-car comes through the window; its owner's lawyers want silence.          | Take the twenty dollars, and say nothing. / _Tell the Gazette everything, and leave the hole in the window._                                        |
| `subway_signs`      | the subway                          | from 1904-11      | every 36+ mo, 0.08/mo | The new subway's walls are for sale.                                             | Take the signs for a year. / _Decline: your customers walk._                                                                                        |
| `muckraker`         | the muckrakers                      | 1904-09 – 1906-06 | once, 0.08/mo         | A muckraking writer asks for your formula and your testimonial writers.          | Send the formula and the addresses. / Have a lawyer write that any mention will be met in court. / _Do not reply._                                  |
| `reporter_returns`  | the reporter (chain)                | from 1903-01      | once, 0.12/mo         | The reporter you paid is at a muckraking weekly now, with his notes.             | Send a hundred dollars, for old times' sake. / Give him the name of a rival with worse in his jug. / _Let him write what he likes._                 |
| `ama_council`       | the doctors                         | from 1905-03      | once, 0.08/mo         | 1905: the doctors' council asks for your formula, for its list.                  | Submit the formula and the advertising. / _Decline: your formula is your property._                                                                 |
| `pa_war_chest`      | the Proprietary Association (chain) | 1905-02 – 1906-06 | once, 0.25/mo         | The Association raises a fund to kill the food-and-drug bill.                    | Pay your share. / Resign from the Association. / _Put it with the bills._                                                                           |
| `red_clause_called` | the Proprietary Association (chain) | 1905-11 – 1906-06 | once, 0.3/mo          | The Association asks members to invoke the red clause against the bill.          | Write to every editor you pay. / _Hold off._                                                                                                        |
| `congress_member`   | Congress and the bill               | 1905-12 – 1906-06 | once, 0.3/mo          | Spring 1906: your Congressman would value your views, and your contribution.     | Send a hundred dollars, and your views against the bill. / Write that you favour the bill, and send nothing. / _Have no views._                     |
| `stowe_washington`  | the temperance woman (chain)        | 1904-01 – 1906-06 | once, 0.15/mo         | Twenty years on, the League's secretary will testify about the ward's druggists. | Offer to go with her, and say what is in your bottle. / Send the League a donation, with your respects. / _Let her say what she knows._             |
| `label_lawyer`      | the Act                             | any time          | once, 0.35/mo         | 1906: lawyers offer a label that obeys the Act in six-point type.                | Retain Messrs. Grue & Partridge. / Have the printer set it plainly, on the front. / _Wait and see what the inspectors do._                          |
| `food_not_drug`     | the Act                             | any time          | once, 0.25/mo         | A consulting chemist will make your remedy a 'food' to dodge the Act.            | Pay thirty dollars to become a table beverage. / _Remain a medicine, and take the consequences._                                                    |

### The shop's ordinary life (any decade) (12)

| Letter               | Theme          | When         | Draw                  | What it is                                                                | Replies (the default, sent if the month ends unanswered, _in italics_)                                                                                   |
| -------------------- | -------------- | ------------ | --------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `still_smell`        | the neighbours | from 1885-06 | every 48+ mo, 0.06/mo | The milliner next door says the still has spoiled her bonnets.            | Put a pipe up through the roof. / Buy the returned bonnet, and ask her to be patient. / _Let her write to the Board._                                    |
| `apprentice`         | an apprentice  | from 1886-01 | every 48+ mo, 0.06/mo | A pharmacy student asks to serve his time, weighing every batch.          | Take him on for a year. / _Write that the back room has no room._                                                                                        |
| `sign_painter`       | the street     | from 1885-05 | every 48+ mo, 0.06/mo | A sign painter offers gold leaf for the faded sign.                       | Order the gilt sign. / Have him touch up the old one for two dollars. / _Leave it: everyone knows where the shop is._                                    |
| `burglary`           | the police     | from 1885-03 | every 48+ mo, 0.06/mo | Burglars took the till; the detective can find it, for a consideration.   | Pay the consideration, and get the till back. / Write off the till, and put iron bars on the window. / _Write off the till._                             |
| `sweatshop`          | the contractor | from 1890-01 | every 60+ mo, 0.06/mo | A garment contractor wants a stimulant for his workers' water pail.       | Sell it to him by the gross. / _Tell him the girls are tired because they are tired._                                                                    |
| `gentleman_morphine` | a gentleman    | from 1886-01 | every 60+ mo, 0.06/mo | A man of position wants morphia by messenger, at a discreet price.        | Name a price, and send the messenger back with a parcel. / _Send the messenger back with the name of a doctor who treats the habit._                     |
| `revenue_stamp`      | the tax        | from 1885-09 | every 48+ mo, 0.06/mo | The revenue office says your tonic is a beverage, and taxable.            | Pay the special tax. / Ask the Deputy whether a smaller sum would satisfy the Collector. / _Write that your preparation is a medicine, and undrinkable._ |
| `pushcart`           | the street     | from 1886-01 | every 48+ mo, 0.06/mo | A pushcart man will send you the cough trade from his corner.             | Pay him the fifty cents a month for six months. / _Decline, and buy a lemon._                                                                            |
| `photographer`       | the label      | from 1885-06 | every 48+ mo, 0.06/mo | A photographer offers your likeness for the label.                        | Sit for the portrait, whiskers and all. / _Decline: your face is not your fortune._                                                                      |
| `obituary_desk`      | the paper      | from 1886-01 | every 48+ mo, 0.06/mo | The Gazette will name your remedy in an obituary, unless you advertise.   | Buy a little space, and the paragraph is forgotten. / Write a denial for them to print beside it. / _Let them print it._                                 |
| `phrenologist`       | the street     | from 1886-01 | every 48+ mo, 0.06/mo | The phrenologist upstairs will prescribe your tonic, for a dime a bottle. | Agree to the dime. / _Decline: your head is quite full._                                                                                                 |
| `school_principal`   | the school     | from 1887-01 | every 48+ mo, 0.06/mo | The school principal says boys are buying your syrup by the spoonful.     | Sell no more to boys, and tell the older one so. / Sell the older boy a smaller bottle. / _File it._                                                     |

### The twelve old letters

Their text is unchanged. A letter that came back every year was the same letter three times in a decade, so:

| Letter                    | Cooldown, before → after | Why                                                                         |
| ------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| `sanitary_inspector`      | 12 → 40 months           | 14.7 a run before; 6 now                                                    |
| `customer_wants_stronger` | 8 → 30                   | 15.3 a run before; 7 now. Deliberately the most frequent: it is the turn    |
| `rival_price_war`         | 18 → 48                  | 10.3 → 5                                                                    |
| `spirits_dearer`          | 24 → 60                  | 8.2 → 4                                                                     |
| `temperance_picket`       | 6 (kept)                 | A deliberate recurring letter for a hot shop; 18 was tried and broke greedy |

Two old replies now set a flag, to start chains: both replies to `rival_shop_opens` set `feeney_nearby`, and
`customer_wants_stronger.promise_stronger` sets `promised_stronger`.

## 2. The chains

Each chain reads as a story if the player gets all of it, and each letter stands alone. A chain letter waits on a
flag set by a reply to the one before, and clears it when answered, so it comes once. Follow-ups have a monthly
chance of 0.06–0.25, so they come some months later, not the next morning.

1. **The Kehoe family (4 letters, 1885–1895).** `kehoe_thanks`: a grateful mother on the top floor; the baby is
   teething, and does the druggist keep something for that? _Send up a soothing syrup_ → `kehoe_more` ("Nora sleeps
   the day through now and hardly cries, even for her feed. Only it wants a spoonful more each week") → _a stronger
   bottle_ or _the same again_ → `kehoe_last`, written by a neighbour: the baby could not be woken; the dispensary
   doctor walked her up and down all night and she is living, and he has your bottle. Pay the doctor, pay him to
   call it teething, or send nothing. _Send her to the dispensary_ instead → `kehoe_well`: the baby walks, and the
   mother sends her neighbours (or her letter becomes a testimonial). This is the design document's "a grateful
   testimonial in March can become a request for a stronger bottle in July", without the spectacle.
2. **The temperance woman (4 letters, 1886–1906).** `stowe_tract`: Mrs. Stowe of the League (the existing
   `temperance_picket` letterhead) asks what is in the bottle. _Truthfully_ → `stowe_petition` (head a petition to
   Albany for honest labels; the wholesale houses charge a tenth more for a year). _PURELY VEGETABLE_ →
   `stowe_chemist` (a bake sale paid for an analysis). Either way, `stowe_washington` (1904–06): "I am in my
   eightieth year and still Secretary… I write to ask what you would like me to say about you."
3. **The ward boss (4 letters, 1886–1899).** `ward_boss_favor` (the Alderman's picture in the window; suspicion
   −8) → `ward_boss_due` (election day: six dozen bottles and a dollar a head for gentlemen who vote early and
   often) → `lexow_subpoena` (1894: the wardman's book has your name; also reached by paying the Captain's outing,
   `police_collection`, or the detective's "consideration", `burglary`) → `tammany_back` (1898: if you testified,
   the Organisation remembers).
4. **The reporter (4 letters, 1890–1906).** `reporter_calls` (a penny-press man wants the back room) → shown:
   `reporter_kind` (a kind piece; send him patients?) · paid or refused: `reporter_proofs` (POISON ON MULBERRY
   STREET?, with the question mark; $25 buys a different head) → if you paid, `reporter_returns` (1903+: he is at a
   muckraking weekly now, and kept a note of your twenty-five dollars).
5. **The rival who copies the formula (3 letters + the old one).** `rival_shop_opens` (Feeney opens two doors down)
   → `feeney_boy` (Feeney offers your errand boy fifty cents a week more to tell him what goes in the big jug) →
   `feeney_copy` (FEENEY'S OWN, at half the price) → `feeney_inquest` (a lodger dies of Feeney's copy, and Feeney
   tells the coroner the receipt was yours).
6. **The Proprietary Association (3 letters, 1889–1906).** `proprietary_invite` → `red_clause` (the clause in red
   ink) → `red_clause_called` (1905–06: write to every editor you pay), and `pa_war_chest` (1905–06: $150 for the
   committee room).
7. **The war of 1898 (2 letters).** `army_contract` (a contractor bottles your tonic at Tampa on your certificate
   that it prevents camp fever) → `army_inquiry` (the War Department's commission: "chiefly whisky, and bad
   whisky").
8. **The medicine show (2 letters, may recur).** `medicine_show_offer` → `medicine_show_returns` (your third of the
   summer, less the strong man, who married in Utica).
9. **The soda fountain (2 letters).** `soda_fountain_offer` → `ice_trust` (1900: the ice trust abolishes the
   five-cent piece and regrets the inconvenience to the poor).
10. **The regular who wanted something stronger (old letter + 1).** `customer_wants_stronger` (_promise_) →
    `pawn_ticket` (the pawnbroker sends you the ticket for her wedding ring, pledged against her account).

How often the chains are reached depends on the replies: bots that take the default reply see the Kehoe-dispensary
and Feeney paths; the greedy bot (first reply) reaches `kehoe_more` and `kehoe_last`, the ward boss through to
Lexow, the reporter, the Proprietary Association and the army; the sloppy bot (random replies) reaches every chain
letter in some run (the rarest, `red_clause_called` and `tammany_back`, in about one run in 30).

## 3. The draw

`drawEvents` walked the events in file order and stopped at `maxEventsPerMonth` (2), so with many letters the
first in the file would crowd out the rest. Now (TS `systems/progression.ts`, C# `Progression.DrawEvents`):

1. Every random event that could come this month is collected, in file order: its condition holds, it is not
   already waiting, it is not spent (once-only), and it is not cooling down.
2. The list is shuffled with the game's seeded generator (Fisher–Yates from the end, `rng.int(0, i)`).
3. Each in turn is drawn with its own `chance`, until the post holds `maxEventsPerMonth` random letters.

Both sims consume the generator identically (n−1 draws for the shuffle, then one per chance roll until full); the
parity fixtures check it. New tests (TS `test/letters.test.ts`, C# `LettersTests.cs`): over 400 months with 20
always-drawn letters each comes 21–64 times in 800 (file order would have given the first two all 800); the
cooldown, once, pending and condition rules; a letter's own chance (0.25 → 0.22–0.28 over 2,000 months). The
content tests gain a dominance check: no reply to any letter may be better than another on every count (money,
standing, suspicion, notoriety, the ward's memory, capabilities by direction and months), unless they differ in
the flags they set; and every random letter must have a default reply.

## 4. Two small effects

- **`wardMemory` (new effect type)**: `{ "type": "wardMemory", "delta": n }` nudges the ward's memory of harm
  (balance pass 7), clamped to 0–100, raising its peak. The ward hears of what you did (`lodging_house`,
  `sweatshop`, `kehoe_last`, `reporter_proofs`) or thinks better of the shop (`blizzard_1888`, `panic_relief`,
  `widow_account`). Not allowed on ventures (lint).
- **Callers sent by a letter (a capability convention, no new effect type)**: a capability `callers_<source>`
  sends that many strangers a month to the counter, counted under `source` in `month.callers`
  (`market.ts letterCallers`, C# `Market.LetterCallers`, in ordinal order of the ids, after the ventures). It uses
  the existing timed-capability machinery. The paper's caller line names each source (`paper.en.json`
  `callerSources`: "three sent from the pulpit", "two by the signs in the subway", "eight to see the hole in the
  window"). The existing `walk_in_bonus` still reports as "by the society".
- Unchanged levers used by the letters: `retainer_cents` (a monthly cost: the errand boy's raise, car cards, the
  electric current), `rent_extra_cents`, `ingredient_discount_pct` (also negative, as a surcharge on everything:
  the 1893 credit stop, the jobbers' grudge), `narcotic_discount_pct`, `quality_control` (an apprentice who weighs
  every batch), and `rival_share_pct` / `rival_price_cents` (Feeney's copy, the cut-rate stores).

`Places` (new, in LettersDesk) dates a printed letter from Albany, Washington, Chicago, Newark or Elmira instead of
New-York; it replaces the hard-coded Cincinnati for the diploma mill.

## 5. Frequency and repeats

Measured with the bots (`letters.ts` in the scratchpad: 40 runs each; "letters" are random letters drawn, the
coroner's notices not counted). Before is commit `32acc18`.

| Bot        | Letters a month, before → after | Months with no letter | Different letters a run, before → after | Repeat gap, median (p10) | Repeats within 3 years a run, before → after |
| ---------- | ------------------------------- | --------------------- | --------------------------------------- | ------------------------ | -------------------------------------------- |
| adaptive   | 0.20 → **0.57**                 | 81% → 55%             | 9 of 11 → **70 of 92**                  | 19 (11) → **56 (42)**    | 41 → **3**                                   |
| shopkeeper | — → 0.57                        | 55%                   | 71                                      | 57 (43)                  | 3                                            |
| sloppy     | 0.22 → **0.59**                 | 80% → 54%             | 10 → **74**                             | 19 (10) → **56 (37)**    | 46 → **6**                                   |
| greedy     | 0.36 → 0.73                     | 68% → 45%             | 11 → 72                                 | 14 (7) → 49 (8)          | 78 → 36                                      |
| heavy      | 0.38 → 0.75                     | 67% → 45%             | 11 → 62                                 | 15 (7) → 49 (9)          | 59 → 31                                      |

By decade (adaptive, median a run): 1885–89 **38 letters, 35 different**; 1890–99 **70 letters, 44 different**;
1900–06 **45 letters, 34 different** (before: 16/9, 23/4, 16/4). The repeats within three years that remain are
`customer_wants_stronger` (30-month cooldown) and, for the dark shops, the heat-driven `temperance_picket` (6) and
`board_of_health` (12), which are deliberate.

**The target of 1–2 letters a month is not reached.** With 92 random letters and no repeat within about four
years, the ceiling is about 0.8 a month (the sum, letter by letter, of how often each could come in its window),
and chains only come on the paths that reach them. An honest shop gets a letter in 45% of months, a dark one in
55%. To reach one a month without shortening the cooldowns needs roughly 150–180 letters; the cheapest way there
is more letters of ordinary shop life (§1, "any decade"), which can recur every four years without anyone
noticing. I chose distinct letters and long gaps over frequency.

## 6. Balance

`pnpm sim:batch --bots shopkeeper,cautious,greedy,briber,adaptive,sloppy,heavy --runs 80 --seed ev1`, before
(`32acc18`) and after:

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Ended early | Cash m12 / m24 / m48 after | Product deaths | Peak heat |
| ---------- | ---------------------------- | ----------------- | ----------------- | ----------- | -------------------------- | -------------- | --------- |
| shopkeeper | $15,860 → **$15,743**        | $15,853 → $15,730 | $14,959 / $16,355 | 0% → 0%     | $239 / $368 / $320         | 3 → 2.5        | 29 → 31   |
| cautious   | $6,951 → **$7,003**          | $6,912 → $6,954   | $6,345 / $7,541   | 0% → 0%     | $196 / $226 / $297         | 7 → 7          | 33 → 28   |
| greedy     | $26,984 → **$26,758**        | $22,844 → $22,737 | $9,142 / $30,798  | 44% → 45%   | $271 / $534 / $2,861       | 5.5 → 5        | 100 → 100 |
| briber     | $16,069 → **$15,531**        | $15,029 → $14,069 | $1,498 / $17,947  | 5% → 10%    | $222 / $406 / $710         | 28 → 28        | 100 → 100 |
| adaptive   | $20,819 → **$21,143**        | $20,834 → $21,020 | $20,393 / $21,530 | 0% → 0%     | $221 / $228 / $614         | 2 → 2          | 12 → 19   |
| sloppy     | $15,437 → **$15,772**        | $15,205 → $15,393 | $11,450 / $19,029 | 0% → 3%     | $216 / $305 / $414         | 10.5 → 11      | 61 → 72   |
| heavy      | $15,924 → **$15,010**        | $11,356 → $10,239 | $681 / $18,502    | 44% → 51%   | $328 / $607 / $824         | 76.5 → 77      | 100 → 100 |

Heavy, greedy, briber and sloppy are noisy at 80 runs (heavy's median sits between its two humps), so they were
re-run at 240 runs (`--seed ev1h`), before → after:

| Bot    | Ended early   | Median            | Mean              | p10 / p90                             |
| ------ | ------------- | ----------------- | ----------------- | ------------------------------------- |
| heavy  | 48% → **50%** | $15,430 → $15,114 | $10,676 → $10,903 | $677 / $18,300 → $620 / $18,762       |
| greedy | 41% → **47%** | $27,936 → $26,936 | $23,330 → $22,206 | $7,976 / $31,074 → $7,161 / $29,957   |
| briber | 3% → 2%       | $16,160 → $15,833 | $15,567 → $15,292 | $13,077 / $18,336 → $12,498 / $18,255 |
| sloppy | 0% → 0%       | $15,945 → $15,757 | $15,777 → $15,485 | $11,721 / $19,387 → $10,566 / $19,435 |

- **Honest play is unchanged** to within 2%: adaptive $21.1k (band $15–21k), shopkeeper $15.7k, cautious $7.0k.
  The default replies cost little: the passive reply usually costs a point of standing or two of suspicion.
- **The heavy route is still a gamble**: half its games end early, and its best (p90 $18.8k) is below the honest
  twin's worst. **Greedy** (which always takes the first, usually bolder, reply) ends early slightly more often
  (41% → 47%): the dark offers (the lodging-house keeper, the sweatshop contractor, the gentleman's morphia, Sunday
  bitters) add suspicion to a shop already at the edge.
- How it was tuned. The first draft broke three things, each found with the bots and fixed:
  1. Greedy ended early in 61% of games. Lengthening `temperance_picket`'s cooldown had taken away its cheapest
     suspicion valve (a $5 donation, 20 times a run), so it went back to 6 months. The dark replies' suspicion was
     trimmed (+8 → +6, +6 → +4, +5 → +3), and the three darkest recurring offers come back every five years, not
     four.
  2. Heavy's passive replies added about 66 suspicion a run; the passive replies to officials were trimmed
     (+5 → +3 and so on).
  3. The exploit hunter found a strong-laudanum line that took every first reply rising from 1.16× to 1.55×
     adaptive. Stripping letter effects one kind at a time showed why: **callers** and **standing**. A strong
     bottle turns every extra stranger into a craving regular, so a caller is worth several times more to it than
     to an honest shop, and standing raises the conversion. The callers were cut by about half to two-thirds
     (no letter sends more than eight a month, the permanent ones one or two), and no letter raises standing by
     more than one, except testifying to the Lexow committee and heading the labelling petition (+2 each).

**The exploit hunter** (`pnpm sim:optimize --candidates 120 --seeds 4 --seed ev1`), the top lines re-scored on 40
seeds against adaptive on the same seeds:

| Line (candidate)                                                                                                              | Before | After     |
| ----------------------------------------------------------------------------------------------------------------------------- | ------ | --------- |
| No narcotic, 2.5 oz spirit, valerian, 117c, no ads, four bold claims, testify, branch + almanac + next door, first reply (14) | 1.30×  | **1.28×** |
| 1.1 oz laudanum escalating, cuts at heat 52, 155c, four bold claims, silent, every venture, first reply (72)                  | 1.16×  | **1.25×** |
| 0.57 oz laudanum, 176c, bold claims, silent, every venture, first reply (22)                                                  | 0.91×  | 1.03×     |
| 0.13 oz morphine, 148c, bold claims, testify, default reply (10)                                                              | 1.07×  | 0.96×     |

Wealth and product deaths correlate at −0.05 (−0.07 before).

**What I could not fix.** Two lines sit above the 1.15× limit:

- Candidate 14 was already at 1.30× before this pass, on this search seed (balance pass 7 used `r7b`, whose best
  was 1.07×). It is an honest bottle at $1.17 with no advertising and every bold claim; the letters do not change
  it. It needs its own look (price and claims, not letters).
- Candidate 72 rose from 1.16× to 1.25×, on 40 seeds (about ±0.04 of noise). The remaining gain comes from the
  callers and standing that every promotional letter offers, which a strong bottle converts into habits. It ends
  early in only 3% of games, so it is not a gamble. The next lever, if it matters: let the ward's memory or the
  strength of the bottle reduce what a letter's callers are worth, or price the promotional letters by the shop's
  takings rather than in fixed dollars.

## 7. The game's letters drawer

`godot/scripts/UI/LettersDesk.cs` has a `Heads` entry for all 81 letters (sender, address, sign-off, seal, docket,
salutation, `Voice` empty) and a `Risks` line for every one of their 197 replies, in the existing style ("$5;
suspicion −4. Somebody keeps a book of who gave.", "Costs nothing."). Only existing seal cuts are used:
`seal_hobart` (the jobber's circulars), `seal_league` (Mrs. Stowe), `seal_health` (the cholera circular),
`seal_coroner` (Feeney's inquest); every other letter is a plain letterhead or a hand. The build succeeds, and a
rendered month (March 1886: the pushcart man and the landlord's roof) was checked by eye: letterheads, dockets,
replies and risk lines lay out as the old letters do.

## 8. History I would like checked

- **The Lexow committee** sat at the County Court House on Chambers Street (I believe the hearings were held there,
  March–December 1894).
- **The Third Avenue line** ran by cable from about 1894 (the letter is dated from 1895).
- **The NYC Board of Pharmacy** register (1872 law; the letter avoids naming a statute).
- **The Tenement House Department** at 61 Irving Place (1902); **the AMA** at Dearborn Avenue, Chicago (1905);
  **the College of Pharmacy** (the apprentice no longer names its street, since it moved in 1894).
- **The ice trust** of 1900 doubling the price to 60 cents the hundredweight and ending the five-cent piece.
- **Heroin's makers** are "in the Rhine Province" (Elberfeld, on the Wupper; corrected from "on the Rhine" in review).
- **Camp Black** (Hempstead Plains) and Tampa for the 1898 volunteers; the **war inquiry** (Sept 1898 – Feb 1899) and
  "embalmed beef" testimony.
- **The Internal Revenue's special tax** on medicines sold as beverages ($25 a year for a retail liquor dealer):
  rulings of this kind existed through the period, but the figure is from memory.
- **The Act's list** (alcohol, morphine, opium, cocaine, heroin, eucaine, chloroform, cannabis, chloral,
  acetanilide) is as I remember section 8.

## Plumbing

- **Content**: `events.json` (+81 events; five cooldowns; three flags on old replies), `en.json` (+359 strings),
  `config.json` (`contentVersion` 0.8.0), `schemas.ts` (the `wardMemory` effect; the `callers_` convention
  documented), `lint.ts` (`wardMemory` not allowed on ventures).
- **TS sim**: `progression.ts` (`drawEvents`), `effects.ts` (`wardMemory`), `market.ts` (`letterCallers`).
- **C# sim**: `Progression.DrawEvents`, `Effects.Apply` (`WardMemoryEffect`), `ContentTypes.cs` (the record and its
  JSON), `Market.LetterCallers`.
- **Tests**: `packages/sim/test/letters.test.ts`, `sim-cs/Panacea.Sim.Tests/LettersTests.cs`, two content tests;
  `GameTests` counts updated; fixtures regenerated.
- **Game**: `LettersDesk.cs` (`Heads`, `Risks`, `Places`), `paper.en.json` (`callerSources`).
