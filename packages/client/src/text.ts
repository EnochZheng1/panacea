import type { ContentPack } from '@panacea/content';
import type { GameState, NewsItem } from '@panacea/sim';

/** Looks up a string and fills `{placeholders}`. Missing keys show as the key itself. */
export function t(content: ContentPack, key: string, params: Record<string, string> = {}): string {
  const template = content.strings[key] ?? key;
  return template.replace(/\{(\w+)\}/g, (_, name: string) => params[name] ?? `{${name}}`);
}

export const money = (cents: number): string => {
  const sign = cents < 0 ? '-' : '';
  const abs = Math.abs(cents);
  return abs >= 100 ? `${sign}$${(abs / 100).toFixed(2)}` : `${sign}${abs}¢`;
};

export const monthName = (m: number) =>
  ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'][m - 1] ??
  '?';

/** How a death is described in public: buyer names stand in for infants and strangers. */
export function deathName(
  content: ContentPack,
  d: { name: string; segment: string; consumer: 'self' | 'infant'; anonymous: boolean },
): string {
  if (d.anonymous)
    return `an unknown ${t(content, `segment.${d.segment}.name`).toLowerCase()} customer`;
  if (d.consumer === 'infant') return `the infant child of ${d.name}`;
  return d.name;
}

export function newsText(content: ContentPack, state: GameState, n: NewsItem): string {
  switch (n.kind) {
    case 'timeline':
      return `📰 ${t(content, `timeline.${n.id}.headline`)}: ${t(content, `timeline.${n.id}.body`)}`;
    case 'unlock':
      return `🔓 Unlocked: ${t(content, `unlock.${n.id}.name`)}. ${t(content, `unlock.${n.id}.description`)}`;
    case 'event_resolved':
      return `✉️ ${t(content, `event.${n.eventId}.title`, { name: '…', cost: '…' })}: ${t(
        content,
        `event.${n.eventId}.choice.${n.choiceId}`,
        { cost: '' },
      )}${n.auto ? ' (no decision made; default taken)' : ''}`;
    case 'death': {
      const d = state.deaths.find((x) => x.id === n.deathId);
      return d ? `✝ Died: ${deathName(content, d)}.` : '✝ A death.';
    }
    case 'inquest': {
      const d = state.deaths.find((x) => x.id === n.deathId);
      const who = d ? deathName(content, d) : 'the deceased';
      return `⚖ Inquest into ${who}: "${t(content, `verdict.${n.verdict}`)}"${
        n.outcome === 'implicated'
          ? ' — your medicine is named!'
          : n.outcome === 'bribed'
            ? ' (paid for)'
            : ''
      }`;
    }
    case 'customer_joined': {
      const c = state.customers.find((x) => x.id === n.customerId);
      return `🙋 New regular: ${c?.name ?? 'a new customer'}.`;
    }
    case 'customer_left':
      return `👋 ${n.name} stopped coming (${n.reason}).`;
    case 'bad_batch_caught':
      return `🧪 Your assistant caught a mis-measured batch (${n.bottles} bottles poured away).`;
    case 'stock_seized':
      return `🚔 The Board of Health seized ${n.bottles} bottles.`;
    case 'ads_lapsed':
      return '📉 You could not pay for this month’s advertising; it has stopped.';
    case 'shutdown_warning':
      return `🚨 The authorities are preparing to close you down. Bring the heat below 100 within ${n.monthsLeft} month(s)!`;
  }
}
