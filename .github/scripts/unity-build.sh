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
# Required env: UNITY_EMAIL, UNITY_PASSWORD   Optional: SOLOHERO_DEV_BUILD=1, SOLOHERO_VERSION_CODE=<int>
#   SOLOHERO_CI_TESTS=1     run the EditMode tests first (results in Builds/ci/editmode.xml; a failure fails the job)
#   SOLOHERO_CI_REBUILD_UI=1 regenerate the Boot and Game scenes with GameUiBuilder.BuildBatch before the build
#                            (ci/** branches: the workflow commits the regenerated scenes back to the branch)
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

# One editor run in the background (so the trap can stop it); returns its exit code without tripping `set -e`.
run_unity() {
  unity-editor -batchmode -nographics -logFile /dev/stdout -projectPath "$PROJECT" -buildTarget Android "$@" &
  editor_pid=$!
  local rc=0
  wait "$editor_pid" || rc=$?
  editor_pid=""
  return "$rc"
}

if [[ "${SOLOHERO_CI_TESTS:-}" == "1" ]]; then
  echo "== EditMode tests"
  mkdir -p "$PROJECT/Builds/ci"
  results="$PROJECT/Builds/ci/editmode.xml"
  test_rc=0
  run_unity -runTests -testPlatform EditMode -testResults "$results" || test_rc=$?
  if [[ -f "$results" ]]; then
    summary=$(grep -o '<test-run [^>]*' "$results" | head -1 || true)
    total=$(sed -n 's/.* total="\([0-9]*\)".*/\1/p' <<<"$summary")
    passed=$(sed -n 's/.* passed="\([0-9]*\)".*/\1/p' <<<"$summary")
    failed=$(sed -n 's/.* failed="\([0-9]*\)".*/\1/p' <<<"$summary")
    echo "::notice title=EditMode tests::${passed:-?} passed, ${failed:-?} failed of ${total:-?}"
    grep -o '<test-case [^>]*result="Failed"[^>]*' "$results" | sed -n 's/.* fullname="\([^"]*\)".*/\1/p' | head -30 |
      while read -r name; do echo "::error title=EditMode test failed::$name"; done || true
  else
    echo "::error title=EditMode tests::no results file (exit $test_rc) - see the log"
  fi
  if [[ "$test_rc" != "0" ]]; then
    echo "::error::EditMode tests exited with $test_rc"
    exit "$test_rc"
  fi
fi

if [[ "${SOLOHERO_CI_REBUILD_UI:-}" == "1" ]]; then
  echo "== regenerating the UI scenes (GameUiBuilder.BuildBatch)"
  ui_rc=0
  run_unity -quit -executeMethod SoloHero.Editor.GameUiBuilder.BuildBatch || ui_rc=$?
  if [[ "$ui_rc" != "0" ]]; then
    echo "::error::GameUiBuilder.BuildBatch exited with $ui_rc"
    exit "$ui_rc"
  fi
fi

echo "== building (BuildAutomator.Build)"
build_rc=0
run_unity -quit -executeMethod BuildAutomator.Build || build_rc=$?
if [[ "$build_rc" != "0" ]]; then
  echo "::error::unity-editor exited with $build_rc"
  exit "$build_rc"
fi
