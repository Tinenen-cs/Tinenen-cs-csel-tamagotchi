"""
Packages finished builds into ready-to-download files in Builds/Release/:

  CSEL-Tamagotchi-Windows.zip   unzip, double-click CSEL-Tamagotchi.exe
  CSEL-Tamagotchi-Android.apk   copy to the phone and tap to install
  CSEL-Tamagotchi-macOS.zip     unzip, right-click CSEL-Tamagotchi.app > Open (first time)

Only builds that exist are packaged (run Tools/build.sh windows|android|mac first).
Optionally publishes them as a GitHub Release (needs the GitHub CLI, logged in as the repo owner):

  python Tools/package_release.py                    # just package
  python Tools/package_release.py --publish v1.0.0   # package + create/update release v1.0.0
"""
import os
import shutil
import stat
import subprocess
import sys
import zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BUILDS = os.path.join(ROOT, "Builds")
OUT = os.path.join(BUILDS, "Release")
NAME = "CSEL-Tamagotchi"


def zip_folder(src_dir, zip_path, top_name):
    """Zips src_dir as <top_name>/..., keeping executable bits (needed for the macOS app)."""
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        for folder, dirs, files in os.walk(src_dir):
            dirs[:] = [d for d in dirs if "DoNotShip" not in d]  # Unity debug symbols, not for players
            for f in files:
                full = os.path.join(folder, f)
                rel = os.path.join(top_name, os.path.relpath(full, src_dir)).replace(os.sep, "/")
                info = zipfile.ZipInfo.from_file(full, rel)
                mode = os.stat(full).st_mode
                if f.endswith((".exe", ".dll")) or "/Contents/MacOS/" in rel:
                    mode |= stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH
                info.external_attr = (mode & 0xFFFF) << 16
                info.compress_type = zipfile.ZIP_DEFLATED
                with open(full, "rb") as fh:
                    z.writestr(info, fh.read())


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []

    win = os.path.join(BUILDS, "Windows")
    if os.path.isfile(os.path.join(win, NAME + ".exe")):
        path = os.path.join(OUT, NAME + "-Windows.zip")
        zip_folder(win, path, NAME + "-Windows")
        made.append(path)

    apk = os.path.join(BUILDS, "Android", NAME + ".apk")
    if os.path.isfile(apk):
        path = os.path.join(OUT, NAME + "-Android.apk")
        shutil.copyfile(apk, path)
        made.append(path)

    app = os.path.join(BUILDS, "macOS", NAME + ".app")
    if os.path.isdir(app):
        path = os.path.join(OUT, NAME + "-macOS.zip")
        zip_folder(app, path, NAME + ".app")
        made.append(path)

    if not made:
        sys.exit("No builds found. Run Tools/build.sh windows|android|mac first.")
    for p in made:
        print(f"{os.path.relpath(p, ROOT)}  ({os.path.getsize(p) / 1048576:.1f} MB)")

    if len(sys.argv) >= 3 and sys.argv[1] == "--publish":
        tag = sys.argv[2]
        notes = ("Download and play:\n"
                 "- **Windows:** download CSEL-Tamagotchi-Windows.zip, unzip, double-click CSEL-Tamagotchi.exe\n"
                 "- **Android:** download CSEL-Tamagotchi-Android.apk on the phone, tap it, allow 'Install unknown apps'\n"
                 "- **macOS:** download CSEL-Tamagotchi-macOS.zip, unzip, right-click the app > Open (first time only)\n")
        exists = subprocess.run(["gh", "release", "view", tag], cwd=ROOT, capture_output=True).returncode == 0
        if exists:
            subprocess.run(["gh", "release", "upload", tag, *made, "--clobber"], cwd=ROOT, check=True)
        else:
            subprocess.run(["gh", "release", "create", tag, *made, "--title", f"CSEL Tamagotchi {tag}",
                            "--notes", notes], cwd=ROOT, check=True)
        print(f"Published release {tag}")


if __name__ == "__main__":
    main()
