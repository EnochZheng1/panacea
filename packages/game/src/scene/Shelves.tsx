import type { ContentPack, Ingredient } from '@panacea/content';
import { Bottle, type BottleShape } from './Bottle.tsx';
import { MAT } from './materials.ts';
import { labelTexture } from './textures.ts';

const SHAPE_BY_CLASS: Record<string, BottleShape> = {
  base: 'tall',
  narcotic: 'flask',
  stimulant: 'flask',
  sedative: 'flask',
  analgesic: 'square',
  metal: 'squat',
  poison: 'squat',
  purgative: 'square',
  filler: 'round',
  flavour: 'round',
  botanical: 'square',
};

export const SHELF = { x0: -3.3, x1: 0.7, z: -2.72, levels: [1.18, 1.6, 2.02] } as const;

/** Where each ingredient's bottle stands, in content order, three shelves deep. */
export function shelfSlots(ingredients: readonly Ingredient[]) {
  const perShelf = Math.ceil(ingredients.length / SHELF.levels.length);
  const step = (SHELF.x1 - SHELF.x0) / perShelf;
  return ingredients.map((ing, i) => {
    const level = Math.floor(i / perShelf);
    const col = i % perShelf;
    return {
      ingredient: ing,
      position: [SHELF.x0 + step * (col + 0.5), SHELF.levels[level] ?? 1.2, SHELF.z + 0.05] as [
        number,
        number,
        number,
      ],
    };
  });
}

export interface ShelvesProps {
  content: ContentPack;
  inventory: Record<string, number>;
  available: ReadonlySet<string>;
  ingredientName: (id: string) => string;
  onPick?: (ingredientId: string) => void;
  onHover?: (ingredientId: string | null) => void;
}

/** The apothecary's wall of drugs: one labelled bottle per ingredient, filled by stock on hand. */
export function Shelves({
  content,
  inventory,
  available,
  ingredientName,
  onPick,
  onHover,
}: ShelvesProps) {
  const width = SHELF.x1 - SHELF.x0;
  const cx = (SHELF.x0 + SHELF.x1) / 2;
  return (
    <group>
      {/* Carcass */}
      <mesh position={[cx, 1.6, SHELF.z - 0.12]} material={MAT.darkWood} receiveShadow>
        <boxGeometry args={[width + 0.2, 1.5, 0.04]} />
      </mesh>
      {[SHELF.x0 - 0.08, SHELF.x1 + 0.08].map((x) => (
        <mesh key={x} position={[x, 1.6, SHELF.z]} material={MAT.darkWood} castShadow>
          <boxGeometry args={[0.06, 1.5, 0.3]} />
        </mesh>
      ))}
      {SHELF.levels.map((y) => (
        <mesh
          key={y}
          position={[cx, y - 0.02, SHELF.z]}
          material={MAT.wood}
          castShadow
          receiveShadow
        >
          <boxGeometry args={[width + 0.1, 0.04, 0.3]} />
        </mesh>
      ))}
      {/* Drawers below */}
      {Array.from({ length: 8 }, (_, i) => (
        <group
          key={i}
          position={[SHELF.x0 + (width * ((i % 4) + 0.5)) / 4, i < 4 ? 0.82 : 0.5, SHELF.z]}
        >
          <mesh material={MAT.wood} castShadow>
            <boxGeometry args={[width / 4 - 0.04, 0.28, 0.32]} />
          </mesh>
          <mesh position={[0, 0, 0.17]} material={MAT.brass}>
            <sphereGeometry args={[0.018, 8, 6]} />
          </mesh>
        </group>
      ))}
      {shelfSlots(content.ingredients).map(({ ingredient, position }) => {
        const isAvailable = available.has(ingredient.id);
        const onHand = inventory[ingredient.id] ?? 0;
        return (
          <Bottle
            key={ingredient.id}
            shape={SHAPE_BY_CLASS[ingredient.substanceClass] ?? 'round'}
            color={ingredient.appearance.color}
            fill={isAvailable ? Math.min(1, 0.55 + onHand / 100) : 0}
            label={labelTexture(
              ingredientName(ingredient.id),
              isAvailable ? `${onHand.toFixed(0)} oz` : 'not yet known',
            )}
            position={position}
            scale={1.45}
            {...(isAvailable && onPick
              ? { onClick: (e) => (e.stopPropagation(), onPick(ingredient.id)) }
              : {})}
            onHover={(h) => onHover?.(h ? ingredient.id : null)}
          />
        );
      })}
    </group>
  );
}
