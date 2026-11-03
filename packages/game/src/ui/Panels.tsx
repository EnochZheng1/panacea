import { PROPERTY_KEYS } from '@panacea/content';
import { deltasByCause, type FormulaComponent } from '@panacea/sim';
import { useState } from 'react';
import { analyze, content, type Game } from '../state/useGame.ts';
import { deathName, money, monthName, newsText, t } from './text.ts';

const PROPERTY_LABELS: Record<string, string> = {
  kick: 'Kick',
  calm: 'Calm',
  habit: 'Habit',
  acuteToxicity: 'Danger',
  accumulation: 'Lingers',
  bitterness: 'Bitter',
  detectability: 'Detectable',
};

/** A row of engraved gauge bars for a formula's properties. */
export function PropertyBars({ components }: { components: FormulaComponent[] }) {
  if (components.length === 0) return <p className="muted">The glass is empty.</p>;
  const a = analyze(components);
  return (
    <div className="bars">
      {PROPERTY_KEYS.map((k) => {
        const v = a.properties[k];
        const warn = (k === 'acuteToxicity' && v > 3) || (k === 'accumulation' && v > 1.5);
        return (
          <div key={k} className={`bar ${warn ? 'warn' : ''}`}>
            <span>{PROPERTY_LABELS[k]}</span>
            <div className="track">
              <div
                className="fill"
                style={{ width: `${Math.min(100, Math.max(0, (v / 10) * 100))}%` }}
              />
            </div>
            <em>{v.toFixed(1)}</em>
          </div>
        );
      })}
      <p className="small muted">Costs {money(Math.round(a.unitCostCents))} a bottle to make.</p>
    </div>
  );
}

export function OverviewPanel() {
  return (
    <section className="paper">
      <h2>The Shop</h2>
      <p>
        New York, 1885. One room, one still, a dozen regulars, and no law against anything you might
        put in a bottle.
      </p>
      <ul className="plain">
        <li>
          <b>The Bench</b>: pour drugs into the measuring glass and write the recipe in your formula
          book.
        </li>
        <li>
          <b>The Still</b>: order ingredients and brew a batch.
        </li>
        <li>
          <b>The Counter</b>: set prices and label claims; watch your regulars.
        </li>
        <li>
          <b>The Desk</b>: the morning post, the Ledger, and the bell that closes the month.
        </li>
      </ul>
      <p className="small muted">Click anything in the shop, or use keys 1–5.</p>
    </section>
  );
}

export function BenchPanel({
  game,
  mix,
  setMix,
  pourSize,
  setPourSize,
}: {
  game: Game;
  mix: Record<string, number>;
  setMix: (m: Record<string, number>) => void;
  pourSize: number;
  setPourSize: (n: number) => void;
}) {
  const components = Object.entries(mix)
    .filter(([, v]) => v > 0)
    .map(([ingredientId, amount]) => ({ ingredientId, amount: Math.round(amount * 100) / 100 }));
  const volume = components.reduce((s, c) => s + c.amount, 0);
  const capacity = content.config.bottleCapacity;
  const [name, setName] = useState("Dr. Harlow's Soothing Elixir");
  const [price, setPrice] = useState(60);
  const formulas = game.state.lab.formulas;
  const [target, setTarget] = useState<string>('new');

  const topUp = () => {
    if (volume >= capacity) return;
    setMix({ ...mix, water: (mix['water'] ?? 0) + (capacity - volume) });
  };
  const record = () => {
    if (components.length === 0) return;
    const r =
      target === 'new'
        ? game.act({
            type: 'CreateFormula',
            name,
            components,
            processId: 'simple_mix',
            priceCents: price,
            claims: [],
          })
        : game.act({ type: 'Reformulate', formulaId: target, components, processId: 'simple_mix' });
    if (r.ok)
      game.toast(
        target === 'new' ? `"${name}" entered in the formula book.` : 'Formula quietly revised.',
      );
  };

  return (
    <section className="paper">
      <h2>The Bench</h2>
      <p className="small">
        Click a bottle on the shelf to pour. Measuring costs nothing; you pay when you brew.
      </p>
      <div className="row">
        Pour
        {[0.1, 0.25, 0.5, 1].map((n) => (
          <button key={n} className={pourSize === n ? 'on' : ''} onClick={() => setPourSize(n)}>
            {n} oz
          </button>
        ))}
      </div>
      <ul className="mix">
        {components.map((c) => (
          <li key={c.ingredientId}>
            <span
              className="swatch"
              style={{ background: content.byId.ingredients.get(c.ingredientId)?.appearance.color }}
            />
            {t(content, `ingredient.${c.ingredientId}.name`)} — {c.amount.toFixed(2)} oz
            <button
              className="x"
              onClick={() =>
                setMix(
                  Object.fromEntries(Object.entries(mix).filter(([k]) => k !== c.ingredientId)),
                )
              }
            >
              ×
            </button>
          </li>
        ))}
      </ul>
      <p className={volume > capacity ? 'error' : 'small'}>
        {volume.toFixed(2)} of {capacity} oz{volume > capacity ? ': too much for one bottle!' : ''}
      </p>
      <div className="row">
        <button onClick={topUp} disabled={volume >= capacity}>
          Top up with water
        </button>
        <button onClick={() => setMix({})}>Empty the glass</button>
      </div>
      <PropertyBars components={components} />
      <div className="formbook">
        <select value={target} onChange={(e) => setTarget(e.target.value)}>
          <option value="new">A new formula…</option>
          {formulas.map((f) => (
            <option key={f.id} value={f.id}>
              Revise “{f.name}”
            </option>
          ))}
        </select>
        {target === 'new' && (
          <>
            <input value={name} onChange={(e) => setName(e.target.value)} aria-label="Name" />
            <label>
              Price{' '}
              <input
                type="number"
                min={0}
                step={5}
                value={price}
                onChange={(e) => setPrice(Number(e.target.value))}
              />
              ¢
            </label>
          </>
        )}
        <button
          className="primary"
          onClick={record}
          disabled={components.length === 0 || volume > capacity}
        >
          {target === 'new' ? 'Write it in the formula book' : 'Revise the formula'}
        </button>
      </div>
    </section>
  );
}

