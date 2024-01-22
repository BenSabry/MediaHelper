using Domain.Interfaces;
using Shared.Helpers;
using System.Diagnostics;
using System.Text;

namespace Infrastructure.Wrappers;
public sealed class ExifToolWrapper : IDisposable
{
    #region Fields-Static
    private const string ExifTool = "exiftool.exe";
    private const string ExifReadyStatement = "{ready}";
    private const string ExifArgsExecuteTag = "-execute";

    //private const string Separator = "\t";
    private static bool Initialized;

    public static string Version { get; private set; } = "0.0.0";
    #endregion

    #region Fields-Instance
    private readonly string ArgsPath;
    private readonly Process RunningProcess;
    #endregion

    #region Constructors
    public ExifToolWrapper(ISettings settings, ExifWatcherWrapper exifWatcher)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(exifWatcher);

        Initialize(settings, exifWatcher);

        ArgsPath = CreateNewArgsFileAndGetPath(settings);
        RunningProcess = CreateAndStartAlwaysOpenExifProcess(settings, ArgsPath);
    }
    #endregion

    #region Destructor
    ~ExifToolWrapper()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Static
    private static void Initialize(ISettings settings, ExifWatcherWrapper exifWatcher)
    {
        if (Initialized) return;
        Initialized = true;

        if (!File.Exists(Path.Combine(settings.ToolsDirectory, ExifTool)))
            throw new FileNotFoundException($"{ExifTool} is missing!");

        if (!Directory.Exists(settings.TempExifPath))
            Directory.CreateDirectory(settings.TempExifPath);

        Version = ProcessHelper.RunAndGetOutput(Path.Combine(settings.ToolsDirectory, ExifTool), "-ver").Trim();

        exifWatcher.StartWatching();
    }

    private static string CreateNewArgsFileAndGetPath(ISettings settings)
    {
        var path = Path.Combine(settings.TempExifPath,
            $"{Guid.NewGuid().ToString().Replace("-", string.Empty)}.txt");

        if (!File.Exists(path))
            File.Create(path).Close();

        return path;
    }
    private static Process CreateAndStartAlwaysOpenExifProcess(ISettings settings, string argsFilePath)
    {
        var ExifPath = Path.Combine(settings.ToolsDirectory, ExifTool);
        var args = $"-stay_open true -@ \"{argsFilePath}\"";

        var p = Process.Start(new ProcessStartInfo
        {
            FileName = ExifPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        p.ErrorDataReceived += (sender, e) =>
        {
            //TODO: inject logger here to log errors
            //LogHelper.Error(e.Data);
        };

        p.Start();
        p.BeginErrorReadLine();

        return p;
    }
    #endregion

    #region Behavior-Instance
    public string Execute(params string[] args)
    {
        AppendArguments(args);
        return ReadOutput();
    }

    private void AppendArguments(params string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        File.AppendAllLines(ArgsPath, new List<string>(args) { "-s", ExifArgsExecuteTag });
    }
    private string ReadOutput()
    {
        if (RunningProcess.HasExited) return string.Empty;

        var sb = new StringBuilder();
        while (true)
        {
            var line = RunningProcess.StandardOutput.ReadLine();

            if (string.IsNullOrWhiteSpace(line)
                || line.Contains(ExifReadyStatement, StringComparison.Ordinal))
                break;

            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppendArguments("-stay_open\nFalse");
            RunningProcess.Dispose();

            if (File.Exists(ArgsPath))
                CommonHelper.RetryIfFails(() => { File.Delete(ArgsPath); });
        }
    }
    #endregion
}