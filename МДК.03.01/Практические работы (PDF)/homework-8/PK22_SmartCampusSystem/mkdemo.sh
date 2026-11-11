#!/bin/bash
# usage: mkdemo.sh <dir> <mode: ok|badhash>
D=$1; rm -rf $D; mkdir -p $D/server
cp -r SmartCampusSystem/CampusClient/bin/Release/net8.0/. $D/
cp -r SmartCampusSystem/UpdaterHelper/bin/Release/net8.0/UpdaterHelper.* $D/
echo "CampusClient.exe build 2.0.0" > $D/CampusClient.exe
echo "2.0.0" > $D/version.txt
echo "CampusClient.exe build 2.1.0" > $D/server/CampusClient.exe
echo "2.1.0" > $D/server/version.txt
h1=$(sha256sum $D/server/CampusClient.exe | cut -d' ' -f1); h2=$(sha256sum $D/server/version.txt | cut -d' ' -f1)
[ "$2" = badhash ] && h1=0000000000000000000000000000000000000000000000000000000000000000
cat > $D/server/manifest.json <<J
{ "version": "2.1.0", "files": [ { "name": "CampusClient.exe", "sha256": "$h1" }, { "name": "version.txt", "sha256": "$h2" } ] }
J
