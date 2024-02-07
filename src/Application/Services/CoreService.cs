using Application.Helpers;
using Application.Infrastructure.Services;
using Domain.DTO;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;
using Shared.Extensions;
using Shared.Helpers;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace Application.Services;
public sealed class CoreService : IDisposable
{
    #region Fields
    private readonly ISettings settings;
    private readonly IServiceProvider serviceProvider;
    private readonly IExifCoreService exifService;
    private readonly ILoggerService loggerService;

    private readonly Statistics statistics = new Statistics();
    private readonly Process process = Process.GetCurrentProcess();
    private readonly Dictionary<string, string> versions;

    private readonly int TasksCount;
    private DateTime startDateTime;
    private bool IsStarted;
    #endregion

    #region Constructors
    public CoreService(ISettings settings, IServiceProvider serviceProvider, IExifCoreService exifService, ILoggerService loggerService)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(exifService);
        ArgumentNullException.ThrowIfNull(loggerService);

        this.settings = settings;
        this.serviceProvider = serviceProvider;
        this.exifService = exifService;
        this.loggerService = loggerService;

        TasksCount = GetValidTasksCount(settings);
        versions = new Dictionary<string, string>
        {
            { "MediaHelper", TrimVersion(Assembly.GetEntryAssembly()!.GetName()!.Version!.ToString()) },
            { ".NET", TrimVersion(Environment.Version.ToString()) },
            { "Exif", TrimVersion(exifService.ToolVersion) },
            { "ExifWatcher", TrimVersion(exifService.WatcherVersion) },
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
    private static int GetValidTasksCount(ISettings settings)
    {
        const int DefaultTasksCount = 1;

        if (settings.TasksCount < 1) return DefaultTasksCount;
        else if (settings.EnableSuperUserMode) return settings.TasksCount;
        else if (settings.TasksCount > Environment.ProcessorCount / 2)
            return Environment.ProcessorCount / 2;

        return settings.TasksCount;
    }

    private static bool ThisFileNeedsToBeProcessed(IMediaFile file, ref Dictionary<string, LogRecord> records, ref string[] ignores, Statistics statistics)
    {
        var skip = Array.Exists(ignores, i => file.OriginalSource.Contains(i, StringComparison.OrdinalIgnoreCase))
            || (records.TryGetValue(file.OriginalSource.ToLower(), out var value) && File.Exists(value.Destination));

        if (skip)
            Interlocked.Increment(ref statistics.Skipped);

        return !skip;
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

    private static void UpdateMedia(FileInfo file, IExifService exif, ILoggerService logger, Statistics statistics, Dictionary<string, string> tags)
    {
        if (exif.TryWriteMetadata(file.FullName, tags))
            LogUpdate(logger, statistics, file.FullName);
        else LogFail(logger, statistics, file.FullName);
    }
    private static string GetNewDestinationPath(FileInfo file, ISettings settings, DateTime dateTime)
    {
        return Path.Combine(settings.Target,
            CommonHelper.FormatNumberToLength(dateTime.Year, 4),
            CommonHelper.FormatNumberToLength(dateTime.Month, 2),
            file.Name);
    }

    //TODO: delete this operation because it's already copying the file not working on same file so it's not needed anymore
    //TODO: move it ExifToolWrapper
    private static void CleanUp(IExifCoreService exifService, ILoggerService loggerService, ISettings settings)
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
        await SkipProcessedFiles(GetMediaFiles())
            .ParallelForEachAsync(
                TasksCount,
                (IMediaFile file, long i, (IExifService exif, ILoggerService logger) arg)
                    => ProcessFile(file, arg.exif, arg.logger),
                () => (GetService<IExifService>(), GetService<ILoggerService>()),
                LogProgress,
                DelayInMilliseconds);
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
        IsStarted = true;
    }

    private void ProcessFile(IMediaFile item, IExifService exif, ILoggerService logger)
    {
        //TODO: extract or copy files with temp Exif compatable name while processing then rename
        (var file, var json, var src) = (item.GetFile(), item.GetJsonFile(), item.OriginalSource);

        //TODO: remove empty tags
        var original = exif.ReadMetadata(file.FullName);
        var temp = new Dictionary<string, string>(original);

        if (json.Exists)
        {
            var jsonMeta = exif.ReadJsonMetadata(json.FullName);
            foreach (var tag in jsonMeta)
                if (!temp.TryAdd(tag.Key, tag.Value))
                    temp[tag.Key] = tag.Value;
        }

        if (!exif.TryUpdateCreationDateTagsWithMinAcceptableValue(ref temp,
            DateHelper.ExtractPossibleDateTimes(file.Name.ReplaceArabicNumbers()), out var date))
        {
            LogNoDate(logger, statistics, src);
            return;
        }

#if DEBUG
        if (date.Year < 2000)
        {

        }
#endif

        var updates = new Dictionary<string, string>(temp
            .Where(a => !original.TryGetValue(a.Key, out var value)
            || !a.Value.Equals(value, StringComparison.Ordinal)));

        if (TryCopyFileToDirectoryBasedOnDate(ref file, logger, settings, statistics, src, date))
            UpdateMedia(file, exif, logger, statistics, updates);

#if DEBUG
        else
        {

        }
#endif
    }

    private IEnumerable<IMediaFile> GetMediaFiles()
    {
        foreach (var source in settings.Sources)
            if (!string.IsNullOrWhiteSpace(source))
            {
                if (File.Exists(source))
                    foreach (var file in GetMediaFiles(new FileInfo(source)))
                        yield return file;

                else if (Directory.Exists(source))
                {
                    var sources = settings.Sources
                        .Where(i => !i.Equals(source, StringComparison.Ordinal))
                        .ToArray();

                    foreach (var item in GetMediaFiles(new DirectoryInfo(source), sources))
                        yield return item;
                }
            }
    }
    private IEnumerable<IMediaFile> GetMediaFiles(FileInfo file)
    {
        if (IsSupportedArchiveFile(file))
            foreach (var item in GetMediaFilesOfArchive(file, settings))
                yield return item;

        else if (IsSupportedMediaFile(file))
            yield return new MediaFile(file);
    }
    private IEnumerable<IMediaFile> GetMediaFiles(DirectoryInfo directory, string[] sources)
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
    private IEnumerable<IMediaFile> GetMediaFilesOfArchive(FileInfo file, ISettings settings)
    {
        using (var archive = ZipFile.OpenRead(file.FullName))
            return archive.Entries
                .Where(i => IsSupportedMediaFile(i))
                .Select(i => new ArchivedMediaFile(i, file, settings));
    }
    private IEnumerable<IMediaFile> SkipProcessedFiles(IEnumerable<IMediaFile> files)
    {
        loggerService.LogSuccess("Reading log files to resume...");

        var operationsToSkip = new LogOperation[] { LogOperation.Copy, LogOperation.Duplicate };
        var ignores = settings.Ignores;

        Dictionary<string, LogRecord> records;
        if (settings.EnableLogAndResume)
            try
            {
                records = new Dictionary<string, LogRecord>(loggerService.ReadAllLogs()
                    .Where(i => operationsToSkip.Contains(i.Operation))
                    .DistinctBy(i => i.Source)
                    .Select(i => new KeyValuePair<string, LogRecord>(i.Source.ToLower(), i)));

                return files.Where(file => ThisFileNeedsToBeProcessed(file, ref records, ref ignores, statistics));
            }
            catch { loggerService.LogError("Incompatable log files, Please clean log directory and restart."); }

        return Array.Empty<IMediaFile>();
    }

    private bool IsSupportedMediaFile(FileInfo file)
    {
        return !string.IsNullOrWhiteSpace(file.Name)
            && MediaHelper.IsSupportedMediaFile(file)
            && exifService.IsSupportedMediaFile(file);
    }
    private bool IsSupportedMediaFile(ZipArchiveEntry file)
    {
        return !string.IsNullOrWhiteSpace(file.FullName)
            && MediaHelper.IsSupportedMediaFile(file.FullName)
            && exifService.IsSupportedMediaFile(file.FullName);
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
            + $"Tasks Running: {TasksCount}   RAM: {MegaBytesOfRAM}MB\n\n"

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
        loggerService.LogCritical($"https://github.com/BenSabry/MediaHelper");

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
    #endregion

    #region Dispose
    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing)
        {
            process.Dispose();
        }

        if (IsStarted)
            CleanUp(exifService, loggerService, settings);

        loggerService.Dispose();
        disposed = true;
    }
    #endregion

    #region Nested
    private sealed class Statistics
    {
        public int Copies;
        public int Updates;
        public int Skipped;
        public int Fails;
        public int Duplicates;
    }
    #endregion
}
