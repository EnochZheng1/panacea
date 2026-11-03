import type { ContentPack } from '@panacea/content';
import type { FormulaComponent } from '@panacea/sim';
import * as THREE from 'three';

/** The colour of a mixture: ingredient colours weighted by amount and opacity. */
export function mixColor(components: readonly FormulaComponent[], content: ContentPack): string {
  let weight = 0;
  const acc = new THREE.Color(0, 0, 0);
  const tmp = new THREE.Color();
  for (const c of components) {
    const look = content.byId.ingredients.get(c.ingredientId)?.appearance;
    if (!look || c.amount <= 0) continue;
    const w = c.amount * (0.05 + look.opacity);
    tmp.set(look.color);
    acc.r += tmp.r * w;
    acc.g += tmp.g * w;
    acc.b += tmp.b * w;
    weight += w;
  }
  if (weight === 0) return '#9fc4d6';
  acc.multiplyScalar(1 / weight);
  return `#${acc.getHexString()}`;
}

export const ingredientColor = (id: string, content: ContentPack) =>
  content.byId.ingredients.get(id)?.appearance.color ?? '#d8d0c0';
