#!/usr/bin/env bash
# Publishes Builds/WebGL to the gh-pages branch, which GitHub Pages serves at
#   https://<owner>.github.io/<repo>/
# Run Tools/build.sh webgl first. Uses your normal git login; adds one commit to gh-pages
# per publish (no force-push) and never touches your working folder.
set -euo pipefail

cd "$(dirname "$0")/.."
ROOT=$(pwd)
[ -f Builds/WebGL/index.html ] || { echo "No WebGL build. Run: Tools/build.sh webgl"; exit 1; }

TMP=$(mktemp -d)
trap 'cd "$ROOT"; git worktree remove --force "$TMP" >/dev/null 2>&1 || true; rm -rf "$TMP"' EXIT

git fetch -q origin gh-pages 2>/dev/null || true
if git rev-parse -q --verify origin/gh-pages >/dev/null; then
  git worktree add -q --detach "$TMP" origin/gh-pages
else
  git worktree add -q --detach "$TMP"
  (cd "$TMP" && git checkout -q --orphan gh-pages-new && git rm -rfq . )
fi

cd "$TMP"
find . -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
cp -r "$ROOT"/Builds/WebGL/. .
touch .nojekyll   # serve files exactly as built
git add -A
if git diff --cached --quiet; then
  echo "==> gh-pages already up to date"
else
  git commit -q -m "Publish WebGL build"
  git push -q origin HEAD:gh-pages
  echo "==> Published to gh-pages"
fi
