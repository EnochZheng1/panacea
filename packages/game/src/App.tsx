import type { ObservedCustomer } from '@panacea/sim';
import { useEffect, useRef, useState } from 'react';
import type { Station } from './scene/CameraRig.tsx';
import { ingredientColor, mixColor } from './scene/liquids.ts';
import { Scene } from './scene/Scene.tsx';
import { content, useGame } from './state/useGame.ts';
import { Broadsheet } from './ui/Broadsheet.tsx';
import { SealButton, Settings } from './ui/controls.tsx';
import { Calendar, Medallion, Odometer, Regulars, TalkGauge } from './ui/instruments.tsx';
import { Icon, Plate, type IconName } from './ui/ornaments.tsx';
import { BenchPanel, CounterPanel, DeskPanel, OverviewPanel, StillPanel } from './ui/Panels.tsx';
import { setMuted, sfx } from './ui/sound.ts';
import { money, t } from './ui/text.ts';
import { TitleScreen } from './ui/TitleScreen.tsx';

const STATION_KEYS: Station[] = ['overview', 'bench', 'still', 'counter', 'desk'];
const STATION_META: Record<Station, { name: string; icon: IconName }> = {
  overview: { name: 'The Shop', icon: 'shop' },
  bench: { name: 'The Bench', icon: 'bench' },
  still: { name: 'The Still', icon: 'still' },
  counter: { name: 'The Counter', icon: 'counter' },
  desk: { name: 'The Desk', icon: 'desk' },
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
function usePref(key: string, fallback: boolean) {
  const [value, setValue] = useState(() => readPref(key, fallback));
  return [value, (v: boolean) => (setValue(v), writePref(key, v))] as const;
}

export function App() {
  const game = useGame();
  const { obs, state } = game;
  const [started, setStarted] = useState(false);
  const [fontsReady, setFontsReady] = useState(false);
  const [station, setStationRaw] = useState<Station>('overview');
  const [collapsed, setCollapsed] = useState(false);
  const [ink, setInk] = usePref('panacea.ink', true);
  const [reducedMotion, setReducedMotion] = usePref(
    'panacea.reducedMotion',
    window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false,
  );
  const [sound, setSound] = usePref('panacea.sound', true);
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
  const lastCash = useRef(obs.cashCents);

  useEffect(() => setMuted(!sound), [sound]);

  const setStation = (s: Station) => {
    if (s !== station) sfx.paper();
    setStationRaw(s);
    setCollapsed(false);
  };

  useEffect(() => {
    Promise.all([
      document.fonts.load('32px "IM Fell English"'),
      document.fonts.load('32px "IM Fell English SC"'),
      document.fonts.load('32px "IM Fell Double Pica"'),
    ])
      .catch(() => undefined)
      .finally(() => setFontsReady(true));
  }, []);

  // The still cools when the month turns.
  useEffect(() => {
    if (still.turn !== -1 && still.turn !== state.meta.turn)
      setStill((s) => ({ ...s, pressure: 0, turn: -1 }));
  }, [state.meta.turn, still.turn]);

  // The till rings when money comes in at month's end.
  useEffect(() => {
    if (game.monthReport && game.monthReport.sales.length > 0) sfx.till();
    lastCash.current = obs.cashCents;
  }, [game.monthReport, obs.cashCents]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement)?.tagName;
      if (!started || tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA') return;
      const i = Number(e.key) - 1;
      if (i >= 0 && i < STATION_KEYS.length) setStation(STATION_KEYS[i]!);
      if (e.key === 'Escape') setStation('overview');
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });

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
    sfx.pour();
  };

  const onBrew = (formulaId: string) => {
    const f = state.lab.formulas.find((x) => x.id === formulaId);
    setStill({
      pressure: 0.82,
      color: f ? mixColor(f.components, content) : still.color,
      received: 1,
      turn: state.meta.turn,
    });
    sfx.fire();
  };

  const ringBell = () => {
    sfx.bell();
    game.endMonth();
  };

  return (
    <div className={`game ${started ? '' : 'at-title'} ${reducedMotion ? 'calm' : ''}`}>
      {fontsReady && (
        <Scene
          game={game}
          station={started ? station : 'overview'}
          setStation={setStation}
          ink={ink}
          reducedMotion={reducedMotion}
          mix={mix}
          pour={pour}
          onPour={onPour}
          onHoverIngredient={setHoverIngredient}
          onHoverCustomer={setHoverCustomer}
          onBell={ringBell}
          still={still}
        />
      )}

      {!started && (
        <TitleScreen
          onStart={(bg) => {
            sfx.bell();
            game.newRun('panacea', bg);
            setStarted(true);
          }}
        />
      )}

      {started && (
        <>
          <header className="hud">
            <Calendar month={obs.date.month} year={obs.date.year} />
            <div className="till">
              <small>The Till</small>
              <Odometer cents={obs.cashCents} />
            </div>
            <Medallion value={obs.reputation} />
            <TalkGauge heat={obs.heat} />
            <Regulars count={obs.customers.length} capacity={obs.customerCapacity} />
            <span className="spacer" />
            <Settings
              ink={ink}
              setInk={setInk}
              reducedMotion={reducedMotion}
              setReducedMotion={setReducedMotion}
              sound={sound}
              setSound={setSound}
            />
          </header>

          <aside className={`panel-host ${collapsed ? 'collapsed' : ''}`} key={station}>
            <button
              className="collapse"
              onClick={() => setCollapsed(!collapsed)}
              aria-label={collapsed ? 'Show the papers' : 'Put the papers aside'}
            >
              {collapsed ? '☜ Papers' : '☞'}
            </button>
            {!collapsed && (
              <>
                {station === 'overview' && (
                  <OverviewPanel game={game} go={(i) => setStation(STATION_KEYS[i]!)} />
                )}
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
                {station === 'desk' && <DeskPanel game={game} onBell={ringBell} />}
              </>
            )}
          </aside>

          {(hoverIngredient || hoverCustomer) && (
            <div className="tag-tooltip" role="tooltip">
              {hoverIngredient && (
                <>
                  <b>{t(content, `ingredient.${hoverIngredient}.name`)}</b>
                  <p>{t(content, `ingredient.${hoverIngredient}.description`)}</p>
                  <p className="fineprint">
                    {(obs.inventory[hoverIngredient] ?? 0).toFixed(1)} oz on hand ·{' '}
                    {money(
                      Math.round(
                        (obs.ingredients.find((i) => i.id === hoverIngredient)?.costPerUnitCents ??
                          0) * 100,
                      ) / 100,
                    )}{' '}
                    the ounce
                    {station === 'bench' ? ' · click to pour' : ''}
                  </p>
                </>
              )}
              {hoverCustomer && (
                <>
                  <b>{hoverCustomer.name}</b>
                  <p>
                    {t(content, `ailment.${hoverCustomer.ailment}.name`)}. Looks{' '}
                    {hoverCustomer.looks.replace('_', ' ')}; {hoverCustomer.mood}.
                  </p>
                  {hoverCustomer.craving && (
                    <p className="fineprint">Asks for your tonic by name.</p>
                  )}
                  {hoverCustomer.withdrawal && (
                    <p className="fineprint red">Hands shaking. Sweating.</p>
                  )}
                </>
              )}
            </div>
          )}

          <nav className="stations" aria-label="Stations">
            {STATION_KEYS.map((s, i) => (
              <button
                key={s}
                className={station === s ? 'on' : ''}
                onClick={() => setStation(s)}
                aria-current={station === s}
              >
                <Icon name={STATION_META[s].icon} size={24} />
                <span>{STATION_META[s].name}</span>
                {s === 'desk' && obs.pendingEvents.length > 0 && (
                  <em className="badge">{obs.pendingEvents.length}</em>
                )}
                <kbd>{i + 1}</kbd>
              </button>
            ))}
          </nav>

          <div className="notes" aria-live="polite">
            {game.toasts.map((x) => (
              <div key={x.id} className={`note ${x.tone}`}>
                {x.text}
              </div>
            ))}
          </div>

          {game.monthReport && (
            <Broadsheet
              report={game.monthReport}
              state={state}
              onClose={() => (sfx.paper(), game.setMonthReport(null))}
            />
          )}

          {obs.status === 'ended' && (
            <div className="overlay">
              <Plate
                tone="mourning"
                kicker="Finis"
                title={
                  obs.endReason === 'shut_down'
                    ? 'Closed by Order of the Board of Health'
                    : obs.endReason === 'bankrupt'
                      ? 'Bankrupt'
                      : 'The Year 1906'
                }
              >
                <p className="lede">
                  {obs.deaths.length} people who took your medicine are dead. You end with{' '}
                  {money(obs.cashCents)}.
                </p>
                <SealButton onClick={() => (game.newRun(`run-${Date.now()}`), setStarted(false))}>
                  Begin again
                </SealButton>
              </Plate>
            </div>
          )}
        </>
      )}
    </div>
  );
}
