#!/usr/bin/env bash
# dotnet restore && dotnet build
# dotnet new --info
# dotnet  --info
# dotnet  new --help
# dotnet  test --help
# cd src/Core
# cd src/Mail
cd src/Api
dotnet restore
dotnet build
#dotnet build -f 461
 dotnet run

