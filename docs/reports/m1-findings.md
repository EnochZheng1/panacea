# M1 balance findings

Source data: [m1-balance.md](m1-balance.md) (8 bots × 50 seeds) and [m1-optimizer.md](m1-optimizer.md) (120 random strategies × 4 seeds). Content version 0.2.0.

## What M1 shows

**The core trade-off works.** Strategies separate the way the design intends:

| Strategy                                                                        | Median final cash | Product deaths (median) | Outcome                                 |
| ------------------------------------------------------------------------------- | ----------------- | ----------------------- | --------------------------------------- |
| `adaptive`: raises the laudanum when customers grumble, cuts it when heat rises | $8,654            | 7                       | Best performer; rarely shut down (6%)   |
| `shopkeeper`: fixed modest laudanum tonic                                       | $6,440            | 8                       | Steady; survives to 1906 in 98% of runs |
| `briber`: strong tonic, buys off every inquest                                  | $1,108            | 34                      | Bribes escalate; 32% go bankrupt        |
| `cautious`: no narcotics                                                        | $193              | 1                       | Viable but marginal; 24% bankrupt       |
| `greedy`: morphine, cocaine, wild claims, escalation                            | $29               | 27                      | Shut down within ~4 months              |
| `mercurial`: cheap calomel purgative                                            | bankrupt          | 3                       | Slow mercury deaths and a thin margin   |

- **Tolerance escalation is real.** Regulars on a fixed dose grumble as tolerance builds; the strategy that manages dose against heat wins.
- **Overdoing it is punished fast.** Strong morphine kills customers in the first months and the authorities close the shop.
- **Bribery is a trap at scale.** The coroner's fee compounds with each bribe and with heat.
- **The honest shop survives but barely earns.** This matches the premise (customers want narcotic punch), but the planned "go legit" path needs better tools (aspirin from 1899 is the first; more in M2+).

## Open balance issues (for M2)

1. **Wild claims are the strongest lever.** 7 of the optimizer's top 10 strategies print every outrageous claim. Label claims now draw heat even without advertising, but the cost is still too low before 1905. Planned counterweights: newspapers and the red clause (M2), the 1905 exposé raising scrutiny of claim-heavy brands, and the 1906 labelling rule.
2. **Deadliness is mildly rewarded.** The correlation between wealth and product deaths across 120 strategies is 0.32. Target: near zero or negative, so the richest path is not the deadliest. Levers: heat from deaths, implication odds, and customer loss when a regular dies.
3. **`cautious` is close to break-even.** Fine for now; revisit with the legit product line.
4. **Shopkeeper shutdowns** fell from 17–25% to 2% after complaint heat was lowered from 0.5 to 0.2. Watch this as more heat sources arrive.

## Tuning changes made during M1

| Change                                                       | Why                                                      |
| ------------------------------------------------------------ | -------------------------------------------------------- |
| Alcohol habit 1.0 → 0.4 per oz                               | Every regular became heavily dependent on spirits alone  |
| Complaint reputation −0.5 → −0.15; word of mouth 0.05 → 0.08 | Reputation collapsed to 0 for every strategy             |
| Ailment severities lowered by ~1                             | Non-narcotic tonics could never satisfy anyone           |
| Added valerian root                                          | Honest tonics had no way to calm "nerves"                |
| Reputation drifts toward a baseline of 15                    | Small shops had no way to recover their name             |
| Shutdown after 2 consecutive months at heat 100              | Shops were closed with no warning                        |
| Natural-looking deaths raise half the heat                   | Old-age deaths drove as much talk as poisonings          |
| Label claims draw heat (0.3/boldness/month)                  | Wild claims were free without advertising                |
| Complaint heat 0.5 → 0.2                                     | Complaints, not deaths, were the main cause of shutdowns |
