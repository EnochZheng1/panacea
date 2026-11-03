# PANACEA — Game Design Document

> Working title. A Victorian patent-medicine tycoon: historical chemistry, mass-market quackery, and the slow arrival of the law.

**Status:** Pre-production · **Target:** Full commercial release (Steam, PC/Mac/Linux) · **Engine:** Godot 4 (.NET). The simulation is currently a TypeScript library; see [TECHNICAL_PLAN.md](TECHNICAL_PLAN.md).

---

## 1. Pitch

New York, 1885. There is no FDA, no labeling law, and no limit on what a man may bottle and call a cure. You start as the proprietor of a single cramped apothecary with one brass still and a dozen regulars. Twenty-one years later, the Pure Food and Drug Act takes effect on January 1st, 1907.

Between those dates you can build a national medicine empire: develop secret formulas, flood the newspapers with advertising, push product through wholesalers, medicine shows and mail order, and keep the coroners, editors and legislators sweet. The more you grow, the more people drink what you make, and the more of them it hurts.

**One line:** _Sell miracle cures in 1880s New York. Write the lies, buy the press, and read your victims' obituaries in the paper that runs your ads._

### The originality rule

**Every mechanic must come from the history of 1885–1906, not from another game.** If we cannot point to the real practice a feature dramatises, it does not go in.

Two consequences follow:

- **In public:** no "X meets Y" descriptions on the store page, in trailers or in press materials. Panacea is pitched on its own terms.
- **In design:** general techniques (procedural text, seeded runs, multiple endings) are fine to use, but they are never the pitch.

Before the Steam page and the trailer go public, we compare our screenshots side by side with the closest-looking games on Steam and change anything that reads as a lookalike.

### Pillars

Each pillar is grounded in what the trade actually did:

1. **You play inside a newspaper.** The page is the game board. Your adverts compete for column inches with the consequences of your choices, because patent medicine was what kept the period's papers in business. Each turn is an edition.
2. **Writing the lies is the core verb.** There was no truth-in-advertising law, so every claim you print is a bet: "Cures consumption" brings in buyers and draws inquests. The player composes claims, labels and testimonials by hand.
3. **Testimonials are a currency.** You solicit them, buy them and fake them, and they can outlive their authors. They work as both marketing and evidence against you.
4. **You can buy the press.** The red clause turned newspapers into the industry's lobbyists. You can capture your own critics, and the papers that depend on your money can turn on you.
5. **Addiction is the retention model.** The body simulation makes dependency the trade's real business model. Players discover it through their own choices, and that discovery is the game's thesis.
6. **The law is the clock.** Real events arrive on their real dates, and the 1906 Act ends the trade as it was. Runs differ in what you have become when it comes.
7. **Earned growth.** There are no chapters. Every new part of the business is unlocked by something the player achieved, survived or discovered.

**Tone:** dark satire that punches up. The game never pretends what you're doing is good; it makes it _tempting_. The joke is always on the seller and the system, never on the victims. Humour comes from juxtaposition on the page, period bombast ("Cures Consumption, Cholera, Melancholy and Bad Temper!") printed beside plain consequences, and the funniest material is usually true.

---

## 2. Historical grounding

The systems should follow history; the flavour can be as loud as the era itself.

