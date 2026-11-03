import type { ObservedCustomer } from '@panacea/sim';
import { useEffect, useState } from 'react';
import type { Station } from './scene/CameraRig.tsx';
import { ingredientColor, mixColor } from './scene/liquids.ts';
import { Scene } from './scene/Scene.tsx';
import { content, useGame } from './state/useGame.ts';
import { BenchPanel, CounterPanel, DeskPanel, OverviewPanel, StillPanel } from './ui/Panels.tsx';
import { money, monthName, newsText, t } from './ui/text.ts';

const STATION_KEYS: Station[] = ['overview', 'bench', 'still', 'counter', 'desk'];
const STATION_NAMES: Record<Station, string> = {
  overview: 'The Shop',
  bench: 'The Bench',
  still: 'The Still',
  counter: 'The Counter',
  desk: 'The Desk',
};

function readPref(key: string, fallback: boolean): boolean {
  try {
    const v = localStorage.getItem(key);
    return v === null ? fallback : v === '1';
  } catch {
    return fallback;
  }
}
function writePref(key: string, value: boolean) {
  try {
    localStorage.setItem(key, value ? '1' : '0');
  } catch {
    /* storage unavailable: the preference lasts for this session */
  }
}

export function App() {
  const game = useGame();
  const { obs, state } = game;
  const [fontsReady, setFontsReady] = useState(false);
  const [station, setStation] = useState<Station>('overview');
  const [ink, setInk] = useState(() => readPref('panacea.ink', true));
  const [reducedMotion, setReducedMotion] = useState(() =>
    readPref(
      'panacea.reducedMotion',
      window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false,
    ),
  );
  const [mix, setMix] = useState<Record<string, number>>({
    grain_alcohol: 2,
    laudanum: 0.25,
    sugar_syrup: 1,
  });
  const [pourSize, setPourSize] = useState(0.25);
  const [pour, setPour] = useState<{ key: number; color: string } | null>(null);
  const [hoverIngredient, setHoverIngredient] = useState<string | null>(null);
  const [hoverCustomer, setHoverCustomer] = useState<ObservedCustomer | null>(null);
  const [still, setStill] = useState({ pressure: 0, color: '#c2461b', received: 0, turn: -1 });

  useEffect(() => {
    Promise.all([
      document.fonts.load('32px "IM Fell English"'),
      document.fonts.load('32px "IM Fell English SC"'),
    ])
      .catch(() => undefined)
      .finally(() => setFontsReady(true));
  }, []);

  // The still cools when the month turns.
  useEffect(() => {
    if (still.turn !== -1 && still.turn !== state.meta.turn)
      setStill((s) => ({ ...s, pressure: 0, turn: -1 }));
  }, [state.meta.turn, still.turn]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (
        (e.target as HTMLElement)?.tagName === 'INPUT' ||
        (e.target as HTMLElement)?.tagName === 'SELECT'
      )
        return;
      const i = Number(e.key) - 1;
      if (i >= 0 && i < STATION_KEYS.length) setStation(STATION_KEYS[i]!);
      if (e.key === 'Escape') setStation('overview');
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  const onPour = (ingredientId: string) => {
    const volume = Object.values(mix).reduce((s, v) => s + v, 0);
    const room = content.config.bottleCapacity - volume;
    if (room <= 0.001) {
      game.toast('The measuring glass is full.', 'error');
      return;
    }
    const amount = Math.min(pourSize, room);
    setMix({ ...mix, [ingredientId]: Math.round(((mix[ingredientId] ?? 0) + amount) * 100) / 100 });
    setPour({ key: Date.now(), color: ingredientColor(ingredientId, content) });
  };

  const onBrew = (formulaId: string) => {
    const f = state.lab.formulas.find((x) => x.id === formulaId);
    setStill({
      pressure: 0.82,
      color: f ? mixColor(f.components, content) : still.color,
      received: 1,
      turn: state.meta.turn,
    });
    game.toast('The still is fired. The batch will be bottled by month’s end.');
  };

  const report = game.monthReport;

  return (
    <div className="game">
      {fontsReady && (
        <Scene
          game={game}
          station={station}
          setStation={setStation}
          ink={ink}
          reducedMotion={reducedMotion}
          mix={mix}
          pour={pour}
          onPour={onPour}
          onHoverIngredient={setHoverIngredient}
          onHoverCustomer={setHoverCustomer}
          still={still}
        />
      )}

      <header className="hud">
        <span className="date">
          {monthName(obs.date.month)} {obs.date.year}
        </span>
        <span title="Cash">{money(obs.cashCents)}</span>
        <span title="Your good name">Good name {obs.reputation.toFixed(0)}</span>
        <span
          title="How much the neighbourhood, the press and the authorities are talking"
          className="heat"
        >
          Talk
          <span className="thermo">
            <span style={{ width: `${Math.min(100, obs.heat)}%` }} />
          </span>
        </span>
        <span title="Regulars">{obs.customers.length} regulars</span>
        <span className="spacer" />
        <label>
          <input
            type="checkbox"
            checked={ink}
            onChange={(e) => (setInk(e.target.checked), writePref('panacea.ink', e.target.checked))}
          />{' '}
          Engraving
        </label>
        <label>
          <input
            type="checkbox"
            checked={reducedMotion}
            onChange={(e) => (
              setReducedMotion(e.target.checked),
              writePref('panacea.reducedMotion', e.target.checked)
            )}
          />{' '}
          Reduce motion
        </label>
      </header>

      <aside className="panel-host">
        {station === 'overview' && <OverviewPanel />}
        {station === 'bench' && (
          <BenchPanel
            game={game}
            mix={mix}
            setMix={setMix}
            pourSize={pourSize}
            setPourSize={setPourSize}
          />
        )}
        {station === 'still' && <StillPanel game={game} onBrew={onBrew} />}
        {station === 'counter' && <CounterPanel game={game} />}
        {station === 'desk' && <DeskPanel game={game} />}
      </aside>

      {(hoverIngredient || hoverCustomer) && (
        <div className="tooltip">
          {hoverIngredient && (
            <>
              <b>{t(content, `ingredient.${hoverIngredient}.name`)}</b>
              <p>{t(content, `ingredient.${hoverIngredient}.description`)}</p>
              <p className="small">
                {(obs.inventory[hoverIngredient] ?? 0).toFixed(1)} oz on hand ·{' '}
                {money(
                  Math.round(
                    (obs.ingredients.find((i) => i.id === hoverIngredient)?.costPerUnitCents ?? 0) *
                      100,
                  ) / 100,
                )}
                /oz
              </p>
            </>
          )}
          {hoverCustomer && (
            <>
              <b>{hoverCustomer.name}</b>
              <p>
                {t(content, `ailment.${hoverCustomer.ailment}.name`)} · looks{' '}
                {hoverCustomer.looks.replace('_', ' ')} · {hoverCustomer.mood}
              </p>
              {hoverCustomer.craving && <p className="small">Asks for your tonic by name.</p>}
              {hoverCustomer.withdrawal && <p className="small">Hands shaking. Sweating.</p>}
            </>
          )}
        </div>
      )}

      <nav className="stations">
        {STATION_KEYS.map((s, i) => (
          <button key={s} className={station === s ? 'on' : ''} onClick={() => setStation(s)}>
            <kbd>{i + 1}</kbd> {STATION_NAMES[s]}
            {s === 'desk' && obs.pendingEvents.length > 0 ? ` (${obs.pendingEvents.length})` : ''}
          </button>
        ))}
      </nav>

      <div className="toasts">
        {game.toasts.map((x) => (
          <div key={x.id} className={`toast ${x.tone}`}>
            {x.text}
          </div>
        ))}
      </div>

      {report && (
        <div className="month-overlay" onClick={() => game.setMonthReport(null)}>
          <div className="broadsheet">
            <p className="masthead">The Evening Ledger</p>
            <h1>
              {monthName(report.date.month)} {report.date.year}
            </h1>
            <p className="summary">
              {report.sales.reduce((n, s) => n + s.bottles, 0)} bottles sold ·{' '}
              {money(report.cashEndCents - report.cashStartCents)} · {report.customers} regulars
            </p>
            <ul className="plain">
              {report.news.slice(0, 8).map((n, i) => (
                <li key={i}>{newsText(content, state, n)}</li>
              ))}
              {report.news.length === 0 && <li>A quiet month on the Bowery.</li>}
            </ul>
            <p className="small muted">Click to continue</p>
          </div>
        </div>
      )}

      {obs.status === 'ended' && (
        <div className="month-overlay">
          <div className="broadsheet">
            <h1>
              {obs.endReason === 'shut_down'
                ? 'Closed by Order of the Board of Health'
                : obs.endReason === 'bankrupt'
                  ? 'Bankrupt'
                  : 'The Year 1906'}
            </h1>
            <p>
              {obs.deaths.length} people died who took your medicine. You end with{' '}
              {money(obs.cashCents)}.
            </p>
            <button className="primary" onClick={() => game.newRun(`run-${Date.now()}`)}>
              Begin again
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
