"""
Prepares UI sprites for the game (requires Python 3 + Pillow: pip install pillow).

1. Cleans the button sprites in Assets/Art/UI/Buttons: some were cut from a sheet and
   contain thin slivers of neighbouring sprites along their edges, which are trimmed off.
   Sprites that are clipped in the pack (the star) are re-cut from the pack's source sheet.
2. Cuts the round icons (smiley, heart, fork, droplet) out of Assets/Art/UI/Status/*.png
   into Assets/Art/UI/Icons/, plus a tight sad-hamster icon for the SCOLD button.
3. Draws small pixel-art 9-slice frames (bar frame, bar fill, button tile, panel), and a
   stretchable speech bubble made from the pack's chat_bubble.png, in the
   asset pack's palette into Assets/Art/UI/Generated/.

Safe to run repeatedly. Usage:  python Tools/make_ui_sprites.py
"""
import math
import os
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "UI")

# Palette sampled from the asset pack.
OUTLINE = (90, 48, 29, 255)
OUTLINE_SOFT = (145, 87, 67, 255)
CREAM = (252, 242, 219, 255)
TRACK = (240, 214, 186, 255)
SHADOW = (214, 178, 150, 255)
WHITE = (255, 255, 255, 255)


def runs(flags):
    """List of (start, end) runs of True values."""
    out, start = [], None
    for i, f in enumerate(flags + [False]):
        if f and start is None:
            start = i
        elif not f and start is not None:
            out.append((start, i))
            start = None
    return out


def trim_edge_slivers(flags, max_width=4):
    """Bounds after dropping thin runs (<= max_width) that sit at either edge."""
    r = runs(flags)
    while len(r) > 1 and r[0][1] - r[0][0] <= max_width:
        r.pop(0)
    while len(r) > 1 and r[-1][1] - r[-1][0] <= max_width:
        r.pop()
    return r[0][0], r[-1][1]


def clean_button(path):
    im = Image.open(path).convert("RGBA")
    a = im.split()[3]
    w, h = im.size
    cols = [any(a.getpixel((x, y)) > 0 for y in range(h)) for x in range(w)]
    x0, x1 = trim_edge_slivers(cols)
    rows = [any(a.getpixel((x, y)) > 0 for x in range(x0, x1)) for y in range(h)]
    y0, y1 = trim_edge_slivers(rows)
    bbox = im.crop((x0, y0, x1, y1)).getbbox()
    crop = im.crop((x0, y0, x1, y1)).crop(bbox)
    if crop.size != (w, h):
        crop.save(path)
        print(f"cleaned {os.path.basename(path)}: {w}x{h} -> {crop.size[0]}x{crop.size[1]}")


# Sprites that are clipped in the pack's individual files, re-cut from the pack's full source
# sheet (path relative to this project): name -> crop box (left, top, right, bottom).
SHEET = os.path.join(os.path.dirname(__file__), "..", "..",
                     "tamagotchi_unity_asset_pack(1)", "Source", "complete_generated_asset_sheet.png")
SHEET_CUTS = {
    "star.png": (712, 895, 763, 965),   # star.png in the pack is missing its left arm
    "music.png": (668, 895, 713, 965),  # music.png in the pack has its left note head cut off
    "sleep_z.png": (762, 905, 811, 955),  # sleep_z.png in the pack is cut on the left and top
}


def cut_from_sheet(box, dst):
    """Crops a sprite from the source sheet and removes the cream background by flood fill."""
    from collections import deque
    im = Image.open(SHEET).convert("RGBA").crop(box)
    w, h = im.size
    px = im.load()
    bg = px[0, 0]
    near = lambda c: sum(abs(c[i] - bg[i]) for i in range(3)) < 40
    queue = deque([(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)])
    seen = set()
    while queue:
        x, y = queue.popleft()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h):
            continue
        seen.add((x, y))
        if not near(px[x, y]):
            continue
        px[x, y] = (0, 0, 0, 0)
        queue.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    im.crop(im.getbbox()).save(dst)
    print(f"re-cut {os.path.basename(dst)} from source sheet")


