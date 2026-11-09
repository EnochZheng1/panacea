import type { ContentIssue, ContentPack } from './pack.ts';
import { SYSTEM_FLAGS, type Effect, type GameDate, type Requirement } from './schemas.ts';
import { stringKeys } from './stringKeys.ts';

const dateIndex = (d: GameDate) => d.year * 12 + (d.month - 1);

/** Walks a requirement tree, calling `visit` on every leaf and branch. */
export function walkRequirement(req: Requirement, visit: (r: Requirement) => void): void {
  visit(req);
  if (req.type === 'all' || req.type === 'any') req.of.forEach((r) => walkRequirement(r, visit));
  if (req.type === 'not') walkRequirement(req.requirement, visit);
}

/**
 * Cross-reference and consistency checks that schemas alone cannot express.
 * Errors must be fixed before content ships; warnings flag content that is probably wrong.
 */
export function lintContent(pack: ContentPack): ContentIssue[] {
  const issues: ContentIssue[] = [];
  const error = (file: string, path: string, message: string) =>
    issues.push({ severity: 'error', file, path, message });
  const warn = (file: string, path: string, message: string) =>
    issues.push({ severity: 'warning', file, path, message });

  // --- Unique ids per collection -------------------------------------------------------
  const collections = {
    ingredients: pack.ingredients,
    processes: pack.processes,
    unlocks: pack.unlocks,
    events: pack.events,
    timeline: pack.timeline,
    regulars: pack.regulars,
    backgrounds: pack.backgrounds,
    investments: pack.investments,
  } as const;
  for (const [file, items] of Object.entries(collections)) {
    const seen = new Set<string>();
    items.forEach((item, i) => {
      if (seen.has(item.id)) error(file, `${i}.id`, `duplicate id "${item.id}"`);
      seen.add(item.id);
    });
  }

  // --- Strings -------------------------------------------------------------------------
  const requireStrings = (file: string, path: string, keys: readonly string[]) => {
    for (const key of keys) {
      const value = pack.strings[key];
      if (value === undefined) error(file, path, `missing string "${key}"`);
      else if (value.trim() === '') error(file, path, `empty string "${key}"`);
    }
  };
  pack.ingredients.forEach((x, i) =>
    requireStrings('ingredients', `${i}`, stringKeys.ingredient(x.id)),
  );
  pack.processes.forEach((x, i) => requireStrings('processes', `${i}`, stringKeys.process(x.id)));
  pack.unlocks.forEach((x, i) => requireStrings('unlocks', `${i}`, stringKeys.unlock(x.id)));
  pack.timeline.forEach((x, i) => requireStrings('timeline', `${i}`, stringKeys.timeline(x.id)));
  pack.backgrounds.forEach((x, i) =>
    requireStrings('backgrounds', `${i}`, stringKeys.background(x.id)),
  );
  pack.events.forEach((ev, i) => {
    requireStrings('events', `${i}`, stringKeys.event(ev.id));
    ev.choices.forEach((c, j) =>
      requireStrings('events', `${i}.choices.${j}`, stringKeys.eventChoice(ev.id, c.id)),
    );
  });
  pack.investments.forEach((x, i) =>
    requireStrings('investments', `${i}`, stringKeys.investment(x.id)),
  );
  pack.segments.forEach((x, i) => requireStrings('segments', `${i}`, stringKeys.segment(x.id)));
  pack.ailments.forEach((x, i) => requireStrings('ailments', `${i}`, stringKeys.ailment(x.id)));
  pack.claims.forEach((x, i) => requireStrings('claims', `${i}`, stringKeys.claim(x.id)));
  pack.config.scrutiny.benignVerdicts.forEach((v, i) =>
    requireStrings('config', `scrutiny.benignVerdicts.${i}`, stringKeys.verdict(v)),
  );
  requireStrings('config', 'scrutiny', stringKeys.verdict('proprietary_poisoning'));

  // --- Segments, ailments, regulars, claims ------------------------------------------
  pack.regulars.forEach((r, i) => {
    if (!pack.byId.segments.has(r.segment))
      error('regulars', `${i}.segment`, `unknown segment "${r.segment}"`);
    if (!pack.byId.ailments.has(r.ailment))
      error('regulars', `${i}.ailment`, `unknown ailment "${r.ailment}"`);
  });
  pack.segments.forEach((sg, i) =>
    sg.ailments.forEach((a, j) => {
      if (!pack.byId.ailments.has(a.id))
        error('segments', `${i}.ailments.${j}`, `unknown ailment "${a.id}"`);
    }),
  );
  pack.claims.forEach((c, i) => {
    c.ailments.forEach((a, j) => {
      if (!pack.byId.ailments.has(a))
        error('claims', `${i}.ailments.${j}`, `unknown ailment "${a}"`);
    });
    if (c.requiresUnlock && !pack.byId.unlocks.has(c.requiresUnlock)) {
      error('claims', `${i}.requiresUnlock`, `unknown unlock "${c.requiresUnlock}"`);
    }
  });
  if (!pack.segments.some((sg) => sg.weight > 0))
    error('segments', '', 'no segment has a positive weight');

  // --- Config --------------------------------------------------------------------------
  const { config } = pack;
  if (dateIndex(config.startDate) >= dateIndex(config.endDate)) {
    error('config', 'endDate', 'endDate must be after startDate');
  }
  if (!pack.byId.backgrounds.has(config.defaultBackground)) {
    error('config', 'defaultBackground', `unknown background "${config.defaultBackground}"`);
  }

  // --- Events --------------------------------------------------------------------------
  pack.events.forEach((ev, i) => {
    const ids = new Set<string>();
    ev.choices.forEach((c, j) => {
      if (ids.has(c.id)) error('events', `${i}.choices.${j}`, `duplicate choice id "${c.id}"`);
      ids.add(c.id);
    });
    if (ev.choices.filter((c) => c.default).length > 1) {
      error('events', `${i}.choices`, 'at most one choice may be the default');
    }
    if (ev.trigger === 'random') {
      if (!ev.condition) error('events', `${i}.condition`, 'random events need a condition');
      if (ev.chance === 0) warn('events', `${i}.chance`, 'event can never be drawn (chance 0)');
    } else if (ev.condition || ev.chance > 0) {
      error(
        'events',
        `${i}`,
        'system events are raised by the simulation; remove condition/chance',
      );
    }
    ev.choices.forEach((c, j) => {
      if (c.default && c.requires) {
        error('events', `${i}.choices.${j}`, 'the default choice must always be available');
      }
      if (ev.trigger === 'random' && c.effects.some((e) => e.type === 'inquest')) {
        error('events', `${i}.choices.${j}`, 'inquest effects are only valid in system events');
      }
    });
  });

  // --- Timeline ------------------------------------------------------------------------
  pack.timeline.forEach((t, i) => {
    const d = dateIndex(t.date);
    if (d < dateIndex(config.startDate) || d > dateIndex(config.endDate)) {
      warn('timeline', `${i}.date`, 'entry falls outside the playable date range');
    }
  });

  // --- References in requirements and effects ------------------------------------------
  const producedFlags = new Set<string>(SYSTEM_FLAGS);
  const collectFlags = (effects: readonly Effect[]) =>
    effects.forEach((e) => e.type === 'setFlag' && producedFlags.add(e.id));
  pack.unlocks.forEach((u) => collectFlags(u.effects));
  pack.events.forEach((ev) => ev.choices.forEach((c) => collectFlags(c.effects)));
  pack.timeline.forEach((t) => collectFlags(t.effects));
  pack.investments.forEach((x) => collectFlags(x.effects));
  pack.backgrounds.forEach((b) => b.flags.forEach((f) => producedFlags.add(f)));

  const checkRequirement = (file: string, path: string, req: Requirement) =>
    walkRequirement(req, (r) => {
      if (r.type === 'unlocked' && !pack.byId.unlocks.has(r.id)) {
        error(file, path, `references unknown unlock "${r.id}"`);
      }
      if (r.type === 'flag' && !producedFlags.has(r.id)) {
        warn(file, path, `flag "${r.id}" is never set by any content, so this can never be met`);
      }
    });
  pack.unlocks.forEach((u, i) => checkRequirement('unlocks', `${i}.requirement`, u.requirement));
  pack.events.forEach((ev, i) => {
    if (ev.condition) checkRequirement('events', `${i}.condition`, ev.condition);
    ev.choices.forEach((c, j) => {
      if (c.requires) checkRequirement('events', `${i}.choices.${j}.requires`, c.requires);
    });
  });

  pack.investments.forEach((x, i) => {
    if (x.requires) checkRequirement('investments', `${i}.requires`, x.requires);
    if (x.lostWhen) checkRequirement('investments', `${i}.lostWhen`, x.lostWhen);
    x.effects.forEach((e, j) => {
      if (e.type === 'capability' && e.months !== undefined) {
        error('investments', `${i}.effects.${j}`, 'investment capabilities last while it is held');
      }
      if (e.type === 'inquest' || e.type === 'seizeStock' || e.type === 'wardMemory') {
        error('investments', `${i}.effects.${j}`, `"${e.type}" is not an investment effect`);
      }
    });
  });

  pack.processes.forEach((p, i) => {
    if (p.requiresUnlock && !pack.byId.unlocks.has(p.requiresUnlock)) {
      error('processes', `${i}.requiresUnlock`, `unknown unlock "${p.requiresUnlock}"`);
    }
  });
  if (!pack.processes.some((p) => !p.requiresUnlock)) {
    error('processes', '', 'at least one process must be available without an unlock');
  }

  // --- Unlock dependency cycles --------------------------------------------------------
  const deps = new Map<string, string[]>();
  for (const u of pack.unlocks) {
    const refs: string[] = [];
    walkRequirement(u.requirement, (r) => r.type === 'unlocked' && refs.push(r.id));
    deps.set(u.id, refs);
  }
  const state = new Map<string, 'visiting' | 'done'>();
  const visit = (id: string, trail: string[]): void => {
    if (state.get(id) === 'done') return;
    if (state.get(id) === 'visiting') {
      error('unlocks', id, `dependency cycle: ${[...trail, id].join(' → ')}`);
      return;
    }
    state.set(id, 'visiting');
    for (const dep of deps.get(id) ?? []) visit(dep, [...trail, id]);
    state.set(id, 'done');
  };
  pack.unlocks.forEach((u) => visit(u.id, []));

  return issues;
}
