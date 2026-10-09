"""
Generates the game's background music: one minimal loop per scene (original, made for this
project, so no third-party license applies). Python 3 standard library only.

  music_home.wav     cozy home        soft bells over a quiet pad              (72 BPM)
  music_garden.wav   sunny garden     light plucks + a few bird chirps         (96 BPM)
  music_beach.wav    beach            slow ukulele-like strums + gentle waves  (80 BPM)
  music_forest.wav   forest stream    breathy flute melody + babbling stream   (66 BPM)
  music_rooftop.wav  sunset rooftop   warm lo-fi electric piano + soft thump   (64 BPM)
  music_night.wav    moonlit bedroom  music-box lullaby (also plays while the pet sleeps) (60 BPM)

Every track is 8 bars, loops seamlessly (notes ringing past the end wrap to the start, ambient
noise is crossfaded), and is normalised to the same quiet average loudness (about -24 dB).

Output: Assets/Audio/Music/music_<scene>.wav  (22.05 kHz, mono, 16-bit)
Usage:  python Tools/make_music.py
"""
import math
import os
import random
import struct
import wave

RATE = 22050
TARGET_RMS = 0.063  # about -24 dB average loudness, the same for every track
PEAK_LIMIT = 0.60   # never louder than this at any moment
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Music")
NAMES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
TAU = 2 * math.pi


def midi(note):
    """'C5' -> 72, 'Bb4' -> 70, 'F#4' -> 66."""
    n = NAMES[note[0]]
    if note[1] == "b":
        n -= 1
    elif note[1] == "#":
        n += 1
    return 12 * (int(note[-1]) + 1) + n


def freq(note):
    return 440.0 * 2 ** ((midi(note) - 69) / 12.0)