export function StillPanel({
  game,
  onBrew,
}: {
  game: Game;
  onBrew: (formulaId: string, color: string) => void;
}) {
  const formulas = game.state.lab.formulas;
  const [formulaId, setFormulaId] = useState(formulas[0]?.id ?? '');
  const [bottles, setBottles] = useState(30);
  const fid = formulas.some((f) => f.id === formulaId) ? formulaId : (formulas[0]?.id ?? '');
  const plan = fid ? game.brewPlan(fid, bottles) : null;
  const brew = () => {
    if (!plan) return;
    const r = game.actAll([
      ...plan.orders.map((o) => ({
        type: 'BuyIngredient' as const,
        ingredientId: o.ingredientId,
        units: o.units,
      })),
      { type: 'BrewBatch', formulaId: fid, bottles },
    ]);
    if (r.ok) onBrew(fid, '');
  };
  return (
    <section className="paper">
      <h2>The Still</h2>
      {formulas.length === 0 ? (
        <p>Your formula book is empty. Compound something at the bench first.</p>
      ) : (
        <>
          <label>
            Formula{' '}
            <select value={fid} onChange={(e) => setFormulaId(e.target.value)}>
              {formulas.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.name}
                </option>
              ))}
            </select>
          </label>
          <label className="slider">
            Batch: {bottles} bottles
            <input
              type="range"
              min={5}
              max={200}
              step={5}
              value={bottles}
              onChange={(e) => setBottles(Number(e.target.value))}
            />
          </label>
          {plan && (
            <div className="small">
              {plan.orders.length === 0 ? (
                <p>All ingredients on hand.</p>
              ) : (
                <ul className="plain">
                  {plan.orders.map((o) => (
                    <li key={o.ingredientId}>
                      Order {o.units} oz {t(content, `ingredient.${o.ingredientId}.name`)} —{' '}
                      {money(o.cents)}
                    </li>
                  ))}
                </ul>
              )}
              <p>
                Bottles, corks and labels {money(plan.bottling)}. <b>Total {money(plan.total)}</b>{' '}
                of your {money(game.obs.cashCents)}.
              </p>
            </div>
          )}
          <p className="small muted">Stills free this month: {game.obs.stillsFree}</p>
          <button className="primary" onClick={brew} disabled={!plan || game.obs.stillsFree === 0}>
            Order ingredients and fire the still
          </button>
        </>
      )}
    </section>
  );
}

