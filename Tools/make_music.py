"""
Generates the game's background music: a minimal, calm loop (original, made for this project,
so no third-party license applies). Python 3 standard library only.

Style: slow (72 BPM), soft bell-like arpeggio notes with long decays over a very quiet warm pad.
Chords: Cmaj7 | Am7 | Fmaj7 | G6, two bars each (8 bars, ~27 s), loops seamlessly.

Output: Assets/Audio/Music/hammy_theme.wav  (22.05 kHz, mono, 16-bit)
Usage:  python Tools/make_music.py
"""
import math
import os
import struct
import wave

RATE = 22050
BPM = 72
BEAT = 60.0 / BPM          # seconds per quarter note
BARS_PER_CHORD = 2
VOLUME = 0.30              # quiet: music should sit under the sound effects

NAMES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(note):
    """'C5' -> 72."""
    return 12 * (int(note[-1]) + 1) + NAMES[note[0]]


def freq(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# Each chord: pad notes (held) and an arpeggio pattern (one note per beat, None = rest).
CHORDS = [
    (["C3", "G3", "E4"], ["E5", "G5", "B5", None, "G5", None, "E5", None]),   # Cmaj7
    (["A2", "E3", "C4"], ["C5", "E5", "G5", None, "E5", None, "A4", None]),   # Am7
    (["F2", "C3", "A3"], ["A4", "C5", "E5", None, "C5", None, "F5", None]),   # Fmaj7
    (["G2", "D3", "B3"], ["B4", "D5", "E5", None, "D5", None, "B4", None]),   # G6
]


def bell(t, f):
    """Soft bell: sine plus a quiet octave, with a long exponential decay."""
    env = min(1.0, t / 0.008) * math.exp(-2.2 * t)
    return env * (math.sin(2 * math.pi * f * t) + 0.25 * math.sin(2 * math.pi * 2 * f * t))


def main():
    beats_per_chord = 4 * BARS_PER_CHORD
    chord_len = int(round(beats_per_chord * BEAT * RATE))
    total = chord_len * len(CHORDS)
    mix = [0.0] * total
    beat = int(round(BEAT * RATE))

    for c, (pad, arp) in enumerate(CHORDS):
        start = c * chord_len
        # Warm pad: slow swell in and out across the chord (very quiet).
        for note in pad:
            f = freq(midi(note))
            for i in range(chord_len):
                swell = math.sin(math.pi * i / chord_len) ** 2
                t = (start + i) / RATE
                mix[start + i] += 0.07 * swell * math.sin(2 * math.pi * f * t)
        # Arpeggio: one soft bell per beat, ringing for up to 3 beats (wraps around the loop end).
        for b, note in enumerate(arp):
            if note is None:
                continue
            f = freq(midi(note))
            onset = start + b * beat
            for i in range(3 * beat):
                mix[(onset + i) % total] += 0.32 * bell(i / RATE, f)

    peak = max(abs(s) for s in mix) or 1.0
    out = [VOLUME * s / peak for s in mix]

    path = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Music", "hammy_theme.wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in out))
    print(f"wrote {os.path.normpath(path)} ({total / RATE:.1f}s loop)")


if __name__ == "__main__":
    main()
