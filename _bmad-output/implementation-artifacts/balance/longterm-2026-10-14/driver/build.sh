#!/bin/sh
# Compiles Core + the sim driver with Unity's Mono csc.
MONO="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/bin/mono.exe"
CSC="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe"
cd "$(dirname "$0")"
find /d/git/solohero/Assets/SoloHero/Scripts/Core -name "*.cs" | sed 's#^/d/#D:/#' > files.txt
"$MONO" "$CSC" -nologo -langversion:9 -nowarn:1591,0649,0414,0169 -target:exe -out:sim.exe -r:System.Core.dll @files.txt driver.cs 2>&1 | grep -v "^$" | head -30