export function CounterPanel({ game }: { game: Game }) {
  const { obs } = game;
  return (
    <section className="paper wide">
      <h2>The Counter</h2>
      {obs.formulas.map((f) => (
        <div key={f.id} className="card">
          <h3>{f.name}</h3>
          <p className="small">
            {f.stock} on the shelf · sells at {money(f.priceCents)}
            <button
              onClick={() =>
                game.act({
                  type: 'SetPrice',
                  formulaId: f.id,
                  priceCents: Math.max(0, f.priceCents - 5),
                })
              }
            >
              −5¢
            </button>
            <button
              onClick={() =>
                game.act({ type: 'SetPrice', formulaId: f.id, priceCents: f.priceCents + 5 })
              }
            >
              +5¢
            </button>
          </p>
          <details>
            <summary>
              The label:{' '}
              {f.claims.length
                ? f.claims.map((c) => t(content, `claim.${c}.name`)).join('; ')
                : 'no claims'}
            </summary>
            {obs.claims.map((id) => (
              <label key={id} className="claim">
                <input
                  type="checkbox"
                  checked={f.claims.includes(id)}
                  onChange={(e) =>
                    game.act({
                      type: 'SetClaims',
                      formulaId: f.id,
                      claims: e.target.checked
                        ? [...f.claims, id]
                        : f.claims.filter((c) => c !== id),
                    })
                  }
                />{' '}
                {t(content, `claim.${id}.name`)}
              </label>
            ))}
          </details>
        </div>
      ))}
      <h3>
        Your regulars ({obs.customers.length}/{obs.customerCapacity})
      </h3>
      <table>
        <tbody>
          {obs.customers.map((c) => (
            <tr key={c.id}>
              <td>
                {c.name}
                {c.consumer === 'infant' ? ' (for the baby)' : ''}
              </td>
              <td className="muted">{t(content, `ailment.${c.ailment}.name`)}</td>
              <td className={`looks-${c.looks}`}>{c.looks.replace('_', ' ')}</td>
              <td>
                {c.mood}
                {c.craving ? ' · asks for it by name' : ''}
                {c.withdrawal ? ' · shaking' : ''}
              </td>
              <td>{c.bottlesLastMonth} last month</td>
            </tr>
          ))}
        </tbody>
      </table>
      {obs.canAdvertise && (
        <p>
          Newspaper advertising: {money(obs.adSpendCents)} a month{' '}
          {[0, 300, 800, 1500].map((c) => (
            <button
              key={c}
              className={obs.adSpendCents === c ? 'on' : ''}
              onClick={() => game.act({ type: 'SetAdSpend', cents: c })}
            >
              {money(c)}
            </button>
          ))}
        </p>
      )}
    </section>
  );
}

export function DeskPanel({ game }: { game: Game }) {
  const { obs, state } = game;
  const last = obs.lastMonth;
  return (
    <section className="paper wide">
      <h2>The Desk</h2>
      <h3>The Post</h3>
      {obs.pendingEvents.length === 0 && <p className="muted">No letters this month.</p>}
      {obs.pendingEvents.map((ev) => {
        const params = {
          name: ev.subjectName || 'a customer',
          cost: ev.bribeCostCents === undefined ? '' : money(ev.bribeCostCents),
        };
        return (
          <article key={ev.instanceId} className="letter">
            <h4>{t(content, `event.${ev.eventId}.title`, params)}</h4>
            <p>{t(content, `event.${ev.eventId}.body`, params)}</p>
            {(content.byId.events.get(ev.eventId)?.choices ?? []).map((c) => (
              <button
                key={c.id}
                disabled={!ev.choiceIds.includes(c.id)}
                onClick={() =>
                  game.act({ type: 'ResolveEvent', instanceId: ev.instanceId, choiceId: c.id })
                }
              >
                {t(content, `event.${ev.eventId}.choice.${c.id}`, params)}
              </button>
            ))}
          </article>
        );
      })}
      <h3>The Ledger {last ? `— ${monthName(last.date.month)} ${last.date.year}` : ''}</h3>
      {last ? (
        <div className="ledger">
          {(['cash', 'reputation', 'heat'] as const).map((m) => (
            <div key={m}>
              <h4>{m === 'heat' ? 'Talk' : m === 'cash' ? 'Money' : 'Good name'}</h4>
              <ul className="plain">
                {Object.entries(deltasByCause(last, m)).map(([cause, amount]) => (
                  <li
                    key={cause}
                    className={
                      (amount ?? 0) < 0
                        ? m === 'heat'
                          ? 'good'
                          : 'bad'
                        : m === 'heat'
                          ? 'bad'
                          : 'good'
                    }
                  >
                    {t(content, `cause.${cause}`)}:{' '}
                    {m === 'cash' ? money(amount ?? 0) : (amount ?? 0).toFixed(1)}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      ) : (
        <p className="muted">No month has closed yet.</p>
      )}
      {obs.deaths.length > 0 && (
        <>
          <h3>In memoriam</h3>
          <ul className="plain small">
            {[...obs.deaths]
              .reverse()
              .slice(0, 8)
              .map((d) => (
                <li key={d.id}>
                  {monthName(d.date.month)} {d.date.year}: {deathName(content, d)}
                  {d.verdict
                    ? ` — “${t(content, `verdict.${d.verdict}`)}”`
                    : d.inquest === 'pending'
                      ? ' — inquest to sit'
                      : ''}
                </li>
              ))}
          </ul>
        </>
      )}
      <button className="primary bell" onClick={game.endMonth} disabled={obs.status !== 'running'}>
        🔔 Ring the bell: close the month
      </button>
      {last && (
        <details>
          <summary>Last month’s news</summary>
          <ul className="plain small">
            {last.news.map((n, i) => (
              <li key={i}>{newsText(content, state, n)}</li>
            ))}
          </ul>
        </details>
      )}
    </section>
  );
}
