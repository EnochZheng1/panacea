# Balance report — content 0.4.0

Seeds `r5-0..79`, 80 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | p10 final cash | p90 final cash | Cash m6 (median) | Cash m12 (median) | Cash m48 (median) | Cash m120 (median) | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | -------------- | -------------- | ---------------- | ----------------- | ----------------- | ------------------ | ------------------- | --------------------- | ------ |
| cautious   | 1%       | 0%        | 107                              | $5,672            | $5,482          | $3,901         | $6,798         | $127             | $163              | $291              | $1,260             | 36157               | 70                    | 194    |
| shopkeeper | 0%       | 3%        | 187.5                            | $12,886           | $12,170         | $7,983         | $15,358        | $144             | $212              | $485              | $4,314             | 45698               | 70                    | 168    |
| adaptive   | 0%       | 0%        | —                                | $17,483           | $16,775         | $12,629        | $19,996        | $137             | $210              | $950              | $6,695             | 50928               | 70                    | 185    |
| briber     | 6%       | 31%       | 127                              | $9,704            | $9,224          | $168           | $15,815        | $154             | $286              | $1,013            | $4,920             | 38567.5             | 70                    | 165    |
| greedy     | 5%       | 57%       | 107.5                            | $16,142           | $15,791         | $2,566         | $27,852        | $147             | $131              | $975              | $8,497             | 18512               | 70                    | 119    |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| cautious   | 40            | 7                 | 0.0                  | 14       | 0.9               | 0      | 31        |
| shopkeeper | 44            | 13                | 1.0                  | 17       | 8.3               | 0      | 81        |
| adaptive   | 35            | 8                 | 0.6                  | 14       | 3.2               | 0      | 60        |
| briber     | 64.5          | 45                | 3.6                  | 46.5     | 21.4              | 44.5   | 100       |
| greedy     | 24            | 10                | 0.3                  | 17       | 6.1               | 15.5   | 100       |

## Unlock reach (share of runs · median month)

| Unlock          | cautious     | shopkeeper   | adaptive   | briber     | greedy      |
| --------------- | ------------ | ------------ | ---------- | ---------- | ----------- |
| second_still    | 100% · m10   | 100% · m8.5  | 100% · m8  | 100% · m6  | 99% · m11   |
| percolator      | 100% · m9    | 100% · m9    | 100% · m9  | 100% · m9  | 100% · m9   |
| newspaper_ads   | 100% · m2    | 100% · m2    | 100% · m2  | 100% · m2  | 100% · m3   |
| doctor_title    | 100% · m62.5 | 99% · m66    | 100% · m45 | —          | 100% · m7.5 |
| third_still     | 100% · m31   | 100% · m26   | 100% · m21 | 99% · m17  | 95% · m27   |
| coroner_fees    | 100% · m39   | 100% · m30.5 | 100% · m28 | 100% · m11 | 99% · m16   |
| quality_control | 100% · m39   | 100% · m39   | 100% · m39 | 98% · m39  | 95% · m39   |
| larger_shop     | 100% · m22   | 100% · m19   | 100% · m11 | 100% · m9  | 95% · m18   |
| bulk_supplier   | 100% · m24   | 100% · m24   | 100% · m24 | 99% · m24  | 95% · m24   |

## Investments bought (share of runs · median month first bought)

| Investment         | cautious   | shopkeeper | adaptive     | briber    | greedy    |
| ------------------ | ---------- | ---------- | ------------ | --------- | --------- |
| branch_shop        | 100% · m75 | 100% · m48 | 100% · m36.5 | 99% · m31 | 95% · m39 |
| almanac            | 100% · m3  | 100% · m3  | 100% · m3    | 100% · m3 | —         |
| wholesale_contract | 100% · m48 | 100% · m35 | 100% · m27   | 43% · m24 | —         |
| laboratory         | —          | —          | —            | —         | 95% · m30 |
