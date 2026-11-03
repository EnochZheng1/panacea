import type { ObservedCustomer } from '@panacea/sim';
import { Canvas } from '@react-three/fiber';
import { InkPipeline } from '../render/InkPipeline.tsx';
import { content, type Game } from '../state/useGame.ts';
import { money, monthName, t } from '../ui/text.ts';
import { Bench } from './Bench.tsx';
import { CameraRig, STATIONS, type Station } from './CameraRig.tsx';
import { Counter } from './Counter.tsx';
import { Desk } from './Desk.tsx';
import { mixColor } from './liquids.ts';
import { Room } from './Room.tsx';
import { Shelves } from './Shelves.tsx';
import { Still } from './Still.tsx';

export interface SceneProps {
  game: Game;
  station: Station;
  setStation: (s: Station) => void;
  ink: boolean;
  reducedMotion: boolean;
  mix: Record<string, number>;
  pour: { key: number; color: string } | null;
  onPour: (ingredientId: string) => void;
  onHoverIngredient: (id: string | null) => void;
  onHoverCustomer: (c: ObservedCustomer | null) => void;
  onBell: () => void;
  still: { pressure: number; color: string; received: number };
}

/** The 3D shop. Every object reflects live simulation state. */
export function Scene(props: SceneProps) {
  const { game, station, setStation, mix } = props;
  const { obs, state } = game;
  const available = new Set(obs.ingredients.map((i) => i.id));
  const mixComponents = Object.entries(mix).map(([ingredientId, amount]) => ({
    ingredientId,
    amount,
  }));
  const volume = mixComponents.reduce((s, c) => s + c.amount, 0);

  const stock = state.lab.formulas.map((f) => ({
    formulaId: f.id,
    color: mixColor(f.components, content),
    count: obs.formulas.find((x) => x.id === f.id)?.stock ?? 0,
  }));
  const atCounter = [...obs.customers].sort((a, b) => b.bottlesLastMonth - a.bottlesLastMonth);
  const last = obs.lastMonth;
  const ledgerLines = last
    ? [
        `${monthName(last.date.month)} ${last.date.year}`,
        `Opening balance … ${money(last.cashStartCents)}`,
        ...last.sales.map(
          (s) =>
            `${state.lab.formulas.find((f) => f.id === s.formulaId)?.name ?? 'Tonic'}: ${s.bottles} sold`,
        ),
        `Closing balance … ${money(last.cashEndCents)}`,
      ]
    : ['The Ledger', 'No month has closed yet.'];

  return (
    <Canvas
      shadows="percentage"
      dpr={[1, 2]}
      gl={{ antialias: false, powerPreference: 'high-performance' }}
      camera={{
        position: STATIONS.overview.position,
        fov: STATIONS.overview.fov,
        near: 0.05,
        far: 30,
      }}
    >
      <color attach="background" args={['#120c08']} />
      <Room />
      <group onClick={(e) => (e.stopPropagation(), station !== 'bench' && setStation('bench'))}>
        <Shelves
          content={content}
          inventory={obs.inventory}
          available={available}
          ingredientName={(id) => t(content, `ingredient.${id}.name`)}
          {...(station === 'bench' ? { onPick: props.onPour } : {})}
          onHover={props.onHoverIngredient}
        />
        <Bench
          volume={volume}
          capacity={content.config.bottleCapacity}
          color={mixColor(mixComponents, content)}
          pour={props.pour}
        />
      </group>
      <Still
        pressure={props.still.pressure}
        color={props.still.color}
        received={props.still.received}
        onClick={(e) => (e.stopPropagation(), setStation('still'))}
      />
      <group onClick={(e) => (e.stopPropagation(), setStation('counter'))}>
        <Counter stock={stock} customers={atCounter} onCustomerHover={props.onHoverCustomer} />
      </group>
      <group onClick={(e) => (e.stopPropagation(), station !== 'desk' && setStation('desk'))}>
        <Desk
          month={monthName(obs.date.month)}
          year={obs.date.year}
          letters={obs.pendingEvents.length}
          ledgerLines={ledgerLines}
          {...(station === 'desk' ? { onBell: (e) => (e.stopPropagation(), props.onBell()) } : {})}
        />
      </group>
      <CameraRig station={station} reducedMotion={props.reducedMotion} />
      <InkPipeline enabled={props.ink} />
    </Canvas>
  );
}
