# Balance report — content 0.2.0

Seeds `r2fix-0..79`, 80 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | p10 final cash | p90 final cash | Cash m6 (median) | Cash m12 (median) | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | -------------- | -------------- | ---------------- | ----------------- | ------------------- | --------------------- | ------ |
| idle       | 100%     | 0%        | 53                               | -$9               | -$9             | -$9            | -$9            | $132             | $114              | 0                   | 12                    | 3      |
| shopkeeper | 0%       | 4%        | 244                              | $7,816            | $7,423          | $5,122         | $8,807         | $178             | $228              | 19909.5             | 50                    | 286    |
| cautious   | 0%       | 0%        | —                                | $3,044            | $2,994          | $1,646         | $4,128         | $163             | $190              | 12720.5             | 50                    | 357    |
| greedy     | 3%       | 57%       | 22                               | $1,387            | $9,656          | $227           | $25,684        | $249             | $452              | 38358               | 50                    | 237    |
| briber     | 9%       | 14%       | 84.5                             | $11,182           | $9,141          | -$10           | $17,088        | $205             | $353              | 36171               | 50                    | 292    |
| adaptive   | 0%       | 0%        | —                                | $11,630           | $11,470         | $9,304         | $13,160        | $177             | $270              | 26823               | 50                    | 334    |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| idle       | 0             | 0                 | 0.0                  | 0        | 0.0               | 0      | 0         |
| shopkeeper | 30            | 11                | 0.8                  | 13       | 5.0               | 0      | 70        |
| cautious   | 25            | 4                 | 0.0                  | 8        | 0.6               | 0      | 20        |
| greedy     | 61.5          | 40.5              | 7.3                  | 35.5     | 7.0               | 31     | 100       |
| briber     | 49.5          | 32.5              | 3.9                  | 29       | 3.5               | 25     | 81        |
| adaptive   | 33            | 11                | 0.7                  | 13       | 3.6               | 0      | 66        |

## Unlock reach (share of runs · median month)

| Unlock          | idle | shopkeeper   | cautious   | greedy    | briber     | adaptive     |
| --------------- | ---- | ------------ | ---------- | --------- | ---------- | ------------ |
| second_still    | —    | 100% · m10   | 100% · m11 | 100% · m5 | 100% · m6  | 100% · m7    |
| percolator      | —    | 100% · m9    | 100% · m9  | 96% · m9  | 100% · m9  | 100% · m9    |
| newspaper_ads   | —    | 100% · m2    | 100% · m2  | 100% · m2 | 100% · m2  | 100% · m2    |
| doctor_title    | —    | 100% · m103  | 90% · m117 | 98% · m7  | 85% · m70  | 99% · m46    |
| third_still     | —    | 100% · m32.5 | 100% · m37 | 95% · m12 | 99% · m16  | 100% · m20   |
| coroner_fees    | —    | 100% · m35.5 | 100% · m49 | 100% · m7 | 100% · m11 | 100% · m24.5 |
| quality_control | —    | 100% · m39   | 100% · m39 | 78% · m27 | 96% · m39  | 100% · m39   |
| larger_shop     | —    | 100% · m34.5 | 100% · m33 | 99% · m5  | 100% · m8  | 100% · m10   |
| bulk_supplier   | —    | 100% · m24   | 100% · m24 | 69% · m24 | 96% · m24  | 100% · m24   |
