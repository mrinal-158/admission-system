#!/bin/sh
# Writes the runtime config read by index.html, so the API URL can change
# through the API_BASE_URL env var without rebuilding the image.
set -eu
escaped=$(printf '%s' "${API_BASE_URL:-http://localhost:8080}" | sed 's/\\/\\\\/g; s/"/\\"/g')
printf 'window.__APP_CONFIG__ = { apiBaseUrl: "%s" };\n' "$escaped" > /usr/share/nginx/html/config.js
echo "app-config: API_BASE_URL=${API_BASE_URL:-http://localhost:8080}"
