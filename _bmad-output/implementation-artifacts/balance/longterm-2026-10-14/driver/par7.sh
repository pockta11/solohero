#!/bin/sh
# usage: par.sh <tag> [driver args...]  -> runs the six job paths in parallel (3 seeds each, 7 days) and prints a digest
cd "$(dirname "$0")"
MONO="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
TAG="$1"; shift
mkdir -p out
for p in knight berserker pyro cryo ranger sniper; do
  "$MONO" sim.exe --seeds=3 --path=$p -v "$@" > out/$TAG.$p.txt 2>&1 &
done
wait
python digest7.py out/$TAG.*.txt
