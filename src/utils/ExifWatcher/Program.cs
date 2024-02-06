using System.Diagnostics;
using System.Reflection;

#region Fields
const string MainProcessName = "MediaHelper";
const string ExifProcessName = "exiftool";

const int GenericDelayTime = 1_000;
const int GenericRetryCount = 10;

const string ExifCloseCommand = "-stay_open\nfalse";
const string ExifArgsExecuteTag = "-execute";
#endregion

#region Main
HandleArgs(args);
ChangeTerminalColor();

WaitUntilProcessesExits();
RequestExifToClose();

WaitingExifToClose();
ForceKillAnyExifRunning();

CleanTempFiles();
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
void RequestExifToClose()
{
    Console.WriteLine("Requesting all Exif tools to close.");

    Parallel.ForEach(Process.GetProcessesByName(ExifProcessName), process =>
    {
        try
        {
            process.StandardInput.WriteLine(ExifCloseCommand);
            process.StandardInput.WriteLine(ExifArgsExecuteTag);
            process.StandardInput.Flush();
        }
        catch (Exception) { }
    });
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
void CleanTempFiles()
{
    Console.WriteLine("Cleaning any temp files left.");

    var dir = new DirectoryInfo(@".temp");
    if (!dir.Exists) return;

    var exif = new DirectoryInfo(Path.Combine(dir.FullName, ".exif"));
    var zip = new DirectoryInfo(Path.Combine(dir.FullName, ".zip"));
    var logs = new DirectoryInfo(Path.Combine(dir.FullName, "logs"));

    RetryIfFails(() => { if (exif.Exists) exif.Delete(true); });
    RetryIfFails(() => { if (zip.Exists) zip.Delete(true); });
    RetryIfFails(() =>
    {
        if (!logs.Exists) return;

        Parallel.ForEach(logs.EnumerateFiles("*.csv"), file =>
        {
            if (string.IsNullOrEmpty(File.ReadAllText(file.FullName)))
                file.Delete();
        });

        if (!logs.EnumerateFiles().Any()) logs.Delete();
    });

    RetryIfFails(() => { if (dir.Exists && !dir.EnumerateDirectories().Any()) dir.Delete(); });
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
