#!/usr/bin/env bash
# Fills a running stack with demo classes, held seats and paid admissions, so the class list
# shows realistic numbers. Holds expire after HOLD_DURATION_SECONDS, so seed just before a demo.
#   docker compose up -d --build
#   ./scripts/seed-demo.sh
set -euo pipefail

API=${API:-http://localhost:8080}

# name | seats | held | confirmed
CLASSES=(
  "Class 6 - Morning Shift|120|7|84"
  "Class 6 - Day Shift|120|4|52"
  "Class 7 - Morning Shift|100|5|95"
  "Class 7 - Day Shift|100|2|31"
  "Class 8 - Morning Shift|90|3|68"
  "Class 9 - Day Shift|60|2|23"
)

apply() { # class_id student_id confirm?
  local hold_id
  hold_id=$(curl -fsS -X POST "$API/classes/$1/holds" -H "X-Student-Id: $2" |
    sed -E 's/.*"holdId":"([^"]+)".*/\1/')
  if [ "$3" = yes ]; then
    curl -fsS -o /dev/null -X POST "$API/holds/$hold_id/confirm" -H "X-Student-Id: $2"
  fi
}

n=0
for row in "${CLASSES[@]}"; do
  IFS='|' read -r name seats held confirmed <<< "$row"
  class_id=$(curl -fsS -X POST "$API/classes" -H 'Content-Type: application/json' \
    -d "{\"name\":\"$name\",\"totalSeats\":$seats}" | sed -E 's/.*"id":"([^"]+)".*/\1/')
  for i in $(seq 1 $((held + confirmed))); do
    n=$((n + 1))
    confirm=no; [ "$i" -le "$confirmed" ] && confirm=yes
    apply "$class_id" "$(printf '2016%013d' "$n")" "$confirm" &
    # A few requests at a time keeps the demo quick without flooding the API.
    [ $((n % 20)) -eq 0 ] && wait
  done
  wait
  echo "$name: $seats seats, $confirmed confirmed, $held held"
done
