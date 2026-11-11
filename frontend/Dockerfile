# syntax=docker/dockerfile:1

# ---- build ----
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---- serve ----
FROM nginx:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Runs on container start (nginx image convention) to write config.js from env vars.
COPY docker/40-app-config.sh /docker-entrypoint.d/40-app-config.sh
# Strip Windows CRLF line endings; otherwise the shebang reads "#!/bin/sh<CR>" and the script fails with "not found".
RUN tr -d '\015' < /docker-entrypoint.d/40-app-config.sh > /tmp/app-config.sh \
 && mv /tmp/app-config.sh /docker-entrypoint.d/40-app-config.sh \
 && chmod +x /docker-entrypoint.d/40-app-config.sh
COPY --from=build /app/dist /usr/share/nginx/html
ENV API_BASE_URL=http://localhost:8080
EXPOSE 80
