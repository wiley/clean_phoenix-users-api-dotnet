#!/bin/bash -e

if [[ "${BUILD:-0}" = "1" ]]; then
  dotnet nuget remove source crossknowledge/phoenix &>/dev/null || true
  dotnet nuget add source \
    --name crossknowledge/phoenix "${ART_URL}" \
    --username "${ART_USER}" \
    --password "${ART_PASS}" \
    --store-password-in-clear-text
  
  dotnet build \
    --configuration Development \
    /app/WLSUser
fi

set -x
exec dotnet run \
  --no-build \
  --project /app/WLSUser \
  --no-launch-profile \
  --configuration Development \
  --verbosity detailed \
  -- \
  "--urls=${ASPNETCORE_URLS}" \
  --no-launch-profile \
  ./release/WLSUser.dll
