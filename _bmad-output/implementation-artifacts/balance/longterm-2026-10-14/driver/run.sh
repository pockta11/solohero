#!/bin/sh
MONO="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
cd "$(dirname "$0")"
"$MONO" sim.exe "$@"
