#!/usr/bin/env bash
# Runs inside unityci/editor:<version>-android-<n>. Called by .github/workflows/build.yml.
#
# Unity removed manual (.ulf) activation for Personal licenses, so the classic GameCI
# UNITY_LICENSE strategy no longer works on a free seat. Instead we ask the Unity Licensing
# Client for a Personal seat with the account's email/password (the approach merged into
# game-ci/cli on 2026-09-05), build, and ALWAYS return the seat afterwards - a leaked seat
# degrades every later run on this account.
#
# Required env: UNITY_EMAIL, UNITY_PASSWORD   Optional: SOLOHERO_DEV_BUILD=1
set -euo pipefail

CLIENT=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client
PROJECT=${PROJECT_PATH:-/project}

if [[ ! -x "$CLIENT" ]]; then
  echo "::error::Unity.Licensing.Client not found at $CLIENT"
  exit 1
fi

return_seat() {
  echo "== returning Personal seat"
  "$CLIENT" --return-ulf || true
}
trap return_seat EXIT

echo "== activating Personal license via Unity.Licensing.Client"
"$CLIENT" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"

echo "== building (BuildAutomator.Build)"
unity-editor \
  -batchmode -nographics -quit \
  -logFile /dev/stdout \
  -projectPath "$PROJECT" \
  -buildTarget Android \
  -executeMethod BuildAutomator.Build
