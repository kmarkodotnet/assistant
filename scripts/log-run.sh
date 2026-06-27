#!/usr/bin/env bash
# Egy agent-feladat kimenetelének naplózása a retrospektívához.
# Hasznalat: log-run.sh <run-id> <agent> <modell> <feladat-rovid> <kimenetel> <korok> [megjegyzes]
# kimenetel: success | fail | escalated
set -euo pipefail
mkdir -p factory-metrics/runs
RUN="${1:?run-id}"; AGENT="${2:?agent}"; MODEL="${3:?modell}"
TASK="${4:?feladat}"; OUTCOME="${5:?kimenetel}"; ROUNDS="${6:?korok}"; NOTE="${7:-}"
printf '{"ts":"%s","run":"%s","agent":"%s","model":"%s","task":"%s","outcome":"%s","rounds":%s,"note":"%s"}\n' \
  "$(date -u +%FT%TZ)" "$RUN" "$AGENT" "$MODEL" "$TASK" "$OUTCOME" "$ROUNDS" "$NOTE" \
  >> "factory-metrics/runs/${RUN}.jsonl"
