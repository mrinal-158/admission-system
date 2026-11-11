#!/usr/bin/env bash
# Live demo of the seat invariant across two API processes sharing one database.
#   docker compose --profile multi up -d --build
#   ./scripts/concurrency-demo.sh [seats] [applicants]
# Sends all applications at once, alternating between :8080 and :8081.
set -euo pipefail

SEATS=${1:-10}
APPLICANTS=${2:-100}
API_A=${API_A:-http://localhost:8080}
API_B=${API_B:-http://localhost:8081}

class_id=$(curl -fsS -X POST "$API_A/classes" -H 'Content-Type: application/json' \
  -d "{\"name\":\"Concurrency demo $(date +%s%N)\",\"totalSeats\":$SEATS}" |
  sed -E 's/.*"id":"([^"]+)".*/\1/')

echo "Class $class_id with $SEATS seats; $APPLICANTS applicants racing on $API_A and $API_B"

results=$(mktemp)
trap 'rm -f "$results"' EXIT

for i in $(seq 1 "$APPLICANTS"); do
  api=$API_B; [ $((i % 2)) -eq 0 ] && api=$API_A
  curl -s -o /dev/null -w "%{http_code}\n" -X POST "$api/classes/$class_id/holds" \
    -H "X-Student-Id: $(printf '2015%013d' "$i")" >> "$results" &
done
wait

sort "$results" | uniq -c | sed 's/^/  HTTP /'

echo "Seat summary:"
curl -fsS "$API_A/classes/$class_id"; echo
