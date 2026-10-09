"""Original deterministic CatapultCats synthesis; no samples or dependencies.

Run: python Tools/generate_r5_sfx.py
Audit only: python Tools/generate_r5_sfx.py --check
Existing WAVs are never replaced unless --overwrite is explicitly supplied.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/_CatapultCats/Audio/SFX"
SOUNDS = {
    "SlingshotStretch": (0.19, 0.40),
    "CatLaunch": (0.32, 0.62),
    "ImpactThump": (0.18, 0.62),
    "WoodBreak": (0.24, 0.60),
    "GlassBreak": (0.42, 0.54),
    "MouseDefeat": (0.20, 0.50),
    "LevelWin": (0.66, 0.58),
    "LevelFail": (0.56, 0.52),
}


def sweep(t, start, end, duration):
    """Phase-integrated exponential chirp (not sin(time * changing pitch))."""
    k = math.log(end / start) / duration
    phase = start * math.expm1(k * t) / k if abs(k) > 1e-10 else start * t
    return math.sin(math.tau * phase)


def bell(t, frequency, decay):
    return (math.sin(math.tau * frequency * t)
            + 0.22 * math.sin(math.tau * frequency * 2 * t)) * math.exp(-decay * t)


def synthesize(name, duration, peak):
    rng = random.Random(5100 + list(SOUNDS).index(name))
    filtered = 0.0
    slow = 0.0
    samples = []
    # Rounded attacks, filtered noise and sub-3 kHz partials keep transients friendly.
    for i in range(round(RATE * duration)):
        t = i / RATE
        filtered += 0.19 * (rng.uniform(-1, 1) - filtered)
        slow += 0.045 * (filtered - slow)
        texture = filtered - slow
        if name == "SlingshotStretch":
            value = (0.32 * sweep(t, 210, 310, duration)
                     + 0.12 * texture) * math.exp(-14 * t)
        elif name == "CatLaunch":
            value = (0.62 * sweep(t, 490, 170, duration) * math.exp(-10 * t)
                     + 0.30 * texture * math.exp(-((t - 0.065) / 0.055) ** 2))
        elif name == "ImpactThump":
            value = (0.85 * sweep(t, 185, 75, duration)
                     + 0.35 * texture) * math.exp(-26 * t)
        elif name == "WoodBreak":
            value = (0.62 * texture * math.exp(-30 * t)
                     + 0.27 * bell(t, 370, 29) + 0.20 * bell(t, 690, 37))
            for delay in (0.024, 0.049):
                if t >= delay:
                    value += 0.17 * texture * math.exp(-70 * (t - delay))
        elif name == "GlassBreak":
            value = 0.42 * texture * math.exp(-46 * t)
            for delay, frequency, strength in ((0, 1450, .23), (.026, 1910, .17),
                                                (.060, 2330, .12), (.102, 1720, .10)):
                if t >= delay:
                    u = t - delay
                    value += strength * math.sin(math.tau * frequency * u) * math.exp(-15 * u)
        elif name == "MouseDefeat":
            value = (0.54 * sweep(t, 780, 440, duration) * math.exp(-19 * t)
                     + 0.22 * sweep(t, 180, 95, duration) * math.exp(-40 * t))
        else:
            notes = ((0, 392), (.16, 494), (.32, 587)) if name == "LevelWin" else ((0, 392), (.18, 294))
            value = 0.0
            for delay, frequency in notes:
                if t >= delay:
                    u = t - delay
                    attack = min(1.0, u / .012)
                    value += 0.42 * attack * bell(u, frequency, 10)
        samples.append(value)
    mean = sum(samples) / len(samples)
    samples = [(v - mean) * min(1.0, i / (RATE * .004))
               * min(1.0, (len(samples) - 1 - i) / (RATE * .015))
               for i, v in enumerate(samples)]
    # Remove residual DC without changing silent endpoints (a tapered correction).
    weights = [math.sin(math.pi * i / (len(samples) - 1)) ** 2 for i in range(len(samples))]
    correction = sum(samples) / sum(weights)
    samples = [v - correction * w for v, w in zip(samples, weights)]
    scale = peak / max(abs(v) for v in samples)
    return [round(max(-.70, min(.70, v * scale)) * 32767) for v in samples]


def audit():
    results = []
    for name, (expected_duration, _) in SOUNDS.items():
        path = OUTPUT / (name + ".wav")
        with wave.open(str(path), "rb") as source:
            assert (source.getnchannels(), source.getsampwidth(), source.getframerate(), source.getcomptype()) == (1, 2, RATE, "NONE"), path
            count = source.getnframes()
            samples = struct.unpack("<" + "h" * count, source.readframes(count))
        peak = max(abs(v) for v in samples) / 32768
        rms = math.sqrt(sum(v * v for v in samples) / count) / 32768
        assert abs(count / RATE - expected_duration) < 1 / RATE
        assert .01 < peak <= .70 and rms > .005
        assert abs(sum(samples) / count / 32768) < .00001
        assert samples[0] == samples[-1] == 0
        assert path.stat().st_size < 100000
        results.append(dict(file=path.name, seconds=count / RATE, bytes=path.stat().st_size,
                            peak=round(peak, 4), rms=round(rms, 4),
                            sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    print(json.dumps(results, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()
    if not args.check:
        OUTPUT.mkdir(parents=True, exist_ok=True)
        for name, (duration, peak) in SOUNDS.items():
            path = OUTPUT / (name + ".wav")
            pcm = struct.pack("<" + "h" * round(RATE * duration), *synthesize(name, duration, peak))
            if path.exists() and not args.overwrite:
                with wave.open(str(path), "rb") as existing:
                    if existing.readframes(existing.getnframes()) != pcm:
                        raise RuntimeError(f"Refusing to replace {path}; use --overwrite intentionally.")
                continue
            with wave.open(str(path), "wb") as destination:
                destination.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
                destination.writeframes(pcm)
    audit()


if __name__ == "__main__":
    main()
