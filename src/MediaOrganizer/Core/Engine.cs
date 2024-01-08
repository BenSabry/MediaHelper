using MediaOrganizer.Helpers;
using MediaOrganizer.Models;
using MediaOrganizer.Models.Interfaces;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace MediaOrganizer.Core;
public static class Engine
{
    #region Fields-Static
    private static readonly Settings Settings = SettingsHelper.GetSettings();
    private static DateTime StartDateTime;

    private static int Copies;
    private static int Updates;
    private static int Fails;

    private static readonly Dictionary<string, string> Versions;
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    #endregion

    #region Constructors
    static Engine()
    {
        Versions = new Dictionary<string, string>
        {
            { "MediaHelper", Assembly.GetEntryAssembly().GetName().Version.ToString() },
            { ".NET", Environment.Version.ToString() },
            { "Exif", ExifHelper.ReadVersion() }
        };
    }
    #endregion

    #region Behavior
    private static bool SourcesAreValidToUse()
    {
        if (Settings?.Sources is null
            || Settings.Sources.All(string.IsNullOrWhiteSpace))
        {
            LogHelper.Error("Please add Sources to AppSettings");
            LogHelper.Warning(SettingsHelper.AppSettingsPath);

            return false;
        }
        return true;
    }
    private static void RegisterExitEvent()
    {
        AppDomain.CurrentDomain.ProcessExit += (o, e) =>
        {
            CleanUp();
        };
    }
    private static void PressAnyKeyToStart()
    {
        if (Debugger.IsAttached) return;

        LogHelper.Clear();
        ShowWelcomeMessage();
        LogHelper.Warning("Press any key to start.");

        LogHelper.ReadKey();
        LogHelper.Clear();
        ShowWelcomeMessage();
    }
    private static void InitializeTimer()
    {
        StartDateTime = DateTime.Now;
    }
    private static void ValidateTasksConfig()
    {
        if (Settings.TasksCount < 0)
            Settings.TasksCount = 1;

        else if (!Settings.EnableSuperUserMode
            && Settings.TasksCount > Environment.ProcessorCount / 2)
            Settings.TasksCount = Environment.ProcessorCount / 2;
    }

    public static void Run()
    {
        if (!SourcesAreValidToUse())
            return;

        RegisterExitEvent();
        PressAnyKeyToStart();
        InitializeTimer();
        ValidateTasksConfig();

        const int DelayInMilliseconds = 100;
        GetMediaFiles()
            .SkipProcessedFiles()
            .ParallelForEachTask(
                Settings.TasksCount,
                (IMediaFile file, long i, (ExifHelper exif, LogHelper logger) arg)
                    => ProcessFile(file, i, arg.exif, arg.logger),

                () => (new ExifHelper(
                        Settings.AttemptToFixIncorrectOffsets,
                        Settings.ClearBackupFilesOnComplete),
                    new LogHelper(Settings.EnableLogAndResume)),
                LogProgress, DelayInMilliseconds)

            .RepeatWhileTaskRunning(LogHelper.SaveLog, 60_000)
            .Wait();

        CleanUp();
    }
    private static void ProcessFile(IMediaFile item, long index, ExifHelper exif, LogHelper logger)
    {
        (FileInfo file, FileInfo? json, string src) = (item.GetFile(), item.GetJsonFile(), item.OriginalSource);

        var dates = new DateTime[][]
        {
            DateHelper.ExtractAllPossibleDateTimes(file.Name),
            exif.ReadAllDates(file.FullName),
            exif.ReadAllDatesFromJson(json),

            [ exif.ReadMediaDefaultCreationDate(file.FullName) ]
        }
            .SelectMany()
            .Distinct()
            .Where(DateHelper.IsValidDateTime)
            .ToArray();

        if (dates.Length != default)
        {
            var date = dates.Min();
            if (TryCopyFileToDirectoryBasedOnDate(ref file, src, index, date, logger))
                UpdateMediaTargetedDateTime(file, index, date, exif, logger);
        }
        else LogFail(logger, index, src);
    }
    private static void CleanUp()
    {
        LogHelper.Message("\nCleaning Up...");

        if (Settings.ClearBackupFilesOnComplete) ExifHelper.ClearBackupFiles(Settings.Sources);
        if (Settings.DeleteEmptyDirectoriesOnComplete) DeleteEmptyDirectories();

        ArchivedMediaFile.CleanUp();
        ExifHelper.CleanUp();
    }

