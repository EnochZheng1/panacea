# Panacea · balance pass 5 · ventures within a hand player's reach

Content 0.4.0 → **0.5.0**. Both sims changed together (TypeScript reference and the C# port), the parity
fixtures were regenerated (now 28 runs, with the new `sloppy` bot), and `dotnet test` is green.

This pass answers the round-5 finding that six of eight testers never got past the $40 almanac
([SUMMARY.md](SUMMARY.md), [tycoon-veteran.md](tycoon-veteran.md) findings 1–4). It adds a bot that plays the
way the testers did and tunes the ventures against it. It also turns the wholesale contract and the almanac into real
decisions, and gives the prospectus numbers for each venture's progress.

All tables are 80 runs per bot, seeds `r5b-0..79`, unless marked otherwise. "Before" is commit `b259626` (content
0.4.0) with the same `sloppy` bot copied in, so the two columns compare like with like.

## What changed

### 1. A `sloppy` bot that plays like the testers (`packages/playtest/src/bots/sloppy.ts`, in `BOTS`)

- **The bottle.** It picks a price from {60, 65, 70, 75}c and a laudanum dose from {0.15 … 0.35} oz once per
  game, with 2 oz spirit, ½ oz valerian and the usual flavouring. It never reformulates. Its claims are "Soothes the
  Nerves" and "Banishes Rheumatism" once allowed.
- **Stock by eye.** It keeps a shelf target and corrects it by hand. After a month with plenty left over, it lowers
  the target. After a bare shelf, or after the paper says the orders went short, it raises the target, but only 40%
  of the time, and only by 10–25%. It adds the orders from away that the paper printed, with ±20% noise, and brews
  in dozens, up to 48 a still. In 8% of months it forgets the still. So it under-stocks one month and over-stocks
  the next, as the testers did.
- **Letters.** Inquests get testimony or silence, half and half. Other letters get the plain (default) answer 70%
  of the time and a random answer otherwise. That includes the $50 diploma, the $3 inspector, the $60 lease and so
  on.
- **Advertising.** A quarter or a half column, picked once per game, while cash is above $60.
- **Ventures.** It buys the cheapest venture on offer whenever $60 plus six months of rent and upkeep would be left
  after the purchase. It buys everything but the laboratory, and renews the almanac when it lapses.

Its first two years in the **old** game, against the hand reports:

| Measure                    | Hand reports (round 5)                                                                             | `sloppy`, old game (80 runs)              |
| -------------------------- | -------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| Cash at month 9–12         | $160 (tycoon, m9), $197 (lapsed, m10), $219 (period, m10), $216 (careful, m11), $220 (casual, m11) | **$196** at m12 (p25–p75 $175–$216)       |
| Cash at month 24           | "$150–300"; the jump-ahead proprietor had $160–$245 at m24–26                                      | **$326** (p25–p75 $286–$367)              |
| Net a month                | "$10–28" (best months); +$13–20 excluding outlays (tycoon)                                         | $4 (months 1–12), **$12** (months 13–24)  |
| Bottles sold by month 9–12 | 216 (tycoon, 9 months) to ~530 (period, 10 months)                                                 | 297 by m12                                |
| Regulars at month 9–12     | 15 (tycoon) to 30 (lapsed)                                                                         | 17                                        |
| Ventures                   | Only the almanac by hand; wholesale "at month 35–45"; branch "year three or four"                  | almanac m3, wholesale **m35**, branch m48 |

At month 24 it sits at the top of the band. The jump-ahead proprietor that the m24 figures come from is a weaker
player than a person: one still, a fixed order, and the first reply to every letter. I took this as a fair stand-in
for a hand player.

### 2. Ventures within reach: a first rung, and a lower bar for the contract (`investments.json`)

| Venture                                                  | Before                                                   | After                                                                                                                                              |
| -------------------------------------------------------- | -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`druggists_order`** (new): A Druggist's Standing Order | —                                                        | **$100**, offered at **400 bottles sold** while suspicion is under 30; lost at 50; 16 bottles a month (+2% a point of name) at 55% of shelf, ≤ 50c |
| `wholesale_contract`                                     | $360, 1,500 bottles, 36 a month                          | **$300**, **1,200 bottles**, 32 a month                                                                                                            |
| `almanac`                                                | $40/12 months, 10 callers, heat 0.10 a point of boldness | $40/12 months, 10 callers, **heat 0.15**, and **orders by post: 12 bottles a month at the shelf price**                                            |
| `branch_shop`, `laboratory`                              | —                                                        | unchanged                                                                                                                                          |

Why a first rung, and not lower costs across the board? A hand shop makes $10–15 a month. A $300–600 venture
is two to four years of savings, and a cheaper contract on the same terms would only bring the bots' purchases
forward. A $100 rung at 400 bottles costs about ten months of savings. A hand shop reaches it at month 13–17 and
pays it back in about 20 months (see the table below). The big contract then follows in year three. I didn't
use instalment terms or costs that scale with the shop. Both bring the purchase forward without making it pay any
better, and an instalment of $10–20 a month would eat the whole income of the shops it is meant for.

### 3. Each venture a decision

**Wholesale: the price has a ceiling, and the orders draw on the shelf.**

- **Price sensitivity** (new `refPriceCents`, `priceElasticity`, `maxOrderBoost` on every order spec). The druggists
  sell your bottle on at your shelf price, so they order fewer the dearer it is. The order is multiplied by
  1 + 1.2 × (60 − price) / 60, between 0 and 1.5. At 90c they order 40% of the base and pay the 50c cap; at 110c they
  order nothing. What the contract earns now peaks near 70c and falls after it. On top of branch, druggist and
  almanac, the contract adds this much to adaptive's final cash (40 runs): **60c +$2.3k, 70c +$3.1k, 90c +$1.9k,
  110c −$0.6k**.
- **Orders tie up the shelf.** Orders from away (the druggists and the post) are now filled after the regulars and
  before the walk-ins, from at most half of what the regulars left (`config.market.ordersShelfShare` 0.5). A shop
  that holds contracts on a thin shelf loses walk-in sales and the regulars they would have become. All the orders
  draw on the same half, so stacking them needs more stock. I tried filling them first, before the regulars (the
  suggestion in the brief). For `sloppy` the contract alone then lost money (−$236), and holding all three ventures
  cost $1.6k against holding none. Draining the regulars compounds too hard. The half-share rule keeps the downside
  real but bounded.
- The heat ceiling (offered under 30, lost at 50) still keeps heavy lines out. It now also bites honest lines that
  combine the contract with the almanac's print or a big branch (below).

**The almanac pays for itself, and its print is a real risk.** It still brings ten callers. Its readers now also send
for about a dozen bottles a month by post, at the shelf price, fewer the dearer it is (refPrice 60c, elasticity 1).
They are strangers with no tolerance (health 65), so a strong bottle kills some of them. The print's heat rises from
0.10 to 0.15 a point of boldness.

| Almanac on its own (Δ final cash against no ventures)                        | Before                | After                       |
| ---------------------------------------------------------------------------- | --------------------- | --------------------------- |
| sloppy                                                                       | $0, payback 40 months | **+$924, payback 8 months** |
| cautious                                                                     | +$554                 | +$2,012                     |
| shopkeeper                                                                   | +$575                 | +$2,193                     |
| adaptive                                                                     | +$331                 | +$1,710                     |
| greedy, laboratory + almanac: early endings (laboratory alone, after: 37/80) | 43/80                 | **47/80**                   |
| greedy, laboratory + branch + almanac: early endings                         | 72/80, median $7.0k   | **78/80**, median $4.1k     |

For an honest bottle it is now the quickest payback in the prospectus. For a bold label it raises the shut-downs. For
`sloppy`, the almanac's print pushes suspicion past 50 often enough to lose the contracts about twice a game, and they
must then be bought again.

### 4. Progress for the prospectus (machine API, both sims)

Every `ObservedInvestment` now carries `progress`: one line per condition of its requirement, met or not, in content
order, followed by its price against the cash in hand. The list is empty while the venture is held. An `unlocked`
condition that is not yet met is followed by that unlock's own conditions, tagged `via`. For example, the branch's
"cobbler's shop" lists 18 of 25 regulars and $150 of $200. A `not heat ≥ 30` reads as a ceiling.

C# (`Panacea.Sim`):

```csharp
// Systems/Progression.cs
public sealed record ConditionProgress(
    string Kind,    // "bottlesSold", "regulars", "cash", "heat", "unlocked", "flag", "date", …; "not" (a negated
                    // non-threshold); "cost" (the venture's price against the cash in hand)
    string? Id,     // the unlock or flag waited on ("unlocked"/"flag"), else null
    string Sense,   // "atLeast": met once Have ≥ Need; "below": met while Have < Need (a ceiling)
    double Have,    // cash/cost in cents; unlocked/flag 1 or 0; date a month index (Dates.Index)
    double Need,
    bool Met,
    string? Via);   // set on the conditions of an unlock that is itself waited on: that unlock's id

// Machine.cs, ObservedInvestment (new members)
int? PostBottles                              // orders by post a month at the present shelf price (almanac)
int? PostPriceCents                           // what each brings
IReadOnlyList<ConditionProgress> Progress     // last positional member
// Requirements.Progress(Requirement, GameState, ContentPack, string? via = null) builds the lines.
```

Example, a fresh game with 1,120 bottles sold and suspicion 12:
`wholesale_contract.Progress` = `[bottlesSold atLeast 1120/1200 ✗, heat below 12/30 ✓, cost atLeast 15000/30000 ✗]`.

TypeScript mirrors it as `ConditionProgress` (`packages/sim/src/systems/requirements.ts`) and
`ObservedInvestment.progress`, `postBottles` and `postPriceCents`.

### Other plumbing

- **Investment schema.** The `wholesale` block and the new `postOrders` block share `OrdersSchema`. C#:
  `WholesaleSpec.RefPriceCents`, `PriceElasticity` and `MaxOrderBoost`; `Investment.PostOrders`. The signatures
  changed to `Investments.WholesaleOrder(state, spec, int? priceCents)` and
  `Investments.WholesalePrice(spec, priceCents)`. There is a new `Investments.OrderPriceFactor`.
- **Ledger and month.** A new `post` record (`WholesaleMonth`: ordered, delivered, revenue) sits beside `wholesale`,
  which now holds the druggists only, both the standing order and the contract. There is a new cause `sales.post`,
  "Orders by post". `wholesaleBottles` now reflects the shelf price.
- **Batch report.** It gains a "Cash m24" column. The optimizer's invest sets include `druggists_order`.

## Before and after (80 runs, `--seed r5b`)

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Early endings (bankrupt + shut), before → after | Cash m12    | Cash m24    | Cash m48        |
| ---------- | ---------------------------- | ----------------- | ----------------- | ----------------------------------------------- | ----------- | ----------- | --------------- |
| cautious   | $5,963 → **$6,754**          | $5,836 → $6,598   | $5,367 / $7,829   | 0% → 0%                                         | $162 → $196 | $221 → $229 | $286 → $350     |
| shopkeeper | $13,392 → **$15,170**        | $12,664 → $14,196 | $10,640 / $16,822 | 6% → 4%                                         | $214 → $250 | $362 → $317 | $411 → $452     |
| adaptive   | $17,905 → **$19,414**        | $17,209 → $18,722 | $13,895 / $21,924 | 0% → 1%                                         | $213 → $218 | $470 → $243 | $952 → $1,211   |
| briber     | $8,437 → **$11,930**         | $8,727 → $10,552  | $1,383 / $17,292  | 47% → 51%                                       | $284 → $235 | $552 → $457 | $1,010 → $1,136 |
| greedy     | $14,844 → **$14,844**        | $15,447 → $15,447 | −$6 / $27,349     | 70% → 70%                                       | $114 → $114 | $311 → $311 | $982 → $982     |
| sloppy     | $9,936 → **$10,117**         | $9,585 → $10,066  | $7,462 / $12,202  | 3% → 1%                                         | $196 → $224 | $326 → $310 | $470 → $572     |

- **Adaptive** has a median of $19.4k, inside the $15–20k band. The honest lines gain 8–13%, so steady play doesn't
  run away.
- **Greedy** is unchanged to the cent: it buys none of the changed ventures. Its p90 of $27.3k is 1.41× adaptive's
  median, with 70% early endings. The heavy route keeps its high variance and its jackpot for survivors.
- **Briber** gains $3.5k from the almanac's post and the first rung, which it reaches before its heat shuts it out.
  It still ends early in half its games.
- **Cautious and shopkeeper** stay viable and both improve. Cautious's m18 and m36 dips are the $100 and $300
  outlays.
- **Sloppy's monthly net, excluding outlays**, rises from $7 to $10 (months 1–12), $15 to $23 (13–24), $25 to $41
  (25–36) and $39 to $46 (37–48). Cash at m12 is $224 and at m24 $310, still near the hand band.

Human cost barely moves. Median product-caused deaths: cautious 7 → 6, shopkeeper 10 → 10, adaptive 6 → 6, sloppy
22.5 → 20.5.

## When each bot takes up its ventures (share of runs · median month first bought)

| Venture            | cautious      | shopkeeper    | adaptive      | briber          | greedy    | sloppy, before → after |
| ------------------ | ------------- | ------------- | ------------- | --------------- | --------- | ---------------------- |
| almanac            | m3            | m3            | m3            | m3              | —         | 85% · m3 → 85% · m3    |
| druggists_order    | **m14**       | **m13**       | **m12**       | 93% · m10       | —         | — → **100% · m13**     |
| wholesale_contract | m48 → **m33** | m35 → **m24** | m27 → **m23** | 44% → 61% · m21 | —         | m35 → **m26**          |
| branch_shop        | m79.5 → m62   | m47 → m41     | m36 → m34     | m31 → m30       | 89% · m38 | m48 → m41              |
| laboratory         | —             | —             | —             | —               | 90% · m30 | —                      |

`sloppy`'s **n-th venture**, before → after:

- First: m3 → m3.
- Second: m35 → **m13**, in year 2 (p25–p75 m12–15).
- Third: m47 → **m26**, in year 3 (p25–p75 m24–31).
- Fourth: none → m40 (78% of runs).

## Payback per venture

Each venture is bought alone against the same bot with no ventures, on paired seeds (80 runs).

- **Payback:** the months after purchase until the median cash gap is back to zero.
- **Δ final:** the median gain in final cash.

| Venture            | sloppy, before → after            | cautious, before → after                   | shopkeeper, before → after      | adaptive, before → after            |
| ------------------ | --------------------------------- | ------------------------------------------ | ------------------------------- | ----------------------------------- |
| almanac            | 40 mo, $0 → **8 mo, +$924**       | 41 mo, +$554 → 8 mo, +$2,012               | 12 mo, +$575 → 7 mo, +$2,193    | 18 mo, +$331 → 9 mo, +$1,710        |
| druggists_order    | — → **20 mo, +$1,289**            | — → 31 mo, +$1,592                         | — → 24 mo, +$1,779              | — → 20 mo, +$2,067                  |
| wholesale_contract | 44 mo, +$627 → **27 mo, +$1,097** | 36 mo, **+$2,776 (2.2×)** → 69 mo, +$1,346 | 29 mo, +$3,551 → 37 mo, +$2,241 | 21 mo, **+$4,658** → 25 mo, +$3,148 |
| branch_shop        | 82 mo, +$1,256 (unchanged)        | 106 mo, +$333 (unchanged)                  | 64 mo, +$2,025 (unchanged)      | 44 mo, +$3,081 (unchanged)          |
| _no ventures_      | $10,052                           | $2,374                                     | $7,387                          | $10,144                             |

- **Wholesale is no longer the outsized buy.** Before, it was worth 1.5–4× any other venture, and twice the shop's
  whole income for cautious. Now it is in line with the others for every line, its p25 for shopkeeper is only +$470,
  and above about 100c it loses money.
- **Payback by type.** The cheap ventures (almanac, standing order) pay back inside two years for a hand player. The
  big ones take two to three years, as the design says.
- **Interactions** make the full set less than the sum of its parts. For `sloppy` (40 runs), the almanac,
  standing order and contract together are +$1.1k. Adding the branch takes it to −$0.1k: the branch's extra regulars
  bring more deaths (median 50 against 32), suspicion peaks at about 100, and the contracts are cancelled six times.
  A hand player who buys everything on offer is about where one who bought nothing is. One who matches the venture
  to the shop is $1–1.5k ahead. That's the intended shape: a portfolio, not a checklist.

## The exploit hunter

`pnpm sim:optimize --candidates 120 --seeds 4 --seed r5b`:

- **Best line:** no narcotic, 2.9 oz alcohol, capsicum and gentian, **101c**, no ads, the four bold claims, silent at
  inquests, every venture. It averages **$17,606**, which is **0.94× adaptive's mean** ($18,722). Its correlation
  between wealth and product deaths is 0.16.
- **60 candidates, seed `search`** (the default search): best $17,454, against $16,803 before (0.98× before, 0.93×
  now).
- **Targeted.** The round-5 tycoon's strongest line was adaptive at 90c, at 1.23× adaptive. It is now **$21,284
  against $19,036** (40 runs), **1.12×**; before, on these seeds, it was $22,761 against $17,635, 1.29×. What is
  left of the gain is the shop's own price response, not the druggists. Adaptive at 100c is $18.2k and at 110c
  $15.4k.

## Notes for the desk (`godot/`, not touched)

- **Prospectus.** Its "Offered after…" text needs a `druggists_order` line ("four hundred bottles sold, while the
  talk in the ward is low"), and wholesale's "fifteen hundred" is now **twelve hundred**. The almanac card should add
  its post (`PostBottles`, `PostPriceCents`), and the wholesale cards could say that the druggists order fewer the
  dearer the bottle. The cancel wording ("runs until the druggists cancel") already keys off `WholesaleBottles`, so it
  covers the standing order.
- **The Trade.** `report.Wholesale` now covers the druggists only. Readers' orders are in `report.Post`
  (Ordered/Delivered/RevenueCents), with the cause `sales.post`. EditionWriter subtracts `WholesaleDelivered` from
  the shop's count, and should subtract `Post.Delivered` too. Otherwise post bottles read as sold at the shop.
- **Stock hint.** The tycoon's finding 5 (a "the druggists want N" hint) matters more now. Orders take up to half of
  what the regulars leave, before the walk-ins.
