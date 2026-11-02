# PANACEA — Playtest & Release Pipeline

How we decide the game is ready to launch. The core idea: **run simulated players and critics over and over**, fix what they find, and re-run, until the build passes defined release gates. Simulated testing is backed by real human tests at key milestones (see §7 for why).

Companion to [GAME_DESIGN.md](GAME_DESIGN.md) and [TECHNICAL_PLAN.md](TECHNICAL_PLAN.md).

---

## 1. The four layers

| Layer                  | What runs                                                                                                                 | What it finds                                                                                    | Cost / speed                         |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------------------------ |
| **L1 Automated tests** | Unit, property, golden replays, content linter, E2E smoke                                                                 | Bugs, regressions, broken content, save corruption                                               | Seconds; every push                  |
| **L2 Strategy bots**   | Scripted and search-based bots playing thousands of seeded runs headless                                                  | Dominant strategies, death spirals, pacing holes, dead unlocks, economy breaks                   | Minutes; nightly and on demand       |
| **L3 AI playtesters**  | LLM agents with player personas, playing full or partial runs and keeping a diary, then writing a Steam-style review      | Clarity and onboarding issues, confusing causality, boring stretches, tone problems, UX friction | Hours and tokens; per playtest cycle |
| **L4 AI critic panel** | LLM critics scoring the build against a fixed rubric, using L3 diaries, L2 reports, screenshots and a guided play session | Holistic quality, comparisons to genre standards, launch-readiness                               | Per cycle                            |

---

## 2. L2: Strategy bots

**Bot roster**

- `idle`: does nothing (economy floor; must not crash or hit absurd states)
- `random`: legal random actions (fuzzing)
- `greedy-narcotic`: maximise felt effect and habit
- `cautious`: minimise heat and toxicity
- `ad-blitz`: maximise advertising spend
- `briber`: solve every incident with money
- `legit`: pursue the low-toxicity "go legit" path
- `optimizer`: evolutionary/search over policy parameters to _find_ the best strategy (this is the exploit hunter)

**Metrics per build** (across N seeds × backgrounds × difficulties)

- Score / ending distribution per bot
- Bankruptcy rate by month (early death-spiral detection)
- Time to each unlock; % of unlocks ever reached; unlocks never reached
- Decisions per month that change outcomes (detects "nothing to do" stretches)
- Share of the top-decile scores taken by each strategy (dominance)
- Distribution of deaths by cause and segment (does the body model produce sensible stories?)
- Cash curves (snowball detection)

**Balance bands (initial targets, tuned over time)**

- No single strategy produces > 40% of top-decile scores.
- `optimizer` beats `cautious` and `legit` by less than a set margin (so non-ruthless play remains viable).
- Bankruptcy before month 12 on Normal: 5–20% for `random`, < 5% for sensible bots.
- Every unlock reached in ≥ 10% of `optimizer` runs.
- ≥ 2 meaningful decisions in ≥ 85% of months for active bots.

Results go into a dated **balance report** committed under `playtest/reports/` and diffed against the previous build.

---

## 3. L3: AI playtesters

**Personas** (each with play goals, patience, genre knowledge and pet peeves)

- _The Tycoon Veteran:_ has played every management sim; hunts depth and exploits
- _The Cozy Newcomer:_ first management game; low patience for spreadsheets
- _The History Buff:_ checks accuracy and period flavour
- _The Min-Maxer:_ pushes the dominant strategy as hard as possible
- _The Story Player:_ plays for events, characters and endings
- _The Streamer:_ looks for memorable, shareable moments, and for dead air
- _The Accessibility Tester:_ keyboard-only, large text, colour-blind palettes
- _The Skeptic:_ expects to dislike the theme; tests whether the satire lands

**How they play**

