#!/bin/sh
# usage: combo.sh <outdir> <persona> "<paths>" <seeds> [FIELD=value ...]  (probe build: prints RATIO / PROBE lines into .log)
cd "$(dirname "$0")"
MONO="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
OUT="$1"; PERSONA="$2"; PATHS="$3"; SEEDS="$4"; shift 4
mkdir -p "$OUT"
for p in $PATHS; do
  s=1
  while [ $s -le "$SEEDS" ]; do
    "$MONO" probe.exe --persona=$PERSONA --path=$p --seed=$s --days=365 --out="$OUT/${PERSONA}_${p}_s$s.txt" "$@" > "$OUT/${PERSONA}_${p}_s$s.log" 2>&1 &
    s=$((s+1))
  done
done
wait
