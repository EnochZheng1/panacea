# Balance report — content 0.2.0

Seeds `bal2-0..79`, 80 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | p10 final cash | p90 final cash | Cash m6 (median) | Cash m12 (median) | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | -------------- | -------------- | ---------------- | ----------------- | ------------------- | --------------------- | ------ |
| idle       | 100%     | 0%        | 53                               | -$9               | -$9             | -$9            | -$9            | $132             | $114              | 0                   | 12                    | 5      |
| shopkeeper | 0%       | 8%        | 212.5                            | $7,419            | $7,220          | $5,277         | $9,061         | $179             | $232              | 19792.5             | 50                    | 324    |
| cautious   | 0%       | 0%        | —                                | $2,815            | $2,817          | $1,738         | $3,756         | $164             | $187              | 12871.5             | 50                    | 410    |
| greedy     | 8%       | 93%       | 18                               | $253              | $507            | $54            | $1,017         | $234             | $388              | 1645.5              | 50                    | 32     |
| briber     | 4%       | 36%       | 80.5                             | $6,043            | $6,525          | $101           | $15,770        | $200             | $335              | 33995.5             | 50                    | 296    |
| adaptive   | 0%       | 3%        | 184.5                            | $11,148           | $11,158         | $9,778         | $12,712        | $182             | $255              | 26901               | 50                    | 356    |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| idle       | 0             | 0                 | 0.0                  | 0        | 0.0               | 0      | 0         |
| shopkeeper | 30            | 12                | 0.6                  | 11.5     | 4.9               | 0      | 74        |
| cautious   | 25            | 4                 | 0.0                  | 8        | 0.7               | 0      | 22        |
| greedy     | 17            | 16                | 4.6                  | 12       | 6.9               | 6      | 100       |
| briber     | 45            | 28                | 3.5                  | 26.5     | 12.9              | 22.5   | 100       |
| adaptive   | 29            | 9.5               | 0.7                  | 12       | 2.9               | 0      | 62        |

## Unlock reach (share of runs · median month)

| Unlock          | idle | shopkeeper   | cautious   | greedy      | briber       | adaptive     |
| --------------- | ---- | ------------ | ---------- | ----------- | ------------ | ------------ |
| second_still    | —    | 100% · m10   | 100% · m11 | 100% · m5.5 | 100% · m6    | 100% · m8    |
| percolator      | —    | 100% · m9    | 100% · m9  | 99% · m9    | 100% · m9    | 100% · m9    |
| newspaper_ads   | —    | 100% · m2    | 100% · m2  | 100% · m2   | 100% · m2    | 100% · m2    |
| doctor_title    | —    | 94% · m101   | 98% · m119 | 95% · m7    | 53% · m116.5 | 100% · m46   |
| third_still     | —    | 100% · m32   | 100% · m38 | 88% · m12.5 | 100% · m17   | 100% · m21   |
| coroner_fees    | —    | 100% · m42.5 | 100% · m56 | 100% · m8   | 100% · m11   | 100% · m29.5 |
| quality_control | —    | 100% · m39   | 100% · m39 | 48% · m15   | 98% · m35    | 100% · m39   |
| larger_shop     | —    | 100% · m32   | 100% · m35 | 100% · m5   | 100% · m8.5  | 100% · m11   |
| bulk_supplier   | —    | 100% · m24   | 100% · m24 | 28% · m24   | 96% · m24    | 100% · m24   |
