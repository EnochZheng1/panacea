import { loadContent, type ContentPack } from '@panacea/content';
import {
  analyzeFormula,
  applyAction,
  deltasByCause,
  deserializeGame,
  endTurn,
  newGame,
  observe,
  serializeGame,
  type Action,
  type FormulaComponent,
  type GameState,
  type Observation,
} from '@panacea/sim';
import { useMemo, useState } from 'react';
import { deathName, money, monthName, newsText, t } from './text.ts';

/**
 * M1 debug client: plays the real simulation by hand. Deliberately plain; the real game client
 * (M2+) replaces it. Everything goes through the same actions bots and AI playtesters use.
 */

const content: ContentPack = loadContent();
const SAVE_KEY = 'panacea.debug.save';

function readSave(): GameState | null {
  try {
    const text = localStorage.getItem(SAVE_KEY);
    return text ? deserializeGame(text) : null;
  } catch {
    return null;
  }
}

export function App() {
  const [state, setState] = useState<GameState>(() => newGame(content, { seed: 'debug' }));
  const [message, setMessage] = useState<string>('');
  const [showTruth, setShowTruth] = useState(false);
  const obs = useMemo(() => observe(state, content), [state]);

  const act = (action: Action): boolean => {
    const { state: next, result } = applyAction(state, action, content);
    if (result.ok) {
      setState(next);
      setMessage('');
    } else {
      setMessage(`✗ ${result.error}`);
    }
    return result.ok;
  };
  const endMonth = () => {
    if (state.meta.status !== 'running') return;
    setState(endTurn(state, content).state);
    setMessage('');
  };
  const save = () => {
    try {
      localStorage.setItem(SAVE_KEY, serializeGame(state));
      setMessage('Saved.');
    } catch {
      setMessage('Could not save (storage unavailable).');
    }
  };
  const load = () => {
    const loaded = readSave();
    if (loaded) setState(loaded);
    setMessage(loaded ? 'Loaded.' : 'No save found.');
  };

  return (
    <div className="app">
      <header>
        <h1>
          Panacea <small>debug client</small>
        </h1>
        <StatusBar obs={obs} />
        <div className="controls">
          <button className="primary" onClick={endMonth} disabled={obs.status !== 'running'}>
            End month ▸
          </button>
          <NewGame onStart={(seed, bg) => setState(newGame(content, { seed, backgroundId: bg }))} />
          <button onClick={save}>Save</button>
          <button onClick={load}>Load</button>
          <label className="toggle">
            <input
              type="checkbox"
              checked={showTruth}
              onChange={(e) => setShowTruth(e.target.checked)}
            />{' '}
            Show hidden truth
          </label>
        </div>
        {message && <p className="message">{message}</p>}
        {obs.status === 'ended' && <p className="ended">The game has ended: {obs.endReason}.</p>}
      </header>
      <main>
        <section>
          <Post obs={obs} act={act} />
          <LastMonth state={state} obs={obs} />
          <Unlocks obs={obs} />
        </section>
        <section>
          <Lab obs={obs} act={act} state={state} showTruth={showTruth} />
        </section>
        <section>
          <Customers obs={obs} state={state} showTruth={showTruth} />
          <Deaths obs={obs} state={state} showTruth={showTruth} />
        </section>
      </main>
    </div>
  );
}

function StatusBar({ obs }: { obs: Observation }) {
  return (
    <div className="status">
      <span>
        📅 {monthName(obs.date.month)} {obs.date.year}
      </span>
      <span>💰 {money(obs.cashCents)}</span>
      <span title="Reputation">⭐ {obs.reputation.toFixed(0)}</span>
      <span title="Heat: talk among neighbours, press and authorities">
        🔥 {obs.heat.toFixed(0)}
      </span>
      <span title="Notoriety">🎭 {obs.notoriety.toFixed(0)}</span>
      <span>
        👥 {obs.customers.length}/{obs.customerCapacity}
      </span>
      <span>⚗ stills free: {obs.stillsFree}</span>
    </div>
  );
}

function NewGame({ onStart }: { onStart: (seed: string, bg: string) => void }) {
  const [seed, setSeed] = useState('debug');
  const [bg, setBg] = useState(content.config.defaultBackground);
  return (
    <span className="newgame">
      <input value={seed} onChange={(e) => setSeed(e.target.value)} size={8} aria-label="Seed" />
      <select value={bg} onChange={(e) => setBg(e.target.value)} aria-label="Background">
        {content.backgrounds.map((b) => (
          <option key={b.id} value={b.id}>
            {t(content, `background.${b.id}.name`)}
          </option>
        ))}
      </select>
      <button onClick={() => onStart(seed, bg)}>New game</button>
    </span>
  );
}

