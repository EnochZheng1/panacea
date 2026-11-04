# Balance report — content 0.4.0

Seeds `r4-0..79`, 80 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | p10 final cash | p90 final cash | Cash m6 (median) | Cash m12 (median) | Cash m48 (median) | Cash m120 (median) | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | -------------- | -------------- | ---------------- | ----------------- | ----------------- | ------------------ | ------------------- | --------------------- | ------ |
| idle       | 100%     | 0%        | 53                               | -$9               | -$9             | -$9            | -$9            | $132             | $114              | $6                | -$9                | 0                   | 12                    | 6      |
| shopkeeper | 0%       | 3%        | 185.5                            | $13,925           | $13,111         | $9,318         | $15,747        | $147             | $217              | $324              | $4,736             | 47993.5             | 70                    | 244    |
| cautious   | 0%       | 0%        | —                                | $6,250            | $6,187          | $4,791         | $7,251         | $128             | $165              | $274              | $1,678             | 38297               | 70                    | 206    |
| greedy     | 4%       | 65%       | 119                              | $14,987           | $15,363         | $3,655         | $26,776        | $143             | $116              | $869              | $8,558             | 17953.5             | 70                    | 132    |
| briber     | 8%       | 44%       | 124                              | $9,529            | $8,915          | $99            | $16,537        | $154             | $286              | $1,043            | $5,137             | 36980               | 70                    | 171    |
| adaptive   | 0%       | 0%        | —                                | $17,476           | $16,981         | $12,911        | $20,488        | $137             | $210              | $1,001            | $6,594             | 50593.5             | 70                    | 201    |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| idle       | 0             | 0                 | 0.0                  | 0        | 0.0               | 0      | 0         |
| shopkeeper | 42.5          | 10                | 1.0                  | 15       | 6.8               | 0      | 58        |
| cautious   | 39            | 7                 | 0.0                  | 13       | 0.9               | 0      | 33        |
| greedy     | 22            | 8                 | 0.3                  | 17       | 5.3               | 16     | 100       |
| briber     | 62.5          | 44.5              | 2.9                  | 43.5     | 19.2              | 41.5   | 100       |
| adaptive   | 33            | 7                 | 0.4                  | 13       | 2.7               | 0      | 60        |

## Unlock reach (share of runs · median month)

| Unlock          | idle | shopkeeper | cautious     | greedy      | briber     | adaptive   |
| --------------- | ---- | ---------- | ------------ | ----------- | ---------- | ---------- |
| second_still    | —    | 100% · m8  | 100% · m10   | 99% · m11   | 100% · m6  | 100% · m8  |
| percolator      | —    | 100% · m9  | 100% · m9    | 100% · m9   | 100% · m9  | 100% · m9  |
| newspaper_ads   | —    | 100% · m2  | 100% · m2    | 100% · m3   | 100% · m2  | 100% · m2  |
| doctor_title    | —    | 100% · m60 | 100% · m61.5 | 100% · m7.5 | —          | 100% · m45 |
| third_still     | —    | 100% · m26 | 100% · m31   | 96% · m27   | 99% · m17  | 100% · m21 |
| coroner_fees    | —    | 100% · m27 | 100% · m42   | 99% · m15   | 100% · m10 | 100% · m25 |
| quality_control | —    | 100% · m39 | 100% · m39   | 96% · m39   | 95% · m39  | 100% · m39 |
| larger_shop     | —    | 100% · m18 | 100% · m21   | 96% · m20   | 100% · m9  | 100% · m11 |
| bulk_supplier   | —    | 100% · m24 | 100% · m24   | 96% · m24   | 99% · m24  | 100% · m24 |

## Investments bought (share of runs · median month first bought)

| Investment         | idle | shopkeeper | cautious   | greedy    | briber    | adaptive     |
| ------------------ | ---- | ---------- | ---------- | --------- | --------- | ------------ |
| branch_shop        | —    | 100% · m46 | 100% · m69 | 96% · m39 | 99% · m31 | 100% · m36   |
| almanac            | —    | 100% · m3  | 100% · m3  | —         | 100% · m3 | 100% · m3    |
| wholesale_contract | —    | 100% · m34 | 100% · m41 | —         | 64% · m23 | 100% · m27.5 |
| laboratory         | —    | —          | —          | 96% · m31 | —         | —            |
