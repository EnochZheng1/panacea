import { describe, expect, it } from 'vitest';
import {
  IdSchema,
  lintContent,
  loadContent,
  parseContent,
  rawContent,
  type ContentPack,
  type RawContent,
} from '../src/index.ts';

const clone = <T>(x: T): T => structuredClone(x);

function packFrom(
  mutate: (raw: Record<keyof RawContent, unknown[] & Record<string, unknown>>) => void,
) {
  const raw = clone(rawContent) as unknown as Record<
    keyof RawContent,
    unknown[] & Record<string, unknown>
  >;
  mutate(raw);
  const parsed = parseContent(raw as unknown as RawContent);
  if (!parsed.ok) throw new Error(JSON.stringify(parsed.issues));
  return parsed.pack;
}

const messages = (pack: ContentPack, severity: 'error' | 'warning') =>
  lintContent(pack)
    .filter((i) => i.severity === severity)
    .map((i) => i.message);

describe('bundled content', () => {
  it('passes schema validation', () => {
    const pack = loadContent();
    expect(pack.ingredients.length).toBeGreaterThan(0);
    expect(pack.byId.backgrounds.has(pack.config.defaultBackground)).toBe(true);
  });

  it('has no lint errors or warnings', () => {
    expect(lintContent(loadContent())).toEqual([]);
  });
});

describe('schemas', () => {
  it('rejects ids that are not lower_snake_case', () => {
    expect(IdSchema.safeParse('Laudanum').success).toBe(false);
    expect(IdSchema.safeParse('9lives').success).toBe(false);
    expect(IdSchema.safeParse('laudanum_2').success).toBe(true);
  });

  it('reports schema errors with a file and path', () => {
    const raw = clone(rawContent) as unknown as Record<string, unknown>;
    (raw['ingredients'] as Record<string, unknown>[])[0]!['costPerUnitCents'] = -1;
    const parsed = parseContent(raw as unknown as RawContent);
    expect(parsed.ok).toBe(false);
    if (!parsed.ok) {
      expect(parsed.issues[0]).toMatchObject({ file: 'ingredients', path: '0.costPerUnitCents' });
    }
  });

  it('rejects unknown fields (typo protection)', () => {
    const raw = clone(rawContent) as unknown as Record<string, unknown>;
    (raw['unlocks'] as Record<string, unknown>[])[0]!['requirment'] = {};
    expect(parseContent(raw as unknown as RawContent).ok).toBe(false);
  });
});

describe('linter', () => {
  it('catches duplicate ids', () => {
    const pack = packFrom((raw) => {
      raw.ingredients.push(clone(raw.ingredients[0]));
    });
    expect(messages(pack, 'error')).toContain('duplicate id "water"');
  });

  it('catches missing strings', () => {
    const pack = packFrom((raw) => {
      delete raw.strings['ingredient.water.name'];
    });
    expect(messages(pack, 'error')).toContain('missing string "ingredient.water.name"');
  });

  it('catches references to unknown unlocks', () => {
    const pack = packFrom((raw) => {
      (raw.unlocks[0] as Record<string, unknown>)['requirement'] = { type: 'unlocked', id: 'nope' };
    });
    expect(messages(pack, 'error')).toContain('references unknown unlock "nope"');
  });

  it('catches unlock dependency cycles', () => {
    const pack = packFrom((raw) => {
      const [a, b] = raw.unlocks as Record<string, unknown>[];
      a!['requirement'] = { type: 'unlocked', id: b!['id'] };
      b!['requirement'] = { type: 'unlocked', id: a!['id'] };
    });
    expect(messages(pack, 'error').some((m) => m.startsWith('dependency cycle'))).toBe(true);
  });

  it('warns about flags nothing ever sets', () => {
    const pack = packFrom((raw) => {
      (raw.unlocks[0] as Record<string, unknown>)['requirement'] = {
        type: 'flag',
        id: 'never_set',
      };
    });
    expect(messages(pack, 'warning')[0]).toContain('"never_set" is never set');
  });

  it('rejects more than one default choice', () => {
    const pack = packFrom((raw) => {
      const ev = raw.events[0] as { choices: { default?: boolean }[] };
      ev.choices.forEach((c) => (c.default = true));
    });
    expect(messages(pack, 'error')).toContain('at most one choice may be the default');
  });
});
