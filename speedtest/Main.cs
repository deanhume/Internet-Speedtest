using System.Diagnostics;

internal class Program
{
    private static readonly HttpClient _httpClient = new();

    private const string Version = "1.0.0";

    private static async Task Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            Console.WriteLine($"speedtest {Version}");
            return;
        }

        // Set up cancellation so Ctrl+C gracefully stops the test
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // Prevent immediate process termination
            cts.Cancel();
            Console.WriteLine("\n  Speed test cancelled.");
        };

        await TestDownloadSpeed(cts.Token);
    }

    /// <summary>
    /// Writes a speed value in colour: green (≥50), yellow (≥20), or red (&lt;20 Mbps).
    /// </summary>
    private static void WriteSpeedColoured(double speedMbps)
    {
        var originalColour = Console.ForegroundColor;
        Console.ForegroundColor = speedMbps switch
        {
            >= 50 => ConsoleColor.Green,
            >= 20 => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };
        Console.WriteLine($"{speedMbps:F2} Mbps");
        Console.ForegroundColor = originalColour;
    }

    /// <summary>
    /// Downloads a test file multiple times and calculates the average download speed in Mbps.
    /// </summary>
    public static async Task TestDownloadSpeed(CancellationToken cancellationToken = default)
    {
        // 10MB test file hosted on GitHub
        string fileUrl = "https://github.com/deanhume/Internet-Speedtest/raw/refs/heads/main/misc/content.zip";
        var speedResults = new List<double>();
        int testCount = 5;
        int completedTests = 0;

        try
        {
            for (int i = 1; i <= testCount; i++)
            {
                // Check for cancellation before starting each test
                cancellationToken.ThrowIfCancellationRequested();

                Console.Write($"  Test {i}/{testCount}... ");

                // Stream the response to measure actual download throughput
                var sw = Stopwatch.StartNew();
                using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Read the response body in chunks to simulate a real download
                long totalBytes = 0;
                var buffer = new byte[81920];
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalBytes += bytesRead;
                }
                sw.Stop();

                // Convert bytes and time into megabits per second
                double seconds = sw.Elapsed.TotalSeconds;
                double megabytes = totalBytes / (1024.0 * 1024.0);
                double speedMbps = megabytes * 8 / seconds;
                speedResults.Add(speedMbps);
                completedTests++;

                WriteSpeedColoured(speedMbps);
            }

            // Discard the fastest and slowest results to reduce outlier impact
            var speeds = speedResults.OrderBy(s => s).ToList();
            var trimmed = speeds.Skip(1).Take(speeds.Count - 2).ToList();
            double averageSpeed = trimmed.Average();
            Console.Write("\n  Average Download speed (excluding outliers): ");
            WriteSpeedColoured(averageSpeed);
        }
        catch (OperationCanceledException)
        {
            // User pressed Ctrl+C — show partial results if any tests completed
            if (completedTests > 0)
            {
                double averageSpeed = speedResults.Average();
                Console.Write($"\n  Average Download speed ({completedTests} test(s) completed): ");
                WriteSpeedColoured(averageSpeed);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Connection failed. Looks like you might be offline.");
        }
    }
}
