#!/usr/bin/env bash
# Runs inside unityci/editor:<version>-android-<n>. Called by .github/workflows/build.yml.
#
# Unity removed manual (.ulf) activation for Personal licenses, so the classic GameCI
# UNITY_LICENSE strategy no longer works on a free seat. Instead we ask the Unity Licensing
# Client for a Personal seat with the account's email/password (the approach merged into
# game-ci/cli on 2026-09-05), build, and ALWAYS return the seat afterwards - a leaked seat
# degrades every later run on this account.
#
# Signal handling: the container runs with --init so SIGTERM from a cancelled job reaches
# this script; unity-editor runs in the background so the trap can kill it and still return
# the seat before exiting.
#
# Required env: UNITY_EMAIL, UNITY_PASSWORD   Optional: SOLOHERO_DEV_BUILD=1
set -euo pipefail

CLIENT=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client
PROJECT=${PROJECT_PATH:-/project}

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "::error::UNITY_EMAIL / UNITY_PASSWORD secrets are missing or empty"
  exit 1
fi
if [[ "${UNITY_PASSWORD}" == -* ]]; then
  echo "::error::UNITY_PASSWORD starts with '-' and would be parsed as an option by the licensing client"
  exit 1
fi
if [[ ! -x "$CLIENT" ]]; then
  echo "::error::Unity.Licensing.Client not found at $CLIENT"
  exit 1
fi

activated=0
editor_pid=""

cleanup() {
  local rc=$?
  if [[ -n "$editor_pid" ]] && kill -0 "$editor_pid" 2>/dev/null; then
    echo "== stopping unity-editor (pid $editor_pid)"
    kill -TERM "$editor_pid" 2>/dev/null || true
    wait "$editor_pid" 2>/dev/null || true
  fi
  if [[ "$activated" == "1" ]]; then
    echo "== returning Personal seat"
    "$CLIENT" --return-ulf || echo "::warning::seat return failed - check the Unity account for a stuck activation"
  fi
  exit "$rc"
}
trap cleanup EXIT
trap 'echo "== signal received"; exit 143' INT TERM

echo "== activating Personal license via Unity.Licensing.Client"
"$CLIENT" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"
activated=1

echo "== building (BuildAutomator.Build)"
unity-editor \
  -batchmode -nographics -quit \
  -logFile /dev/stdout \
  -projectPath "$PROJECT" \
  -buildTarget Android \
  -executeMethod BuildAutomator.Build &
editor_pid=$!
wait "$editor_pid"
build_rc=$?
editor_pid=""
if [[ "$build_rc" != "0" ]]; then
  echo "::error::unity-editor exited with $build_rc"
  exit "$build_rc"
fi
