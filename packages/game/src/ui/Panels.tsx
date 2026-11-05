import { PROPERTY_KEYS } from '@panacea/content';
import { deltasByCause, type FormulaComponent, type ObservedCustomer } from '@panacea/sim';
import { useState } from 'react';
import { mixColor } from '../scene/liquids.ts';
import { analyze, content, type Game } from '../state/useGame.ts';
import { Dial, SealButton, Stamp } from './controls.tsx';
import { Icon, Plate, Rule, type IconName } from './ornaments.tsx';
import { sfx } from './sound.ts';
import { deathName, money, monthName, t } from './text.ts';

const PROPERTY_LABELS: Record<string, string> = {
  kick: 'Kick',
  calm: 'Calm',
  habit: 'Habit',
  acuteToxicity: 'Danger',
  accumulation: 'Lingers',
  bitterness: 'Bitter',
  detectability: 'Detectable',
};

/** The assay: each property as an engraved ten-cell meter. */
export function Assay({ components }: { components: FormulaComponent[] }) {
  if (components.length === 0)
    return <p className="hint">The glass is empty. Click a bottle on the shelf.</p>;
  const a = analyze(components);
  return (
    <div className="assay">
      {PROPERTY_KEYS.map((k) => {
        const v = a.properties[k];
        const danger = (k === 'acuteToxicity' && v > 3) || (k === 'accumulation' && v > 1.5);
        const cells = Math.round(Math.min(10, Math.max(0, v)));
        return (
          <div key={k} className={`meter ${danger ? 'danger' : ''}`}>
            <span className="meter-label">
              {PROPERTY_LABELS[k]}
              {danger && <Icon name="skull" size={14} />}
            </span>
            <span className="cells" aria-label={`${PROPERTY_LABELS[k]} ${v.toFixed(1)}`}>
              {Array.from({ length: 10 }, (_, i) => (
                <i key={i} className={i < cells ? 'on' : ''} />
              ))}
            </span>
            <em>{v.toFixed(1)}</em>
          </div>
        );
      })}
      <p className="fineprint">Costs {money(Math.round(a.unitCostCents))} a bottle to make.</p>
    </div>
  );
}

const STATIONS_INTRO: [IconName, string, string][] = [
  ['bench', 'The Bench', 'Pour drugs into the measuring glass and inscribe a formula.'],
  ['still', 'The Still', 'Requisition ingredients and fire a batch.'],
  ['counter', 'The Counter', 'Design the label, set the price, mind your regulars.'],
  ['desk', 'The Desk', 'Open the post, read the Ledger, ring the bell to close the month.'],
];

export function OverviewPanel({ game, go }: { game: Game; go: (i: number) => void }) {
  const { obs } = game;
  const stock = obs.formulas.reduce((n, f) => n + f.stock, 0);
  return (
    <Plate kicker="Proprietor's Notice" title="Panacea" tone="card">
      <p className="lede">
        One room, one still, a dozen regulars, and no law against anything you might put in a
        bottle.
      </p>
      <ul className="station-list">
        {STATIONS_INTRO.map(([icon, name, text], i) => (
          <li key={name}>
            <button onClick={() => go(i + 1)}>
              <Icon name={icon} size={26} />
              <span>
                <b>{name}</b>
                <small>{text}</small>
              </span>
              <kbd>{i + 2}</kbd>
            </button>
          </li>
        ))}
      </ul>
      <Rule glyph="✠" />
      <p className="fineprint center">
        {obs.formulas.length} formula{obs.formulas.length === 1 ? '' : 's'} · {stock} bottles on the
        shelf · {obs.pendingEvents.length} letter{obs.pendingEvents.length === 1 ? '' : 's'} waiting
      </p>
    </Plate>
  );
}

