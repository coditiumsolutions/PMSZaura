using System.Linq;
using System.Text.Json;

namespace Salzee.Services
{
    internal static class AgentDebugLog
    {
        private static IEnumerable<string> ResolvePaths()
        {
            var bin = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            yield return Path.Combine(bin, "debug-fd3d45.log");
            const string deployRoot = @"C:\PMSDeploy";
            if (Directory.Exists(deployRoot))
                yield return Path.Combine(deployRoot, "debug-fd3d45.log");
            var up3 = Path.GetFullPath(Path.Combine(bin, "..", "..", ".."));
            yield return Path.Combine(up3, "debug-fd3d45.log");
        }

        internal static void Write(string hypothesisId, string location, string message, object? data = null)
        {
            var line = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["sessionId"] = "fd3d45",
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["runId"] = "post-fix-otpauth-window",
                ["data"] = data
            }) + Environment.NewLine;

            foreach (var path in ResolvePaths().Select(p => Path.GetFullPath(p)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.AppendAllText(path, line);
                }
                catch
                {
                    // ignore per-path failures
                }
            }
        }
    }
}
