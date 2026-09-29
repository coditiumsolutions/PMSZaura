using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace PMS.Services
{
    /// <summary>
    /// Local runs use an SSH tunnel to SQL Server on 127.0.0.1:14330.
    /// If that port is closed, SQL connections fail with "target machine actively refused it".
    /// </summary>
    public static class DevSqlTunnel
    {
        private const int TunnelPort = 14330;

        public static void EnsureStarted(string contentRoot, string? connectionString)
        {
            if (!OperatingSystem.IsWindows() || !TargetsLocalTunnel(connectionString))
            {
                return;
            }

            if (IsTunnelOpen())
            {
                Console.WriteLine($"SQL tunnel already listening on 127.0.0.1:{TunnelPort}.");
                return;
            }

            var scriptPath = Path.Combine(contentRoot, "start-gcp-sql-tunnel.ps1");
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"SQL tunnel script not found: {scriptPath}");
                return;
            }

            Console.WriteLine($"Starting SQL tunnel on 127.0.0.1:{TunnelPort}...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = contentRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            var deadline = DateTime.UtcNow.AddSeconds(25);
            while (DateTime.UtcNow < deadline)
            {
                if (IsTunnelOpen())
                {
                    Console.WriteLine($"SQL tunnel is listening on 127.0.0.1:{TunnelPort}.");
                    return;
                }

                Thread.Sleep(500);
            }

            Console.WriteLine(
                $"SQL tunnel did not open 127.0.0.1:{TunnelPort}. " +
                "Check the SSH key and network, or run start-gcp-sql-tunnel.ps1.");
        }

        private static bool TargetsLocalTunnel(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                var source = new SqlConnectionStringBuilder(connectionString).DataSource;
                return source.Equals($"127.0.0.1,{TunnelPort}", StringComparison.OrdinalIgnoreCase)
                    || source.Equals($"localhost,{TunnelPort}", StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool IsTunnelOpen()
        {
            try
            {
                using var client = new TcpClient();
                var connect = client.BeginConnect("127.0.0.1", TunnelPort, null, null);
                var opened = connect.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                if (!opened || !client.Connected)
                {
                    return false;
                }

                client.EndConnect(connect);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
