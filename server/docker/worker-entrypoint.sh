#!/bin/sh
set -eu

chown "$APP_UID:$APP_UID" /workspace/assets
exec setpriv --reuid="$APP_UID" --regid="$APP_UID" --init-groups \
  dotnet L2.Studio.Worker.dll "$@"
