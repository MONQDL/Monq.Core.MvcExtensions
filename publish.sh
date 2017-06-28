#/bin/bash

if [ -z ${3} ]; then
versionSuffix="";
else
versionSuffix="--version-suffix $3";
fi

dotnet pack -c Release $versionSuffix /app/src/Monq.Tools.MvcExtensions/project.json
nuget push bin/Release/*.nupkg -Source $1 -ApiKey $2