    private static IEnumerable<IMediaFile> SkipProcessedFiles(this IEnumerable<IMediaFile> files)
    {
        var ignores = Settings.Ignores;
        var processed = Settings.EnableLogAndResume
            ? LogHelper.ReadAllLogs().Where(i => i.Operation != LogOperation.Fail).Select(i => i.Source).ToArray()
            : [];

        foreach (var file in files)
            if (!Array.Exists(processed, i => i.Equals(file.OriginalSource, StringComparison.OrdinalIgnoreCase))
                && !Array.Exists(ignores, i => file.OriginalSource.Contains(i, StringComparison.OrdinalIgnoreCase)))
                yield return file;
    }
    private static IEnumerable<IMediaFile> GetMediaFiles()
    {
        foreach (var source in Settings.Sources)
            if (!string.IsNullOrWhiteSpace(source))
            {
                if (File.Exists(source))
                    foreach (var file in GetMediaFiles(new FileInfo(source)))
                        yield return file;

                else if (Directory.Exists(source))
                {
                    var sources = Settings.Sources
                        .Where(i => !i.Equals(source, StringComparison.Ordinal))
                        .ToArray();

                    foreach (var item in GetMediaFiles(new DirectoryInfo(source), sources))
                        yield return item;
                }
            }
    }
    private static IEnumerable<IMediaFile> GetMediaFiles(FileInfo file)
    {
        if (IsSupportedMediaFile(file))
            yield return new MediaFile(file);

        else if (IsSupportedArchiveFile(file))
            foreach (var item in GetMediaFilesOfArchive(file))
                yield return item;
    }
    private static IEnumerable<IMediaFile> GetMediaFiles(DirectoryInfo directory, string[] sources)
    {
        if (!sources.Contains(directory.FullName))
        {
            foreach (var file in directory.EnumerateFiles())
                foreach (var mediaFile in GetMediaFiles(file))
                    yield return mediaFile;

            foreach (var dir in directory.EnumerateDirectories())
                foreach (var file in GetMediaFiles(dir, sources))
                    yield return file;
        }
    }
    private static IEnumerable<IMediaFile> GetMediaFilesOfArchive(FileInfo file)
    {
        using (var archive = ZipFile.OpenRead(file.FullName))
            return archive.Entries
                .Where(IsSupportedMediaFile)
                .Select(i => new ArchivedMediaFile(i, file));
    }

