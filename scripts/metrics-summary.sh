#!/usr/bin/env bash
# Tömör összesítő a factory-engineer számára (token-kímélő bemenet).
set -euo pipefail
DIR=factory-metrics/runs
[ -d "$DIR" ] || { echo "Nincs telemetria."; exit 0; }
echo "== Futasok =="; ls "$DIR" | sed 's/.jsonl//'
echo "== Agentenkenti kimenetelek =="
cat "$DIR"/*.jsonl | python3 -c '
import sys, json, collections
c = collections.Counter(); rounds = collections.defaultdict(list)
for line in sys.stdin:
    r = json.loads(line)
    c[(r["agent"], r["model"], r["outcome"])] += 1
    rounds[r["agent"]].append(r["rounds"])
for (a, m, o), n in sorted(c.items()):
    print(f"{a:18s} {m:8s} {o:10s} {n}")
print("== Atlagos korok ==")
for a, rs in sorted(rounds.items()):
    print(f"{a:18s} {sum(rs)/len(rs):.1f}")
'
