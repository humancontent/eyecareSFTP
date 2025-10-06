using System;
using System.IO;
using Renci.SshNet;

class Program
{
    static void Main(string[] args)
    {
        string host = "ecgsftp.blob.core.windows.net";
        int port = 22;
        string username = "ecgsftp.alexvanhuman";
        string password = "2Dw6I/cAHcuIhEjEsXoUc4Afn35zt6yv"; // your actual password
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