const POURS: [number, string][] = [
  [0.1, '⅒'],
  [0.25, '¼'],
  [0.5, '½'],
  [1, '1'],
];

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
  const [inscribed, setInscribed] = useState(0);
  const colour = mixColor(components, content);

  const topUp = () =>
    volume < capacity && setMix({ ...mix, water: (mix['water'] ?? 0) + (capacity - volume) });
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
    if (r.ok) {
      sfx.stamp();
      setInscribed(Date.now());
    }
  };

  return (
    <Plate kicker="℞ Compounding" title="The Bench" className="prescription">
      <div className="measures" role="group" aria-label="Pour size">
        <span className="fineprint">Pour</span>
        {POURS.map(([n, glyph]) => (
          <button
            key={n}
            className={`token ${pourSize === n ? 'on' : ''}`}
            onClick={() => setPourSize(n)}
            aria-pressed={pourSize === n}
          >
            {glyph}
            <small>oz</small>
          </button>
        ))}
      </div>

      <div className="recipe">
        <div className="cylinder" aria-label={`${volume.toFixed(2)} of ${capacity} fluid ounces`}>
          <div
            className="cyl-fill"
            style={{
              height: `${Math.min(100, (volume / capacity) * 100)}%`,
              background: colour,
              boxShadow: `0 0 18px ${colour}`,
            }}
          />
          {Array.from({ length: 8 }, (_, i) => (
            <i key={i} style={{ bottom: `${((i + 1) / 8) * 100}%` }} />
          ))}
        </div>
        <ol className="ingredients">
          {components.map((c) => (
            <li key={c.ingredientId}>
              <span
                className="drop"
                style={{
                  background: content.byId.ingredients.get(c.ingredientId)?.appearance.color,
                }}
              />
              <span className="ing-name">{t(content, `ingredient.${c.ingredientId}.name`)}</span>
              <span className="leader" />
              <em>{c.amount.toFixed(2)} oz</em>
              <button
                className="strike"
                aria-label="Remove"
                onClick={() =>
                  setMix(
                    Object.fromEntries(Object.entries(mix).filter(([k]) => k !== c.ingredientId)),
                  )
                }
              >
                ✕
              </button>
            </li>
          ))}
          {components.length === 0 && <li className="hint">Nothing measured yet.</li>}
          <li className={`volume ${volume > capacity ? 'over' : ''}`}>
            {volume.toFixed(2)} of {capacity} fl oz
            {volume > capacity ? ' — too much for one bottle' : ''}
          </li>
        </ol>
      </div>
      <div className="row">
        <button className="paper-button" onClick={topUp} disabled={volume >= capacity}>
          Top up with water
        </button>
        <button className="paper-button" onClick={() => setMix({})}>
          Empty the glass
        </button>
      </div>

      <Rule glyph="❧" />
      <Assay components={components} />
      <Rule glyph="❧" />

      <div className="formbook">
        <div className="tabs" role="tablist" aria-label="Formula book">
          <button
            role="tab"
            aria-selected={target === 'new'}
            className={target === 'new' ? 'on' : ''}
            onClick={() => setTarget('new')}
          >
            New formula
          </button>
          {formulas.map((f) => (
            <button
              key={f.id}
              role="tab"
              aria-selected={target === f.id}
              className={target === f.id ? 'on' : ''}
              onClick={() => setTarget(f.id)}
            >
              Revise “{f.name}”
            </button>
          ))}
        </div>
        {target === 'new' && (
          <div className="inscription">
            <label>
              <span className="fineprint">Name</span>
              <input className="inked" value={name} onChange={(e) => setName(e.target.value)} />
            </label>
            <label className="price-tag">
              <span className="fineprint">Price</span>
              <input
                className="inked"
                type="number"
                min={0}
                step={5}
                value={price}
                onChange={(e) => setPrice(Number(e.target.value))}
              />
              <span>¢</span>
            </label>
          </div>
        )}
        <div className="seal-row">
          <SealButton onClick={record} disabled={components.length === 0 || volume > capacity}>
            {target === 'new' ? 'Inscribe in the formula book' : 'Quietly revise the formula'}
          </SealButton>
          {inscribed > 0 && (
            <Stamp key={inscribed} text={target === 'new' ? 'Inscribed' : 'Revised'} />
          )}
        </div>
      </div>
    </Plate>
  );
}

