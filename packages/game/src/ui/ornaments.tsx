import type { ReactNode } from 'react';

/**
 * Engraved line icons and printer's ornaments, drawn as inline SVG so they stay crisp, take the
 * current colour, and need no image assets.
 */

const stroke = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.6,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
} as const;

export type IconName =
  | 'shop'
  | 'bench'
  | 'still'
  | 'counter'
  | 'desk'
  | 'gear'
  | 'sound'
  | 'mute'
  | 'flame'
  | 'skull'
  | 'figure'
  | 'seal';

export function Icon({ name, size = 22 }: { name: IconName; size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" aria-hidden="true" className="icon">
      {name === 'shop' && (
        <g {...stroke}>
          <path d="M3 10 L12 4 L21 10" />
          <path d="M5 10 V20 H19 V10" />
          <path d="M10 20 V14 H14 V20" />
          <path d="M7 12 H9 M15 12 H17" />
        </g>
      )}
      {name === 'bench' && (
        <g {...stroke}>
          <path d="M5 11 H19 L17 18 Q12 21 7 18 Z" />
          <path d="M14 11 L19 3" />
          <path d="M8 21 H16" />
        </g>
      )}
      {name === 'still' && (
        <g {...stroke}>
          <path d="M5 20 Q3 13 8 11 Q12 9 16 11 Q21 13 19 20 Z" />
          <path d="M11 9 V6 Q11 3 15 3 Q20 3 21 9" />
          <circle cx="15.5" cy="15.5" r="2" />
        </g>
      )}
      {name === 'counter' && (
        <g {...stroke}>
          <path d="M4 12 Q12 3 20 12" />
          <path d="M3 12 H21 V14 H3 Z" />
          <circle cx="12" cy="5" r="1" />
          <path d="M5 14 V20 M19 14 V20" />
        </g>
      )}
      {name === 'desk' && (
        <g {...stroke}>
          <path d="M19 3 Q10 6 6 16 L5 21 L8 18 Q16 13 19 3 Z" />
          <path d="M9 13 L13 9" />
        </g>
      )}
      {name === 'gear' && (
        <g {...stroke}>
          <circle cx="12" cy="12" r="3" />
          <path d="M12 2 V5 M12 19 V22 M2 12 H5 M19 12 H22 M4.9 4.9 L7 7 M17 17 L19.1 19.1 M4.9 19.1 L7 17 M17 7 L19.1 4.9" />
        </g>
      )}
      {(name === 'sound' || name === 'mute') && (
        <g {...stroke}>
          <path d="M4 9 H8 L13 5 V19 L8 15 H4 Z" />
          {name === 'sound' ? (
            <path d="M16 9 Q18 12 16 15 M18.5 7 Q22 12 18.5 17" />
          ) : (
            <path d="M16 9 L21 15 M21 9 L16 15" />
          )}
        </g>
      )}
      {name === 'flame' && (
        <g {...stroke}>
          <path d="M12 21 Q5 19 6 12 Q7 8 10 5 Q10 10 13 11 Q12 7 15 3 Q19 8 18 14 Q17 20 12 21 Z" />
        </g>
      )}
      {name === 'skull' && (
        <g {...stroke}>
          <path d="M5 11 Q5 4 12 4 Q19 4 19 11 Q19 14 16 15 V18 H8 V15 Q5 14 5 11 Z" />
          <circle cx="9.5" cy="11" r="1.5" />
          <circle cx="14.5" cy="11" r="1.5" />
          <path d="M10 18 V20 M14 18 V20" />
        </g>
      )}
      {name === 'figure' && (
        <g {...stroke}>
          <circle cx="12" cy="7" r="3" />
          <path d="M6 21 Q6 12 12 12 Q18 12 18 21" />
        </g>
      )}
      {name === 'seal' && (
        <g>
          <path
            d="M12 1.5 L14 3.6 L16.9 2.9 L17.7 5.8 L20.6 6.6 L19.9 9.5 L22 11.6 L19.9 13.7 L20.6 16.6 L17.7 17.4 L16.9 20.3 L14 19.6 L12 21.7 L10 19.6 L7.1 20.3 L6.3 17.4 L3.4 16.6 L4.1 13.7 L2 11.6 L4.1 9.5 L3.4 6.6 L6.3 5.8 L7.1 2.9 L10 3.6 Z"
            fill="currentColor"
          />
          <circle
            cx="12"
            cy="11.6"
            r="5.5"
            fill="none"
            stroke="rgba(255,235,200,.6)"
            strokeWidth="1"
          />
        </g>
      )}
    </svg>
  );
}

/** A printer's rule with a fleuron in the middle. */
export function Rule({ glyph = '❦' }: { glyph?: string }) {
  return (
    <div className="rule" aria-hidden="true">
      <span />
      <em>{glyph}</em>
      <span />
    </div>
  );
}

/** Ornamental corner brackets for printed plates. */
export function Corners() {
  const corner = (
    <svg viewBox="0 0 40 40" aria-hidden="true">
      <path d="M2 38 V10 Q2 2 10 2 H38" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <path d="M7 38 V14 Q7 7 14 7 H38" fill="none" stroke="currentColor" strokeWidth="0.8" />
      <path d="M12 12 Q16 4 20 12 Q12 16 12 12 Z" fill="currentColor" />
    </svg>
  );
  return (
    <div className="corners" aria-hidden="true">
      <i className="tl">{corner}</i>
      <i className="tr">{corner}</i>
      <i className="bl">{corner}</i>
      <i className="br">{corner}</i>
    </div>
  );
}

/** A printed plate: the base of every in-game paper surface. */
export function Plate({
  title,
  kicker,
  children,
  className = '',
  tone = 'paper',
}: {
  title?: string;
  kicker?: string;
  children: ReactNode;
  className?: string;
  tone?: 'paper' | 'card' | 'mourning';
}) {
  return (
    <section className={`plate plate-${tone} ${className}`}>
      <Corners />
      {kicker && <p className="kicker">{kicker}</p>}
      {title && <h2 className="plate-title">{title}</h2>}
      {title && <Rule />}
      {children}
    </section>
  );
}
