---
description: Build, test, and check for pending EF model changes
---

Run the full verification suite:

!`dotnet build JGBackend.sln --configuration Release`

!`dotnet test JGBackend.sln --configuration Release --no-build`

!`ConnectionStrings__JDBConnection="Host=unused;Database=unused;Username=unused;Password=unused" dotnet ef migrations has-pending-model-changes --project src/Portfolio/Portfolio.csproj --startup-project src/Portfolio/Portfolio.csproj`

Summarize the result. If any step failed, identify the first failure and the file
that caused it.