| Real practice                                                                                                                                                          | In the game                                                       |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| Ingredients cost pennies; advertising was the main expense                                                                                                             | Margins are huge, ad spend is the main sink                       |
| Patent medicine makers were among the largest newspaper advertisers                                                                                                    | Newspapers are both your biggest channel and your shield          |
| The "red clause": ad contracts voided if the state passed anti-nostrum laws                                                                                            | Unlockable contract term that turns papers into lobbyists         |
| Distribution via wholesale druggists ("jobbers") → retail druggists and general stores                                                                                 | Wholesaler contracts with margin, credit and shelf placement      |
| Medicine shows toured rural territories                                                                                                                                | Hire troupes; spectacle events; rural awareness                   |
| Almanacs, trade cards, painted barns                                                                                                                                   | Ad media with different reach by region and literacy              |
| Letters to a (sometimes long-dead) brand persona, answered by clerks; lists sold to brokers                                                                            | Letter bureau / mail-order department; letters are a resource     |
| Proprietary Association of America (founded 1881) lobbied as an industry                                                                                               | Membership unlock; industry-level politics                        |
| Bitters sold as medicine at ~40% alcohol, often in saloons                                                                                                             | Saloon channel; tax-agent risk                                    |
| Infant "soothing syrups" containing morphine                                                                                                                           | A heavy, historically central product choice                      |
| NYC coroners were elected and notoriously corrupt (replaced by a Medical Examiner in 1918)                                                                             | The coroner is a real, bribable office                            |
| State boards of health and state chemists published analyses                                                                                                           | Chemist assays are a scrutiny event                               |
| 1906 Pure Food and Drug Act required _labeling_ of alcohol, morphine, opium, cocaine, heroin, chloroform, cannabis, chloral hydrate, acetanilide (it did not ban them) | 1906 changes the rules: secret formulas must admit their contents |

**Timeline anchors (fire on their dates):**

- 1885: start. Pemberton's French Wine Coca in Atlanta (reference only).
- 1886: acetanilide ("Antifebrin") available; headache-powder products.
- 1892: Ladies' Home Journal stops taking patent medicine ads.
- 1896: Rural Free Delivery begins (mail order expands rurally; made permanent 1902).
- 1898: heroin marketed (as a cough suppressant).
- 1899: aspirin marketed (a legitimate-path ingredient).
- 1902: Harvey Wiley's "Poison Squad" experiments begin.
- 1903: cocaine taken out of a famous cola (era signal; reference only, no brand).
- 1904–1905: magazine exposés; Collier's "The Great American Fraud" (Oct 1905).
- 1906: Pure Food and Drug Act signed (June 30), effective Jan 1, 1907.
- Epilogue hooks: 1912 Sherley Amendment (false cure claims), 1914 Harrison Narcotics Act.

**Naming policy:** All player and rival _brands_ are fictional. Real historical figures and institutions appear only as era context (e.g. reformers, journalists, legislation). No real trademarks. Legal review before launch.

---

## 3. Structure & time

- **One continuous run** from January 1885 through the 1906 Act and into an epilogue period (to ~1910), when endings resolve.
- **Turn = one edition of the paper.** Inside a turn the player works freely (lab, ad copy and layout, contracts, The Post). Going to press runs the simulation, and the next edition shows what happened.
- **Cadence widens with the business.** Weekly editions while the shop is small and every customer matters, then monthly once wholesale arrives. This also answers the early-pacing question in section 15.
- **Quiet editions auto-advance** (configurable) when no decision is pending, so the long run never turns into end-turn spam.
- **Target campaign length:** 8–12 hours for a full run; built for replays through backgrounds, map variation and endings.
- **Difficulty:** affects starting capital, scrutiny sensitivity and rival aggression. Plus an "Ironman" mode (single save).

---

## 4. Core loops

**Short loop (each edition):**

1. **Read the paper.** It is the payoff and the briefing at once:
   - your own adverts, as printed;
   - testimonials and letters;
   - the deaths column and the coroner's court;
   - rival advertisements and attacks;
   - the markets column (ingredient prices);
   - the news of the era.

   Causes stay explainable: every figure on the page can be traced in the Ledger.

2. **Make 2–4 meaningful decisions:**
   - brew or reformulate;
   - write ad copy and choose its size and position on the page;
   - buy, solicit or fake testimonials;
   - answer letters;
   - handle the coroner, the editor or a rival.
3. **Go to press.** The simulation runs and the next edition is printed. This moment gets full audiovisual polish: press clatter, the paper landing, the first look at the front page.

**Medium loop (every few months):** reach a new unlock (facility, channel, relationship, product line, automation).

