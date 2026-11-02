import backgrounds from '../data/backgrounds.json' with { type: 'json' };
import config from '../data/config.json' with { type: 'json' };
import events from '../data/events.json' with { type: 'json' };
import ingredients from '../data/ingredients.json' with { type: 'json' };
import processes from '../data/processes.json' with { type: 'json' };
import regulars from '../data/regulars.json' with { type: 'json' };
import timeline from '../data/timeline.json' with { type: 'json' };
import unlocks from '../data/unlocks.json' with { type: 'json' };
import strings from '../strings/en.json' with { type: 'json' };
import { parseContent, type ContentPack, type RawContent } from './pack.ts';

/** The game's bundled content, unvalidated. */
export const rawContent: RawContent = {
  config,
  ingredients,
  processes,
  unlocks,
  events,
  timeline,
  regulars,
  backgrounds,
  strings,
};

let cached: ContentPack | undefined;

/** Loads and validates the bundled content. Throws if it fails schema validation. */
export function loadContent(): ContentPack {
  if (cached) return cached;
  const result = parseContent(rawContent);
  if (!result.ok) {
    const details = result.issues.map((i) => `  ${i.file}:${i.path} ${i.message}`).join('\n');
    throw new Error(`Bundled content failed validation:\n${details}`);
  }
  cached = result.pack;
  return cached;
}
