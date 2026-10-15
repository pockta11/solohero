"""Synthesized SFX, 44.1 kHz mono 16-bit: skill fire, thunder, ice, heal, magic (D-078); reward collect and fanfare (D-171).

    python tools/art/sfxgen.py Assets/SoloHero/Audio/Sfx                   -> every sound
    python tools/art/sfxgen.py Assets/SoloHero/Audio/Sfx collect fanfare   -> only these (the others keep their bytes)
"""
import sys, os, wave
import numpy as np

OUT = sys.argv[1]
SR = 44100
rng = np.random.default_rng(7)


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, a, d):
    e = np.ones(n)
    na = max(1, int(SR * a))
    e[:na] = np.linspace(0, 1, na)
    e[na:] = np.exp(-np.arange(n - na) / (SR * d))
    return e


def lowpass(x, k):
    # simple one-pole
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += k * (v - acc)
        y[i] = acc
    return y


def save(name, x, gain=0.8):
    x = x / (np.max(np.abs(x)) + 1e-9) * gain
    fade = int(SR * 0.01)
    x[-fade:] *= np.linspace(1, 0, fade)
    with wave.open(os.path.join(OUT, 'sfx_%s.wav' % name), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def fire():
    n = len(t(0.7))
    noise = rng.standard_normal(n)
    body = lowpass(noise, 0.08) * env(n, 0.03, 0.22)
    crackle = (rng.random(n) < 0.004) * rng.standard_normal(n) * 3
    crackle = lowpass(crackle, 0.5) * env(n, 0.0, 0.35)
    boom = np.sin(2 * np.pi * 70 * t(0.7) * (1 - t(0.7) * 0.5)) * env(n, 0.005, 0.12)
    return body * 1.2 + crackle + boom * 0.8


def thunder():
    n = len(t(1.0))
    crack = rng.standard_normal(n) * env(n, 0.0, 0.05)
    rumble = lowpass(rng.standard_normal(n), 0.02) * env(n, 0.02, 0.45) * 4
    zap = np.sign(np.sin(2 * np.pi * (900 - 700 * t(1.0)) * t(1.0))) * env(n, 0.0, 0.06) * 0.4
    return crack + rumble + zap


def ice():
    tt = t(0.8)
    n = len(tt)
    x = np.zeros(n)
    for k, f in enumerate([1760, 2349, 2637, 3136, 3520]):
        start = int(SR * k * 0.045)
        seg = tt[: n - start]
        x[start:] += np.sin(2 * np.pi * f * seg) * np.exp(-seg / 0.18) * 0.5
    crash = lowpass(rng.standard_normal(n), 0.6) * env(n, 0.0, 0.08) * 0.6
    return x + crash


def heal():
    tt = t(0.9)
    n = len(tt)
    x = np.zeros(n)
    for k, f in enumerate([523, 659, 784, 1047, 1319]):
        start = int(SR * k * 0.08)
        seg = tt[: n - start]
        tone = np.sin(2 * np.pi * f * seg) + 0.3 * np.sin(2 * np.pi * f * 2 * seg)
        x[start:] += tone * np.exp(-seg / 0.35) * 0.4
    shimmer = np.sin(2 * np.pi * 6 * tt) * 0.15 + 1
    return x * shimmer


def magic():
    tt = t(0.8)
    n = len(tt)
    f = 300 + 1500 * tt / 0.8
    sweep = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.05, 0.35)
    sparkle = np.zeros(n)
    for k in range(10):
        start = int(rng.uniform(0.05, 0.6) * SR)
        seg = tt[: n - start]
        sparkle[start:] += np.sin(2 * np.pi * rng.uniform(2500, 4500) * seg) * np.exp(-seg / 0.05) * 0.3
    return sweep * 0.7 + sparkle


def tone(freq, sec, decay, harmonics=(1.0, 0.35, 0.12)):
    """A bell-like note: a few harmonics under an exponential decay."""
    tt = t(sec)
    x = sum(a * np.sin(2 * np.pi * freq * (k + 1) * tt) for k, a in enumerate(harmonics))
    return x * env(len(tt), 0.004, decay)


def place(buf, x, at):
    i = int(SR * at)
    buf[i:i + len(x)] += x[:max(0, len(buf) - i)]


def collect():
    """Reward icons landing in a counter: a bright two-note chime with a short sparkle."""
    buf = np.zeros(len(t(0.45)))
    place(buf, tone(1318.5, 0.3, 0.09), 0.0)
    place(buf, tone(1975.5, 0.35, 0.12) * 0.9, 0.06)
    sparkle = rng.standard_normal(len(buf)) * env(len(buf), 0.0, 0.03)
    return buf + lowpass(sparkle, 0.6) * 0.15


def fanfare():
    """Job advance, chapter clear, set complete: a rising major arpeggio into a held chord."""
    buf = np.zeros(len(t(1.25)))
    notes = (523.25, 659.25, 783.99)
    for i, f in enumerate(notes):
        place(buf, tone(f, 0.25, 0.1, (1.0, 0.5, 0.25, 0.1)), i * 0.09)
    chord = sum(tone(f * 2, 0.95, 0.38, (1.0, 0.45, 0.2, 0.08)) for f in notes)
    place(buf, chord * 0.7, 0.3)
    shimmer = np.zeros(len(buf))
    for k in range(14):
        place(shimmer, tone(rng.uniform(2600, 4200), 0.12, 0.03, (1.0,)) * 0.2, 0.3 + k * 0.045)
    return buf + shimmer


SOUNDS = {
    'skill_fire': (fire, 0.8), 'skill_thunder': (thunder, 0.9), 'skill_ice': (ice, 0.7), 'skill_heal': (heal, 0.7),
    'skill_magic': (magic, 0.7), 'collect': (collect, 0.6), 'fanfare': (fanfare, 0.75),
}

os.makedirs(OUT, exist_ok=True)
wanted = sys.argv[2:] or list(SOUNDS)
for name in wanted:
    make, gain = SOUNDS[name]
    save(name, make(), gain)
print('ok', ', '.join(wanted))
