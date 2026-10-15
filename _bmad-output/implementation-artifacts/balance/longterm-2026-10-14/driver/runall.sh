#!/bin/sh
# usage: runall.sh <outdir> <persona> <seeds> [FIELD=value ...]
# Runs the 6 job paths x seeds in parallel (12 at a time) and writes <outdir>/<persona>_<path>_s<seed>.txt
cd "$(dirname "$0")"
MONO="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
OUT="$1"; PERSONA="$2"; SEEDS="$3"; shift 3
mkdir -p "$OUT"
JOBS=""
for p in knight berserker pyro cryo ranger sniper; do
  s=1
  while [ $s -le "$SEEDS" ]; do
    JOBS="$JOBS $p:$s"
    s=$((s+1))
  done
done
for j in $JOBS; do echo "$j"; done | xargs -P 12 -I{} sh -c 'p=$(echo {} | cut -d: -f1); s=$(echo {} | cut -d: -f2); "$0" lt.exe --persona='"$PERSONA"' --path=$p --seed=$s --days=365 --out='"$OUT"'/'"$PERSONA"'_${p}_s$s.txt '"$*"'' "$MONO"
echo done "$OUT" "$PERSONA"
