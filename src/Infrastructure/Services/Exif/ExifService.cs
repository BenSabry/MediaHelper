using Application.Infrastructure.Services;
using Domain.Interfaces;
using Infrastructure.Wrappers;
using Shared.Helpers;
using Wrappers;

namespace Infrastructure.Services;

internal sealed class ExifService : ExifBaseService, IExifService
{
    #region Fields
    private readonly ExifServiceSettings Settings;
    private readonly ExifToolWrapper Wrapper;
    #endregion

    #region Constructors
    public ExifService(ISettings settings, ExifServiceSettings exifSettings,
        ExifToolWrapper wrapper, ExifWatcherWrapper watcher)
        : base(settings, exifSettings, watcher)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(exifSettings);
        ArgumentNullException.ThrowIfNull(wrapper);

        Settings = exifSettings;
        Wrapper = wrapper;
    }
    #endregion

    #region Destructor
    ~ExifService()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Static
    private static ExifResult Read(ExifToolWrapper wrapper, ExifServiceSettings settings, List<string> args)
    {
        AppendArgumentsBySettings(settings, ref args, false);

        return BuildResult(wrapper.Execute(args.ToArray()), settings.IgnoredTags);
    }
    private static bool TryWrite(ExifToolWrapper wrapper, ExifServiceSettings settings, List<string> args)
    {
        AppendArgumentsBySettings(settings, ref args, true);

        var r = BuildResult(wrapper.Execute(args.ToArray()), Array.Empty<string>());
        return r.Updates > 0 && r.Errors == default;
    }

    private static ExifResult BuildResult(string output, string[] ignores)
    {
        #region Constants
        const char LineSeparator = '\n';
        const char ExifTagStart = '-';
        const char EqualsChar = '=';
        const char TagSeparator = ':';
        const char StatusSeparator = ' ';

        const string updateMessage = "image files updated";
        const string errorMessage = "files weren't updated due to errors";
        const string couldReadMessage = "image files read";
        const string couldNotReadMessage = "files could not be read";
        const string unchanged = "image files unchanged";

        const StringComparison comp = StringComparison.OrdinalIgnoreCase;
        #endregion

        var updates = 0;
        var errors = 0;

        var tags = new Dictionary<string, string>();
        foreach (var line in output.Split(LineSeparator))
            if (line.StartsWith(ExifTagStart))
            {
                var equal = line.IndexOf(EqualsChar);
                var keyStart = line.IndexOf(TagSeparator);
                var key = line.Substring(keyStart + 1, equal - keyStart - 1);

                if (!Array.Exists(ignores, tag => key.StartsWith(tag, comp)))
                    tags.Add(key, line.Substring(equal + 1).Trim());
            }

            else if (line.Contains(StatusSeparator, comp))
            {
                var index = line.IndexOf(StatusSeparator);
                var value = line.Substring(0, index + 1);

                if (int.TryParse(value, out int number))
                {
                    var message = line.Substring(index + 1);
                    switch (message)
                    {
                        case couldReadMessage: updates += number; break;
                        case couldNotReadMessage: errors += number; break;
                        case updateMessage: updates += number; break;
                        case errorMessage: errors += number; break;
                        case unchanged: errors += number; break;
                        default: break;
                    }
                }
            }
#if DEBUG
            else
            {

            }
#endif

        return new ExifResult(output, tags, updates, errors);
    }
    private static void AppendArgumentsBySettings(ExifServiceSettings settings, ref List<string> args, bool isWrite)
    {
        if (settings.IgnoreMinorErrorsAndWarnings) args.Add("-m");

        if (isWrite)
        {
            if (settings.AttemptToFixIncorrectOffsets) args.Add("-F");
            if (settings.ClearBackupFilesOnComplete) args.Add("-overwrite_original");
        }
    }
    #endregion

    #region Behavior-Instance
    public Dictionary<string, string> ReadMetadata(string path)
    {
        return Read(Wrapper, Settings, new List<string> { path }).Tags;
    }
    public Dictionary<string, string> ReadJsonMetadata(string path)
    {
        return ConvertToExifTags(ReadMetadata(path));
    }

    public bool TryWriteMetadata(string path, Dictionary<string, string> tags)
    {
        var args = new List<string> { path };
        args.AddRange(tags.Select(i => $"-{i.Key}={i.Value}"));

        return TryWrite(Wrapper, Settings, args);
    }

    private Dictionary<string, string> ConvertToExifTags(Dictionary<string, string> tags)
    {
        var pairs = new Dictionary<string, string>();
        StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        //JSON
        foreach (var item in tags.Where(tag => Settings.JsonTags.Keys.Contains(tag.Key)))
        {
            if (string.IsNullOrWhiteSpace(item.Value)) continue;

            var value = item.Value;
            if (double.TryParse(item.Value, out var dValue))
            {
                if (item.Key.EndsWith("TimestampMs", comparison)
                    && DateHelper.TryParseDateTimeFromJavaTimeStamp(dValue, out var date))
                    value = DateTimeFormat(date);

                else if (item.Key.EndsWith("Timestamp", comparison)
                    && DateHelper.TryParseDateTimeFromUnixTimeStamp(dValue, out date))
                    value = DateTimeFormat(date);

#if DEBUG
                else if (!item.Key.StartsWith("Geo", comparison))
                {

                }
#endif
            }

            else if (item.Key.EndsWith("Formatted", comparison))
            {
                var formats = new string[] {
                    "MMM dd, yyyy, h:mm:ss tt 'UTC'",
                    "MMM d, yyyy, h:mm:ss tt 'UTC'"
                };

                foreach (var format in formats)
                    if (DateHelper.TryParseDateTime(item.Value, format, out var date))
                    {
                        value = DateTimeFormat(date);
                        break;
                    }
#if DEBUG
                if (!formats.Any(format => DateHelper.TryParseDateTime(item.Value, format, out _)))
                {

                }
#endif
            }
#if DEBUG
            else
            {

            }
#endif

            pairs.TryAdd(Settings.JsonTags[item.Key], value);
        }

        //EXIF
        foreach (var tag in tags.Where(tag => !Settings.JsonTags.Keys.Contains(tag.Key)))
            pairs.TryAdd(tag.Key, tag.Value);

        return pairs;
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

        }

        Wrapper.Dispose();
        disposed = true;
    }
    #endregion

    #region Nested
    private record struct ExifResult(string output, Dictionary<string, string> Tags, int Updates, int Errors);
    #endregion
}
