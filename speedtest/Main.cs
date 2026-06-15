using System.Diagnostics;

internal class Program
{
    private static readonly HttpClient _httpClient = new();

    private static async Task Main()
    {
        await TestDownloadSpeed();
    }

    public static async Task TestDownloadSpeed()
    {
        string fileUrl = "https://github.com/deanhume/Internet-Speedtest/raw/refs/heads/main/misc/content.zip";
        double totalSpeed = 0;
        int testCount = 5;

        try
        {
            for (int i = 1; i <= testCount; i++)
            {
                Console.Write($"  Test {i}/{testCount}... ");

                var sw = Stopwatch.StartNew();
                using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = 0;
                var buffer = new byte[81920];
                using var stream = await response.Content.ReadAsStreamAsync();

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    totalBytes += bytesRead;
                }
                sw.Stop();

                double seconds = sw.Elapsed.TotalSeconds;
                double megabytes = totalBytes / (1024.0 * 1024.0);
                double speedMbps = megabytes * 8 / seconds;
                totalSpeed += speedMbps;

                Console.WriteLine($"{speedMbps:F2} Mbps");
            }

            double averageSpeed = totalSpeed / testCount;
            Console.WriteLine($"\n  Average Download speed: {averageSpeed:F2} Mbps");
        }
        catch (Exception)
        {
            Console.WriteLine("Connection failed. Looks like you might be offline.");
        }
    }
}
