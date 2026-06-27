#!/usr/bin/env bash
# Tömör üzemeltetési pillanatkép az ops-watcher számára (token-kímélő).
# Hasznalat: collect-ops.sh <termek> [namespace]
set -euo pipefail
P="${1:?termek}"; NS="${2:-$P}"
echo "== Idopont: $(date -u +%FT%TZ) | Termek: $P =="
if kubectl get ns "$NS" >/dev/null 2>&1; then
  echo "== Podok =="; kubectl -n "$NS" get pods --no-headers
  echo "== Restartok/eventek (utolso 30) =="
  kubectl -n "$NS" get events --sort-by=.lastTimestamp --no-headers | tail -30
  echo "== Hibas logsorok (utolso 1h, max 80 sor) =="
  for pod in $(kubectl -n "$NS" get pods -o name); do
    kubectl -n "$NS" logs "$pod" --since=1h --all-containers 2>/dev/null \
      | grep -iE 'error|exception|fatal|panic' | tail -20 || true
  done | tail -80
else
  echo "== docker compose mod =="
  docker compose ps 2>/dev/null || true
  echo "== Hibas logsorok (utolso 1h, max 80 sor) =="
  docker compose logs --since 1h 2>/dev/null \
    | grep -iE 'error|exception|fatal|panic' | tail -80 || true
fi
echo "== Nyitott incidensek =="
ls docs/incidents/ 2>/dev/null | grep -v resolved || echo "nincs"
