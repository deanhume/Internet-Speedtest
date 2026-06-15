# Copilot Instructions

## Build & Run

```bash
# Build
dotnet build speedtest/speedtest.csproj

# Run
dotnet run --project speedtest/speedtest.csproj

# Publish as single-file executable (win-x64)
dotnet publish speedtest/speedtest.csproj -c Release
```

## Architecture

This is a single-file .NET 9 console application (`speedtest/Main.cs`) that measures internet download speed by fetching a 10MB test file 5 times and averaging the results. It publishes as a trimmed, ReadyToRun, single-file Windows executable intended to be placed in the system PATH.

## Key Conventions

- Target framework is .NET 9 with nullable reference types and implicit usings enabled.
- The app is designed as a self-contained single-file deployment (`PublishSingleFile`, `PublishTrimmed`, `PublishReadyToRun`) targeting `win-x64`.
- No test project exists — there are no unit tests.
- No external NuGet dependencies; only BCL types are used.
