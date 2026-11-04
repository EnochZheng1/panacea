# Balance report — content 0.3.0

Seeds `bal3-0..79`, 80 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | p10 final cash | p90 final cash | Cash m6 (median) | Cash m12 (median) | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | -------------- | -------------- | ---------------- | ----------------- | ------------------- | --------------------- | ------ |
| idle       | 100%     | 0%        | 53                               | -$9               | -$9             | -$9            | -$9            | $132             | $114              | 0                   | 12                    | 7      |
| shopkeeper | 0%       | 0%        | —                                | $7,275            | $7,129          | $4,718         | $8,823         | $178             | $228              | 19403.5             | 50                    | 426    |
| cautious   | 0%       | 0%        | —                                | $2,410            | $2,401          | $1,357         | $3,500         | $161             | $184              | 11712               | 50                    | 542    |
| greedy     | 5%       | 49%       | 32                               | $5,396            | $5,742          | $252           | $11,576        | $190             | $295              | 16985.5             | 50                    | 236    |
| briber     | 0%       | 10%       | 138                              | $11,278           | $10,488         | $5,747         | $13,512        | $195             | $302              | 28611               | 50                    | 364    |
| adaptive   | 0%       | 8%        | 176.5                            | $10,082           | $10,017         | $8,411         | $11,765        | $171             | $231              | 25066               | 50                    | 339    |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| idle       | 0             | 0                 | 0.0                  | 0        | 0.0               | 0      | 0         |
| shopkeeper | 29            | 11                | 0.8                  | 11       | 3.9               | 0      | 54        |
| cautious   | 20.5          | 3                 | 0.0                  | 7        | 0.6               | 0      | 13        |
| greedy     | 38.5          | 21                | 3.8                  | 28.5     | 10.7              | 27.5   | 100       |
| briber     | 51.5          | 34.5              | 3.2                  | 33       | 14.6              | 32     | 100       |
| adaptive   | 31            | 15                | 0.5                  | 15       | 6.6               | 0      | 82        |

## Unlock reach (share of runs · median month)

| Unlock          | idle | shopkeeper   | cautious   | greedy     | briber     | adaptive   |
| --------------- | ---- | ------------ | ---------- | ---------- | ---------- | ---------- |
| second_still    | —    | 100% · m10   | 100% · m12 | 100% · m6  | 100% · m7  | 100% · m8  |
| percolator      | —    | 100% · m9    | 100% · m9  | 98% · m9   | 100% · m9  | 100% · m9  |
| newspaper_ads   | —    | 100% · m2    | 100% · m2  | 100% · m2  | 100% · m2  | 100% · m2  |
| doctor_title    | —    | 96% · m88    | 89% · m129 | 99% · m8   | —          | 98% · m48  |
| third_still     | —    | 100% · m33   | 100% · m40 | 95% · m15  | 100% · m19 | 100% · m23 |
| coroner_fees    | —    | 100% · m47   | 100% · m59 | 100% · m10 | 100% · m11 | 100% · m38 |
| quality_control | —    | 100% · m39   | 100% · m39 | 70% · m39  | 100% · m39 | 100% · m39 |
| larger_shop     | —    | 100% · m34.5 | 100% · m41 | 99% · m7   | 100% · m10 | 100% · m13 |
| bulk_supplier   | —    | 100% · m24   | 100% · m24 | 76% · m24  | 100% · m24 | 100% · m24 |
