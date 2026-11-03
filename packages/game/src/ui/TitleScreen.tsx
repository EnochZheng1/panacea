import { useState } from 'react';
import { content } from '../state/useGame.ts';
import { SealButton } from './controls.tsx';
import { Corners, Rule } from './ornaments.tsx';
import { t } from './text.ts';

/** The title: a chromolithographed trade card over the shop at night. */
export function TitleScreen({ onStart }: { onStart: (backgroundId: string) => void }) {
  const [background, setBackground] = useState(content.config.defaultBackground);
  return (
    <div className="title-screen">
      <div className="trade-card">
        <Corners />
        <p className="tc-kicker">Established 1885 · New York</p>
        <h1 className="logo">
          <span>Panacea</span>
        </h1>
        <p className="tc-sub">A Proprietary Remedy for Every Complaint</p>
        <Rule glyph="✠" />
        <p className="tc-premise">
          No law forbids what you put in a bottle. Not yet. Build a medicine empire before the Pure
          Food and Drug Act of 1906.
        </p>
        <div className="backgrounds" role="radiogroup" aria-label="Who are you?">
          {content.backgrounds.map((b) => (
            <button
              key={b.id}
              role="radio"
              aria-checked={background === b.id}
              className={`bg-card ${background === b.id ? 'on' : ''}`}
              onClick={() => setBackground(b.id)}
            >
              <b>{t(content, `background.${b.id}.name`)}</b>
              <small>{t(content, `background.${b.id}.description`)}</small>
            </button>
          ))}
        </div>
        <SealButton onClick={() => onStart(background)}>Open the shop</SealButton>
        <p className="tc-warning">
          Contains depictions of drug use, addiction and death, including of children, as part of
          its historical satire.
        </p>
      </div>
    </div>
  );
}
