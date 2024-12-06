#!/bin/bash

set -o errexit
set -o pipefail

if [[ ! -f .env ]]; then
  cp -v .env.template .env
fi

ART_PASS="$(aws codeartifact get-authorization-token \
  --domain crossknowledge \
  --domain-owner 889859566884 \
  --region us-east-1 \
  --query authorizationToken \
  --output text)" 
sed -i -E -e "s/^ART_PASS=.*\$/ART_PASS=${ART_PASS}/" .env