    private static void DeleteEmptyDirectories()
    {
        var dirctories = new List<string>(Settings.Sources) { Settings.Target };

        foreach (var dir in dirctories)
            if (string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                DeleteEmptyDirectories(new DirectoryInfo(dir));
    }
    private static void DeleteEmptyDirectories(DirectoryInfo directory)
    {
        foreach (var dir in directory.EnumerateDirectories())
            DeleteEmptyDirectories(dir);

        if (!directory.EnumerateFiles().Any()
            && !directory.EnumerateDirectories().Any())
            directory.Delete();
    }
    private static bool IsSupportedMediaFile(FileInfo file)
    {
        return !string.IsNullOrWhiteSpace(file.Name)
            && ExifHelper.IsSupportedMediaFile(file);
    }
    private static bool IsSupportedMediaFile(ZipArchiveEntry file)
    {
        return !string.IsNullOrWhiteSpace(file.FullName)
            && ExifHelper.IsSupportedMediaFile(file.FullName);
    }
    private static bool IsSupportedArchiveFile(FileInfo file)
    {
        return file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCopyFileToDirectoryBasedOnDate(ref FileInfo file, string originalSource, long index, DateTime dateTime, LogHelper logger)
    {
        var dest = GetNewDestinationPath(file, dateTime);

        if (!Equals(file.FullName, dest))
            return TryCopyFile(logger, ref file, originalSource, index, dest);

        return false;
    }
    private static void MakeSureDirectoryExistsForFile(string filePath)
    {
        var dir = new FileInfo(filePath).Directory;

        if (!dir!.Exists)
            dir.Create();
    }
    private static bool TryCopyFile(LogHelper logger, ref FileInfo file, string originalSource, long index, string dest)
    {
        if (File.Exists(dest))
            LogFail(logger, index, file.FullName);

        else
            try
            {
                MakeSureDirectoryExistsForFile(dest);
                var src = file.FullName;
                file = file.CopyTo(dest);
                LogCopy(logger, index, originalSource, dest);
                return true;
            }
            catch
            {
                LogFail(logger, index, dest);
            }

        return false;
    }

    private static void UpdateMediaTargetedDateTime(FileInfo file, long index, DateTime dateTime, ExifHelper exif, LogHelper logger)
    {
        var valid = exif.TryUpdateMediaTargetedDateTime(file.FullName, dateTime);

        if (valid) LogUpdate(logger, index, file.FullName);
        else LogFail(logger, index, file.FullName);
    }
    private static string GetNewDestinationPath(FileInfo file, DateTime dateTime)
        => Path.Combine(Settings.Target,
            CommonHelper.FormatNumberToLength(dateTime.Year, 4),
            CommonHelper.FormatNumberToLength(dateTime.Month, 2),
            file.Name);

    private static void LogCopy(LogHelper logger, long index, string src, string dest)
    {
        Interlocked.Increment(ref Copies);
        logger.Log(LogOperation.Copy, index.ToString(), src, dest);
    }
    private static void LogUpdate(LogHelper logger, long index, string src)
    {
        Interlocked.Increment(ref Updates);
        logger.Log(LogOperation.Update, index.ToString(), src);
    }
    private static void LogFail(LogHelper logger, long index, string src)
    {
        Interlocked.Increment(ref Fails);
        logger.Log(LogOperation.Fail, index.ToString(), src);
    }
    private static void LogProgress(long index, long total)
    {
        var t = DateTime.Now - StartDateTime;
        var r = (t / (index + 1)) * (total - index);
        var processes = index / t.TotalSeconds;

        var length = ((int)(processes * 3600)).ToString().Length;
        var MegaBytesOfRAM = CurrentProcess.PrivateMemorySize64 / 1_048_576;
        const char separator = ' ';

        LogHelper.Clear();
        ShowWelcomeMessage();

        LogHelper.Message(
            $"Elapsed time: {CommonHelper.FormatNumberToLength(t.Hours, 2)}:{CommonHelper.FormatNumberToLength(t.Minutes, 2)}:{CommonHelper.FormatNumberToLength(t.Seconds, 2)}\n"
            + $"Remaining time: {CommonHelper.FormatNumberToLength(r.Hours, 2)}:{CommonHelper.FormatNumberToLength(r.Minutes, 2)}:{CommonHelper.FormatNumberToLength(r.Seconds, 2)}\n"
            + $"Tasks Running: {Settings.TasksCount}   RAM: {MegaBytesOfRAM}MB\n\n"

            + $"Currnet: {index}/{total}\nCopied: {Copies}\nUpdated: {Updates}\nSkipped: {Fails}\n\n"
            + $"Processing Speed:\n"
            + $"\t{CommonHelper.FormatToLength(((int)processes).ToString(), length, separator)} files per second\n"
            + $"\t{CommonHelper.FormatToLength(((int)(processes * 60)).ToString(), length, separator)} files per minute\n"
            + $"\t{CommonHelper.FormatToLength(((int)(processes * 3600)).ToString(), length, separator)} files per hour\n");

        LogCurrentProgress(index, total);
        LogHelper.Warning("\n\tDon’t be alarmed by any warning messages that may appear.\n\tThey’re associated with incorrect offsets in media files, ");
        LogHelper.Success("\twhich will be rectified automatically.");
    }

    private static void ShowWelcomeMessage()
    {
        LogHelper.Notice($"MediaOrganizer by BenSabry\n"
            + $"https://github.com/BenSabry/MediaOrganizer");

        foreach (var item in Versions)
        {
            LogHelper.Message($"{item.Key} ", false);
            LogHelper.Success($"{item.Value}   ", false);
        }

        LogHelper.Message("\n");
    }
    private static void LogCurrentProgress(long index, long total)
    {
        LogHelper.Success(GenerateProgressBar(index, total, Console.WindowWidth));
    }
    public static string GenerateProgressBar(long index, long total, long width)
    {
        if (total == default)
        {
            index++;
            total++;
        }

        var perc = Math.Round(index / (decimal)total * 100, 2);
        var percText = $"[ {perc}% ";

        if (width < percText.Length + 2)
            return $"{percText}]";

        var done = Math.Max(((width - percText.Length) / (decimal)100 * perc) - 2, default);
        var remain = width - percText.Length - (int)done;

        return $"{percText}{new string('-', (int)done)}{new string(' ', (int)remain - 1)}]";
    }
    #endregion
}
