using System;
using System.IO;
using Renci.SshNet;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // Important
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();
        string host = config["FtpSettings:Host"];
        int port = 22;
        string username = config["FtpSettings:Username"];
        string password = config["FtpSettings:Password"];
        string remoteDirectory = "."; // start in root of container
        string localDirectory = @"C:\Temp\SftpFiles\";
        string lastCheckFile = Path.Combine(localDirectory, "last-check.txt");

        Directory.CreateDirectory(localDirectory);

        DateTime lastCheckDate = DateTime.MinValue;
        if (File.Exists(lastCheckFile))
        {
            string content = File.ReadAllText(lastCheckFile).Trim();
            if (DateTime.TryParse(content, out var parsedDate))
            {
                lastCheckDate = parsedDate;
            }
        }

        using (var sftp = new SftpClient(host, port, username, password))
        {
            try
            {
                sftp.Connect();
                Console.WriteLine("✅ Connected to Azure SFTP");

                var files = sftp.ListDirectory(remoteDirectory);
                foreach (var file in files)
                {
                    if (file.IsDirectory || file.Name.StartsWith(".")) continue;
                    if (!(file.Name.StartsWith("EOD_Payments", StringComparison.OrdinalIgnoreCase) ||
                         file.Name.StartsWith("EOD_Sales", StringComparison.OrdinalIgnoreCase) ||
                         file.Name.StartsWith("EOD_TillShift", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // --- Step 2: Skip files older than last check ---
                    if (file.LastWriteTime <= lastCheckDate)
                        continue;

                    string localPath = Path.Combine(localDirectory, file.Name);
                    using (var fs = File.Create(localPath))
                    {
                        sftp.DownloadFile(file.FullName, fs);
                    }

                    Console.WriteLine($"⬇️ Downloaded: {file.Name}");
                }

                sftp.Disconnect();

                File.WriteAllText(lastCheckFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        Console.WriteLine("✅ All files downloaded.");
    }
}
