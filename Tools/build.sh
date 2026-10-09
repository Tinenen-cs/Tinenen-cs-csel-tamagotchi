#!/usr/bin/env bash
# Builds the game from the command line (Unity must be closed).
#   Tools/build.sh webgl     -> Builds/WebGL/            (needs "WebGL Build Support" module)
#   Tools/build.sh android   -> Builds/Android/CSEL-Tamagotchi.apk (needs "Android Build Support")
#   Tools/build.sh windows   -> Builds/Windows/CSEL-Tamagotchi.exe
#   Tools/build.sh mac       -> Builds/macOS/CSEL-Tamagotchi.app    (on a Mac, or with "Mac Build Support")
# Override the editor path with:  UNITY="/path/to/Unity" Tools/build.sh webgl
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

case "${1:-}" in
  webgl)   TARGET=WebGL;               METHOD=BuildScript.BuildWebGL ;;
  android) TARGET=Android;             METHOD=BuildScript.BuildAndroid ;;
  windows) TARGET=StandaloneWindows64; METHOD=BuildScript.BuildWindows ;;
  mac)     TARGET=StandaloneOSX;       METHOD=BuildScript.BuildMac ;;
  *) echo "usage: Tools/build.sh webgl|android|windows|mac"; exit 2 ;;
esac

mkdir -p Logs
LOG="Logs/build-$1.log"
echo "==> Building $TARGET (log: $LOG)"
"$UNITY" -batchmode -nographics -quit -projectPath . -buildTarget "$TARGET" \
  -executeMethod "$METHOD" -logFile "$LOG" || { grep -E "error|Error|\[BuildScript\]" "$LOG" | tail -30; exit 1; }
grep "\[BuildScript\]" "$LOG" | grep -v "^UnityEngine" | head -1
