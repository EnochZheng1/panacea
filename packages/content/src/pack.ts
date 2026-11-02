import { z } from 'zod';
import {
  BackgroundSchema,
  ConfigSchema,
  GameEventSchema,
  IngredientSchema,
  ProcessSchema,
  RegularSchema,
  TimelineEntrySchema,
  UnlockSchema,
  type Background,
  type Config,
  type GameEvent,
  type Ingredient,
  type Process,
  type Regular,
  type TimelineEntry,
  type Unlock,
} from './schemas.ts';

/** Raw, unvalidated content as read from the data files. */
export interface RawContent {
  config: unknown;
  ingredients: unknown;
  processes: unknown;
  unlocks: unknown;
  events: unknown;
  timeline: unknown;
  regulars: unknown;
  backgrounds: unknown;
  strings: unknown;
}

/** Validated content, ready for the simulation. Immutable by convention. */
export interface ContentPack {
  config: Config;
  ingredients: readonly Ingredient[];
  processes: readonly Process[];
  unlocks: readonly Unlock[];
  events: readonly GameEvent[];
  timeline: readonly TimelineEntry[];
  regulars: readonly Regular[];
  backgrounds: readonly Background[];
  /** Source-language strings (English). */
  strings: Readonly<Record<string, string>>;
  byId: {
    ingredients: ReadonlyMap<string, Ingredient>;
    processes: ReadonlyMap<string, Process>;
    unlocks: ReadonlyMap<string, Unlock>;
    events: ReadonlyMap<string, GameEvent>;
    backgrounds: ReadonlyMap<string, Background>;
  };
}

export interface ContentIssue {
  severity: 'error' | 'warning';
  file: string;
  path: string;
  message: string;
}

const RawSchemas = {
  config: ConfigSchema,
  ingredients: z.array(IngredientSchema),
  processes: z.array(ProcessSchema),
  unlocks: z.array(UnlockSchema),
  events: z.array(GameEventSchema),
  timeline: z.array(TimelineEntrySchema),
  regulars: z.array(RegularSchema),
  backgrounds: z.array(BackgroundSchema),
  strings: z.record(z.string(), z.string()),
} as const;

export type ParseResult = { ok: true; pack: ContentPack } | { ok: false; issues: ContentIssue[] };

/** Validates raw content against the schemas. Does not run cross-reference checks (see lint). */
export function parseContent(raw: RawContent): ParseResult {
  const issues: ContentIssue[] = [];
  const out: Record<string, unknown> = {};

  for (const key of Object.keys(RawSchemas) as (keyof typeof RawSchemas)[]) {
    const result = RawSchemas[key].safeParse(raw[key]);
    if (result.success) {
      out[key] = result.data;
    } else {
      for (const issue of result.error.issues) {
        issues.push({
          severity: 'error',
          file: key,
          path: issue.path.map(String).join('.'),
          message: issue.message,
        });
      }
    }
  }
  if (issues.length > 0) return { ok: false, issues };

  const v = out as {
    config: Config;
    ingredients: Ingredient[];
    processes: Process[];
    unlocks: Unlock[];
    events: GameEvent[];
    timeline: TimelineEntry[];
    regulars: Regular[];
    backgrounds: Background[];
    strings: Record<string, string>;
  };

  const byIdOf = <T extends { id: string }>(items: readonly T[]) =>
    new Map(items.map((item) => [item.id, item] as const));

  return {
    ok: true,
    pack: {
      ...v,
      byId: {
        ingredients: byIdOf(v.ingredients),
        processes: byIdOf(v.processes),
        unlocks: byIdOf(v.unlocks),
        events: byIdOf(v.events),
        backgrounds: byIdOf(v.backgrounds),
      },
    },
  };
}
