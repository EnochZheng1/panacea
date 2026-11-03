# Balance report — content 0.2.0

Seeds `m1-0..49`, 50 runs per bot.

## Business

| Bot        | Bankrupt | Shut down | Median end month (early endings) | Median final cash | Mean final cash | Median bottles sold | Median peak customers | ms/run |
| ---------- | -------- | --------- | -------------------------------- | ----------------- | --------------- | ------------------- | --------------------- | ------ |
| idle       | 100%     | 0%        | 28                               | -$15              | -$15            | 0                   | 12                    | 2      |
| random     | 100%     | 0%        | 16                               | -$13              | -$13            | 0                   | 12                    | 2      |
| shopkeeper | 0%       | 2%        | 154                              | $6,440            | $6,616          | 17523               | 50                    | 83     |
| cautious   | 24%      | 0%        | 186.5                            | $193              | $184            | 4063                | 24                    | 70     |
| greedy     | 32%      | 68%       | 4                                | $29               | $24             | 34                  | 10                    | 2      |
| briber     | 32%      | 16%       | 99                               | $1,108            | $3,375          | 24303               | 50                    | 72     |
| adaptive   | 0%       | 6%        | 230                              | $8,654            | $8,776          | 22393.5             | 50                    | 95     |
| mercurial  | 100%     | 0%        | 68.5                             | -$13              | -$13            | 527.5               | 13                    | 16     |

## Human cost and scrutiny

| Bot        | Median deaths | Caused by product | Infant deaths (mean) | Inquests | Implicated (mean) | Bribes | Peak heat |
| ---------- | ------------- | ----------------- | -------------------- | -------- | ----------------- | ------ | --------- |
| idle       | 0             | 0                 | 0.0                  | 0        | 0.0               | 0      | 0         |
| random     | 0             | 0                 | 0.0                  | 0        | 0.1               | 0      | 0         |
| shopkeeper | 23            | 8                 | 0.6                  | 8        | 2.8               | 0      | 44        |
| cautious   | 11.5          | 1                 | 0.0                  | 4        | 0.3               | 0      | 7         |
| greedy     | 27.5          | 27                | 2.4                  | 12       | 10.3              | 1      | 100       |
| briber     | 42            | 34                | 0.0                  | 24       | 3.6               | 20.5   | 75        |
| adaptive   | 21.5          | 7                 | 0.4                  | 8.5      | 2.9               | 0      | 54        |
| mercurial  | 3.5           | 3                 | 0.0                  | 2        | 0.9               | 0      | 26        |

## Unlock reach (share of runs · median month)

| Unlock          | idle | random    | shopkeeper | cautious     | greedy    | briber      | adaptive     | mercurial  |
| --------------- | ---- | --------- | ---------- | ------------ | --------- | ----------- | ------------ | ---------- |
| second_still    | —    | —         | 100% · m13 | 100% · m16   | 2% · m22  | 100% · m13  | 100% · m14.5 | 100% · m19 |
| percolator      | —    | —         | 100% · m9  | 100% · m9    | 2% · m9   | 100% · m9   | 100% · m9    | 100% · m9  |
| newspaper_ads   | —    | —         | 100% · m10 | 92% · m82.5  | 2% · m45  | 100% · m9.5 | 100% · m17.5 | 6% · m2    |
| doctor_title    | —    | 2% · m8   | 94% · m138 | —            | 2% · m7   | 66% · m99   | 96% · m128   | —          |
| third_still     | —    | —         | 100% · m42 | 96% · m71.5  | 2% · m60  | 96% · m34   | 100% · m38.5 | 2% · m63   |
| coroner_fees    | —    | 4% · m7.5 | 100% · m73 | 98% · m91    | 100% · m1 | 100% · m19  | 98% · m35    | 84% · m30  |
| quality_control | —    | —         | 100% · m39 | 96% · m40    | 8% · m3   | 96% · m39   | 100% · m39   | 34% · m39  |
| larger_shop     | —    | —         | 100% · m72 | 48% · m140.5 | —         | 92% · m39.5 | 100% · m45   | —          |
| bulk_supplier   | —    | —         | 100% · m24 | 100% · m24   | 2% · m24  | 100% · m24  | 100% · m24   | 100% · m24 |
