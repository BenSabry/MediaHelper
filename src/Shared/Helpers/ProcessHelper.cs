using System.Diagnostics;
using System.Text;

namespace Shared.Helpers;
public static class ProcessHelper
{
    public static void RunShell(string path, string args) => RunAsync(path, args, true, false).Wait();
    public static async Task RunShellAsync(string path, string args) => await RunAsync(path, args, true, false);

    public static void Run(string path, string args, bool useShell = false) => RunAsync(path, args, useShell, false).Wait();
    public static async Task RunAsync(string path, string args, bool useShell = false) => await RunAsync(path, args, useShell, false);

    public static string RunAndGetOutput(string path, string args) => RunAsync(path, args, false, true).GetAwaiter().GetResult();
    public static async Task<string> RunAndGetOutputAsync(string path, string args) => await RunAsync(path, args, false, true);

    private static async Task<string> RunAsync(string path, string args, bool useShell, bool readOutput)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            UseShellExecute = useShell,
            RedirectStandardOutput = !useShell,
            CreateNoWindow = !useShell
        });

        var output = string.Empty;
        if (p is null)
            return output;

        if (readOutput && !useShell)
            output = await ReadStreamLinesAsync(p.StandardOutput);

        await p.WaitForExitAsync();
        p.Dispose();

        return output;
    }
    private static async Task<string> ReadStreamLinesAsync(StreamReader reader)
    {
        var builder = new StringBuilder();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            builder.AppendLine(line);
        }

        return builder.ToString();
    }
}
