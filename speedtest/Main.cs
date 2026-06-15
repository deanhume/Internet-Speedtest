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
    /// Downloads a test file multiple times and calculates the average download speed in Mbps.
    /// </summary>
    public static async Task TestDownloadSpeed(CancellationToken cancellationToken = default)
    {
        // 10MB test file hosted on GitHub
        string fileUrl = "https://github.com/deanhume/Internet-Speedtest/raw/refs/heads/main/misc/content.zip";
        double totalSpeed = 0;
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
                totalSpeed += speedMbps;
                completedTests++;

                Console.WriteLine($"{speedMbps:F2} Mbps");
            }

            double averageSpeed = totalSpeed / testCount;
            Console.WriteLine($"\n  Average Download speed: {averageSpeed:F2} Mbps");
        }
        catch (OperationCanceledException)
        {
            // User pressed Ctrl+C — show partial results if any tests completed
            if (completedTests > 0)
            {
                double averageSpeed = totalSpeed / completedTests;
                Console.WriteLine($"\n  Average Download speed ({completedTests} test(s) completed): {averageSpeed:F2} Mbps");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Connection failed. Looks like you might be offline.");
        }
    }
}
