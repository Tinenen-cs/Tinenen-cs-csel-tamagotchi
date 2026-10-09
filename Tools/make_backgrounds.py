"""
Makes the 6 background scenes portrait 1080x1920 (9:16, the game's screen size).
Requires Python 3 + Pillow.

The pack's scenes are wide (about 490x390 or 495x297), so each one is first cropped to a 9:16
slice (trimming the rounded corners) and then scaled up with nearest-neighbour so the pixel art
stays crisp. Stretching the whole wide image to 1080x1920 would squash it, so we crop instead.

Source: ../tamagotchi_unity_asset_pack(1)/Scenes/*/*.png  (the original pack, left untouched)
Output: Assets/Art/Backgrounds/<name>.png                 (same names, so references stay valid)
Usage:  python Tools/make_backgrounds.py
"""
import glob
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(HERE, "..", "..", "tamagotchi_unity_asset_pack(1)", "Scenes")
OUTPUT = os.path.join(HERE, "..", "Assets", "Art", "Backgrounds")
SIZE = (1080, 1920)
INSET = 6  # px trimmed from every edge (rounded corners of the pack's scene cards)

# Which part of each wide scene to keep: 0 = left edge, 0.5 = centre, 1 = right edge.
FOCUS = {
    "cozy_home": 0.5,
    "sunny_garden": 0.5,
    "beach": 0.5,
    "forest_stream": 0.5,
    "sunset_rooftop": 0.5,
    "moonlit_bedroom": 1.0,  # right side: window, poster, lamp; keeps its painted bed off screen
}


def portrait(path, focus):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    left, top, right, bottom = INSET, INSET, w - INSET, h - INSET
    crop_h = bottom - top
    crop_w = round(crop_h * SIZE[0] / SIZE[1])
    x0 = left + round((right - left - crop_w) * focus)
    return im.crop((x0, top, x0 + crop_w, bottom)).resize(SIZE, Image.NEAREST)


def main():
    os.makedirs(OUTPUT, exist_ok=True)
    for path in sorted(glob.glob(os.path.join(SOURCE, "*", "*.png"))):
        name = os.path.splitext(os.path.basename(path))[0]
        if name not in FOCUS:
            continue
        portrait(path, FOCUS[name]).save(os.path.join(OUTPUT, name + ".png"), optimize=True)
        print(f"{name}.png -> {SIZE[0]}x{SIZE[1]}")


if __name__ == "__main__":
    main()
