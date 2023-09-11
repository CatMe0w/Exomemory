#!/bin/sh
dotnet tool restore
if [ ! -e "paket.lock" ]
then
    dotnet paket install
else
    dotnet paket restore
fi
dotnet restore
dotnet build --no-restore
dotnet test --no-build
