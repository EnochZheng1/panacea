#!/usr/bin/env python3
"""
Synthesise Panacea's sound effects into godot/assets/sound/*.wav (44.1 kHz, mono, 16-bit).
Everything is generated here from noise and sine partials, so the sounds are original and
project-owned. Deterministic: fixed seeds, so re-running reproduces the same files.

Usage: python3 tools/synth_sounds.py [name ...]
"""
import os
import sys
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets', 'sound')


def t(d):
    return np.arange(int(SR * d)) / SR


def noise(d, seed):
    return np.random.default_rng(seed).standard_normal(int(SR * d))


def filt(x, lo=None, hi=None, order=4, circular=False):
    """Zero-phase spectral band filter. Pads one-shots so the filter cannot wrap."""
    n = len(x)
    pad = 0 if circular else min(n, SR // 2)
    y = np.concatenate([x, np.zeros(pad)])
    f = np.fft.rfftfreq(len(y), 1 / SR)
    g = np.ones_like(f)
    if lo:
        g *= 1 / (1 + (lo / np.maximum(f, 1)) ** order)
    if hi:
        g *= 1 / (1 + (f / hi) ** order)
    return np.fft.irfft(np.fft.rfft(y) * g, n=len(y))[:n]


def partials(d, freqs, amps, decays, phase_seed=0):
    tt = t(d)
    rng = np.random.default_rng(phase_seed)
    y = np.zeros_like(tt)
    for f, a, k in zip(freqs, amps, decays):
        y += a * np.sin(2 * np.pi * f * tt + rng.uniform(0, 2 * np.pi)) * np.exp(-tt / k)
    return y


def thump(d, f0, f1, decay):
    """A low knock whose pitch falls as it dies (a heavy thing landing)."""
    tt = t(d)
    f = f1 + (f0 - f1) * np.exp(-tt / (decay * 0.5))
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tt / decay)


def burst(d, seed, decay, lo=None, hi=None):
    tt = t(d)
    return filt(noise(d, seed), lo, hi) * np.exp(-tt / decay)


def reverb(x, rt, mix, seed=99, circular=False, bright=5000):
    """Convolution with a decaying-noise room. Circular mode keeps loops seamless."""
    m = int(SR * rt)
    ir = noise(rt, seed) * np.exp(-6.9 * t(rt) / rt)
    ir = filt(ir, hi=bright)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    if circular:
        n = len(x)
        irp = np.zeros(n)
        irp[: min(m, n)] = ir[:n]
        wet = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(irp), n=n)
        return (1 - mix) * x + mix * wet
    n = len(x) + m
    wet = np.fft.irfft(np.fft.rfft(x, n) * np.fft.rfft(ir, n), n=n)
    dry = np.concatenate([x, np.zeros(m)])
    return (1 - mix) * dry + mix * wet


def place(buf, x, at):
    """Adds x into buf at time `at` (seconds), wrapping around the end (for loops)."""
    i = int(at * SR) % len(buf)
    j = i + len(x)
    if j <= len(buf):
        buf[i:j] += x
    else:
        k = len(buf) - i
        buf[i:] += x[:k]
        buf[: len(x) - k] += x[k:]


def fade(x, a=0.003, r=0.02):
    n = len(x)
    e = np.ones(n)
    na, nr = int(SR * a), int(SR * r)
    if na:
        e[:na] = np.linspace(0, 1, na)
    if nr:
        e[-nr:] *= np.linspace(1, 0, nr)
    return x * e


def norm(x, peak):
    return x / (np.max(np.abs(x)) + 1e-9) * peak


def write(name, x, peak=0.7, loop=False):
    x = norm(x, peak) if loop else norm(fade(x), peak)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())
    print(f'{name}: {len(x) / SR:.2f}s')


# ---------------------------------------------------------------- the sounds


def tick():  # the cursor finding a ticket: barely there
    return burst(0.02, 1, 0.0015, lo=3500)


def ticket():  # pressing a printed ticket: a card tap on wood
    d = 0.16
    x = partials(d, [690, 1270, 2210], [1, 0.45, 0.2], [0.02, 0.011, 0.006]) + 0.8 * burst(d, 2, 0.004, 900, 6000)
    return reverb(x, 0.18, 0.12)


