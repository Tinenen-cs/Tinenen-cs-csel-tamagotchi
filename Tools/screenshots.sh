#!/usr/bin/env bash
# Regenerates the README screenshots (run after every change, before pushing):
#   Docs/screenshot.png       main phone screen
#   Docs/states_overview.png  happy (100), sad (<=50), exhausted (Play/Study locked), sleeping
# Unity must be closed. Needs Python 3 + Pillow for the overview strip.
# Override the editor path with:  UNITY="/path/to/Unity" Tools/screenshots.sh
set -euo pipefail

cd "$(dirname "$0")/.."
VERSION=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
if [ -z "${UNITY:-}" ]; then
  case "$(uname -s)" in
    Darwin) UNITY="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" ;;
    Linux)  UNITY="$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity" ;;
    *)      UNITY="/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" ;;
  esac
fi

mkdir -p Logs
for method in Capture CaptureStates; do
  echo "==> ScreenshotCapture.$method"
  "$UNITY" -batchmode -quit -projectPath . -executeMethod "ScreenshotCapture.$method" \
    -logFile "Logs/screenshots-$method.log" || { tail -20 "Logs/screenshots-$method.log"; exit 1; }
done

python - <<'EOF'
from PIL import Image
frames = ["Docs/state_happy.png", "Docs/state_sad.png", "Docs/state_exhausted.png", "Docs/state_sleeping.png"]
shots = [Image.open(f).convert("RGB").resize((432, 768)) for f in frames]
strip = Image.new("RGB", (432 * 4 + 30, 768), (30, 30, 30))
for i, shot in enumerate(shots):
    strip.paste(shot, (i * 442, 0))
strip.save("Docs/states_overview.png")
EOF
echo "==> Updated Docs/screenshot.png and Docs/states_overview.png"
