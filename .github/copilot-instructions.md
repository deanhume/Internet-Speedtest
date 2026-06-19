# Copilot Instructions

## Build & Run

```bash
# Build
dotnet build speedtest/speedtest.csproj

# Run
dotnet run --project speedtest/speedtest.csproj

# Publish as single-file executable (win-x64)
dotnet publish speedtest/speedtest.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true
```

## Architecture

This is a single-file .NET 8 console application (`speedtest/Main.cs`) that measures internet latency and download speed. The published executable is intended to be placed in `C:\Windows\System32` so it can be invoked from any terminal.

The test flow runs in two phases:
1. **Latency** — 5 HTTP `HEAD` requests to the test server, measuring round-trip time in milliseconds.
2. **Download speed** — 5 streamed downloads of a 10MB test file, measuring throughput in Mbps.

Both phases discard the fastest and slowest results and average the remaining three. Ctrl+C is handled gracefully — partial results are shown if any tests completed before cancellation. The app version is maintained as a `const string` in `Main.cs` and displayed via the `--version` / `-v` flag.

## Key Conventions

- Target framework is .NET 8 (`net8.0`) with nullable reference types and implicit usings enabled.
- No test project exists — there are no unit tests.
- No external NuGet dependencies; only BCL types are used.
- New test types should follow the existing pattern: add a `Measure*` method returning a `Task<double>` and wire it into `RunTest` with an appropriate colour-threshold function. Avoid duplicating the loop/outlier/error-handling scaffolding.
- Console output uses colour-coded results: latency (green ≤30ms, yellow ≤100ms, red >100ms) and speed (green ≥50 Mbps, yellow ≥20 Mbps, red <20 Mbps).
