import { loadContent } from '@panacea/content';
import { hashValue, replay } from '@panacea/sim';
import { describe, expect, it } from 'vitest';
import { idleBot, randomBot, runBatch, runGame, shopkeeperBot } from '../src/index.ts';

const content = loadContent();

describe('M0 exit criterion: bots play complete runs deterministically', () => {
  it('the idle bot plays to a terminal state', () => {
    const r = runGame({ content, bot: idleBot, seed: 'm0' });
    expect(r.violations).toEqual([]);
    expect(r.endReason).toBe('bankrupt');
    expect(r.months).toBeGreaterThan(12);
  });

  it('the shopkeeper bot survives the full 1885–1906 run', () => {
    for (const seed of ['m0-a', 'm0-b', 'm0-c']) {
      const r = runGame({ content, bot: shopkeeperBot(), seed });
      expect(r.violations).toEqual([]);
      expect(r.endReason).toBe('timeline_end');
      expect(r.months).toBe(264);
      expect(r.bottlesSold).toBeGreaterThan(1000);
    }
  });

  it('identical seeds give identical runs, and the action log replays them', () => {
    const a = runGame({ content, bot: shopkeeperBot(), seed: 'same' });
    const b = runGame({ content, bot: shopkeeperBot(), seed: 'same' });
    expect(a.stateHash).toBe(b.stateHash);
    const replayed = replay(content, { seed: 'same' }, a.finalState.log);
    expect(hashValue(replayed)).toBe(a.stateHash);
  });

  it('the random bot (fuzzer) never breaks invariants', () => {
    for (let i = 0; i < 10; i++) {
      expect(runGame({ content, bot: randomBot, seed: `fuzz-${i}` }).violations).toEqual([]);
    }
  });

  it('a full run stays within the performance budget', () => {
    runGame({ content, bot: shopkeeperBot(), seed: 'warmup', checkEveryStep: false });
    const r = runGame({ content, bot: shopkeeperBot(), seed: 'perf', checkEveryStep: false });
    // Technical plan budget is 200 ms on CI hardware; allow headroom for slow shared runners.
    expect(r.ms).toBeLessThan(1000);
  });
});

describe('batch reports', () => {
  it('summarise outcomes per bot', () => {
    const report = runBatch({
      content,
      bots: [idleBot, shopkeeperBot()],
      runs: 3,
      seedPrefix: 't',
    });
    const [idle, shop] = report.summaries;
    expect(idle).toMatchObject({ botId: 'idle', runs: 3, bankruptcyRate: 1 });
    expect(shop).toMatchObject({ botId: 'shopkeeper', bankruptcyRate: 0, medianMonths: 264 });
    expect(shop!.unlockReach['second_still']).toBe(1);
    expect(report.violations).toEqual([]);
  });
});
