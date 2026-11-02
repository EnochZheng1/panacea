/**
 * Content linter CLI: validates the bundled data against the schemas, then runs cross-reference
 * checks. Exits non-zero on errors (warnings are printed but do not fail).
 */
import { lintContent, parseContent, rawContent } from '../src/index.ts';

const parsed = parseContent(rawContent);
const issues = parsed.ok ? lintContent(parsed.pack) : parsed.issues;
const errors = issues.filter((i) => i.severity === 'error');
const warnings = issues.filter((i) => i.severity === 'warning');

for (const i of issues) {
  const tag = i.severity === 'error' ? 'ERROR' : 'warn ';
  console.log(`${tag} ${i.file}${i.path ? `:${i.path}` : ''} — ${i.message}`);
}

if (parsed.ok) {
  const p = parsed.pack;
  console.log(
    `Content ${p.config.contentVersion}: ${p.ingredients.length} ingredients, ` +
      `${p.processes.length} processes, ${p.unlocks.length} unlocks, ${p.events.length} events, ` +
      `${p.timeline.length} timeline entries, ${p.regulars.length} regulars, ` +
      `${Object.keys(p.strings).length} strings.`,
  );
}
console.log(`${errors.length} error(s), ${warnings.length} warning(s).`);
process.exit(errors.length > 0 ? 1 : 0);