- **Fast mode (sim API):** the agent receives the `player` observation and legal actions as structured text and plays many months quickly. Good for systems, pacing and clarity of the information shown.
- **UI mode (Playwright):** the agent drives the real client through screenshots and the accessibility tree. Good for UX, readability, onboarding and visual feedback. Slower; used on scripted segments (first hour, a crisis, the 1906 transition, an ending).

**Output per session**

- A **diary**: turn-by-turn thoughts, confusions ("I don't know why sales dropped"), moments of delight, intent vs result.
- A **Steam-style review**: recommend yes/no, hours played, short and long text.
- **Structured issues**: category (bug, clarity, balance, pacing, tone, UX, accessibility), severity, reproduction (seed + action log), and quote.

---

## 4. L4: AI critic panel

Three to five critic personas styled after different kinds of outlets (a strategy specialist, a mainstream gaming site, an indie/narrative critic, a historian reviewer). All fictional.

**Rubric (each 1–10, with written justification)**

1. Core loop: is a month satisfying?
2. Clarity: can the player understand cause and effect?
3. Depth & strategy: are there real, diverse viable strategies?
4. Pacing: early, mid and late game; dead air
5. Progression pull: do unlocks create "one more turn"?
6. The Lab: tactile, satisfying, worth returning to
7. Tone & writing: does the satire land without being gratuitous?
8. Historical flavour
9. Presentation: art, audio, UI polish
10. Stability & technical quality
11. Replayability
12. Accessibility

Critics read L2 reports and L3 diaries and also run their own guided sessions, so their scores aren't just averages of the playtesters.

---

## 5. The cycle

```
build → L1 tests → L2 bot batch → L3 persona sessions → L4 critic panel
      → triage (dedupe, rank by severity × frequency) → GitHub issues
      → fix / tune / write content → next build → repeat
```

- **Triage:** issues from all layers are deduplicated and ranked. P0 = crash/data loss/softlock; P1 = breaks a system or a major clarity failure; P2 = balance/pacing; P3 = polish.
- **Regression tracking:** every cycle's scores and metrics are compared to the previous cycles; any rubric score falling by ≥ 1 point is investigated.
- **Persona rotation:** new persona variants are added periodically so the agents don't overfit to known feedback.
- **Cost control:** token budget per cycle; fast-mode sessions most of the time, UI mode for targeted segments.

---

## 6. Release gates

The game is launch-ready when **all** of these pass on **three consecutive builds**:

**Stability**

- Zero P0 and zero open P1 issues
- 10,000 headless runs (all bots, all backgrounds/difficulties) with no crash, softlock or invariant violation
- Saves from every previous beta version load and migrate correctly
- Performance budgets met on min-spec hardware

**Balance (L2)**

- All balance bands in §2 met

**Player experience (L3)**

- ≥ 85% of persona reviews recommend the game, including the Cozy Newcomer and the Skeptic
- No clarity issue reported by ≥ 3 personas in the latest cycle

**Critic panel (L4)**

- Average ≥ 8.0/10; no rubric category below 7.0

**Human gates (required; see §7)**

- A closed human beta with a target sample size, with recommend rate and median session length meeting targets
- A public demo (e.g. Steam Next Fest) with its feedback triaged
- An external accessibility check and a legal/content review

**Content & platform**

- Content linter clean; all strings externalised; localisation pipeline verified
- Steam achievements, cloud saves and store assets complete; rating and content survey done

---

## 7. Why humans are still required

Simulated players are very good at finding bugs, exploits, balance outliers and many clarity problems, quickly and repeatedly. They are poor at predicting _fun_: game feel, whether a joke lands, whether the theme is compelling or off-putting, and whether people will actually buy the game. They also tend to share blind spots with each other.

So AI cycles do the bulk of the iteration, and humans validate at the milestones where it matters most:

- **M2:** friends-and-family test of the vertical slice
- **M3:** small closed alpha
- **M4:** closed beta + Steam Playtest + Next Fest demo
- **M5:** final beta before launch

When human results and AI results disagree, humans win, and the AI personas are re-tuned to catch what they missed.
