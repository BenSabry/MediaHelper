using Application.Infrastructure.Services;
using Domain.Interfaces;
using Infrastructure.Wrappers;
using Shared.Extensions;
using Shared.Helpers;
using System.Collections.Concurrent;
using System.Text;

namespace Infrastructure.Services;
public sealed class ExifService : IExifService
{
    #region Fields-Static
    //TODO: find if there is a better way instead of static fields

    private const bool IgnoreMinorErrorsAndWarnings = true;
    private const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";
    private const string ExifDefaultCreationTag = "CreateDate";

    private static readonly string[] ExifTargetedDateTimeTags = [ExifDefaultCreationTag, "FileCreateDate", "FileModifyDate", "DateTimeOriginal"];
    private static readonly string[] ExifReadAllDatesArgs = ["-AllDates", "-FileCreateDate", "-FileModifyDate", "-FileAccessDate", $"-{ExifDefaultCreationTag}"];

    private static string[] SupportedMediaExtensions = Array.Empty<string>();
    private static bool Initialized;
    #endregion

    #region Fields-Instance
    public string ExifToolVersion => ExifToolWrapper.Version;
    public string ExifWatcherVersion => ExifWatcherWrapper.Version;

    private readonly ExifToolWrapper Wrapper;

    private readonly bool AttemptToFixIncorrectOffsets;
    private readonly bool ClearBackupFilesOnComplete;
    #endregion

    #region Constructors
    public ExifService(ISettings settings, ExifToolWrapper wrapper)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Wrapper = wrapper;

        AttemptToFixIncorrectOffsets = settings.AttemptToFixIncorrectOffsets;
        ClearBackupFilesOnComplete = settings.ClearBackupFilesOnComplete;

        Initialize(wrapper);
    }
    #endregion

    #region Destructor
    ~ExifService()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Static
    private static void Initialize(ExifToolWrapper wrapper)
    {
        if (Initialized) return;
        Initialized = true;

        SupportedMediaExtensions = GetSupportedFileExtensions(wrapper);
    }

    private static string DateTimeFormat(DateTime dateTime)
    {
        return dateTime.ToString(ExifDateFormat);
    }
    private static string[] GetSupportedFileExtensions(ExifToolWrapper wrapper)
    {
        const string Temp = "supported file extensions:";
        return wrapper!.Execute("-listf")
            .Replace(Temp, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split("\n").SelectMany(i => i.Split(" ")).Select(i => i.Trim())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => $".{i.ToLower()}")
            .ToArray();
    }
    private static ExifResult Read(ExifToolWrapper wrapper, List<string> args)
    {
        if (IgnoreMinorErrorsAndWarnings) args.Add("-m");

        return BuildResult(wrapper.Execute(args.ToArray()));
    }
    private static bool TryWrite(ExifToolWrapper wrapper, List<string> args,
        bool attemptToFixIncorrectOffsets, bool clearBackupFilesOnComplete)
    {
        if (attemptToFixIncorrectOffsets) args.Add("-F");
        if (clearBackupFilesOnComplete) args.Add("-overwrite_original");

        var r = Read(wrapper, args);
        return r.Updates > 0 && r.Errors == default;
    }

    private static ExifResult BuildResult(string output)
    {
        const string LineSeparator = "\n";
        const string TagSeparator = ": ";
        const string MessageSeparator = " ";

        const string updateMessage = "image files updated";
        const string errorMessage = "files weren't updated due to errors";
        const string couldReadMessage = "image files read";
        const string couldNotReadMessage = "files could not be read";

        var updates = 0;
        var errors = 0;

        var pairs = new ConcurrentDictionary<string, string>();
        Parallel.ForEach(output.Split(LineSeparator), line =>
        {
            if (line.Contains(TagSeparator, StringComparison.Ordinal))
            {
                var parts = line.Split(TagSeparator);
                pairs.TryAdd(parts[0].Trim(), parts[1].Trim());
            }

            else if (line.Contains(MessageSeparator, StringComparison.Ordinal))
            {
                var parts = line.Split(MessageSeparator);
                if (int.TryParse(parts[0], out int value))
                {
                    var message = line.Remove(0, parts[0].Length + 1);
                    switch (message)
                    {
                        case couldReadMessage: Interlocked.Add(ref updates, value); break;
                        case couldNotReadMessage: Interlocked.Add(ref errors, value); break;
                        case updateMessage: Interlocked.Add(ref updates, value); break;
                        case errorMessage: Interlocked.Add(ref errors, value); break;
                        default: break;
                    }
                }
            }
        });

        return new ExifResult(output, pairs.ToDictionary(), updates, errors);
    }
    private static string ClearBackupFiles(ExifToolWrapper wrapper, DirectoryInfo directory)
    {
        var builder = new StringBuilder();
        foreach (var dir in directory.GetDirectories())
            builder.AppendLine(ClearBackupFiles(wrapper, dir));

        return builder.AppendLine(wrapper.Execute(
            ["-overwrite_original", "-delete_original!", $"{directory.FullName}"]))
            .ToString();
    }
    #endregion

    #region Behavior-Instance
    public string ClearBackupFiles(params string[] sources)
    {
        if (sources is null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var src in sources)
            if (string.IsNullOrWhiteSpace(src) && Directory.Exists(src))
                builder.AppendLine(ClearBackupFiles(Wrapper, new DirectoryInfo(src)));

        var lines = CommonHelper.SplitStringLines(builder.ToString());

        var directories = 0;
        var images = 0;
        var originals = 0;

        const string directoryMessage = "directories scanned";
        const string imagesMessage = "image files found";
        const string originalsMessage = "original files deleted";
        const char Splitter = ' ';

        foreach (var line in lines)
        {
            var parts = line.Split(Splitter);
            if (!int.TryParse(parts[0], out int value))
                continue;

            var msg = line.Remove(0, parts[0].Length + 1);
            switch (msg)
            {
                case directoryMessage: directories += value; break;
                case imagesMessage: images += value; break;
                case originalsMessage: originals += value; break;
                default: break;
            }
        }

        return $"\n{directories} {directoryMessage}\n{images} {imagesMessage}\n{originals} backup files deleted";
    }

    public bool IsSupportedMediaFile(FileInfo file)
    {
        return SupportedMediaExtensions.Contains(file.Extension.ToLower());
    }
    public bool IsSupportedMediaFile(string fileName)
    {
        return SupportedMediaExtensions.Any(ext =>
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public DateTime[] ReadAllDates(string path)
    {
        var args = new List<string> { path };
        args.AddRange(ExifReadAllDatesArgs);

        return Read(Wrapper, args)
            .Tags
            .Select(i => i.Value)
            .Distinct()
            .Select(DateHelper.ExtractAllPossibleDateTimes)
            .SelectMany()
            .Distinct()
            .ToArray();
    }
    public DateTime[] ReadAllDatesFromJson(FileInfo jsonFile)
    {
        ArgumentNullException.ThrowIfNull(jsonFile);
        return ReadAllDates(jsonFile.FullName);
    }
    public bool TryUpdateMediaTargetedDateTime(string path, DateTime dateTime)
    {
        var args = new List<string> { path };
        var date = DateTimeFormat(dateTime);

        args.AddRange(ExifTargetedDateTimeTags.Select(i => $"-{i}={date}"));

        return TryWrite(Wrapper, args, AttemptToFixIncorrectOffsets, ClearBackupFilesOnComplete);
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
            Wrapper.Dispose();
        }
    }
    #endregion

    private record struct ExifResult(string output, Dictionary<string, string> Tags, int Updates, int Errors);
}