def slug():  # a claim set in type: a lead slug dropped into the composing stick
    d = 0.3
    x = partials(d, [2370, 3610, 5230, 7020], [1, 0.6, 0.4, 0.25], [0.07, 0.045, 0.03, 0.02], 3)
    x += 0.6 * partials(d, [410, 830], [1, 0.4], [0.02, 0.01]) + 0.7 * burst(d, 3, 0.003, 2000)
    return reverb(x, 0.25, 0.15)


def slip():  # a slip of paper slid across the table
    d = 0.34
    tt = t(d)
    env = np.sin(np.pi * np.clip(tt / d, 0, 1)) ** 2 * np.exp(-tt / 0.25)
    grain = 0.6 + 0.4 * filt(noise(d, 5), hi=60) / 0.05
    return filt(noise(d, 4), 1500, 8000) * env * np.clip(grain, 0.2, 1.6)


def page():  # turning to the other side of the table
    d = 0.5
    x = np.zeros(int(SR * d))
    place(x, slip()[: int(SR * 0.22)] * 0.8, 0.0)
    place(x, slip() * 1.0, 0.12)
    place(x, 0.5 * burst(0.12, 6, 0.02, hi=500), 0.33)
    return x


def lever():  # pulling the press lever: a ratchet, then the clunk
    d = 0.9
    x = np.zeros(int(SR * d))
    for i in range(5):
        place(x, 0.35 * partials(0.06, [3100, 4700], [1, 0.5], [0.008, 0.005], 10 + i) + 0.2 * burst(0.06, 20 + i, 0.002, 2000), 0.03 * i)
    clunk = thump(0.6, 95, 52, 0.12) + 0.6 * burst(0.6, 7, 0.03, hi=500) + 0.25 * partials(0.6, [1080, 1710], [1, 0.6], [0.22, 0.15], 8)
    place(x, clunk, 0.17)
    return reverb(x, 0.6, 0.22)


def press_loop():  # a flatbed press at work: a seamless four-second loop
    d = 4.0
    cycle = 0.8
    x = np.zeros(int(SR * d))
    for i in range(int(d / cycle)):
        at = i * cycle + 0.2
        place(x, thump(0.4, 80, 48, 0.09) + 0.5 * burst(0.4, 30 + i, 0.025, hi=700), at)
        place(x, 0.32 * partials(0.3, [910, 1460, 2310], [1, 0.6, 0.35], [0.07, 0.05, 0.03], 40 + i) + 0.2 * burst(0.3, 50 + i, 0.01, 1500), at + 0.4)
        place(x, 0.18 * burst(0.25, 60 + i, 0.06, 2500, 9000), at + 0.55)  # the sheet carried off
    tt = t(d)
    whir = filt(noise(d, 70), 180, 900, circular=True) * (0.6 + 0.4 * np.sin(2 * np.pi * tt / cycle)) * 0.08
    return reverb(x + whir, 0.5, 0.2, circular=True)


def sheet():  # a printed sheet landing on the pile
    d = 0.4
    tt = t(d)
    whoosh = filt(noise(d, 80), 800, 6000) * np.clip(tt / 0.14, 0, 1) ** 2 * (tt < 0.16)
    x = 0.4 * whoosh
    place(x, 0.9 * burst(0.25, 81, 0.03, hi=700) + 0.3 * burst(0.25, 82, 0.015, 1500, 6000), 0.15)
    return x


def roller():  # the inked roller passing down the page: tacky and even
    d = 1.7
    tt = t(d)
    env = np.clip(tt / 0.15, 0, 1) * np.clip((d - tt) / 0.3, 0, 1)
    tack = filt(noise(d, 90), 300, 3000) * (0.6 + 0.4 * np.sin(2 * np.pi * 14 * tt))
    rumble = filt(noise(d, 91), 60, 220) * 1.5
    return reverb((tack + rumble) * env, 0.3, 0.15)


def slam():  # the headline comes down: the platen meets the forme
    d = 1.4
    x = thump(d, 70, 44, 0.28) + 0.6 * thump(d, 130, 90, 0.12)
    x += 0.9 * burst(d, 100, 0.02, hi=2200) + 0.12 * partials(d, [620, 985, 1530], [1, 0.7, 0.4], [0.35, 0.25, 0.15], 101)
    return reverb(x, 1.1, 0.3)