export function StillPanel({ game, onBrew }: { game: Game; onBrew: (formulaId: string) => void }) {
  const formulas = game.state.lab.formulas;
  const [formulaId, setFormulaId] = useState(formulas[0]?.id ?? '');
  const [wanted, setBottles] = useState(30);
  // a still makes at most its copper's worth a batch
  const bottles = Math.min(wanted, game.obs.stillBottles);
  const fid = formulas.some((f) => f.id === formulaId) ? formulaId : (formulas[0]?.id ?? '');
  const plan = fid ? game.brewPlan(fid, bottles) : null;
  const busy = game.obs.stillsFree === 0;
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
    if (r.ok) onBrew(fid);
  };
  return (
    <Plate kicker="Requisition & Distillation" title="The Still" className="requisition">
      {formulas.length === 0 ? (
        <p className="hint">Your formula book is empty. Compound something at the bench first.</p>
      ) : (
        <>
          <div className="formula-cards" role="radiogroup" aria-label="Formula">
            {formulas.map((f) => (
              <button
                key={f.id}
                role="radio"
                aria-checked={fid === f.id}
                className={`formula-card ${fid === f.id ? 'on' : ''}`}
                onClick={() => setFormulaId(f.id)}
              >
                <span
                  className="drop big"
                  style={{ background: mixColor(f.components, content) }}
                />
                {f.name}
              </button>
            ))}
          </div>
          <Dial
            label="Batch size"
            unit="bottles"
            value={bottles}
            min={4}
            max={game.obs.stillBottles}
            step={4}
            onChange={setBottles}
          />
          {plan && (
            <div className="receipt">
              <p className="kicker">Order of goods</p>
              {plan.orders.length === 0 && (
                <p className="fineprint center">All ingredients on hand.</p>
              )}
              {plan.orders.map((o) => (
                <div key={o.ingredientId} className="line">
                  <span>
                    {o.units} oz {t(content, `ingredient.${o.ingredientId}.name`)}
                  </span>
                  <span className="leader" />
                  <span>{money(o.cents)}</span>
                </div>
              ))}
              <div className="line">
                <span>{bottles} bottles, corks & labels</span>
                <span className="leader" />
                <span>{money(plan.bottling)}</span>
              </div>
              <div className={`line total ${plan.total > game.obs.cashCents ? 'over' : ''}`}>
                <span>Total</span>
                <span className="leader" />
                <span>{money(plan.total)}</span>
              </div>
              <p className="fineprint center">You have {money(game.obs.cashCents)} in the till.</p>
            </div>
          )}
          <div className="seal-row">
            <button
              className="fire-button"
              onClick={brew}
              disabled={!plan || busy || plan.total > game.obs.cashCents}
            >
              <Icon name="flame" size={26} />
              {busy ? 'The still is fired' : 'Order & fire the still'}
            </button>
            {busy && <Stamp text="Brewing" tone="black" />}
          </div>
        </>
      )}
    </Plate>
  );
}

/** The product's label, set like a chromolithographed trade card. */
export function LabelPreview({
  name,
  claims,
  priceCents,
  colour,
}: {
  name: string;
  claims: string[];
  priceCents: number;
  colour: string;
}) {
  return (
    <div className="label-preview" style={{ ['--liquor' as string]: colour }}>
      <div className="label-inner">
        <p className="label-kicker">Prepared only at the Panacea Pharmacy, New York</p>
        <h3>{name}</h3>
        <div className="banners">
          {claims.length === 0 && <span className="banner muted">— no claims printed —</span>}
          {claims.map((c) => (
            <span key={c} className="banner">
              {t(content, `claim.${c}.name`)}
            </span>
          ))}
        </div>
        <p className="label-price">
          <span>Price</span> {money(priceCents)}
        </p>
      </div>
    </div>
  );
}

const MOOD_GLYPH: Record<ObservedCustomer['mood'], string> = {
  delighted: '☺',
  content: '·',
  grumbling: '☹',
  unhappy: '☹',
};

function CallingCard({ c }: { c: ObservedCustomer }) {
  return (
    <li className={`calling-card looks-${c.looks} ${c.withdrawal ? 'shaking' : ''}`}>
      <svg viewBox="0 0 40 40" className="cameo" aria-hidden="true">
        <ellipse cx="20" cy="20" rx="18" ry="19" fill="#2a1a10" />
        <circle cx="20" cy="15" r="7" fill="#efe3c8" />
        <path d="M8 36 Q8 24 20 24 Q32 24 32 36 Z" fill="#efe3c8" />
      </svg>
      <div>
        <b>{c.name}</b>
        <small>
          {t(content, `ailment.${c.ailment}.name`)}
          {c.consumer === 'infant' ? ' (the baby)' : ''}
        </small>
        <span className="tags">
          <span title="Looks">{c.looks.replace('_', ' ')}</span>
          <span title={`Mood: ${c.mood}`}>
            {MOOD_GLYPH[c.mood]} {c.mood}
          </span>
          {c.craving && <span className="tag-dark">asks for it by name</span>}
          {c.withdrawal && <span className="tag-red">shaking</span>}
        </span>
      </div>
      <span className="tally" title="Bottles bought last month">
        {'|'.repeat(Math.min(6, c.bottlesLastMonth)) || '–'}
      </span>
    </li>
  );
}