**Long loop (the run):** grow the brand while reform rises; decide how to face 1906; reach one of several endings; replay with a different background.

**"One more turn" hooks:** visible countdowns on pending threats and opportunities ("Exposé in preparation: 2 months", "Inquest convenes next month", "Wholesaler decision pending").

---

## 5. Systems

### 5.1 The Laboratory (formulation & production)

The lab is the tactile heart of the game and the screen we'll polish most.

**Ingredient properties** (each ingredient carries a vector of these):

| Property       | Meaning                                                                       |
| -------------- | ----------------------------------------------------------------------------- |
| Kick           | Felt effect: warmth, buzz, numbness, purging. What customers judge "works" by |
| Calm           | Sedation / pain relief                                                        |
| Habit          | Dependency formation                                                          |
| Acute toxicity | Risk per dose, compared against the drinker's tolerance and vulnerability     |
| Accumulation   | Chronic load that does not clear (heavy metals)                               |
| Bitterness     | Too little reads as weak; too much and nobody can stomach it                  |
| Detectability  | How easily a state chemist identifies it                                      |
| Stability      | Shelf life, separation, spoilage                                              |
| Cost / supply  | Price per unit; subject to shocks                                             |

**Ingredient roster (launch target ~24):**

- _Bases:_ water, grain alcohol, wine, sugar syrup
- _Narcotics & stimulants:_ laudanum (opium tincture), morphine, coca extract / cocaine, chloroform, cannabis indica tincture, chloral hydrate, heroin (era-gated 1898)
- _Heavy metals & poisons:_ calomel / blue mass (mercury), Fowler's solution (arsenic), nux vomica (strychnine "tonic")
- _Purgatives (huge real "felt effect"):_ aloe, cascara, senna
- _Fraud fillers:_ dilute sulfuric acid (microbe-killer style), turpentine, mineral oil, capsicum (the "burn")
- _Flavour & colour:_ peppermint oil, sassafras, gentian (bitters), burnt-sugar colouring, wintergreen
- _Era drugs:_ acetanilide (1886), aspirin (1899; legitimate path)
- _Botanicals with real (mild) effect:_ ginger, willow bark, wild cherry

**Processes** transform the mixture:

- **Maceration / steeping:** time-based extraction; longer is stronger but costs production time.
- **Percolation:** faster extraction, needs equipment.
- **Distillation (the brass still):** an interactive gauge-driven process. Heat and pressure raise proof and purity; overpush and you risk boil-over, fire or explosion. This is the signature interaction.
- **Reduction:** boiling concentrates actives but drives off alcohol.
- **Filtration / clarifying:** appearance and stability.
- **Cutting / dilution:** stretches margin and lowers potency.
- **Ageing / resting:** stability and taste.

**Formula vs batch.** You design a _formula_ (a recipe and process). Production runs _batches_ of it. At larger scale, batch variance matters: a bad vat can carry 3× the morphine. Quality control is an unlockable investment, and bad batches are crisis events.

