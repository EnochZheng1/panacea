import { describe, expect, it } from 'vitest';
import {
  IdSchema,
  lintContent,
  loadContent,
  parseContent,
  rawContent,
  type ContentPack,
  type Effect,
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

/**
 * Each reply to a letter as numbers that are better higher: money, standing, and less suspicion,
 * notoriety and talk in the ward; capabilities by what they do, over the months they last. A
 * flag set or cleared is a consequence of its own, so replies that differ in flags are not
 * compared.
 */
function replyValue(effects: readonly Effect[]): { numbers: Map<string, number>; flags: string } {
  const numbers = new Map<string, number>();
  const add = (k: string, v: number) => numbers.set(k, (numbers.get(k) ?? 0) + v);
  const capSign: Record<string, number> = {
    rent_extra_cents: -1,
    retainer_cents: -1,
    rival_share_pct: -1,
    rival_price_cents: 1,
    ingredient_discount_pct: 1,
    narcotic_discount_pct: 1,
    shop_capacity: 1,
    walk_in_bonus: 1,
    quality_control: 1,
  };
  const flags: string[] = [];
  for (const e of effects) {
    if (e.type === 'cash') add('cash', e.deltaCents);
    else if (e.type === 'reputation') add('reputation', e.delta);
    else if (e.type === 'heat' || e.type === 'notoriety' || e.type === 'wardMemory')
      add(e.type, -e.delta);
    else if (e.type === 'capability') {
      const sign = e.id.startsWith('callers_')
        ? 1
        : e.id.startsWith('surcharge_pct_')
          ? -1
          : capSign[e.id];
      if (sign === undefined) throw new Error(`no direction known for capability "${e.id}"`);
      add(`cap:${e.id}`, sign * e.delta * (e.months ?? 240));
    } else if (e.type === 'seizeStock') add('stock', -1);
    else if (e.type === 'setFlag' || e.type === 'clearFlag') flags.push(`${e.type}:${e.id}`);
  }
  return { numbers, flags: flags.sort().join(',') };
}

describe('the letters', () => {
  it('offer no reply that is better than another on every count', () => {
    const dominated: string[] = [];
    for (const ev of loadContent().events) {
      if (ev.trigger !== 'random') continue;
      const replies = ev.choices.map((c) => ({ id: c.id, ...replyValue(c.effects) }));
      for (const a of replies) {
        for (const b of replies) {
          if (a === b || a.flags !== b.flags) continue;
          const keys = new Set([...a.numbers.keys(), ...b.numbers.keys()]);
          const ge = [...keys].every((k) => (a.numbers.get(k) ?? 0) >= (b.numbers.get(k) ?? 0));
          const gt = [...keys].some((k) => (a.numbers.get(k) ?? 0) > (b.numbers.get(k) ?? 0));
          if (ge && gt) dominated.push(`${ev.id}: ${a.id} beats ${b.id}`);
        }
      }
    }
    expect(dominated).toEqual([]);
  });

  it('give every random letter a default reply', () => {
    const missing = loadContent()
      .events.filter((ev) => ev.trigger === 'random' && !ev.choices.some((c) => c.default))
      .map((ev) => ev.id);
    expect(missing).toEqual([]);
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
