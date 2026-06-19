using System.Diagnostics;

internal class Program
{
    private static readonly HttpClient _httpClient = new();

    private const string Version = "1.0.0";
    private const string TestFileUrl = "https://github.com/deanhume/Internet-Speedtest/raw/refs/heads/main/misc/content.zip";
    private const int TestCount = 5;

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

        await RunTest("Latency", "Latency", MeasureLatency, GetLatencyColour, cts.Token);
        Console.WriteLine();
        await RunTest("Download Speed", "Download speed", MeasureDownloadSpeed, GetSpeedColour, cts.Token);
    }

    /// <summary>
    /// Runs a measurement multiple times, trims outliers, and displays the average.
    /// </summary>
    private static async Task RunTest(
        string header,
        string label,
        Func<CancellationToken, Task<double>> measure,
        Func<double, ConsoleColor> getColour,
        CancellationToken cancellationToken)
    {
        var results = new List<double>();
        int completedTests = 0;

        try
        {
            Console.WriteLine($"  {header}");

            for (int i = 1; i <= TestCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.Write($"  Test {i}/{TestCount}... ");

                double value = await measure(cancellationToken);
                results.Add(value);
                completedTests++;

                WriteColoured(value, getColour);
            }

            // Discard the fastest and slowest results to reduce outlier impact
            var trimmed = results.OrderBy(r => r).Skip(1).Take(results.Count - 2).ToList();
            Console.Write($"\n  Average {label}: ");
            WriteColoured(trimmed.Average(), getColour);
        }
        catch (OperationCanceledException)
        {
            if (completedTests > 0)
            {
                Console.Write($"\n  Average {label} ({completedTests} test(s) completed): ");
                WriteColoured(results.Average(), getColour);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Connection failed. Looks like you might be offline.");
        }
    }

    /// <summary>
    /// Writes a value in the colour determined by the provided threshold function.
    /// </summary>
    private static void WriteColoured(double value, Func<double, ConsoleColor> getColour)
    {
        var originalColour = Console.ForegroundColor;
        Console.ForegroundColor = getColour(value);
        Console.WriteLine($"{value:F2}");
        Console.ForegroundColor = originalColour;
    }

    private static ConsoleColor GetLatencyColour(double ms) => ms switch
    {
        <= 30 => ConsoleColor.Green,
        <= 100 => ConsoleColor.Yellow,
        _ => ConsoleColor.Red
    };

    private static ConsoleColor GetSpeedColour(double mbps) => mbps switch
    {
        >= 50 => ConsoleColor.Green,
        >= 20 => ConsoleColor.Yellow,
        _ => ConsoleColor.Red
    };

    /// <summary>
    /// Measures round-trip latency via an HTTP HEAD request. Returns milliseconds.
    /// </summary>
    private static async Task<double> MeasureLatency(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, TestFileUrl);
        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        sw.Stop();
        response.EnsureSuccessStatusCode();
        return sw.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Downloads the test file and measures throughput. Returns Mbps.
    /// </summary>
    private static async Task<double> MeasureDownloadSpeed(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var response = await _httpClient.GetAsync(TestFileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        long totalBytes = 0;
        var buffer = new byte[81920];
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
        }
        sw.Stop();

        double seconds = sw.Elapsed.TotalSeconds;
        double megabytes = totalBytes / (1024.0 * 1024.0);
        return megabytes * 8 / seconds;
    }
}
