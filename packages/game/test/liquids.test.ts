import { loadContent } from '@panacea/content';
import * as THREE from 'three';
import { describe, expect, it } from 'vitest';
import { mixColor } from '../src/scene/liquids.ts';

const content = loadContent();

describe('liquid colours', () => {
  it('every ingredient has a colour and opacity', () => {
    for (const i of content.ingredients) {
      expect(i.appearance.color).toMatch(/^#[0-9a-f]{6}$/i);
      expect(i.appearance.opacity).toBeGreaterThanOrEqual(0);
    }
  });

  it('strong tinctures dominate a mixture; water barely tints it', () => {
    const tonic = new THREE.Color(
      mixColor(
        [
          { ingredientId: 'water', amount: 5 },
          { ingredientId: 'grain_alcohol', amount: 2 },
          { ingredientId: 'laudanum', amount: 0.5 },
        ],
        content,
      ),
    );
    // Laudanum is amber-red: the mixture should read clearly warm, not watery blue.
    expect(tonic.r).toBeGreaterThan(tonic.b * 1.5);
  });

  it('an empty glass has a neutral colour', () => {
    expect(mixColor([], content)).toBe('#9fc4d6');
  });
});
