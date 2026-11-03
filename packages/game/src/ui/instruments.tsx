import { useEffect, useRef, useState } from 'react';
import { Icon } from './ornaments.tsx';
import { money, monthName } from './text.ts';

/** A cash-register drum: digits roll to their new value. */
export function Odometer({ cents }: { cents: number }) {
  const text = money(cents);
  const prev = useRef(cents);
  const [flash, setFlash] = useState<'up' | 'down' | null>(null);
  useEffect(() => {
    if (cents !== prev.current) {
      setFlash(cents > prev.current ? 'up' : 'down');
      prev.current = cents;
      const id = setTimeout(() => setFlash(null), 900);
      return () => clearTimeout(id);
    }
    return undefined;
  }, [cents]);
  return (
    <span className={`odometer ${flash ?? ''}`} aria-label={`Cash ${text}`} role="img">
      {[...text].map((ch, i) =>
        /\d/.test(ch) ? (
          <span key={`${text.length}-${i}`} className="drum">
            <span className="strip" style={{ transform: `translateY(-${Number(ch) * 10}%)` }}>
              {'0123456789'.split('').map((d) => (
                <span key={d}>{d}</span>
              ))}
            </span>
          </span>
        ) : (
          <span key={`${text.length}-${i}`} className="sep">
            {ch}
          </span>
        ),
      )}
    </span>
  );
}

/** A tear-off calendar block; the old page flips away when the month turns. */
export function Calendar({ month, year }: { month: number; year: number }) {
  const [old, setOld] = useState<{ month: number; year: number } | null>(null);
  const last = useRef({ month, year });
  useEffect(() => {
    if (last.current.month !== month || last.current.year !== year) {
      setOld(last.current);
      last.current = { month, year };
      const id = setTimeout(() => setOld(null), 900);
      return () => clearTimeout(id);
    }
    return undefined;
  }, [month, year]);
  return (
    <div className="calendar" aria-label={`${monthName(month)} ${year}`}>
      <div className="cal-page">
        <span className="cal-year">{year}</span>
        <span className="cal-month">{monthName(month)}</span>
      </div>
      {old && (
        <div className="cal-page tearing" aria-hidden="true">
          <span className="cal-year">{old.year}</span>
          <span className="cal-month">{monthName(old.month)}</span>
        </div>
      )}
    </div>
  );
}

/** "Talk": a pressure gauge for heat, with a red zone near the authorities' limit. */
export function TalkGauge({ heat }: { heat: number }) {
  const angle = -120 + (Math.min(100, Math.max(0, heat)) / 100) * 240;
  const ticks = Array.from({ length: 11 }, (_, i) => -120 + i * 24);
  return (
    <div
      className={`gauge ${heat >= 70 ? 'danger' : heat >= 40 ? 'warn' : ''}`}
      title="Talk: how much the neighbourhood, the press and the authorities are saying about you"
    >
      <svg viewBox="0 0 100 70" aria-hidden="true">
        <defs>
          <radialGradient id="dialface" cx="50%" cy="60%" r="60%">
            <stop offset="0" stopColor="#f6ead0" />
            <stop offset="1" stopColor="#d9c497" />
          </radialGradient>
        </defs>
        <circle cx="50" cy="52" r="44" fill="url(#dialface)" stroke="#7d5d22" strokeWidth="4" />
        <path
          d="M 89.4 44 A 40 40 0 0 1 84.6 72"
          stroke="#8b1e14"
          strokeWidth="7"
          fill="none"
          transform="rotate(-26 50 52)"
          opacity="0.85"
        />
        {ticks.map((a, i) => (
          <line
            key={a}
            x1="50"
            y1="14"
            x2="50"
            y2={i % 5 === 0 ? 22 : 19}
            stroke="#2a1a10"
            strokeWidth={i % 5 === 0 ? 2 : 1}
            transform={`rotate(${a} 50 52)`}
          />
        ))}
        <g className="needle" style={{ transform: `rotate(${angle}deg)` }}>
          <path d="M50 52 L48.5 50 L50 16 L51.5 50 Z" fill="#2a1a10" />
        </g>
        <circle cx="50" cy="52" r="4" fill="#c79a43" stroke="#2a1a10" />
        <text x="50" y="66" textAnchor="middle" className="gauge-label">
          TALK
        </text>
      </svg>
    </div>
  );
}

/** Your good name, as an engraved medallion. */
export function Medallion({ value }: { value: number }) {
  return (
    <div className="medallion" title="Your good name">
      <svg viewBox="0 0 60 60" aria-hidden="true">
        <circle cx="30" cy="30" r="27" fill="#c79a43" stroke="#5e4416" strokeWidth="2" />
        <circle cx="30" cy="30" r="21" fill="none" stroke="#5e4416" strokeWidth="1" />
        {Array.from({ length: 14 }, (_, i) => {
          const a = (i / 14) * Math.PI * 2;
          return (
            <ellipse
              key={i}
              cx={30 + Math.cos(a) * 24}
              cy={30 + Math.sin(a) * 24}
              rx="3"
              ry="1.4"
              fill="#5e4416"
              transform={`rotate(${(a * 180) / Math.PI + 90} ${30 + Math.cos(a) * 24} ${30 + Math.sin(a) * 24})`}
            />
          );
        })}
      </svg>
      <span>{value.toFixed(0)}</span>
      <small>Good name</small>
    </div>
  );
}

export function Regulars({ count, capacity }: { count: number; capacity: number }) {
  return (
    <div className="regulars" title="Regular customers / room in the shop">
      <Icon name="figure" size={20} />
      <span>
        {count}
        <small>/{capacity}</small>
      </span>
    </div>
  );
}
