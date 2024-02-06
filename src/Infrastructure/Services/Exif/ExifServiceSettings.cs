using Domain.Interfaces;
using Shared.Extensions;

namespace Infrastructure.Services;
internal class ExifServiceSettings
{
    #region Fields
    public string[] ExifDateReadFormats { get; private init; } = ["yyyy:MM:dd HH:mm:sszzz", "yyyy:MM:dd HH:mm:ss"];
    public string ExifDateFormat { get; private init; } = "yyyy:MM:dd HH:mm:sszzz";

    public bool AttemptToFixIncorrectOffsets { get; private init; }
    public bool ClearBackupFilesOnComplete { get; private init; }
    public bool IgnoreMinorErrorsAndWarnings { get; private init; } = true;

    public string[] AllDatesTags { get; private init; }
    public string[] CreationDateTags { get; } =
    [
        "FileCreateDate",
        "DateTimeOriginal",
        "CreateDate",
        "SubSecCreateDate",
        "DateTimeDigitized"
    ];
    public string[] OtherDateTags { get; } =
    [
        "FileAccessDate",
        "FileModifyDate",
        "ModifyDate",
        "SubSecModifyDate",
        "SubSecDateTimeOriginal",
        "TimeStamp",
        "AllDates"
    ];
    public string[] IgnoredTags { get; } =
    [
        "ExifToolVersion",
        "FileType",
        "FileSize",
        "Directory",
        "FileTypeExtension",
        "FilePermissions",
        "Title",
        "Description",
        "ImageViews",
        "People",
        "Url",
        "SharedAlbumComments",
        "GooglePhotos",
        "UploadStatus",
        "SizeBytes",
        "Filename",
        "HasOriginalBytes",
        "ContentVersion",
        "MimeType",
        "Archived",
        "Favorited",
        "Liked",
    ];

    public IReadOnlyDictionary<string, string> JsonTags { get; set; }
    private readonly (string ExifTag, string[] JsonTags)[] exifJsonTags =
    [
        ("FileCreateDate", ["CreationTimestampMs", "CreationTimeFormatted", "CreationTimeTimestamp"]),
        ("DateTimeOriginal", ["PhotoTakenTimeFormatted", "PhotoTakenTimeTimestamp"]),
        ("GPSAltitude", ["GeoDataAltitude", "GeoDataExifAltitude"]),
        ("GPSLatitude", ["GeoDataLatitude", "GeoDataLatitudeSpan", "GeoDataExifLatitude", "GeoDataExifLatitudeSpan"]),
        ("GPSLongitude", ["GeoDataLongitude", "GeoDataLongitudeSpan", "GeoDataExifLongitude", "GeoDataExifLongitudeSpan"]),
    ];
    #endregion

    #region Constructors
    public ExifServiceSettings(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AllDatesTags = new string[][] { CreationDateTags, OtherDateTags }.SelectMany().ToArray();

        AttemptToFixIncorrectOffsets = settings.AttemptToFixIncorrectOffsets;
        ClearBackupFilesOnComplete = settings.ClearBackupFilesOnComplete;

        JsonTags = new Dictionary<string, string>(exifJsonTags.Select(exif =>
            exif.JsonTags.Select(json => new KeyValuePair<string, string>(json, exif.ExifTag))).SelectMany());
    }
    #endregion
}
