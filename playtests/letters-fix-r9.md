# Panacea · the letters fix after round 9 · correspondents who remember

Both sims changed together (the TypeScript reference and the C# port). I regenerated the parity fixtures (32 runs).
`contentVersion` is unchanged, because the sim agent bumps it. 176 letters became **180**. Godot changes are only in
`godot/scripts/UI/LettersDesk.cs`.

This pass answers round 9's item 4 and the letter rows of "Other findings" ([round09/SUMMARY.md](round09/SUMMARY.md)):

- returning correspondents who don't remember you;
- a letter that assumes a boy and a liniment;
- three letters that assume a predecessor;
- the Board circular "the same every June" dated July;
- "Become (a little) better known", which is never explained;
- the ledger's "letters answered".

I also fixed the letters the testers flagged as wrong for their month or year.

## 1. Returning correspondents remember you

> "Feeney never mentions the slander. In June 1885 I chose 'Let it be known that his stock is adulterated.' His December
> handbill and the errand-boy letter never refer to it." (satire reader, #6)
>
> "Feeney (Oct 1885: 'a penny less'; I chose Ignore) comes back in Jan 1886 at 65c with 'Why pay more on Mulberry
> street?', though my price was also 65c, and says nothing of the first round." (lapsed, #6)

A Feeney letter cannot vary its own text: the desk sees the letter's id, not the shop's flags. So a letter that
remembers is a **variant letter**, gated on the flag the earlier reply sets. Only one of each pair can ever hold.

| Chain                                    | The earlier reply now sets                                        | What the follow-up says now                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ---------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Feeney opens → the price war             | `feeney_slandered` or `feeney_ignored`                            | **`rival_price_war`** (if you let him be): "When I opened two doors down, the druggist up the street paid me no mind, which was civil of him, and I return the compliment at sixty-five cents." **`rival_price_war_slander`** (new): "The druggist up the street has told the ward my stock is adulterated. Let the ward taste mine at sixty-five cents, and his at what he asks, and say which is the adulteration." The reply reads "Let it be known again: chalk and creek water". |
| The price war only when it's true        | (new requirement `remedyPrice`)                                   | Either handbill comes only while the shop's own bottle is priced over 65c (`remedyPrice ≥ 66`). A shop at 65c no longer reads "Why pay more?". The odds ("two callers in five…") are no longer in Feeney's voice: they are only in the reply's line, as the satire reader asked.                                                                                                                                                                                                      |
| Feeney opens → the errand boy            | as above                                                          | **`feeney_boy_slander`** (new): "…if I can tell him what goes in your big jug, so he can show the ward whose stock is adulterated." `feeney_copy` waits on whichever boy letter came.                                                                                                                                                                                                                                                                                                 |
| The errand boy goes to Feeney            | `boy_left` (new)                                                  | Tommy is gone. The letters about him, or from him, stop: _The Boy Has the Measles_, _A Wheel for Deliveries_ and _The Wrong Bottle_. The reply's line now says "the shop goes without an errand boy".                                                                                                                                                                                                                                                                                 |
| Feeney's copy                            | —                                                                 | "The very same, at half the price!" became "at forty-five cents!", which is his price. Half the player's price was not always 45c.                                                                                                                                                                                                                                                                                                                                                    |
| The reporter calls → the proofs          | `reporter_paid` / `reporter_refused` (already set)                | **`reporter_proofs`**: "You showed me the door, and the desk took that for an answer." **`reporter_proofs_paid`** (new): "Your five dollars bought me a dinner, and your bottle bought the editor a headache, but neither of them bought the piece." It replaces "Whatever passed between us when I called". `reporter_returns` waits on either.                                                                                                                                      |
| The League's sheet → Mrs. Stowe in 1904  | `stowe_sheet_true` / `stowe_sheet_vegetable` (new, kept for good) | **`stowe_washington`**: "yours among them, the one honest line on any of them". **`stowe_washington_vegetable`** (new): "yours among them, with PURELY VEGETABLE written across it in your hand". The docket reads "with your old sheet folded inside it". These answer a reply made 9–18 years earlier.                                                                                                                                                                              |
| The landlord's lease → his roof          | `moved_across` (new, on "Move across the street")                 | O'Rourke "Upstairs, the front room" no longer writes about the roof over a shop you moved out of. The Estate's _Clause Nine_ no longer comes about his lease either.                                                                                                                                                                                                                                                                                                                  |
| Hobart's exclusive terms → a rival house | `hobart_exclusive` (new)                                          | Lehmann & Stroud no longer offer a trial order in the year after you signed to buy nothing elsewhere.                                                                                                                                                                                                                                                                                                                                                                                 |
| The opening itself                       | —                                                                 | The reply lines say so before you choose: "Feeney will hear what you said" and "Feeney will remember that you let him be".                                                                                                                                                                                                                                                                                                                                                            |

I read every other chain and it already remembered you, so I left it alone:

- the Kehoes ("The syrup you sent up…", "I did as you wrote, the cold spoon");
- the medicine show ("your fifteen dollars and your labels");
- the Alderman (his picture in your window);
- the pawn ticket (the stronger preparation you promised);
- Detective Lynch;
- Mrs. Quaid's sister;
- the bad note;
- the clerk and the apprentice;
- the lodge;
- the Army commission;
- Tammany after the Lexow testimony;
- the red clause;
- the ice trust;
- the Clarion man at the magazine ("a note of your twenty-five dollars").

**The draw.** A letter that waits on another (`since`) is drawn first. In the old rule an `any` counted as waiting if
any branch did. In the new rule it counts only if **every** branch does. So "never signed, or signed a year ago" does
not turn the rival house into a follow-up, and "after either errand-boy letter" still is one. No existing `any` held
a `since`, so nothing else moved.

**Old saves.** A save made mid-chain before this change has neither new flag. It gets the "let him be" handbill and
the "honest line" letter from Mrs. Stowe. I judged that better than a correspondent who never writes again.

**Do the chains arrive?** I ran 30 whole bot games per bot (scratch script `scratchpad/letters9/chains.mts`). The
sloppy bot picks replies at random, so it reaches the variants:

- the slander handbill and boy letter: 1 and 2 games;
- the paid proofs: 3 games;
- the two Stowe letters: 2 and 3 games.

The shopkeeper bot keeps its bottle at 65c or less and never gets a price war, which is the point. The adaptive bot
gets the war in 30 of 30. A bot shop gets about 1.08 letters a month, the same as before.

## 2. Letters that assume what isn't so

> "'Re: The Wrong Bottle' says 'Your boy brought my mother the liniment… instead of the cough syrup'. My jump-ahead
> shop had one remedy and no boy that I could see." (sim optimizer, #4)
>
> "The sign painter ('painted… by your predecessor's nephew') and the wholesaler ('has done since your predecessor')
> state it as fact." (satire reader, #8). "Mr. Haggerty signs himself 'Your customer, and your predecessor's'."
> (lapsed, #6)

- **The Wrong Bottle** now needs a shop that sold **two or more remedies** last month. That is the new requirement
  `remedies`. It also needs an errand boy who hasn't gone to Feeney. It names no product: "Your errand boy brought my
  mother one of your bottles on Thursday when she had sent for the other… she says it was never meant for a cough and
  she has never had less of one." The replies send "the right bottle". The label reply prints "each remedy in a colour
  of its own", not POISON on a liniment. The docket reads "Wrapped round one of your bottles".
- **The predecessor is gone** from every letter that stated one as fact:
  - the sign: "painted, I think, by the man on Canal street who does them while you wait, and it says DRUGS in letters
    that are giving up already";
  - Lehmann & Stroud: "since you opened your account";
  - Haggerty: "Your customer since the week you opened".
    "Write that the scale was your predecessor's" stays, because it is the player's lie.
- **Staff the shop may not have:**
  - Jury duty: "leave the counter to a neighbour's boy", not "the boy".
  - Tasting the batches: "pay a boy".
  - The misprint: "have a boy soak off".
  - The Alderman wants "your vote, and the votes of anybody who owes you money", not "your clerk's".
- **Remedies the shop may not sell, and custom it may not give:**
  - The principal's boys buy "your remedy by the spoonful", not "your cough syrup".
  - The Gazette's city desk no longer says you are "not at present a customer of theirs". That was false for a shop
    that advertises with it. It now reads "would be glad of more of your custom".

## 3. When they are: the date line and the month

> "The Board of Health letter's subtitle says 'A printed circular, the same every June' but is dated 'New-York, July
> 22nd, 1886'." (low-vision, #12; satire reader, #10: "July 20th, 1895")

A letter's date line is set by `LettersDesk`: the edition's Saturday, less 2 to 13 days, so the 12th to the 29th of
the month it comes in.

- **The summer circular** now comes only in June, at a chance of 0.26. That keeps the old two-month chance of 0.14
  (1 − 0.86² ≈ 0.26). The docket says "…with this year's figures: sent out before the hot weather". Its text ("last
  July", "behind the counter in July and August") reads right in June.
- **A day of the month that may already be past.** The cozy player found the bazaar letter dated December 14th "for a
  bazaar 'on the fourteenth'". Five letters now say "of next month":
  - the parish bazaar (the 14th);
  - the Sunday-school excursion (the 22nd);
  - the Flynn Guard (the 14th; the screenshot shows it dated July 23rd, 1885);
  - Hobart's in the 1893 panic ("by the fifteenth");
  - the Columbian committee (the 12th, from September 1892).
- **Kehoe's "none of us have slept since Easter"** no longer comes in April, when Easter can fall as late as the 25th.
  It comes May–August, at a chance of 0.12, up from 0.1.
- **Lent** can begin as late as March 10th. The letter now says "I am giving up the drink for Lent", not "I have given
  up".
- **The distillers' "combine"** (cozy, #9: "a year early for the Whiskey Trust"). The text now says the distillers of
  the West "have pooled their output". That is the pool of the early 1880s, and true in any year the letter can come.

I swept every letter body for season and month words. Each one fits its months, so the rest are unchanged.

## 4. "Better known" says what it does

> "'Become (a little) better known' appears on about 10 replies. Sometimes it sounds good… and sometimes bad ('which
> the coroner may notice too'). There is no meter for it in THE WORKS, and I never learned what it does." (cozy, #5)

"Better known" is the sim's `notoriety`. It is not standing in the ward. It does one thing:
`bribeExposureChance = bribes paid × 3% + notoriety × 0.25%`, up to 50%. That is the chance a coroner's fee comes
out. Nothing else in either sim, or in Godot, reads it. So it is never good, and it matters only to a shop that pays
the coroner.

Every reply that raises it, 83 lines plus the copies on the new letters (86 in all), now ends with the same sentence.
The number is the reply's own notoriety effect:

> **Better known +2: if you ever buy a verdict from the coroner, it is likelier to come out.**

Seven replies raised notoriety without saying so, and now say so:

- Feeney's "chalk and creek water";
- showing the reporter the door;
- Sunday bitters;
- the German traveller;
- selling the letters;
- giving the magazine a rival;
- letting the magazine write.

A scratch check, `scratchpad/letters9/risks.py`, reads `events.json` and the desk's lines and finds no problems. It
checks four things:

- every choice has a line;
- every notoriety effect is stated as "Better known +n", with the right n;
- no "you become better known" is left;
- no line is left for a reply that doesn't exist.

The screenshot `scratchpad/letters9/shots/a.png` is seed 9092, July 1885. It shows the line under the Flynn Guard's
replies.

## 5. Where a letter's money goes in the books

> "The $2 of trade lost to jury duty and the $6 Jersey distiller are both booked as 'letters answered'. The cabinet
> portrait counts as 'laid out on the business' only some of the time." (cozy, #10)

Every effect of a reply was booked under `event` ("Letters answered"). A reply can now carry a `ledger` tag (the
schema's `LEDGER_TAGS`), and its **cash** is booked under the matching cause. Standing, suspicion and notoriety stay
under the letter. The mapping is `LETTER_LEDGER` in `progression.ts` and C# `Progression.LetterLedger`:

| Tag             | Ledger line                   | Replies | e.g.                                                               |
| --------------- | ----------------------------- | ------- | ------------------------------------------------------------------ |
| `outlay`        | "Outlays on the business"     | 56      | the gilt sign, the portrait, the lease, fans, the telephone        |
| `supplies`      | "Ingredient purchases"        | 5       | **the Jersey distiller**, Hobart's deposit, the 1893 settlement    |
| `fees`          | "Consulting fees"             | 23      | the inspector's $3, the Captain's outing, the Commissioner's clerk |
| `subscriptions` | "Subscriptions and retainers" | 21      | the Pedestal Fund, the parish dollar, lodge dues, the boy's wages  |
| `trade`         | "Letters answered" (for now)  | 17      | **jury duty**, Labor Day, the syrups behind the counter            |

Untagged money stays "Letters answered": a pound of tea, a free bottle, the medicine show's takings, a gallon sold for
the mare. The linter refuses a `ledger` tag on a reply with no cash effect.

**Three changes I need in files I don't own:**

1. **A cause for trade given up.** In `packages/sim/src/core/causes.ts` add `tradeLost: 'event.trade_lost'`. In C#
   `Causes` (`sim-cs/Panacea.Sim/Core/Actions.cs`) add `TradeLost = "event.trade_lost"` and put it in `All`. Then
   point `trade` at it, one line in each sim's `LETTER_LEDGER`/`LetterLedger`. Add the string
   `"cause.event.trade_lost": "Trade given up to answer letters"` to `en.json` (it can go in with the letter keys).
   Then regenerate the fixtures. Until then jury duty's $2 stays "Letters answered". At least it no longer shares that
   line with the Jersey distiller.
2. **"Laid out on the business" (`godot/scripts/Game/SimGame.cs`, around line 479).** `InvestedCents` sums the
   negative cash under `expense.investment`, `event`, `expense.bribe` and `expense.retainer`. With the tags, a
   letter's outlays already arrive as `expense.investment`. So `"event"` should come out of that list, or else trade
   given up and a pound of tea count as investment. After (1), keep `event.trade_lost` out too.
3. **The portrait "only some of the time" (`godot/scripts/Paper/EditionWriter.cs:964`).** The brief prints ", $5.00 of
   it laid out on the business" only when the books are **down** (`delta < 0`). In a month that ends up, the same
   portrait is never mentioned. That is the inconsistency the cozy player saw. A fix would print the outlay in either
   case, e.g. "the books up $3.20, after $5.00 laid out on the business".

## 6. New requirement types

| Type          | Shape                                           | Holds when                                                                                        |
| ------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `remedyPrice` | `{ "type": "remedyPrice", "atLeastCents": 66 }` | the shop's remedy (its best seller last month, else its newest formula) is priced at this or more |
| `remedies`    | `{ "type": "remedies", "atLeast": 2 }`          | the shop sold this many different remedies last month (a count, so it has a progress bar)         |

Both are identical in `requirements.ts` and C# `Requirements`. The C# converter reads and writes them, the schema
validates them, and the linter allows them only in a letter's condition. Both are used only by letters.

## Tests

`packages/sim/test/letters.test.ts` has a new block, "correspondents who remember (round 9)", with 7 tests. The same
cases are in `sim-cs/Panacea.Sim.Tests/LettersTests.cs` (5 tests). They check that:

- after the slander only the slander handbill and boy letter can come, and after "Ignore" only the plain ones;
- neither handbill comes to a shop at 65c;
- the letter text says so;
- the copy waits on either boy letter;
- a boy who left takes his letters with him;
- the wrong bottle needs two remedies, and `remedies` and `remedyPrice` evaluate as stated;
- the reporter remembers whether he was paid or shown the door, and his 1904 letter follows either;
- Mrs. Stowe remembers the vegetable sheet and the honest one;
- an `any` waits only if every branch does;
- the rival house waits a year after the exclusive terms;
- each tag books its cash under the right cause, and standing and notoriety stay with the letter.

`GameTests.EmbeddedContentLoads` counts the content. I changed its two numbers: 176 → 180 events and 933 → 952 strings.
The sim agent's new strings will move the string count again.

## Checks

- `pnpm -s typecheck`, `pnpm -s lint`, `npx prettier --check .`, `pnpm content:lint` (180 events, 0 errors,
  0 warnings), `python3 godot/tools/check_cuts.py` and `cd godot && dotnet build`: pass.
- `pnpm -s test`: 147 tests. The only failures were the two `harness.test.ts` bot games, which hit vitest's 5-second
  timeout while the machine's load average was 13. Run alone they pass (6/6).
- `cd sim-cs && dotnet test`: 122/122 pass, with the regenerated fixtures.

## Left open

- The trade-lost cause and the two Godot ledger lines in §5 need files I don't own.
- The paper's own date slips are in `godot/data/paper.en.json`, not mine:
  - New Year's "yesterday" on January 31;
  - "Darkest Africa" in 1885;
  - the _Etruria_;
  - Mrs. Whittle's funeral;
  - baseball on October 31;
  - "Pine coffins advance";
  - "who complains of female complaints".
- I added no meter for "better known". The reply lines now say what it does. A meter would be the UI agent's call.
