#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo "usage: publish-release-packages.sh <version>" >&2
  exit 2
fi

version="$1"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
: "${GITHUB_SHA:?GITHUB_SHA is required}"
: "${HEXALITH_RELEASE_SOURCE_BRANCH:?HEXALITH_RELEASE_SOURCE_BRANCH is required}"
: "${HEXALITH_RELEASE_SOURCE_CI_WORKFLOW:?HEXALITH_RELEASE_SOURCE_CI_WORKFLOW is required}"
: "${NUGET_API_KEY:?NUGET_API_KEY is required}"

python3 scripts/verify-nuget-publication.py eng/release-packages.json "$version" --expect absent
python3 scripts/verify-release-source.py \
  --repository "$GITHUB_REPOSITORY" \
  --workflow "$HEXALITH_RELEASE_SOURCE_CI_WORKFLOW" \
  --dispatch-ref "refs/heads/${HEXALITH_RELEASE_SOURCE_BRANCH}" \
  --dispatch-sha "$GITHUB_SHA"
dotnet nuget push "./nupkgs/*.nupkg" \
  --source https://api.nuget.org/v3/index.json \
  --api-key "$NUGET_API_KEY"
python3 scripts/verify-nuget-publication.py eng/release-packages.json "$version" --expect present
