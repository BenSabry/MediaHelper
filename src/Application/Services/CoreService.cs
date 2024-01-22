using Application.Helpers;
using Application.Infrastructure.Services;
using Domain.DTO;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;
using Shared.Extensions;
using Shared.Helpers;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace Application.Services;
public sealed class CoreService : IDisposable
{
    #region Fields-Static
    private readonly ISettings settings;
    private readonly IServiceProvider serviceProvider;
    private readonly ILoggerService loggerService;
    private readonly IExifService exifService;

    private readonly Statistics statistics = new Statistics();
    private readonly Process process = Process.GetCurrentProcess();
    private readonly Dictionary<string, string> versions;

    private DateTime startDateTime;
    #endregion

    #region Constructors
    public CoreService(ISettings settings, IServiceProvider serviceProvider, ILoggerService loggerService, IExifService exifService)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(loggerService);
        ArgumentNullException.ThrowIfNull(exifService);

        this.settings = settings;
        this.serviceProvider = serviceProvider;
        this.loggerService = loggerService;
        this.exifService = exifService;

        versions = new Dictionary<string, string>
        {
            { "MediaOrganizer", TrimVersion(Assembly.GetEntryAssembly()!.GetName()!.Version!.ToString()) },
            { ".NET", TrimVersion(Environment.Version.ToString()) },
            { "Exif", TrimVersion(exifService.ExifToolVersion) },
            { "ExifWatcher", TrimVersion(exifService.ExifWatcherVersion) },
        };
    }
    #endregion

    #region Destructor
    ~CoreService()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Static
    private static void ProcessFile(IMediaFile item, IExifService exif, ILoggerService logger, ISettings settings, Statistics statistics)
    {
        (FileInfo file, FileInfo? json, string src) = (item.GetFile(), item.GetJsonFile(), item.OriginalSource);

        var list = new List<DateTime[]>()
        {
            DateHelper.ExtractAllPossibleDateTimes(file.Name.ReplaceArabicNumbers()),
            exif.ReadAllDates(file.FullName),
        };

        if (json.Exists) list.Add(exif.ReadAllDatesFromJson(json));

        var dates = list
            .SelectMany()
            .Distinct()
            .Where(DateHelper.IsValidDateTime)
            .ToArray();

        if (dates.Length != default)
        {
            var date = dates.Min();

            if (TryCopyFileToDirectoryBasedOnDate(ref file, logger, settings, statistics, src, date))
                UpdateMediaTargetedDateTime(file, exif, logger, statistics, date);
        }
        else LogNoDate(logger, statistics, src);
    }
    private static int GetValidTasksCount(ISettings settings)
    {
        const int DefaultTasksCount = 1;

        if (settings.TasksCount < 1) return DefaultTasksCount;
        else if (settings.EnableSuperUserMode) return settings.TasksCount;
        else if (settings.TasksCount > Environment.ProcessorCount / 2)
            return Environment.ProcessorCount / 2;

        return DefaultTasksCount;
    }

    private static IEnumerable<IMediaFile> GetMediaFiles(IExifService exifService, ISettings settings)
    {
        foreach (var source in settings.Sources)
            if (!string.IsNullOrWhiteSpace(source))
            {
                if (File.Exists(source))
                    foreach (var file in GetMediaFiles(new FileInfo(source), settings, exifService))
                        yield return file;

                else if (Directory.Exists(source))
                {
                    var sources = settings.Sources
                        .Where(i => !i.Equals(source, StringComparison.Ordinal))
                        .ToArray();

                    foreach (var item in GetMediaFiles(new DirectoryInfo(source), sources, settings, exifService))
                        yield return item;
                }
            }
    }
    private static IEnumerable<IMediaFile> GetMediaFiles(FileInfo file, ISettings settings, IExifService exif)
    {
        if (IsSupportedArchiveFile(file))
            foreach (var item in GetMediaFilesOfArchive(file, settings, exif))
                yield return item;

        else if (IsSupportedMediaFile(file, exif))
            yield return new MediaFile(file);
    }
    private static IEnumerable<IMediaFile> GetMediaFiles(DirectoryInfo directory, string[] sources, ISettings settings, IExifService exif)
    {
        if (!sources.Contains(directory.FullName))
        {
            foreach (var file in directory.EnumerateFiles())
                foreach (var mediaFile in GetMediaFiles(file, settings, exif))
                    yield return mediaFile;

            foreach (var dir in directory.EnumerateDirectories())
                foreach (var file in GetMediaFiles(dir, sources, settings, exif))
                    yield return file;
        }
    }
    private static IEnumerable<IMediaFile> GetMediaFilesOfArchive(FileInfo file, ISettings settings, IExifService exif)
    {
        using (var archive = ZipFile.OpenRead(file.FullName))
            return archive.Entries
                .Where(i => IsSupportedMediaFile(i, exif))
                .Select(i => new ArchivedMediaFile(i, file, settings));
    }
    private static bool ThisFileNeedsToBeProcessed(IMediaFile file, ref LogRecord[] records, ref string[] ignores, Statistics statistics)
    {
        var skip = !Array.Exists(ignores, i => file.OriginalSource.Contains(i, StringComparison.OrdinalIgnoreCase))
            && !Array.Exists(records, i => i.Source.Equals(file.OriginalSource, StringComparison.OrdinalIgnoreCase)
                && File.Exists(i.Destination));

        if (!skip)
            Interlocked.Increment(ref statistics.Skipped);

        return skip;
    }

    private static bool IsSupportedMediaFile(FileInfo file, IExifService exif)
    {
        return !string.IsNullOrWhiteSpace(file.Name)
            && MediaHelper.IsSupportedMediaFile(file)
            && exif.IsSupportedMediaFile(file);
    }
    private static bool IsSupportedMediaFile(ZipArchiveEntry file, IExifService exif)
    {
        return !string.IsNullOrWhiteSpace(file.FullName)
            && MediaHelper.IsSupportedMediaFile(file.FullName)
            && exif.IsSupportedMediaFile(file.FullName);
    }
    private static bool IsSupportedArchiveFile(FileInfo file)
    {
        return file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCopyFileToDirectoryBasedOnDate(ref FileInfo file, ILoggerService logger, ISettings settings, Statistics statistics, string originalSource, DateTime dateTime)
    {
        var dest = GetNewDestinationPath(file, settings, dateTime);

        if (!Equals(file.FullName, dest))
            return TryCopyFile(logger, ref file, originalSource, dest, statistics);

        return false;
    }
    private static void MakeSureDirectoryExistsForFile(string filePath)
    {
        var dir = new FileInfo(filePath).Directory;

        if (!dir!.Exists)
            dir.Create();
    }
    private static bool TryCopyFile(ILoggerService logger, ref FileInfo file, string originalSource, string dest, Statistics statistics)
    {
        if (File.Exists(dest))
            LogDuplicate(logger, statistics, originalSource, dest);

        else
            try
            {
                MakeSureDirectoryExistsForFile(dest);
                file = file.CopyTo(dest);
                LogCopy(logger, statistics, originalSource, dest);
                return true;
            }
            catch { LogFail(logger, statistics, dest); }

        return false;
    }

    private static void UpdateMediaTargetedDateTime(FileInfo file, IExifService exif, ILoggerService logger, Statistics statistics, DateTime dateTime)
    {
        var valid = exif.TryUpdateMediaTargetedDateTime(file.FullName, dateTime);

        if (valid) LogUpdate(logger, statistics, file.FullName);
        else LogFail(logger, statistics, file.FullName);
    }
    private static string GetNewDestinationPath(FileInfo file, ISettings settings, DateTime dateTime)
    {
        return Path.Combine(settings.Target,
            CommonHelper.FormatNumberToLength(dateTime.Year, 4),
            CommonHelper.FormatNumberToLength(dateTime.Month, 2),
            file.Name);
    }

    private static void CleanUp(IExifService exifService, ILoggerService loggerService, ISettings settings)
    {
        loggerService.LogInformation("\nCleaning Up...");

        if (settings.ClearBackupFilesOnComplete)
            exifService.ClearBackupFiles(settings.Sources);

        if (settings.DeleteEmptyDirectoriesOnComplete)
            DeleteEmptyDirectories(settings);
    }
    private static void DeleteEmptyDirectories(ISettings settings)
    {
        var dirctories = new List<string>(settings.Sources) { settings.Target }
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => new DirectoryInfo(i))
            .ToArray();

        Parallel.ForEach(dirctories, DeleteEmptyDirectories);
    }
    private static void DeleteEmptyDirectories(DirectoryInfo directory)
    {
        if (!directory.Exists) return;

        Parallel.ForEach(directory.EnumerateDirectories(), DeleteEmptyDirectories);

        if (!directory.EnumerateFiles().Any()
            && !directory.EnumerateDirectories().Any())
            directory.Delete();
    }

    private static void LogCopy(ILoggerService logger, Statistics statistics, string src, string dest)
    {
        Interlocked.Increment(ref statistics.Copies);
        logger.Log(LogOperation.Copy, src, dest);
    }
    private static void LogDuplicate(ILoggerService logger, Statistics statistics, string src, string dest)
    {
        Interlocked.Increment(ref statistics.Duplicates);
        logger.Log(LogOperation.Duplicate, src, dest);
    }
    private static void LogUpdate(ILoggerService logger, Statistics statistics, string src)
    {
        Interlocked.Increment(ref statistics.Updates);
        logger.Log(LogOperation.Update, src);
    }
    private static void LogFail(ILoggerService logger, Statistics statistics, string src)
    {
        Interlocked.Increment(ref statistics.Fails);
        logger.Log(LogOperation.Fail, src);
    }
    private static void LogNoDate(ILoggerService logger, Statistics statistics, string src)
    {
        Interlocked.Increment(ref statistics.Fails);
        logger.Log(LogOperation.NoDate, src);
    }

    private static string TrimVersion(string version) => version.Replace(".0", string.Empty);
    #endregion

    #region Behavior-Instance
    public async Task RunAsync()
    {
        ShowWelcomeMessage();
        if (!SourcesAreValidToUse())
            return;

        PressAnyKeyToStart();
        InitializeTimer();

        const int DelayInMilliseconds = 100;
        await SkipProcessedFiles(GetMediaFiles(exifService, settings))
            .Take(10)
            .ParallelForEachAsync(
                GetValidTasksCount(settings),
                (IMediaFile file, long i, (IExifService exif, ILoggerService logger) arg)
                    => ProcessFile(file, arg.exif, arg.logger, settings, statistics),
                () => (GetService<IExifService>(), GetService<ILoggerService>()),
                LogProgress, DelayInMilliseconds);
    }

    private bool SourcesAreValidToUse()
    {
        if (settings.Sources.All(string.IsNullOrWhiteSpace))
        {
            loggerService.LogError("Please add Sources to AppSettings");
            loggerService.LogWarning(settings.RootDirectory);

            return false;
        }
        return true;
    }
    private void PressAnyKeyToStart()
    {
        if (settings.EnableSuperUserMode)
            return;

        loggerService.ClearShell();
        ShowWelcomeMessage();
        loggerService.LogWarning("Press any key to start.");

        loggerService.WaitUserInteraction();
        loggerService.ClearShell();
        ShowWelcomeMessage();
    }
    private void InitializeTimer()
    {
        startDateTime = DateTime.Now;
    }
    private IEnumerable<IMediaFile> SkipProcessedFiles(IEnumerable<IMediaFile> files)
    {
        loggerService.LogSuccess("Reading log files to resume...");

        var operationsToSkip = new LogOperation[] { LogOperation.Copy, LogOperation.Duplicate };
        var ignores = settings.Ignores;

        var records = Array.Empty<LogRecord>();

        try
        {
            records = settings.EnableLogAndResume
                ? loggerService.ReadAllLogs()
                .Where(i => operationsToSkip.Contains(i.Operation))
                .ToArray() : [];
        }
        catch
        {
            loggerService.LogError("Incompatable log files, Please clean log directory and restart.");
            return Array.Empty<IMediaFile>();
        }

        return files.Where(file => ThisFileNeedsToBeProcessed(file, ref records, ref ignores, statistics));
    }

    private void LogProgress(long index, long total)
    {
        if (total == default) return;
        if (index == default) index++;

        var t = DateTime.Now - startDateTime;
        var r = (t / index) * (total - index);
        var processes = index / t.TotalSeconds;

        var length = ((int)(processes * 3600)).ToString().Length;
        var MegaBytesOfRAM = process.PrivateMemorySize64 / 1_048_576;
        const char separator = ' ';

        var message = $"Elapsed time: {CommonHelper.FormatNumberToLength(t.Hours, 2)}:{CommonHelper.FormatNumberToLength(t.Minutes, 2)}:{CommonHelper.FormatNumberToLength(t.Seconds, 2)}\n"
            + $"Remaining time: {CommonHelper.FormatNumberToLength(r.Hours, 2)}:{CommonHelper.FormatNumberToLength(r.Minutes, 2)}:{CommonHelper.FormatNumberToLength(r.Seconds, 2)}\n"
            + $"Tasks Running: {settings.TasksCount}   RAM: {MegaBytesOfRAM}MB\n\n"

            + $"Current: {index}/{total}\nCopied: {statistics.Copies}\nUpdated: {statistics.Updates}\nSkipped: {statistics.Skipped}\nFailed: {statistics.Fails}\nDuplicates: {statistics.Duplicates}\n\n"
            + $"Processing Speed:\n"
            + $"\t{CommonHelper.FormatToLength(((int)processes).ToString(), length, separator)} files per second\n"
            + $"\t{CommonHelper.FormatToLength(((int)(processes * 60)).ToString(), length, separator)} files per minute\n"
            + $"\t{CommonHelper.FormatToLength(((int)(processes * 3600)).ToString(), length, separator)} files per hour\n";

        loggerService.ClearShell();
        ShowWelcomeMessage();
        loggerService.LogInformation(message);
        LogCurrentProgress(index + 1, total);
        loggerService.LogWarning("\n\tDon’t be alarmed by any warning messages that may appear.\n\tThey’re associated with incorrect offsets in media files, ");
        loggerService.LogSuccess("\twhich will be rectified automatically.");
    }
    private void ShowWelcomeMessage()
    {
        loggerService.LogCritical($"https://github.com/BenSabry/MediaOrganizer");

        foreach (var item in versions)
        {
            loggerService.LogInformation($"{item.Key} ", false);
            loggerService.LogSuccess($"{item.Value} ", false);
        }

        loggerService.LogInformation("\n");
    }
    private void LogCurrentProgress(long index, long total)
    {
        loggerService.LogSuccess(ConsoleHelper.GenerateProgressBar(index, total, Console.WindowWidth));
    }

    private T GetService<T>()
    {
        return (T)serviceProvider.GetService(typeof(T));
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
            CleanUp(exifService, loggerService, settings);

            exifService.Dispose();
            loggerService.Dispose();
            process.Dispose();
        }
    }
    #endregion

    private sealed class Statistics
    {
        public int Copies;
        public int Updates;
        public int Skipped;
        public int Fails;
        public int Duplicates;
    }
}