def till():  # the books balance: the drawer runs out and the bell rings
    d = 1.3
    x = np.zeros(int(SR * d))
    place(x, 0.5 * thump(0.2, 160, 110, 0.04) + 0.3 * burst(0.2, 110, 0.01, 800, 5000), 0.0)
    place(x, 0.35 * filt(noise(0.18, 111), 400, 2500) * np.exp(-t(0.18) / 0.08), 0.05)
    bell = partials(1.1, [1820, 1820 * 2.76, 1820 * 5.4, 1820 * 8.93], [1, 0.45, 0.25, 0.1], [0.7, 0.35, 0.2, 0.1], 112)
    place(x, 0.8 * bell + 0.2 * burst(1.1, 113, 0.002, 3000), 0.12)
    return reverb(x, 0.5, 0.18)


def coin():  # one coin counted onto the pile
    return partials(0.18, [3260, 4920, 6870], [1, 0.6, 0.3], [0.035, 0.025, 0.015], 120) + 0.3 * burst(0.18, 121, 0.002, 4000)


def knell():  # a death in the paper: one stroke of the parish bell, far off
    d = 7.0
    f = 196.0  # G3
    ratios = [0.5, 1.0, 1.19, 1.5, 2.0, 2.52, 3.01, 4.17, 5.43]
    amps = [0.5, 0.8, 0.6, 0.3, 1.0, 0.3, 0.25, 0.15, 0.1]
    decays = [6.0, 4.0, 3.5, 2.5, 2.4, 1.5, 1.2, 0.8, 0.5]
    x = partials(d, [f * r for r in ratios], amps, decays, 130)
    x += 0.5 * partials(d, [f * r * 1.003 for r in ratios[:5]], amps[:5], decays[:5], 131)  # slow beating
    x += 0.4 * burst(d, 132, 0.01, 500, 4000)  # the clapper
    x = filt(x, hi=3500)
    return reverb(x, 3.0, 0.4, bright=2500)


def room():  # the shop: a clock on the wall and the street beyond the glass (8 s loop)
    d = 8.0
    tt = t(d)
    x = filt(np.cumsum(noise(d, 140)), 30, 300, circular=True)
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.25
    street = filt(noise(d, 141), 80, 600, circular=True) * (0.5 + 0.5 * np.sin(2 * np.pi * tt / d) ** 2) * 0.15
    x += street / (np.max(np.abs(street)) + 1e-9) * 0.12
    for i in range(int(d)):
        tickf = [2650, 4100] if i % 2 == 0 else [2240, 3500]
        place(x, 0.22 * (partials(0.08, tickf, [1, 0.4], [0.012, 0.007], 150 + i) + 0.4 * burst(0.08, 160 + i, 0.003, 1500)), i + 0.25)
    return reverb(x, 0.4, 0.15, circular=True)


def death():  # Death at someone's shoulder: a low, slow, beating breath (6 s loop)
    d = 6.0
    tt = t(d)
    x = 0.6 * np.sin(2 * np.pi * 55 * tt) + 0.6 * np.sin(2 * np.pi * (55 + 1 / 1.5) * tt) + 0.25 * np.sin(2 * np.pi * 82.5 * tt)
    breath = filt(noise(d, 170), 90, 420, circular=True) * (0.5 + 0.5 * np.sin(2 * np.pi * tt / d - np.pi / 2))
    x += 0.9 * breath / (np.max(np.abs(breath)) + 1e-9)
    return x


SOUNDS = {
    'tick': (tick, 0.25, False), 'ticket': (ticket, 0.5, False), 'slug': (slug, 0.55, False),
    'slip': (slip, 0.4, False), 'page': (page, 0.45, False), 'lever': (lever, 0.8, False),
    'press_loop': (press_loop, 0.6, True), 'sheet': (sheet, 0.55, False), 'roller': (roller, 0.5, False),
    'slam': (slam, 0.9, False), 'till': (till, 0.6, False), 'coin': (coin, 0.3, False),
    'knell': (knell, 0.7, False), 'room': (room, 0.35, True), 'death': (death, 0.4, True),
}

if __name__ == '__main__':
    for n in sys.argv[1:] or SOUNDS:
        fn, peak, loop = SOUNDS[n]
        write(n, fn(), peak, loop)
