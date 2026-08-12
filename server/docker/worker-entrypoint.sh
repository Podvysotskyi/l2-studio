#!/bin/sh
set -eu

chown -R "$APP_UID:$APP_UID" /workspace/assets /workspace/import-work
exec setpriv --reuid="$APP_UID" --regid="$APP_UID" --init-groups \
  dotnet L2.Studio.Worker.dll "$@"
