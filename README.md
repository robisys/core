https://ci.appveyor.com/api/projects/status/github/bitwarden/core?branch=master&svg=true 
(https://ci.appveyor.com/project/bitwarden/core/branch/master)

[![Build Status](https://secure.travis-ci.org/robisys/core.svg?branch=master)](https://travis-ci.org/robisys/core)

[forked from](https://github.com/bitwarden/core)

[bitwarden](https://github.com/bitwarden)

# bitwarden Core

The bitwarden Core project contains the APIs, database, and other infrastructure items needed for the "backend" of all other bitwarden projects.

The core infrastructure is written in C# using .NET with ASP.NET Core. The database is SQL Server.

# Build/Run

**Requirements**

- [ASP.NET Core](https://dot.net)
- Recommended: [Visual Studio](https://www.visualstudio.com/)

Open `bitwarden-core.sln`. After restoring the nuget packages, you can build and run the `Api` project.

# Contribute

Code contributions are welcome! Visual Studio or VS Code is required to work on this project. Please commit any pull requests against the `master` branch.

Security audits and feedback are welcome. Please open an issue or email us privately if the report is sensitive in nature. You can read our security policy in the [`SECURITY.md`](SECURITY.md) file.

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
      # dotnet run
