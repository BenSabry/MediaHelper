using System.Diagnostics;
using System.Reflection;

#region Fields
const string MainProcessName = "MediaOrganizer";
const string ExifProcessName = "exiftool";

const int GenericDelayTime = 1_000;
const int GenericRetryCount = 10;

const string ExifCloseCommand = "-stay_open\nFalse";
const string ExifArgsExecuteTag = "-execute";
#endregion

#region Main
HandleArgs(args);
ChangeTerminalColor();

WaitUntilProcessesExits();
var dirs = RequestExifToCloseAngGetTempDirectories();

WaitingExifToClose();
ForceKillAnyExifRunning();

CleanTempFiles(dirs);
#endregion

#region Helpers
void HandleArgs(string[] args)
{
    if (args is null) return;

    if (args.Any(arg => arg.Equals("ver", StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine(Assembly.GetEntryAssembly()!.GetName()!.Version!.ToString());
        Environment.Exit(0);
    }
}
void ChangeTerminalColor()
{
    Console.ForegroundColor = ConsoleColor.Blue;
}
void WaitUntilProcessesExits()
{
    Console.WriteLine($"Waiting {MainProcessName} to exit.");

    while (Process.GetProcessesByName(MainProcessName).Any(i => !i.HasExited))
        Task.Delay(GenericDelayTime).Wait();
}
string[] RequestExifToCloseAngGetTempDirectories()
{
    Console.WriteLine("Requesting all Exif tools to close.");

    var processes = Process.GetProcessesByName(ExifProcessName);
    var pathes = processes.Select(i =>
        new FileInfo(i.MainModule.FileName)?.Directory?.Parent?.FullName)
        .Where(i => !string.IsNullOrWhiteSpace(i)).Distinct()
        .Select(i => Path.Combine(i, ".temp")).ToArray();

    var commands = new[] { ExifCloseCommand, ExifArgsExecuteTag };
    Parallel.ForEach(pathes, path =>
    {
        var dir = new DirectoryInfo(Path.Combine(path!, ".exif"));
        if (dir.Exists)
            Parallel.ForEach(dir.GetFiles("*.txt"), file =>
            {
                RetryIfFails(() => { File.AppendAllLines(file.FullName, commands); });
            });
    });

    return pathes;
}
void WaitingExifToClose()
{
    Console.WriteLine("Waiting all Exif tools to close.");

    var retries = GenericRetryCount;
    while (retries-- > 0 && Process.GetProcessesByName(ExifProcessName).Length > 0)
        Task.Delay(GenericDelayTime).Wait();
}
void ForceKillAnyExifRunning()
{
    Console.WriteLine("Force killing any running Exif.");

    Parallel.ForEach(
        Process.GetProcessesByName(ExifProcessName),
        p => RetryIfFails(() => p.Kill()));
}
void CleanTempFiles(string[] directories)
{
    Console.WriteLine("Cleaning any temp files left.");

    Parallel.ForEach(directories, dir =>
    {
        if (!Directory.Exists(dir)) return;

        var exif = new DirectoryInfo(Path.Combine(dir, ".exif"));
        var zip = new DirectoryInfo(Path.Combine(dir, ".zip"));
        var logs = new DirectoryInfo(Path.Combine(dir, "logs"));

        RetryIfFails(() => { if (exif.Exists) exif.Delete(true); });
        RetryIfFails(() => { if (zip.Exists) zip.Delete(true); });
        RetryIfFails(() =>
        {
            Parallel.ForEach(logs.EnumerateFiles("*.csv"), file =>
            {
                if (string.IsNullOrEmpty(File.ReadAllText(file.FullName)))
                    file.Delete();
            });
        });
    });
}
void RetryIfFails(Action action, int retryCount = GenericRetryCount, int delayOfRetry = GenericDelayTime)
{
    while (retryCount-- > 0)
        try
        {
            action();
            return;
        }
        catch (Exception)
        {
            Task.Delay(delayOfRetry).Wait();
        }
}
#endregion
