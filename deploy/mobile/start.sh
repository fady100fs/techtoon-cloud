#!/bin/bash
# Runs Nginx and the Techtoon API in the same container.
# If either process exits the container exits, so Fly restarts the machine.
set -euo pipefail

dotnet /app/Techtoon.Api.dll &
api_pid=$!

nginx -g 'daemon off;' &
nginx_pid=$!

terminate() {
    kill -TERM "$api_pid" "$nginx_pid" 2>/dev/null || true
}
trap terminate TERM INT

# Wait for whichever process dies first, then stop the other one.
wait -n "$api_pid" "$nginx_pid" || true
terminate
wait || true
