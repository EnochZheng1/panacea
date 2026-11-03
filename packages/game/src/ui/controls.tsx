import { useRef, useState, type ReactNode } from 'react';
import { Icon } from './ornaments.tsx';
import { sfx } from './sound.ts';

/**
 * Game controls in brass and paper, replacing browser widgets. All are keyboard-operable and
 * carry ARIA roles so assistive tech and AI playtesters can drive them.
 */

/** A knurled brass dial: drag, scroll, arrow keys, or the − / + buttons. */
export function Dial({
  value,
  min,
  max,
  step,
  onChange,
  label,
  unit,
}: {
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (v: number) => void;
  label: string;
  unit: string;
}) {
  const drag = useRef<{ y: number; x: number; v: number } | null>(null);
  const set = (v: number) => {
    const next = Math.min(max, Math.max(min, Math.round(v / step) * step));
    if (next !== value) {
      sfx.tick();
      onChange(next);
    }
  };
  const fraction = (value - min) / (max - min);
  const angle = -135 + fraction * 270;
  return (
    <div className="dial-wrap">
      <button className="nudge" onClick={() => set(value - step)} aria-label={`Less ${label}`}>
        −
      </button>
      <div
        className="dial"
        role="slider"
        tabIndex={0}
        aria-label={label}
        aria-valuemin={min}
        aria-valuemax={max}
        aria-valuenow={value}
        aria-valuetext={`${value} ${unit}`}
        onKeyDown={(e) => {
          const up = e.key === 'ArrowUp' || e.key === 'ArrowRight';
          const down = e.key === 'ArrowDown' || e.key === 'ArrowLeft';
          if (!up && !down) return;
          e.preventDefault();
          set(value + (up ? step : -step));
        }}
        onWheel={(e) => set(value + (e.deltaY < 0 ? step : -step))}
        onPointerDown={(e) => {
          (e.target as HTMLElement).setPointerCapture(e.pointerId);
          drag.current = { y: e.clientY, x: e.clientX, v: value };
        }}
        onPointerMove={(e) => {
          if (!drag.current) return;
          const delta = (drag.current.y - e.clientY + (e.clientX - drag.current.x)) / 3;
          set(drag.current.v + Math.round(delta) * step);
        }}
        onPointerUp={() => (drag.current = null)}
      >
        <svg viewBox="0 0 100 100" aria-hidden="true">
          <defs>
            <radialGradient id="brass" cx="35%" cy="30%" r="75%">
              <stop offset="0" stopColor="#f7dc95" />
              <stop offset="0.55" stopColor="#c79a43" />
              <stop offset="1" stopColor="#6b4c18" />
            </radialGradient>
          </defs>
          {Array.from({ length: 28 }, (_, i) => (
            <line
              key={i}
              x1="50"
              y1="2"
              x2="50"
              y2="9"
              stroke="#5e4416"
              strokeWidth="2"
              transform={`rotate(${i * (360 / 28)} 50 50)`}
            />
          ))}
          <circle cx="50" cy="50" r="40" fill="url(#brass)" stroke="#5e4416" strokeWidth="2" />
          <circle cx="50" cy="50" r="30" fill="none" stroke="rgba(94,68,22,.5)" strokeWidth="1" />
          <g
            style={{
              transform: `rotate(${angle}deg)`,
              transformOrigin: '50px 50px',
              transition: 'transform .15s',
            }}
          >
            <rect x="47.5" y="14" width="5" height="18" rx="2" fill="#2a1a10" />
          </g>
        </svg>
        <span className="dial-value">
          {value}
          <small>{unit}</small>
        </span>
      </div>
      <button className="nudge" onClick={() => set(value + step)} aria-label={`More ${label}`}>
        +
      </button>
    </div>
  );
}

/** A small brass toggle switch. */
export function Toggle({
  checked,
  onChange,
  children,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  children: ReactNode;
}) {
  return (
    <label className="toggle">
      <button
        role="switch"
        aria-checked={checked}
        className={checked ? 'on' : ''}
        onClick={() => onChange(!checked)}
      >
        <span />
      </button>
      {children}
    </label>
  );
}

/** A wax-seal button for the important decisions. */
export function SealButton({
  children,
  onClick,
  disabled,
  title,
}: {
  children: ReactNode;
  onClick: () => void;
  disabled?: boolean;
  title?: string;
}) {
  return (
    <button className="seal-button" onClick={onClick} disabled={disabled} title={title}>
      <Icon name="seal" size={30} />
      <span>{children}</span>
    </button>
  );
}

/** A rubber stamp that thumps onto the page. */
export function Stamp({ text, tone = 'red' }: { text: string; tone?: 'red' | 'black' | 'green' }) {
  return <span className={`stamp stamp-${tone}`}>{text}</span>;
}

/** The settings card behind the gear. */
export function Settings({
  ink,
  setInk,
  reducedMotion,
  setReducedMotion,
  sound,
  setSound,
}: {
  ink: boolean;
  setInk: (v: boolean) => void;
  reducedMotion: boolean;
  setReducedMotion: (v: boolean) => void;
  sound: boolean;
  setSound: (v: boolean) => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div className="settings">
      <button
        className="hud-button"
        aria-label="Settings"
        aria-expanded={open}
        onClick={() => setOpen(!open)}
      >
        <Icon name="gear" />
      </button>
      <button
        className="hud-button"
        aria-label={sound ? 'Mute sound' : 'Unmute sound'}
        onClick={() => setSound(!sound)}
      >
        <Icon name={sound ? 'sound' : 'mute'} />
      </button>
      {open && (
        <div className="settings-card" role="dialog" aria-label="Settings">
          <p className="kicker">Settings</p>
          <Toggle checked={ink} onChange={setInk}>
            Engraving effect
          </Toggle>
          <Toggle checked={reducedMotion} onChange={setReducedMotion}>
            Reduce motion
          </Toggle>
          <Toggle checked={sound} onChange={setSound}>
            Sound
          </Toggle>
        </div>
      )}
    </div>
  );
}
