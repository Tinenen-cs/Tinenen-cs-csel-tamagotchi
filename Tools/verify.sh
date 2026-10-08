#!/usr/bin/env bash
# Regenerates the Main scene and runs the PlayMode tests in batch mode.
# Fails (non-zero exit) on compile errors, setup errors, or any test failure
# (including unexpected Debug.LogError / exceptions while the scene runs).
#
# Usage:  Tools/verify.sh
# Override the editor path with:  UNITY="/path/to/Unity" Tools/verify.sh
set -euo pipefail

cd "$(dirname "$0")/.."
VERSION=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt 2>/dev/null || echo "6000.3.24f1")

if [ -z "${UNITY:-}" ]; then
  case "$(uname -s)" in
    Darwin) UNITY="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" ;;
    Linux)  UNITY="$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity" ;;
    *)      UNITY="/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" ;;
  esac
fi

mkdir -p Logs
echo "==> Setup (compile + regenerate scene) with $UNITY"
"$UNITY" -batchmode -nographics -quit -projectPath . \
  -executeMethod ProjectSetup.SetupAll -logFile Logs/verify-setup.log || {
  grep -E "error CS|Exception|Error" Logs/verify-setup.log | head -40; exit 1; }

echo "==> PlayMode tests"
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode \
  -testResults Logs/playmode-results.xml -logFile Logs/verify-tests.log || {
  grep -E "error CS|Exception|failed|Failed" Logs/verify-tests.log | head -40; exit 1; }

grep -o 'result="[A-Za-z()]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/playmode-results.xml | head -1
echo "==> All good."