function Post({ obs, act }: { obs: Observation; act: (a: Action) => boolean }) {
  return (
    <div className="panel">
      <h2>The Post</h2>
      {obs.pendingEvents.length === 0 && <p className="muted">Nothing this month.</p>}
      {obs.pendingEvents.map((ev) => {
        const params = {
          name: ev.subjectName || 'a customer',
          cost: ev.bribeCostCents === undefined ? '' : money(ev.bribeCostCents),
        };
        const all = content.byId.events.get(ev.eventId)?.choices ?? [];
        return (
          <div className="card" key={ev.instanceId}>
            <h3>{t(content, `event.${ev.eventId}.title`, params)}</h3>
            <p>{t(content, `event.${ev.eventId}.body`, params)}</p>
            {all.map((c) => (
              <button
                key={c.id}
                disabled={!ev.choiceIds.includes(c.id)}
                onClick={() =>
                  act({ type: 'ResolveEvent', instanceId: ev.instanceId, choiceId: c.id })
                }
              >
                {t(content, `event.${ev.eventId}.choice.${c.id}`, params)}
                {c.default ? ' (default)' : ''}
              </button>
            ))}
          </div>
        );
      })}
    </div>
  );
}

function LastMonth({ state, obs }: { state: GameState; obs: Observation }) {
  const e = obs.lastMonth;
  if (!e)
    return (
      <div className="panel">
        <h2>The Ledger</h2>
        <p className="muted">No month closed yet.</p>
      </div>
    );
  const metrics = ['cash', 'reputation', 'heat'] as const;
  return (
    <div className="panel">
      <h2>
        The Ledger — {monthName(e.date.month)} {e.date.year}
      </h2>
      <div className="ledger">
        {metrics.map((m) => (
          <div key={m}>
            <h4>{m}</h4>
            <ul>
              {Object.entries(deltasByCause(e, m)).map(([cause, amount]) => (
                <li key={cause} className={(amount ?? 0) < 0 ? 'neg' : 'pos'}>
                  {t(content, `cause.${cause}`)}:{' '}
                  {m === 'cash' ? money(amount ?? 0) : (amount ?? 0).toFixed(1)}
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
      <p>
        Bottles sold: {e.sales.reduce((n, s) => n + s.bottles, 0)} · Complaints: {e.complaints}
      </p>
      <ul className="news">
        {e.news.map((n, i) => (
          <li key={i}>{newsText(content, state, n)}</li>
        ))}
      </ul>
    </div>
  );
}

function Unlocks({ obs }: { obs: Observation }) {
  return (
    <div className="panel">
      <h2>Next unlocks</h2>
      {obs.nextUnlocks.map((u) => (
        <div key={u.id} className="unlock">
          <span>{t(content, `unlock.${u.id}.name`)}</span>
          <progress value={u.progress} max={1} />
        </div>
      ))}
      <p className="muted">
        Unlocked: {obs.unlocked.map((id) => t(content, `unlock.${id}.name`)).join(', ') || 'none'}
      </p>
    </div>
  );
}

const fmtProps = (p: Record<string, number>) =>
  Object.entries(p)
    .map(([k, v]) => `${k} ${v.toFixed(1)}`)
    .join(' · ');

function Lab({
  obs,
  act,
  state,
  showTruth,
}: {
  obs: Observation;
  act: (a: Action) => boolean;
  state: GameState;
  showTruth: boolean;
}) {
  const [bottles, setBottles] = useState(20);
  const [ads, setAds] = useState(obs.adSpendCents);
  return (
    <div className="panel">
      <h2>The Laboratory</h2>
      <h3>Shelves</h3>
      <table>
        <thead>
          <tr>
            <th>Ingredient</th>
            <th>On hand</th>
            <th>Price/oz</th>
            <th>Buy</th>
          </tr>
        </thead>
        <tbody>
          {obs.ingredients.map((i) => (
            <tr key={i.id} title={t(content, `ingredient.${i.id}.description`)}>
              <td>{t(content, `ingredient.${i.id}.name`)}</td>
              <td>{(obs.inventory[i.id] ?? 0).toFixed(1)} oz</td>
              <td>{money(Math.round(i.costPerUnitCents * 100) / 100)}</td>
              <td>
                {[10, 50].map((n) => (
                  <button
                    key={n}
                    onClick={() => act({ type: 'BuyIngredient', ingredientId: i.id, units: n })}
                  >
                    +{n}
                  </button>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h3>Formulas</h3>
      <label>
        Batch size{' '}
        <input
          type="number"
          min={1}
          value={bottles}
          onChange={(e) => setBottles(Number(e.target.value))}
        />
      </label>
      {obs.formulas.map((f) => (
        <div className="card" key={f.id}>
          <h4>
            {f.name} — {money(f.priceCents)} · stock {f.stock} · cost{' '}
            {money(Math.round(f.unitCostCents))}
          </h4>
          <p className="small">{fmtProps(f.properties)}</p>
          <p className="small">
            Claims: {f.claims.map((c) => t(content, `claim.${c}.name`)).join('; ') || 'none'}
          </p>
          {showTruth && (
            <p className="small truth">
              Lots:{' '}
              {state.lab.stock
                .filter((l) => l.formulaId === f.id)
                .map((l) => `${l.bottles}@${l.potency.toFixed(2)}${l.badBatch ? ' BAD' : ''}`)
                .join(', ') || 'none'}
            </p>
          )}
          <button onClick={() => act({ type: 'BrewBatch', formulaId: f.id, bottles })}>
            Brew {bottles}
          </button>
          <button
            onClick={() =>
              act({ type: 'SetPrice', formulaId: f.id, priceCents: f.priceCents + 10 })
            }
          >
            +10¢
          </button>
          <button
            onClick={() =>
              act({ type: 'SetPrice', formulaId: f.id, priceCents: Math.max(0, f.priceCents - 10) })
            }
          >
            −10¢
          </button>
          <ClaimsEditor
            claims={f.claims}
            available={obs.claims}
            onChange={(claims) => act({ type: 'SetClaims', formulaId: f.id, claims })}
          />
          <FormulaEditor
            obs={obs}
            initial={state.lab.formulas.find((x) => x.id === f.id)?.components ?? []}
            submitLabel="Reformulate"
            onSubmit={(components, processId) =>
              act({ type: 'Reformulate', formulaId: f.id, components, processId })
            }
          />
        </div>
      ))}

      <h3>New formula</h3>
      <NewFormula obs={obs} act={act} />

      <h3>Advertising</h3>
      {obs.canAdvertise ? (
        <p>
          <input
            type="number"
            min={0}
            step={100}
            value={ads}
            onChange={(e) => setAds(Number(e.target.value))}
          />{' '}
          cents/month <button onClick={() => act({ type: 'SetAdSpend', cents: ads })}>Set</button>{' '}
          (now {money(obs.adSpendCents)})
        </p>
      ) : (
        <p className="muted">Not yet unlocked.</p>
      )}
    </div>
  );
}

function ClaimsEditor(props: {
  claims: string[];
  available: string[];
  onChange: (c: string[]) => void;
}) {
  return (
    <details>
      <summary>Label claims</summary>
      {props.available.map((id) => (
        <label key={id} className="claim">
          <input
            type="checkbox"
            checked={props.claims.includes(id)}
            onChange={(e) =>
              props.onChange(
                e.target.checked ? [...props.claims, id] : props.claims.filter((c) => c !== id),
              )
            }
          />{' '}
          {t(content, `claim.${id}.name`)}
        </label>
      ))}
    </details>
  );
}

function FormulaEditor(props: {
  obs: Observation;
  initial: FormulaComponent[];
  submitLabel: string;
  onSubmit: (components: FormulaComponent[], processId: string) => boolean;
}) {
  const [rows, setRows] = useState<Record<string, number>>(() =>
    Object.fromEntries(props.initial.map((c) => [c.ingredientId, c.amount])),
  );
  const [processId, setProcessId] = useState(props.obs.processes[0] ?? 'simple_mix');
  const components = Object.entries(rows)
    .filter(([, amount]) => amount > 0)
    .map(([ingredientId, amount]) => ({ ingredientId, amount }));
  const volume = components.reduce((s, c) => s + c.amount, 0);
  const preview = components.length > 0 ? analyzeFormula(components, processId, content) : null;
  return (
    <details>
      <summary>{props.submitLabel}…</summary>
      <div className="grid">
        {props.obs.ingredients.map((i) => (
          <label key={i.id}>
            {t(content, `ingredient.${i.id}.name`)}{' '}
            <input
              type="number"
              min={0}
              step={0.05}
              value={rows[i.id] ?? 0}
              onChange={(e) => setRows({ ...rows, [i.id]: Number(e.target.value) })}
            />
          </label>
        ))}
      </div>
      <select value={processId} onChange={(e) => setProcessId(e.target.value)}>
        {props.obs.processes.map((p) => (
          <option key={p} value={p}>
            {t(content, `process.${p}.name`)}
          </option>
        ))}
      </select>{' '}
      <span className="small">
        {volume.toFixed(2)} / {content.config.bottleCapacity} oz
        {preview ? ` · ${fmtProps(preview.properties)}` : ''}
      </span>{' '}
      <button onClick={() => props.onSubmit(components, processId)}>{props.submitLabel}</button>
    </details>
  );
}

function NewFormula({ obs, act }: { obs: Observation; act: (a: Action) => boolean }) {
  const [name, setName] = useState("Dr. Debug's Elixir");
  const [price, setPrice] = useState(60);
  return (
    <div className="card">
      <input value={name} onChange={(e) => setName(e.target.value)} aria-label="Formula name" />{' '}
      <input
        type="number"
        value={price}
        onChange={(e) => setPrice(Number(e.target.value))}
        aria-label="Price"
      />
      ¢
      <FormulaEditor
        obs={obs}
        initial={[
          { ingredientId: 'grain_alcohol', amount: 2 },
          { ingredientId: 'laudanum', amount: 0.25 },
          { ingredientId: 'sugar_syrup', amount: 1 },
          { ingredientId: 'water', amount: 4.75 },
        ]}
        submitLabel="Create"
        onSubmit={(components, processId) =>
          act({ type: 'CreateFormula', name, components, processId, priceCents: price, claims: [] })
        }
      />
    </div>
  );
}

function Customers({
  obs,
  state,
  showTruth,
}: {
  obs: Observation;
  state: GameState;
  showTruth: boolean;
}) {
  return (
    <div className="panel">
      <h2>Regulars</h2>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Complaint</th>
            <th>Looks</th>
            <th>Mood</th>
            <th>Bought</th>
            {showTruth && <th className="truth">Health · dep · load</th>}
          </tr>
        </thead>
        <tbody>
          {obs.customers.map((c) => {
            const truth = state.customers.find((x) => x.id === c.id);
            return (
              <tr key={c.id}>
                <td>
                  {c.name}
                  {c.consumer === 'infant' ? ' (for baby)' : ''}
                  <div className="small muted">{t(content, `segment.${c.segment}.name`)}</div>
                </td>
                <td>{t(content, `ailment.${c.ailment}.name`)}</td>
                <td className={`looks-${c.looks}`}>{c.looks.replace('_', ' ')}</td>
                <td>
                  {c.mood}
                  {c.craving ? ' · craving' : ''}
                  {c.withdrawal ? ' · shaking' : ''}
                </td>
                <td>{c.bottlesLastMonth}</td>
                {showTruth && truth && (
                  <td className="truth small">
                    {truth.body.health.toFixed(0)} · {truth.body.dependency.toFixed(2)} ·{' '}
                    {truth.body.load.toFixed(1)}
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function Deaths({
  obs,
  state,
  showTruth,
}: {
  obs: Observation;
  state: GameState;
  showTruth: boolean;
}) {
  return (
    <div className="panel">
      <h2>The human ledger</h2>
      {obs.deaths.length === 0 && <p className="muted">No one has died.</p>}
      <ul className="deaths">
        {[...obs.deaths].reverse().map((d) => {
          const truth = state.deaths.find((x) => x.id === d.id);
          return (
            <li key={d.id}>
              {monthName(d.date.month)} {d.date.year}: {deathName(content, d)}
              {d.verdict
                ? ` — “${t(content, `verdict.${d.verdict}`)}”`
                : d.inquest === 'pending'
                  ? ' — inquest pending'
                  : ''}
              {showTruth && truth && (
                <span className="truth small">
                  {' '}
                  [truth: {truth.cause}
                  {truth.productImplicated ? ', your medicine' : ''}
                  {truth.badBatch ? ', bad batch' : ''}]
                </span>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
