import { sumDeltas, type GameState, type LedgerEntry } from '@panacea/sim';
import { mixColor } from '../scene/liquids.ts';
import { content } from '../state/useGame.ts';
import { Rule } from './ornaments.tsx';
import { LabelPreview } from './Panels.tsx';
import { deathName, money, newsText, t } from './text.ts';

const MONTHS = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

function headline(report: LedgerEntry, state: GameState): { head: string; deck: string } {
  const inquest = report.news.find((n) => n.kind === 'inquest');
  if (inquest && inquest.kind === 'inquest' && inquest.outcome === 'implicated') {
    return {
      head: 'Coroner’s Jury Names a Local Remedy',
      deck: 'Proprietor’s preparation blamed; neighbourhood in uproar',
    };
  }
  const timeline = report.news.find((n) => n.kind === 'timeline');
  if (timeline && timeline.kind === 'timeline') {
    return {
      head: t(content, `timeline.${timeline.id}.headline`),
      deck: t(content, `timeline.${timeline.id}.body`),
    };
  }
  const deaths = report.news.filter((n) => n.kind === 'death').length;
  if (deaths > 0)
    return {
      head: deaths > 1 ? 'Sorrow on the Bowery' : 'A Death in the Neighbourhood',
      deck: `${deaths} mourned this month`,
    };
  const sold = report.sales.reduce((n, s) => n + s.bottles, 0);
  const joined = report.news.filter((n) => n.kind === 'customer_joined').length;
  if (sold >= 40)
    return { head: 'Brisk Trade at the Panacea Pharmacy', deck: `${sold} bottles change hands` };
  if (joined > 0)
    return {
      head: 'New Faces at the Counter',
      deck: `${joined} more of the afflicted seek relief`,
    };
  if (state.lab.formulas.length === 0)
    return { head: 'A Quiet Month on the Bowery', deck: 'The new pharmacy has yet to sell a drop' };
  return { head: 'A Quiet Month on the Bowery', deck: 'Trade steady; the neighbourhood at peace' };
}

/** The month-end report as an evening paper: masthead, headline, three columns and an advertisement. */
export function Broadsheet({
  report,
  state,
  onClose,
}: {
  report: LedgerEntry;
  state: GameState;
  onClose: () => void;
}) {
  const { head, deck } = headline(report, state);
  const sold = report.sales.reduce((n, s) => n + s.bottles, 0);
  const receipts = report.sales.reduce((n, s) => n + s.revenueCents, 0);
  const net = sumDeltas(report, 'cash');
  const deaths = report.news.filter((n) => n.kind === 'death' || n.kind === 'inquest');
  const notices = report.news.filter(
    (n) => n.kind !== 'death' && n.kind !== 'inquest' && n.kind !== 'timeline',
  );
  const best = [...report.sales].sort((a, b) => b.bottles - a.bottles)[0];
  const formula = best
    ? state.lab.formulas.find((f) => f.id === best.formulaId)
    : state.lab.formulas[0];

  return (
    <div className="overlay" onClick={onClose} role="dialog" aria-label="The Evening Ledger">
      <article className="broadsheet" onClick={(e) => e.stopPropagation()}>
        <header className="masthead">
          <span className="ears">Vol. I · No. {report.turn + 1}</span>
          <h1>The Evening Ledger</h1>
          <span className="ears">Price One Cent</span>
        </header>
        <p className="dateline">
          New York, {MONTHS[report.date.month - 1]} {report.date.year}
        </p>
        <h2 className="headline">{head}</h2>
        <p className="deck">{deck}</p>
        <div className="columns">
          <section>
            <h3>Commerce</h3>
            <p>
              The Panacea Pharmacy reports <b>{sold}</b> bottles sold for {money(receipts)}, with{' '}
              {report.customers} regular customers on its books.
            </p>
            <p>
              After rent and outlays the month closes {net >= 0 ? 'in profit by' : 'in deficit by'}{' '}
              <b>{money(Math.abs(net))}</b>; the till holds {money(report.cashEndCents)}.
            </p>
          </section>
          <section>
            <h3>Notices</h3>
            {notices.length === 0 && <p>Nothing of note.</p>}
            <ul>
              {notices.slice(0, 6).map((n, i) => (
                <li key={i}>{newsText(content, state, n).replace(/^[^\w"“]+/, '')}</li>
              ))}
            </ul>
          </section>
          <section className="obituaries">
            <h3>Obituaries</h3>
            {deaths.length === 0 && <p>None this month.</p>}
            <ul>
              {deaths.map((n, i) => {
                if (n.kind === 'death') {
                  const d = state.deaths.find((x) => x.id === n.deathId);
                  return (
                    <li key={i}>
                      {d ? deathName(content, d) : 'A customer'}, lately of this parish.
                    </li>
                  );
                }
                return <li key={i}>{newsText(content, state, n).replace(/^[^\w"“]+/, '')}</li>;
              })}
            </ul>
          </section>
        </div>
        {formula && (
          <>
            <Rule glyph="☞" />
            <div className="advert">
              <LabelPreview
                name={formula.name}
                claims={formula.claims}
                priceCents={formula.priceCents}
                colour={mixColor(formula.components, content)}
              />
            </div>
          </>
        )}
        <button className="turn-page" onClick={onClose}>
          Turn the page ☞
        </button>
      </article>
    </div>
  );
}
