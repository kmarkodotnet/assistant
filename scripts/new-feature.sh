#!/usr/bin/env bash
# Új feature worktree létrehozása párhuzamos agent-munkához
set -euo pipefail
NAME="${1:?Hasznalat: new-feature.sh <feature-nev>}"
BRANCH="feature/${NAME}"
WT="../wt-${NAME}"
git worktree add "$WT" -b "$BRANCH" 2>/dev/null || git worktree add "$WT" "$BRANCH"
echo "Worktree kesz: $WT (ag: $BRANCH)"