def cut_status_icon(src, dst):
    """The icon is a circle at the left end of each status bar; keep only that circle."""
    im = Image.open(src).convert("RGBA")
    a = im.split()[3]
    w, h = im.size
    counts = [sum(1 for y in range(h) if a.getpixel((x, y)) > 0) for x in range(w)]
    left = next(x for x, c in enumerate(counts) if c > 0)
    tallest = max(counts[: left + 60])
    radius = tallest / 2.0
    cx = left + radius
    # Vertical centre = middle of the tallest column.
    col = int(cx)
    ys = [y for y in range(h) if a.getpixel((col, y)) > 0]
    cy = (ys[0] + ys[-1] + 1) / 2.0
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= radius + 0.25:
                out.putpixel((x, y), im.getpixel((x, y)))
    bbox = out.getbbox()
    out.crop(bbox).save(dst)
    print(f"icon {os.path.basename(dst)} {bbox}")


def rounded_box(size, fill, outline=None, radius=2, shadow=None):
    w, h = size
    im = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if shadow:  # 2px darker strip along the bottom (pressable look)
        d.rounded_rectangle((0, 0, w - 1, h - 1), radius, fill=shadow, outline=outline)
        d.rounded_rectangle((0, 0, w - 1, h - 3), radius, fill=fill, outline=outline)
    else:
        d.rounded_rectangle((0, 0, w - 1, h - 1), radius, fill=fill, outline=outline)
    return im


def main():
    btn_dir = os.path.join(ROOT, "Buttons")
    if os.path.isfile(SHEET):
        for name, box in SHEET_CUTS.items():
            cut_from_sheet(box, os.path.join(btn_dir, name))
    for f in sorted(os.listdir(btn_dir)):
        if f.endswith(".png"):
            clean_button(os.path.join(btn_dir, f))

    icon_dir = os.path.join(ROOT, "Icons")
    os.makedirs(icon_dir, exist_ok=True)
    for name in ["hunger", "water", "health", "happiness"]:
        cut_status_icon(os.path.join(ROOT, "Status", f"{name}.png"),
                        os.path.join(icon_dir, f"icon_{name}.png"))

    # SCOLD button icon: the sad hamster frame cropped tight (the frame has wide empty margins).
    sad = Image.open(os.path.join(ROOT, "..", "Pet", "Hamster", "sad_01.png")).convert("RGBA")
    sad.crop(sad.getbbox()).save(os.path.join(icon_dir, "icon_sad.png"))
    print("icon icon_sad.png")

    gen = os.path.join(ROOT, "Generated")
    os.makedirs(gen, exist_ok=True)
    rounded_box((16, 10), TRACK, OUTLINE, radius=3).save(os.path.join(gen, "bar_frame.png"))
    rounded_box((8, 6), WHITE, None, radius=2).save(os.path.join(gen, "bar_fill.png"))
    rounded_box((20, 20), CREAM, OUTLINE, radius=3, shadow=SHADOW).save(os.path.join(gen, "button_tile.png"))
    rounded_box((20, 20), CREAM, OUTLINE_SOFT, radius=4).save(os.path.join(gen, "panel.png"))
    # App icon: the hamster face on a cream tile, 512x512, pixels kept crisp.
    face = Image.open(os.path.join(ROOT, "Buttons", "hamster_face.png")).convert("RGBA")
    icon = rounded_box((64, 64), CREAM, OUTLINE, radius=10).resize((512, 512), Image.NEAREST)
    face = face.crop(face.getbbox())
    k = 340 // max(face.size)
    face = face.resize((face.width * k, face.height * k), Image.NEAREST)
    icon.alpha_composite(face, ((512 - face.width) // 2, (512 - face.height) // 2 + 8))
    icon_dir2 = os.path.join(ROOT, "..", "Icon")
    os.makedirs(icon_dir2, exist_ok=True)
    icon.save(os.path.join(icon_dir2, "app_icon.png"))
    print("app_icon.png")

    # Speech bubble from the pack's own chat_bubble.png: paint out the hamster face and heart so
    # the middle can stretch for any text (the face and heart are added back as separate images).
    pack = Image.open(os.path.join(ROOT, "Dialogs", "chat_bubble.png")).convert("RGBA")
    pack = pack.crop(pack.getbbox())
    px = pack.load()
    fill = px[pack.width // 2, 8]          # cream interior just below the top edge
    for (x0, y0, x1, y1) in [(12, 9, 66, 60), (100, 14, 140, 52)]:
        for y in range(y0, y1):
            for x in range(x0, x1):
                if px[x, y][3] > 0:
                    px[x, y] = fill
    pack.save(os.path.join(gen, "bubble_pack.png"))
    print("bubble_pack.png", pack.size)

    print("generated frames in", gen)


if __name__ == "__main__":
    main()
