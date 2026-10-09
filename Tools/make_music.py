"""
Generates the game's background music: a short, gentle chiptune loop (original, made for this
project, so no third-party license applies). Python 3 standard library only.

Output: Assets/Audio/Music/hammy_theme.wav  (22.05 kHz, mono, 16-bit, loops seamlessly)
Usage:  python Tools/make_music.py
"""
import math
import os
import struct
import wave

RATE = 22050
BPM = 96
BEAT = 60.0 / BPM          # seconds per quarter note
VOLUME = 0.22              # keep it soft; SFX play on top

# Note names -> MIDI numbers (octave 4 = middle C).
NAMES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(note):
    """'C5' -> 72, 'A4' -> 69; None stays None (rest)."""
    if note is None:
        return None
    return 12 * (int(note[-1]) + 1) + NAMES[note[0]]


def freq(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# 8 bars of 4/4: (note, beats). Bouncy, cosy melody in C major.
MELODY = [
    ("E5", 1), ("G5", 1), ("C6", 1), ("G5", 1),
    ("A5", 1), ("G5", 1), ("E5", 2),
    ("F5", 1), ("A5", 1), ("G5", 1), ("E5", 1),
    ("D5", 2), (None, 2),
    ("E5", 1), ("G5", 1), ("C6", 1), ("B5", 1),
    ("A5", 1), ("G5", 1), ("E5", 1), ("D5", 1),
    ("C5", 1), ("E5", 1), ("D5", 1), ("B4", 1),
    ("C5", 3), (None, 1),
]
# One bass note per half bar: C, A, F, G progression twice.
BASS = [("C3", 2), ("G3", 2), ("A2", 2), ("E3", 2), ("F2", 2), ("C3", 2), ("G2", 2), ("D3", 2)] * 2


def triangle(phase):
    return 2.0 * abs(2.0 * (phase - math.floor(phase + 0.5))) - 1.0


def square(phase, duty=0.5):
    return 1.0 if (phase % 1.0) < duty else -1.0


def render(track, wave_fn, gain, total):
    out = [0.0] * total
    t = 0
    for note, beats in track:
        length = int(round(beats * BEAT * RATE))
        m = midi(note)
        if m is not None:
            f = freq(m)
            for i in range(length):
                if t + i >= total:
                    break
                # Short attack, gentle decay, release at the end of the note (no clicks).
                env = min(1.0, i / (0.01 * RATE)) * math.exp(-1.6 * i / RATE) * min(1.0, (length - i) / (0.02 * RATE))
                out[t + i] += gain * env * wave_fn(f * (t + i) / RATE)
        t += length
    return out


def main():
    beats = sum(b for _, b in MELODY)
    total = int(round(beats * BEAT * RATE))
    lead = render(MELODY, triangle, 0.55, total)
    bass = render(BASS, lambda p: square(p, 0.25), 0.18, total)
    mix = [VOLUME * (a + b) for a, b in zip(lead, bass)]

    path = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Music", "hammy_theme.wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in mix))
    print(f"wrote {os.path.normpath(path)} ({total / RATE:.1f}s loop)")


if __name__ == "__main__":
    main()
