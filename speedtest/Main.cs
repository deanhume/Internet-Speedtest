using System.Diagnostics;
using System.Net;

internal class Program
{
    private static void Main(string[] args)
    {
        TestDownloadSpeed();
    }


    public static void TestDownloadSpeed()
    {
        string fileUrl = "https://deanhume.com/content/files/2025/07/10MB.zip";
        double totalSpeed = 0;
        int testCount = 5;

        for (int i = 1; i <= testCount; i++)
        {
            using (WebClient client = new WebClient())
            {
                Stopwatch sw = new Stopwatch();
                byte[] data;

                Console.WriteLine("Starting download...");

                sw.Start();
                data = client.DownloadData(fileUrl);
                sw.Stop();

                double seconds = sw.Elapsed.TotalSeconds;
                double megabytes = data.Length / (1024.0 * 1024.0);
                double speedMbps = megabytes * 8 / seconds;

                totalSpeed += speedMbps;
            }
        }
                
        double averageSpeed = totalSpeed / testCount;
        Console.WriteLine($"Average Download speed: {averageSpeed:F2} Mbps");

    }
}
