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

    #region Behavior-Instance
    public Dictionary<string, string> ReadMetadata(string path) => Wrapper.ReadMetadata(path);
    public Dictionary<string, string> ReadJsonMetadata(string path) => ConvertToExifTags(ReadMetadata(path));
    public bool TryWriteMetadata(string path, Dictionary<string, string> tags) => Wrapper.TryWriteMetadata(path, tags);

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
}
