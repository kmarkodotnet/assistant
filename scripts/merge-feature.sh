#!/usr/bin/env bash
# Feature merge a kapuk után + worktree takarítás
set -euo pipefail
NAME="${1:?Hasznalat: merge-feature.sh <feature-nev>}"
git checkout main
git merge --no-ff "feature/${NAME}" -m "feat: merge ${NAME}"
git worktree remove "../wt-${NAME}" --force || true
git branch -d "feature/${NAME}" || true
echo "Merge kesz: ${NAME}"