export function CounterPanel({ game }: { game: Game }) {
  const { obs, state } = game;
  const [selected, setSelected] = useState(obs.formulas[0]?.id ?? '');
  const f = obs.formulas.find((x) => x.id === selected) ?? obs.formulas[0];
  const formula = f ? state.lab.formulas.find((x) => x.id === f.id) : undefined;
  return (
    <Plate kicker="The Shop Floor" title="The Counter">
      {f && formula ? (
        <>
          {obs.formulas.length > 1 && (
            <div className="tabs">
              {obs.formulas.map((x) => (
                <button
                  key={x.id}
                  className={x.id === f.id ? 'on' : ''}
                  onClick={() => setSelected(x.id)}
                >
                  {x.name}
                </button>
              ))}
            </div>
          )}
          <LabelPreview
            name={f.name}
            claims={f.claims}
            priceCents={f.priceCents}
            colour={mixColor(formula.components, content)}
          />
          <div className="row center">
            <button
              className="paper-button"
              onClick={() =>
                game.act({
                  type: 'SetPrice',
                  formulaId: f.id,
                  priceCents: Math.max(0, f.priceCents - 5),
                })
              }
            >
              − 5¢
            </button>
            <span className="stock-count">{f.stock} on the shelf</span>
            <button
              className="paper-button"
              onClick={() =>
                game.act({ type: 'SetPrice', formulaId: f.id, priceCents: f.priceCents + 5 })
              }
            >
              + 5¢
            </button>
          </div>
          <p className="kicker">Claims for the label</p>
          <div className="claim-chips">
            {obs.claims.map((id) => {
              const claim = content.byId.claims.get(id);
              const on = f.claims.includes(id);
              return (
                <button
                  key={id}
                  className={`chip ${on ? 'on' : ''}`}
                  aria-pressed={on}
                  onClick={() => {
                    sfx.paper();
                    game.act({
                      type: 'SetClaims',
                      formulaId: f.id,
                      claims: on ? f.claims.filter((c) => c !== id) : [...f.claims, id],
                    });
                  }}
                  title={`Boldness ${claim?.boldness ?? 0}: bolder claims sell more and draw more talk`}
                >
                  {t(content, `claim.${id}.name`)}
                  <span className="boldness">
                    {'✦'.repeat(Math.max(1, Math.round(claim?.boldness ?? 0)))}
                  </span>
                </button>
              );
            })}
          </div>
        </>
      ) : (
        <p className="hint">
          Nothing to sell yet. Inscribe a formula at the bench and fire the still.
        </p>
      )}
      {obs.canAdvertise && (
        <>
          <p className="kicker">Newspaper advertising</p>
          <div className="row">
            {[0, 300, 800, 1500].map((c) => (
              <button
                key={c}
                className={`token wide ${obs.adSpendCents === c ? 'on' : ''}`}
                onClick={() => game.act({ type: 'SetAdSpend', cents: c })}
              >
                {c === 0 ? 'None' : `${money(c)}/mo`}
              </button>
            ))}
          </div>
        </>
      )}
      <Rule glyph="❦" />
      <p className="kicker">
        Your regulars · {obs.customers.length} of {obs.customerCapacity}
      </p>
      <ul className="calling-cards">
        {obs.customers.map((c) => (
          <CallingCard key={c.id} c={c} />
        ))}
      </ul>
    </Plate>
  );
}

