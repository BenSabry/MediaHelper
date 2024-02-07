using Application.Infrastructure.Services;
using Domain.Interfaces;
using Infrastructure.Wrappers;
using Shared.Extensions;
using Shared.Helpers;
using Wrappers;

namespace Infrastructure.Services;

internal class ExifBaseService : IExifCoreService
{
    #region Fields
    public string ToolVersion { get; private init; }
    public string WatcherVersion { get; private init; }

    private readonly ExifServiceSettings Settings;
    private readonly string[] SupportedMediaExtensions;
    #endregion

    #region Constructors
    public ExifBaseService(ISettings settings, ExifServiceSettings exifSettings, ExifWatcherWrapper watcher)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(exifSettings);
        ArgumentNullException.ThrowIfNull(watcher);

        Settings = exifSettings;
        ToolVersion = ExifToolWrapper.GetVersion();
        WatcherVersion = watcher.Version;
        watcher.StartWatching();

        SupportedMediaExtensions = GetSupportedFileExtensions();
    }
    #endregion

    #region Behavior-Static
    private static string[] GetSupportedFileExtensions()
    {
        const string Temp = "supported file extensions:";
        return ExifToolWrapper.InstaExecute("-listf")
            .Replace(Temp, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split("\n").SelectMany(i => i.Split(" ")).Select(i => i.Trim())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => $".{i.ToLower()}")
            .ToArray();
    }
    #endregion

    #region Behavior-Instance
    public void ClearBackupFiles(params string[] sources) => ExifToolWrapper.DeleteOriginal(sources);
    public bool IsSupportedMediaFile(FileInfo file)
    {
        return SupportedMediaExtensions.Contains(file.Extension.ToLower());
    }
    public bool IsSupportedMediaFile(string fileName)
    {
        return SupportedMediaExtensions.Any(ext =>
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public Dictionary<string, string> FilterValues(Dictionary<string, string> tags)
    {
        throw new NotImplementedException();
    }

    public bool TryUpdateCreationDateTagsWithMinAcceptableValue(
        ref Dictionary<string, string> tags, DateTime[] others, out DateTime min)
    {
#if DEBUG
        if (others.Length == default)
        {

        }
#endif

        var dates = new List<DateTime>(others);

        foreach (var tag in Settings.AllDatesTags)
            if (tags.TryGetValue(tag, out var value))
                foreach (var format in Settings.ExifDateReadFormats)
                    if (DateHelper.TryParseDateTime(value, format, out var date))
                    {
                        dates.Add(date);
                        break;
                    }

        if (dates.Count == default)
        {
            min = default;
            return false;
        }

        min = dates.Min();
        var formated = DateTimeFormat(min);

        foreach (var tag in Settings.CreationDateTags)
            if (!tags.TryAdd(tag, formated))
                tags[tag] = formated;

        return true;
    }

    public string DateTimeFormat(DateTime dateTime) => ExifToolWrapper.FormatDateTime(dateTime);
    #endregion
}
