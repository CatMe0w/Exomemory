dotnet tool restore
IF NOT EXIST paket.lock (
    dotnet paket install
) ELSE (
    dotnet paket restore
)
dotnet restore
dotnet build --no-restore
dotnet test --no-build
