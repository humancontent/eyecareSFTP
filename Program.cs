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

        Directory.CreateDirectory(localDirectory);

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

                    string localPath = Path.Combine(localDirectory, file.Name);
                    using (var fs = File.Create(localPath))
                    {
                        sftp.DownloadFile(file.FullName, fs);
                    }

                    Console.WriteLine($"⬇️ Downloaded: {file.Name}");
                }

                sftp.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        Console.WriteLine("✅ All files downloaded.");
    }
}
