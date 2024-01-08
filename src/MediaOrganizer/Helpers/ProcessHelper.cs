using System.Diagnostics;
using System.Text;

namespace MediaOrganizer.Helpers;
public static class ProcessHelper
{
    public static void RunUsingShell(string name, string path, string args)
    {
        RunProcess(name, path, args, true);
    }
    public static async Task RunUsingShellAsync(string name, string path, string args)
    {
        await Task.Run(() =>
        {
            RunUsingShell(name, path, args);
        });
    }

    public static string? RunAndGetOutput(string name, string path, string args)
    {
        return RunProcess(name, path, args, false);
    }
    public static async Task<string> RunAndGetOutputAsync(string name, string path, string args)
    {
        var builder = new StringBuilder();
        await Task.Run(() =>
        {
            builder.Append(RunAndGetOutput(name, path, args));
        });

        return builder.ToString();
    }

    private static string? RunProcess(string name, string path, string args, bool useShell)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(path, name),
            Arguments = args,
            UseShellExecute = useShell,
            RedirectStandardOutput = !useShell
        });

        if (p is null) return string.Empty;

        var output = useShell ? string.Empty
            : ReadStreamReader(p.StandardOutput);

        p.WaitForExit();
        p.Dispose();

        return output;
    }
    private static string ReadStreamReader(StreamReader reader)
    {
        var builder = new StringBuilder();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            builder.AppendLine(line);
        }

        return builder.ToString();
    }
}
