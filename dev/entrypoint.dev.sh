#!/bin/bash -e

dotnet nuget add source \
  --name crossknowledge/phoenix "${ART_URL}" \
  --username "${ART_USER}" \
  --password "${ART_PASS}" \
  --store-password-in-clear-text || true

exec dotnet watch \
  --project /app/WLSUser \
  run \
  --verbosity normal \
  "--urls=${ASPNETCORE_URLS}" \
  --no-launch-profile