export function DeskPanel({ game, onBell }: { game: Game; onBell: () => void }) {
  const { obs } = game;
  const last = obs.lastMonth;
  const [open, setOpen] = useState<string | null>(obs.pendingEvents[0]?.instanceId ?? null);
  const cash = last ? Object.entries(deltasByCause(last, 'cash')) : [];
  return (
    <Plate kicker="Correspondence & Accounts" title="The Desk">
      <p className="kicker">The Post</p>
      {obs.pendingEvents.length === 0 && <p className="hint">No letters this month.</p>}
      <div className="letters">
        {obs.pendingEvents.map((ev) => {
          const params = {
            name: ev.subjectName || 'a customer',
            cost: ev.bribeCostCents === undefined ? '' : money(ev.bribeCostCents),
          };
          const isOpen = open === ev.instanceId;
          const official = ev.eventId === 'inquest' || ev.eventId === 'board_of_health';
          return (
            <article
              key={ev.instanceId}
              className={`envelope ${isOpen ? 'open' : ''} ${official ? 'official' : ''}`}
            >
              <button
                className="envelope-face"
                onClick={() => {
                  sfx.paper();
                  setOpen(isOpen ? null : ev.instanceId);
                }}
                aria-expanded={isOpen}
              >
                <Icon name="seal" size={28} />
                <span>{t(content, `event.${ev.eventId}.title`, params)}</span>
              </button>
              {isOpen && (
                <div className="letter-body">
                  <p>{t(content, `event.${ev.eventId}.body`, params)}</p>
                  <div className="replies">
                    {(content.byId.events.get(ev.eventId)?.choices ?? []).map((c) => (
                      <button
                        key={c.id}
                        className="reply"
                        disabled={!ev.choiceIds.includes(c.id)}
                        onClick={() => {
                          const r = game.act({
                            type: 'ResolveEvent',
                            instanceId: ev.instanceId,
                            choiceId: c.id,
                          });
                          if (r.ok) (ev.eventId === 'inquest' ? sfx.stamp : sfx.paper)();
                        }}
                      >
                        ☞ {t(content, `event.${ev.eventId}.choice.${c.id}`, params)}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </article>
          );
        })}
      </div>

      <p className="kicker">
        The Ledger{last ? ` · ${monthName(last.date.month)} ${last.date.year}` : ''}
      </p>
      {last ? (
        <table className="ledger-page">
          <thead>
            <tr>
              <th>Particulars</th>
              <th>Dr.</th>
              <th>Cr.</th>
            </tr>
          </thead>
          <tbody>
            {cash.map(([cause, amount]) => (
              <tr key={cause}>
                <td>{t(content, `cause.${cause}`)}</td>
                <td className="debit">{(amount ?? 0) < 0 ? money(-(amount ?? 0)) : ''}</td>
                <td>{(amount ?? 0) > 0 ? money(amount ?? 0) : ''}</td>
              </tr>
            ))}
            <tr className="balance">
              <td>Balance carried forward</td>
              <td colSpan={2}>{money(last.cashEndCents)}</td>
            </tr>
          </tbody>
        </table>
      ) : (
        <p className="hint">No month has closed yet.</p>
      )}

      {obs.deaths.length > 0 && (
        <div className="mourning">
          <p className="kicker">In memoriam</p>
          <ul>
            {[...obs.deaths]
              .reverse()
              .slice(0, 6)
              .map((d) => (
                <li key={d.id}>
                  <b>{deathName(content, d)}</b>
                  <small>
                    {monthName(d.date.month)} {d.date.year}
                    {d.verdict
                      ? ` · “${t(content, `verdict.${d.verdict}`)}”`
                      : d.inquest === 'pending'
                        ? ' · inquest to sit'
                        : ''}
                  </small>
                  {d.inquest === 'implicated' && <Stamp text="Your medicine named" />}
                  {d.inquest === 'bribed' && <Stamp text="Settled" tone="black" />}
                </li>
              ))}
          </ul>
        </div>
      )}

      <button className="bell-button" onClick={onBell} disabled={obs.status !== 'running'}>
        <svg viewBox="0 0 48 40" aria-hidden="true">
          <path
            d="M6 32 Q6 10 24 10 Q42 10 42 32 Z"
            fill="#c79a43"
            stroke="#5e4416"
            strokeWidth="2"
          />
          <rect x="3" y="32" width="42" height="5" rx="2" fill="#3c2617" />
          <circle cx="24" cy="7" r="3" fill="#c79a43" stroke="#5e4416" />
        </svg>
        <span>
          Ring the bell
          <small>Close the month</small>
        </span>
      </button>
    </Plate>
  );
}