class Track:
    """A loop buffer; notes added near the end wrap around to the start (seamless loop)."""

    def __init__(self, bpm, bars=8):
        self.beat = 60.0 / bpm
        self.length = int(round(bars * 4 * self.beat * RATE))
        self.buf = [0.0] * self.length

    def at(self, beat):
        return int(round(beat * self.beat * RATE))

    def add(self, start, samples):
        n = self.length
        for i, s in enumerate(samples):
            self.buf[(start + i) % n] += s

    def save(self, name):
        # Same average loudness for every scene (so switching scenes doesn't jump in volume),
        # without letting any peak go above PEAK_LIMIT.
        rms = math.sqrt(sum(s * s for s in self.buf) / len(self.buf)) or 1.0
        peak = max(abs(s) for s in self.buf) or 1.0
        scale = min(TARGET_RMS / rms, PEAK_LIMIT / peak)
        path = os.path.join(OUT_DIR, name)
        with wave.open(path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(b"".join(struct.pack("<h", int(scale * s * 32767)) for s in self.buf))
        print(f"wrote {name} ({self.length / RATE:.1f}s)")


# ----------------------------------------------------------------- instruments

def bell(f, seconds, gain=1.0, decay=2.2):
    n = int(seconds * RATE)
    return [gain * min(1.0, i / 180) * math.exp(-decay * i / RATE) *
            (math.sin(TAU * f * i / RATE) + 0.25 * math.sin(TAU * 2 * f * i / RATE)) for i in range(n)]


def music_box(f, seconds, gain=1.0):
    """Bright, short bell with a metallic upper partial."""
    n = int(seconds * RATE)
    return [gain * min(1.0, i / 60) * math.exp(-4.0 * i / RATE) *
            (math.sin(TAU * f * i / RATE) + 0.4 * math.sin(TAU * 4.01 * f * i / RATE) * math.exp(-8.0 * i / RATE))
            for i in range(n)]


def pluck(f, seconds, gain=1.0, damping=0.996, seed=1):
    """Karplus-Strong plucked string (guitar / ukulele-like)."""
    rnd = random.Random(seed)
    period = max(2, int(RATE / f))
    ring = [rnd.uniform(-1, 1) for _ in range(period)]
    out = []
    for i in range(int(seconds * RATE)):
        j = i % period
        nxt = ring[(j + 1) % period]
        ring[j] = damping * 0.5 * (ring[j] + nxt)
        out.append(gain * ring[j])
    return out


def flute(f, seconds, gain=1.0, seed=2):
    """Soft sine with slow attack, gentle vibrato and a little breath noise."""
    rnd = random.Random(seed)
    n = int(seconds * RATE)
    out, phase, lp = [], 0.0, 0.0
    for i in range(n):
        t = i / RATE
        env = min(1.0, t / 0.25) * min(1.0, (n - i) / (0.3 * RATE))
        vib = 1.0 + 0.004 * math.sin(TAU * 5.0 * t) * min(1.0, t / 0.6)
        phase += TAU * f * vib / RATE
        lp += 0.1 * (rnd.uniform(-1, 1) - lp)
        out.append(gain * env * (math.sin(phase) + 0.08 * lp))
    return out


def epiano(f, seconds, gain=1.0):
    """Warm electric-piano tone: sine + soft 2nd harmonic, slow tremolo, long decay."""
    n = int(seconds * RATE)
    return [gain * min(1.0, i / 120) * math.exp(-1.3 * i / RATE) * (1.0 + 0.15 * math.sin(TAU * 4.5 * i / RATE)) *
            (math.sin(TAU * f * i / RATE) + 0.18 * math.sin(TAU * 2 * f * i / RATE) * math.exp(-3 * i / RATE))
            for i in range(n)]


def pad(track, notes, start_beat, beats, gain=0.06):
    """Very quiet sustained chord that swells in and out."""
    start, n = track.at(start_beat), track.at(beats)
    for note in notes:
        f = freq(note)
        track.add(start, [gain * math.sin(math.pi * i / n) ** 2 * math.sin(TAU * f * (start + i) / RATE)
                          for i in range(n)])


def thump(seconds=0.35, gain=1.0):
    """Soft low kick: pitch-dropping sine."""
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        phase += TAU * (55 + 60 * math.exp(-30 * t)) / RATE
        out.append(gain * math.exp(-9 * t) * math.sin(phase))
    return out


def chirp(gain=1.0):
    """Tiny bird: two quick upward whistles."""
    out = []
    for k in range(2):
        n = int(0.09 * RATE)
        phase = 0.0
        for i in range(n):
            t = i / RATE
            phase += TAU * (2600 + 1800 * t / 0.09) / RATE
            out.append(gain * math.sin(math.pi * i / n) * math.sin(phase))
        out += [0.0] * int(0.05 * RATE)
    return out


def ambience(track, kind, gain, seed=7):
    """Looping filtered noise: 'waves' (slow swells) or 'stream' (soft babble)."""
    rnd = random.Random(seed)
    n, fade = track.length, int(1.0 * RATE)
    noise, lp, lp2 = [], 0.0, 0.0
    cut = 0.02 if kind == "waves" else 0.08
    for i in range(n + fade):
        lp += cut * (rnd.uniform(-1, 1) - lp)
        lp2 += cut * (lp - lp2)
        t = i / RATE
        if kind == "waves":  # 4 swells per loop
            amp = 0.35 + 0.65 * math.sin(math.pi * 4 * (i % n) / n) ** 2
        else:                # quick irregular babble
            amp = 0.6 + 0.4 * math.sin(TAU * 1.7 * t) * math.sin(TAU * 0.43 * t)
        noise.append(amp * lp2)
    # Crossfade the extra tail into the start so the loop point is seamless.
    for i in range(fade):
        w = i / fade
        noise[i] = noise[i] * w + noise[n + i] * (1 - w)
    peak = max(abs(s) for s in noise[:n]) or 1.0
    track.add(0, [gain * s / peak for s in noise[:n]])


# ----------------------------------------------------------------- tracks

def home():
    tr = Track(72)
    chords = [(["C3", "G3", "E4"], ["E5", "G5", "B5", None, "G5", None, "E5", None]),
              (["A2", "E3", "C4"], ["C5", "E5", "G5", None, "E5", None, "A4", None]),
              (["F2", "C3", "A3"], ["A4", "C5", "E5", None, "C5", None, "F5", None]),
              (["G2", "D3", "B3"], ["B4", "D5", "E5", None, "D5", None, "B4", None])]
    for c, (chord, arp) in enumerate(chords):
        pad(tr, chord, c * 8, 8, gain=0.07)
        for b, note in enumerate(arp):
            if note:
                tr.add(tr.at(c * 8 + b), bell(freq(note), 3 * tr.beat, 0.32))
    tr.save("music_home.wav")


def garden():
    tr = Track(96)
    chords = [(["G3", "D4", "B4"], ["G4", "B4", "D5", "B4"]), (["C3", "G3", "E4"], ["C5", "E5", "G5", "E5"]),
              (["D3", "A3", "F#4"], ["D5", "F#5", "A5", "F#5"]), (["G3", "D4", "B4"], ["B4", "D5", "G5", None])]
    for c, (chord, arp) in enumerate(chords):
        pad(tr, chord, c * 8, 8, gain=0.05)
        for bar in range(2):
            for b, note in enumerate(arp):
                if note and not (bar == 1 and b == 3):
                    tr.add(tr.at(c * 8 + bar * 4 + b), pluck(freq(note), 1.2, 0.5, 0.995, seed=c * 10 + b))
    for beat in (6.5, 21.5):
        tr.add(tr.at(beat), chirp(0.12))
    tr.save("music_garden.wav")


def beach():
    tr = Track(80)
    chords = [["F3", "A3", "C4", "F4"], ["Bb2", "F3", "Bb3", "D4"], ["C3", "G3", "C4", "E4"], ["F3", "A3", "C4", "F4"]]
    for c, chord in enumerate(chords):
        for bar in range(2):
            for beat_in_bar, gain in ((0, 0.45), (2.5, 0.3)):  # gentle strum on 1 and the "and" of 3
                start = tr.at(c * 8 + bar * 4 + beat_in_bar)
                for k, note in enumerate(chord):  # strum: strings 25 ms apart
                    tr.add(start + int(0.025 * k * RATE), pluck(freq(note), 1.8, gain, 0.997, seed=c * 7 + k))
    ambience(tr, "waves", 0.25)
    tr.save("music_beach.wav")


def forest():
    tr = Track(66)
    pads = [["D3", "A3", "C4"], ["G2", "D3", "B3"], ["D3", "A3", "F4"], ["C3", "G3", "E4"]]
    for c, chord in enumerate(pads):
        pad(tr, chord, c * 8, 8, gain=0.06)
    melody = [("A4", 0, 3), ("C5", 4, 2), ("D5", 6, 2), ("B4", 10, 4), ("G4", 14, 2),
              ("A4", 16, 3), ("F4", 20, 4), ("E4", 26, 2), ("G4", 28, 3)]
    for note, beat, length in melody:
        tr.add(tr.at(beat), flute(freq(note), length * tr.beat, 0.35, seed=beat))
    ambience(tr, "stream", 0.12, seed=11)
    tr.save("music_forest.wav")


def rooftop():
    tr = Track(64)
    chords = [["D3", "F#3", "A3", "C#4", "E4"], ["B2", "D3", "F#3", "A3"],
              ["E3", "G3", "B3", "D4"], ["A2", "C#3", "E3", "G3", "B3"]]  # Dmaj9 Bm7 Em7 A9
    for c, chord in enumerate(chords):
        for bar in range(2):
            for beat_in_bar, gain in ((0, 0.22), (2.5, 0.12)):
                for note in chord:
                    tr.add(tr.at(c * 8 + bar * 4 + beat_in_bar), epiano(freq(note), 2.5 * tr.beat, gain))
            for beat_in_bar in (0, 2):
                tr.add(tr.at(c * 8 + bar * 4 + beat_in_bar), thump(0.35, 0.35))
    tr.save("music_rooftop.wav")


def night():
    tr = Track(60)
    chords = [["C3", "G3"], ["A2", "E3"], ["F2", "C3"], ["G2", "D3"]]
    for c, chord in enumerate(chords):
        pad(tr, chord, c * 8, 8, gain=0.05)
    # An original, simple lullaby line on a music box.
    melody = ["E5", "G5", "C6", "G5", "E5", None, "D5", None,
              "C5", "E5", "A5", "E5", "C5", None, "B4", None,
              "A4", "C5", "F5", "C5", "A4", None, "C5", None,
              "B4", "D5", "G5", "F5", "D5", None, "B4", None]
    for b, note in enumerate(melody):
        if note:
            tr.add(tr.at(b), music_box(freq(note), 2.0, 0.3))
    tr.save("music_night.wav")


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for make in (home, garden, beach, forest, rooftop, night):
        make()


if __name__ == "__main__":
    main()
