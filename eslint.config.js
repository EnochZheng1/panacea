import js from '@eslint/js';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

export default defineConfig(
  { ignores: ['**/node_modules/**', '**/dist/**', '**/coverage/**', 'playtest-reports/**'] },
  js.configs.recommended,
  tseslint.configs.recommended,
  {
    // The simulation must be deterministic: no ambient randomness or wall-clock time.
    files: ['packages/sim/src/**/*.ts'],
    rules: {
      'no-restricted-globals': [
        'error',
        {
          name: 'Date',
          message: 'Use game dates (core/date.ts); the sim must not read the clock.',
        },
        { name: 'performance', message: 'The sim must not read the clock.' },
        { name: 'setTimeout', message: 'The sim is synchronous and turn-based.' },
      ],
      'no-restricted-properties': [
        'error',
        { object: 'Math', property: 'random', message: 'Use the seeded Rng (core/rng.ts).' },
      ],
    },
  },
);