**Recurring reasons to return to the lab** (so formulation isn't a one-time task):

- Ingredient price spikes, shortages, adulterated shipments
- Rivals copying your bestseller
- Scrutiny forcing you to cut narcotics, which you then have to cover with bitterness, alcohol or purgatives
- New customer segments wanting new products
- Era-gated ingredients arriving
- The 1906 labeling rule changing what's sellable

### 5.2 The body model

Used both for **named customers** (individually simulated) and for **market cohorts** (simulated as distributions).

- **Tolerance** per substance class (opiates, alcohol, cocaine, sedatives): rises with use, falls in abstinence. The same dose works less over time.
- **Dependency:** drives repeat purchases; withdrawal when you cut the dose leads to complaints, switching to rivals, or desperate behaviour.
- **Toxic load:** accumulating (metals) and clearing (others).
- **Health:** baseline plus ailment, eroded by load and acute events.
- **Vulnerability:** infants, the elderly, the frail. Infants have tiny thresholds.
- **Death hazard** = f(acute dose vs tolerance-adjusted threshold, accumulated load, vulnerability). Most deaths are **delayed** results of earlier choices.
- **Tolerance-loss overdose:** if you cut the opium and later restore it, drinkers whose tolerance dropped are at high risk. This is historically real, darkly ironic, and something a player can learn.

### 5.3 The market

- **Regions** (launch ~6): Manhattan, Brooklyn & the boroughs, Hudson mill towns, Upstate farm belt, New England mill towns, Ohio & the West by rail. Map variation per run (region traits shuffled within plausible bounds).
- **Segments** per region: mothers, labourers, clerks, farm families, the elderly, temperance households, the well-to-do. Each has size, ailment mix, income, literacy, temperance attitude and trust in doctors.
- **Brand state per region:** awareness, trust, habit pool (dependent buyers), availability (from channels), price position, share against rivals.
- **Demand ≈ awareness × trust × felt effect × fit to the segment's ailments × availability × price.** Plus the habit pool, which buys regardless.
- **Seasons:** summer cholera and dysentery, winter coughs and consumption, malaria in river towns, epidemics as events.
- **Explainability is mandatory:** every month-over-month change is broken down by cause in the Ledger, with hover breakdowns on every number.

### 5.4 Distribution channels

| Channel                    | Character                                                                    |
| -------------------------- | ---------------------------------------------------------------------------- |
| Your shop (always on)      | Small volume; named regulars; the human-scale view of your product           |
| Retail druggists           | Need wholesaler supply; shelf placement matters                              |
| Wholesale druggists        | Volume, but margin cuts, credit terms, and they can drop you after a scandal |
| General stores             | Rural reach, low price tolerance                                             |
| Medicine shows             | Spectacle; bursts of rural awareness; troupe quality; local-trouble risk     |
| Mail order / letter bureau | Letters → testimonials and lists; boosted by RFD (1896)                      |
| Saloons                    | Bitters; strong volume; tax agents and temperance backlash                   |

### 5.5 Advertising & brand

- **Media:** newspapers (urban, literate), almanacs (rural, annual, huge print runs), trade cards, painted barns and signage, handbills, medicine-show spectacle.
- **Ad copy:** the player assembles claims from a library (ailments cured, endorsements, imagery). Bigger claims bring more sales and more scrutiny. This is a hands-on, comedic activity, not a slider.
- **Brand persona:** "Dr." or "Professor" with an engraved portrait. Honest credentials vs a diploma mill (cheap, a liability later).
- **Testimonials:** solicited (from letters), bought (ministers, minor celebrities), or fabricated. Each has a cost and a risk of being exposed.
- **Label designer:** name, engraving, typeface, claims, colours. It feeds both appeal and scrutiny.

### 5.6 Scrutiny & opposition

- **Heat** per region plus national **Notoriety**. Heat decays slowly and non-linearly: one inquest is noise, three in a county starts a campaign.
- **Incidents** are generated from the simulation (deaths, complaints, bad batches, a rival's sabotage): coroner's inquests, doctors' letters to the editor, state-chemist assays, a temperance-league campaign, a journalist's investigation (a visible multi-month progress bar once you have an informant).
- **Responses:** bribe, silence (red clause), sue (libel lawyer), counter-advertise, reformulate, rename and relaunch, frame a rival, ride it out.
- **Escalation:** bribed officials come back for more; bought editors become liabilities; opposition scales with your size and targets the biggest name; reformers react to your strategy (heavy opium makes _you_ the chemists' first target).
- **Leading indicators** so consequences feel fair: regional toxic-load meter, druggist rumours, informants.

### 5.7 People & relationships

Recurring named NPCs: coroners, editors, wholesalers, ministers, show managers, legislators, rival proprietors, reformers, a journalist. Each has **price, loyalty, leverage** (what they have on you and what you have on them) and **betrayal risk**. Relationships are unlocks in themselves and can be lost.

### 5.8 Rivals

3–5 AI proprietors with distinct strategies (the opium baron, the temperance-friendly "vegetable compound", the showman, the copycat, the careful one who goes legitimate). They compete for share, copy formulas, smear you and get exposed too. Reformers who catch a rival make the whole industry hotter.

### 5.9 The Post (events)

The main decision surface. Data-driven event cards with conditions, choices and effects: letters, offers, threats, news, personal moments. Target: **300+ events at launch**, with chains (e.g. one family's letters across a run). A grateful testimonial in March can become a request for a stronger bottle in July and an obituary in October.

### 5.10 The human ledger

A permanent record of named people who died from your products, surfaced at intervals and at the ending. Your internal books stay vague ("complaints: 3"); newspapers report what really happened, later.

---

## 6. Progression: earned unlocks (no acts)

Every expansion is earned. The game always shows the **next 2–3 unlocks with progress bars**.

**Requirement types:** Earn (cash, sales, share) · Prove (specific feat) · Discover (lab finding) · Connect (relationship) · Survive (come through a crisis) · Era (date gate).

**Example unlock tree (launch target ~60 unlocks):**

| Unlock                        | Requirement                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| Second still                  | Sell 200 bottles                                             |
| Percolator                    | Brew 10 batches of one formula                               |
| "Dr." title                   | Reputation 50 **or** buy a diploma-mill degree               |
| Newspaper advertising         | $150 saved + 1 testimonial                                   |
| Almanac printing              | Newspaper ads in 2 regions                                   |
| Wholesale druggist contract   | Sustain 300 bottles a month for 3 months with low complaints |
| Coroner "consulting fees"     | Survive your first inquest                                   |
| Libel lawyer                  | Survive your first exposé                                    |
| Red clause                    | Ad contracts with 3+ papers                                  |
| Bottling works                | Wholesale contract + $2,000                                  |
| Quality control               | Suffer a bad batch, or Bottling works + $X                   |
| Medicine show troupe          | Brand persona + sales in a rural region                      |
| Letter bureau / mail order    | Receive 100 customer letters                                 |
| Rural mail push               | Era: 1896 (RFD) + Letter bureau                              |
| Saloon bitters line           | A formula ≥ 35% alcohol + a saloon-keeper connection         |
| Regional manager (automation) | A region running steadily for 6 months                       |
| Proprietary Association seat  | Become a top-3 brand in your region                          |
| State lobbying                | Association seat + a legislator connection                   |
| "Go legit" product line       | A low-toxicity formula that still sells well                 |

**Rules**

- Multiple routes to most unlocks (honest vs crooked).
- Some unlocks are **exclusive or costly** (deep medicine shows _or_ deep mail order), so runs diverge.
- **Failure unlocks things** (first death → the coroner relationship; first exposé → the libel lawyer).
- **Unlocks can be lost** (a wholesaler drops you; an editor turns).
- The shop never closes. Once wholesale unlocks, the regulars become the named sample of the wider market.
- Automation (managers, policies) is an unlock, so micromanagement shrinks as the business grows.

---

## 7. Backgrounds (starting characters)

| Background              | Strength                                          | Weakness                                |
| ----------------------- | ------------------------------------------------- | --------------------------------------- |
| The Druggist            | Lab precision, low batch variance, druggist trust | Little capital, weak marketing          |
| The Showman             | Ad and show bonuses, persona charisma             | Sloppy lab work, rural heat             |
| The Disgraced Physician | Instant credibility, medical knowledge            | A past that resurfaces; doctors hostile |
| The Kitchen Remedy      | Trusted by mothers, letter-bureau bonus           | Small scale; temperance expectations    |

Each background has its own event chains and at least one unique ending.

---

## 8. Endings & scoring

Resolved in the epilogue (~1907–1910) from wealth, notoriety, the human ledger, compliance state and relationships. Examples:

- **Respectable:** go legitimate and survive as a mainstream "tonic" or soda maker.
- **Sold out:** sell the company before 1906 at the top.
- **Fought the law:** keep selling under labels, litigate, lobby.
- **Exposed:** ruin, prison or flight (to Canada).
- **Undone by your own product.**

**Legacy screen:** wealth, notoriety, lives lost (named), brands built, a period-style obituary _of you_. Scoring rewards both success and how you got there, so there's more than one way to "win".

---

## 9. Screens: the paper and the proprietor's desk

_Revised: this replaces the earlier 3D-shop station design._

Everything the player touches is printed or written matter, drawn in the Gazette woodcut style. There are two surfaces:

**The paper (the board).** Each turn opens on the new edition. The front page carries:

- the player's own adverts and reading notices;
- testimonials;
- deaths and the coroner's court;
- personals and classifieds (hiring, leads, widows);
- rival advertisements;
- the markets column;
- era news.

Reading it is the payoff. Clicking an item opens its cause in the Ledger, or the decision it calls for.

**The desk (the workbench).** Each workspace is an object on the proprietor's desk:

| On the desk          | What the player does                                                                                                                                         |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **The formula book** | Compose a recipe from the drug shelf; the mixture's properties show as engraved gauge bars                                                                   |
| **The proof sheet**  | Write ad copy and claims, choose a format (reading notice, display ad, testimonial, almanac, trade card), and buy a size and position on next edition's page |
| **The label press**  | Name the product, set the label's claims and the stock cut                                                                                                   |
| **The letters**      | Customer letters (testimonials to buy or forge, complaints, requests for a stronger bottle) and offers                                                       |
| **The envelopes**    | Coroners, editors, wholesalers: the price of each relationship                                                                                               |
| **The Ledger**       | The true books: sales, cash, heat and the human ledger, with causes                                                                                          |
| **The press lever**  | Go to press: end the turn                                                                                                                                    |

As the business grows, more objects land on the desk: the wholesale contract, the city map (regions and papers), the Association letterhead and the unlock tree.

Production art comes from the Gazette woodcut set (see `godot/art/README.md`). Layouts (the front page, ad formats and the almanac) are built from reusable cuts, so new content needs writing, not new drawings.

---

## 10. Art, audio, presentation

### Visual identity: the Gazette

_Revised: this replaces the earlier 3D engraving direction._

- **Black and white first: a sensational weekly, printed from wood.** Black ink on white newsprint is the main theme everywhere: the paper itself, and the desk (white slips on black). Midtones are carved gouges, edges are unevenly inked, and the paper is folded and worn. The model is the sensational press of 1880s–1900s New York, but the papers, products and people are all fictional. (An earlier pink-newsprint version was retired in favour of black and white.)
- **One spot colour: Scheele's green,** the arsenic pigment that poisoned Victorians through wallpaper, dresses and sweets. It is reserved for the product and its pull: the liquid, the rays around a bottle, the tinge in an arsenic-eater's cheeks.
- **Woodtype and book type:** Rye for headlines and IM Fell for text (both SIL OFL).
- **Stock cuts:** one engraving is reused across different products, people and captions. This is period-accurate and a running gag, and it keeps the art budget small.
- **The era changes the print.** Halftone photographs arrive in the late 1890s, and the paper's look modernises as the reckoning approaches.
- **Accessibility:** high-contrast and plain-paper modes, a reduced paper-grain option, and every printed string is real text, never baked into the art.

### Asset strategy (no artist yet)

1. **Now:** original SVG sources, rendered to sprites by `godot/tools/render_art.mjs`, with a shared woodcut style (`tools/build_woodcut.py`). Pipeline and licence log: `godot/art/README.md`.
2. **If the demo earns traction:** hire an artist to repaint the characters and hero cuts in the same rules. The layouts and the pipeline stay.
3. **Always:** a licence entry for every third-party file (fonts and CC0 assets are logged).

### Audio and writing

- **Audio:** parlour piano, banjo, music-hall brass; tactile foley (cork pops, still hiss, pen scratch, the cash register on the Ledger reveal).
- **Writing:** period bombast in ads; plain, restrained prose for consequences.

---

## 11. Content & sensitivity guidelines

- **Punch up.** The joke is on the seller, the trade and the institutions that let it run, never on the victims or their suffering.
- Satire with visible consequences; never reward cruelty without cost.
- Infant soothing syrups exist because they're historically central. They are a heavy choice with severe consequences, presented without spectacle.
- Medicine shows historically exploited Native American and other stereotypes. **We do not reproduce stereotyped depictions.** If referenced, it's framed critically.
- Content warnings at start (drug use, addiction, death including children, historical fraud). Expect a Mature/PEGI 16–18 rating; complete the Steam content survey honestly.
- No real trademarks; real historical figures only as era context.

---

## 12. Accessibility & localisation

- Text-heavy game, so localisation-ready from day one (all strings externalised, ICU message format, no text baked into art). English at launch; FR/DE/ES/PT-BR/ZH/JA/RU as targets.
- UI scaling, dyslexia-friendly font option, colour-blind-safe palettes for all meters, full keyboard control, remappable shortcuts, adjustable auto-advance and animation speed, a "reduce flashing" option for the lab.

---

## 13. Monetisation

Premium one-time purchase (expected $15–$20 range). No microtransactions. Possible post-launch expansions (e.g. a London campaign under the 1868 Pharmacy Act; the post-1906 era to the Harrison Act).

---

## 14. Risk audit (and mitigations)

| Risk                                       | Mitigation                                                                                                                                    |
| ------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Spreadsheet fatigue / opaque causality     | Causal breakdown of every change; diegetic newspapers and letters; hover explanations                                                         |
| Lab becomes a one-time task                | Supply shocks, scrutiny-driven reformulation, new segments, era ingredients, bad batches, the 1906 rule                                       |
| Monthly turns feel slow or spammy          | 2–4 decisions per month via The Post; auto-advance of quiet months                                                                            |
| Statistical deaths lose moral weight       | Named sample customers, event chains, the human ledger, lagging newspaper truth                                                               |
| Dominant strategy (max narcotics + bribes) | Compounding bribe costs, non-linear heat, adaptive reformers, the 1906 deadline                                                               |
| Snowballing / late-game boredom            | Opposition scales with size; late game shifts to politics                                                                                     |
| Early death spiral                         | Loans, forgiving early months, cheap recovery routes                                                                                          |
| Long feedback lag makes learning hard      | Leading indicators, rumours, informants; "you saw it coming" design                                                                           |
| Micromanagement overload                   | Policies, managers as unlocks; caps on regions and products                                                                                   |
| Dry history                                | Accurate systems, flamboyant presentation; hands-on ad copy, medicine shows                                                                   |
| Tone / sensitivity backlash                | Section 11 guidelines; consequences always visible                                                                                            |
| Low replayability                          | Backgrounds, exclusive unlocks, map variation, multiple endings                                                                               |
| Seen as derivative of other games          | The originality rule (section 1); no "X meets Y" messaging; a side-by-side lookalike check before the store page goes public                  |
| The joke wears thin                        | Procedurally assembled obituaries, testimonials, headlines and verdicts; recurring named people; the era changing the paper itself            |
| Writing volume                             | Content as data with schemas; the stock-cut system means new events need text, not art                                                        |
| No artist                                  | A rule-based woodcut style that code-authored art can hit consistently; hire to repaint only if the demo earns it                             |
| The dark path is simply optimal            | Make it high-variance, not dominant: dead customers stop buying, deaths raise heat, withdrawal kills; an honest-ish path is slower but viable |

---

## 15. Open design questions

- Weekly-to-monthly edition cadence (section 3): what exactly triggers the switch, and does the player choose it?
- How is page position priced, and how much layout freedom does the proof sheet give before it becomes busywork?
- How interactive is distillation at factory scale (direct control vs a foreman policy)?
- How much of the regional map is fixed history vs procedural per run?
- How explicit should the human ledger be at the ending?
- Post-1906: how long should the epilogue play?
