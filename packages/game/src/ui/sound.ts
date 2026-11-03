/**
 * Small synthesized sound effects (Web Audio). No recorded assets, so nothing to license; the
 * commissioned soundtrack and foley replace these before release.
 */

let ctx: AudioContext | null = null;
let muted = false;

export function setMuted(m: boolean) {
  muted = m;
}

function audio(): AudioContext | null {
  if (muted) return null;
  try {
    ctx ??= new AudioContext();
    if (ctx.state === 'suspended') void ctx.resume();
    return ctx;
  } catch {
    return null;
  }
}

function noiseBuffer(a: AudioContext, seconds: number) {
  const buf = a.createBuffer(1, Math.max(1, Math.floor(a.sampleRate * seconds)), a.sampleRate);
  const data = buf.getChannelData(0);
  for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
  return buf;
}

function tone(
  a: AudioContext,
  freq: number,
  start: number,
  dur: number,
  gain: number,
  type: OscillatorType = 'sine',
) {
  const o = a.createOscillator();
  const g = a.createGain();
  o.type = type;
  o.frequency.value = freq;
  g.gain.setValueAtTime(0.0001, start);
  g.gain.exponentialRampToValueAtTime(gain, start + 0.005);
  g.gain.exponentialRampToValueAtTime(0.0001, start + dur);
  o.connect(g).connect(a.destination);
  o.start(start);
  o.stop(start + dur + 0.05);
}

function noise(
  a: AudioContext,
  start: number,
  dur: number,
  gain: number,
  filter: BiquadFilterType,
  freq: number,
  q = 1,
) {
  const src = a.createBufferSource();
  src.buffer = noiseBuffer(a, dur);
  const f = a.createBiquadFilter();
  f.type = filter;
  f.frequency.value = freq;
  f.Q.value = q;
  const g = a.createGain();
  g.gain.setValueAtTime(gain, start);
  g.gain.exponentialRampToValueAtTime(0.0001, start + dur);
  src.connect(f).connect(g).connect(a.destination);
  src.start(start);
}

export const sfx = {
  /** The shop bell: inharmonic partials with a long decay. */
  bell() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    [1, 2.76, 5.4, 8.93].forEach((m, i) => tone(a, 880 * m, t, 2.2 / (i + 1), 0.12 / (i + 1)));
  },
  /** A cork pulled, then a short pour. */
  pour() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    const o = a.createOscillator();
    const g = a.createGain();
    o.frequency.setValueAtTime(900, t);
    o.frequency.exponentialRampToValueAtTime(220, t + 0.06);
    g.gain.setValueAtTime(0.25, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + 0.08);
    o.connect(g).connect(a.destination);
    o.start(t);
    o.stop(t + 0.1);
    noise(a, t + 0.08, 0.5, 0.06, 'bandpass', 1400, 3);
  },
  /** The till: a clack and two bright rings. */
  till() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    noise(a, t, 0.05, 0.3, 'highpass', 2000);
    tone(a, 2093, t + 0.06, 0.5, 0.08, 'triangle');
    tone(a, 2637, t + 0.12, 0.6, 0.07, 'triangle');
  },
  /** Paper handled. */
  paper() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    noise(a, t, 0.12, 0.08, 'highpass', 3000);
    noise(a, t + 0.07, 0.1, 0.05, 'highpass', 4500);
  },
  /** A rubber stamp coming down. */
  stamp() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    const o = a.createOscillator();
    const g = a.createGain();
    o.frequency.setValueAtTime(140, t);
    o.frequency.exponentialRampToValueAtTime(50, t + 0.12);
    g.gain.setValueAtTime(0.4, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
    o.connect(g).connect(a.destination);
    o.start(t);
    o.stop(t + 0.2);
    noise(a, t, 0.06, 0.12, 'lowpass', 900);
  },
  /** The still's fire catching. */
  fire() {
    const a = audio();
    if (!a) return;
    const t = a.currentTime;
    noise(a, t, 1.2, 0.18, 'lowpass', 500, 0.7);
    noise(a, t + 0.1, 0.8, 0.05, 'bandpass', 2500, 2);
  },
  /** A detent on a brass dial. */
  tick() {
    const a = audio();
    if (!a) return;
    noise(a, a.currentTime, 0.02, 0.08, 'highpass', 5000);
  },
};
