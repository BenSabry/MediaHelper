using System.Text;

namespace MediaOrganizer.Helpers;
public class ExifHelper
{
    #region Fields-Static
    private const string ExifTool = "exiftool.exe";
    private const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";
    private const string ExifDefaultCreationTag = "CreateDate";
    private static readonly string[] ExifTargetedDateTimeTags = [ExifDefaultCreationTag, "FileCreateDate", "FileModifyDate"];
    private static readonly string[] SupportedMediaExtensions;

    private const bool IgnoreMinorErrorsAndWarnings = true;

    private static readonly string ToolsDirectory = CommonHelper.ToolsDirectory;
    private static readonly string TempDirectory = Path.Combine(CommonHelper.BaseDirectory, @"Temp\Tools");
    #endregion

    #region Fields-Instance
    private readonly string Id;

    private readonly bool AttemptToFixIncorrectOffsets;
    private readonly bool ClearBackupFilesOnComplete;
    #endregion

    #region Constructors
    static ExifHelper()
    {
        if (!File.Exists(Path.Combine(ToolsDirectory, ExifTool)))
            throw new FileNotFoundException("ExifTool is missing!");

        if (Directory.Exists(TempDirectory))
            Directory.Delete(TempDirectory, true);

        Directory.CreateDirectory(TempDirectory);

        SupportedMediaExtensions =
            ExifExecute(ToolsDirectory, ExifTool, "-T -listf")
            .ToLower()
            .Replace("\r\n", " ")
            .Split(' ')
            .Select(i => $".{i}")
            .ToArray();
    }
    public ExifHelper(bool attemptToFixIncorrectOffsets, bool clearBackupFilesOnComplete)
    {
        AttemptToFixIncorrectOffsets = attemptToFixIncorrectOffsets;
        ClearBackupFilesOnComplete = clearBackupFilesOnComplete;

        Id = $"{Guid.NewGuid().ToString().Replace("-", string.Empty)}.exe";

        File.Copy(Path.Combine(ToolsDirectory, ExifTool),
            Path.Combine(TempDirectory, Id));
    }
    #endregion

    #region Destructor
    ~ExifHelper()
    {
        var path = Path.Combine(TempDirectory, Id);
        if (File.Exists(path))
            File.Delete(path);
    }
    #endregion

    #region Behavior-Static
    private static string ExifExecute(string dir, string id, string arg)
    {
        var output = ProcessHelper.RunAndGetOutput(id, dir, arg);

        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        return output.Trim();
    }
    private static string ExifRead(string id, string args, string path)
    {
        if (IgnoreMinorErrorsAndWarnings) args += " -m";

        return ExifExecute(TempDirectory, id, $"{args} \"{path}\"");
    }
    private static bool TryExifWrite(string id, string args, string path,
        bool attemptToFixIncorrectOffsets, bool clearBackupFilesOnComplete)
    {
        if (attemptToFixIncorrectOffsets) args += " -F";
        if (clearBackupFilesOnComplete) args += " -overwrite_original";

        var output = ExifRead(id, args, path);
        var lines = CommonHelper.SplitStringLines(output);

        var updates = 0;
        var errors = 0;

        const string updateMessage = "image files updated";
        const string errorMessage = "files weren't updated due to errors";
        const char Splitter = ' ';

        foreach (var line in lines)
        {
            var parts = line.Split(Splitter);
            if (!int.TryParse(parts[0], out int value))
                continue;

            var message = line.Remove(0, parts[0].Length + 1);
            switch (message)
            {
                case updateMessage: updates = value; break;
                case errorMessage: errors = value; break;
                default: break;
            }
        }

        return updates > 0 && errors == 0;
    }

    private static string DateTimeFormat(DateTime dateTime)
    {
        return dateTime.ToString(ExifDateFormat);
    }
    public static bool IsSupportedMediaFile(FileInfo file)
    {
        return SupportedMediaExtensions.Contains(file.Extension.ToLower());
    }
    public static bool IsSupportedMediaFile(string fileName)
        => SupportedMediaExtensions.Any(ext =>
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    public static string ClearBackupFiles(params string[] sources)
    {
        if (sources is null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var src in sources)
            if (string.IsNullOrWhiteSpace(src) && Directory.Exists(src))
                builder.AppendLine(ClearBackupFiles(new DirectoryInfo(src)));

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
    private static string ClearBackupFiles(DirectoryInfo directory)
    {
        var builder = new StringBuilder();
        foreach (var dir in directory.GetDirectories())
            builder.AppendLine(ClearBackupFiles(dir));

        return builder.AppendLine(ExifExecute(ToolsDirectory, ExifTool,
            $"-overwrite_original -delete_original! \"{directory.FullName}\""))
            .ToString();
    }
    public static string ReadVersion()
    {
        return ExifExecute(ToolsDirectory, ExifTool, "-ver");
    }
    #endregion

    #region Behavior-Instance
    //private string GenericReadTagValue(string path, string tag)
    //{
    //    TryReadTagValue(path, tag, out var value);
    //    return value;
    //}
    //private bool TryReadTagValue(string path, string tag, out string value)
    //{
    //    const char splitter = ':';
    //    var tags = ExifRead(Id, "-s", path)
    //        .Split("\n").Select(i => i.Split(splitter))
    //        .Where(i => i[0].Trim().Equals(tag, StringComparison.OrdinalIgnoreCase));

    //    if (tags.Any())
    //    {
    //        value = tags.Select(i => string.Join(splitter, i.Skip(1)).Trim()).First();
    //        return true;
    //    }

    //    value = string.Empty;
    //    return false;
    //}

    //public bool TryReadMinimumDate(string path, out DateTime result)
    //{
    //    result = ReadMinimumDate(path);
    //    return result != default;
    //}
    //public DateTime ReadMinimumDate(string path)
    //    => ReadAllDates(path).Where(DateHelper.IsValidDateTime).MinOrDefault();
    public DateTime[] ReadAllDates(string path)
    {
        const string args = "-T -AllDates";
        const char Separator = '\t';

        return ExifRead(Id, args, path)
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Select(DateHelper.ExtractAllPossibleDateTimes)
            .SelectMany()
            .Distinct()
            .ToArray();
    }
    public DateTime[] ReadAllDatesFromJson(FileInfo jsonFile)
    {
        return [];

        //if (!jsonFile.Exists) return [];

        //var dates = ReadAllDates(jsonFile.FullName);
        //if (dates.Any())
        //{

        //}

        //return [];


        //#region CreateTemps
        //var f1 = new FileInfo(mediaFile.FullName);
        //var dest1 = Path.Combine(
        //    f1.FullName.Remove(f1.FullName.Length - f1.Name.Length),
        //    $"{Guid.NewGuid()}{f1.Extension}");
        //var file = f1.CopyTo(dest1, true);

        //        var f2 = new FileInfo(jsonFile.FullName);
        //        var dest2 = Path.Combine(
        //            f2.FullName.Remove(f2.FullName.Length - f2.Name.Length),
        //            $"{Guid.NewGuid()}{f2.Extension}");
        //        var json = f2.CopyTo(dest2, true);
        //        #endregion

        //        /*
        //        FileModifyDate                  : 2023:11:03 00:49:32+02:00
        //FileAccessDate                  : 2024:01:04 00:36:36+02:00
        //FileCreateDate                  : 2024:01:04 00:31:05+02:00

        //        */

        //        var jd = ExifRead(Id, "-a -s", json.FullName);
        //        var fd = ExifRead(Id, "-s -T -AllDates -FileModifyDate -FileAccessDate -FileCreateDate", json.FullName);

        //        var dates = ReadAllDates(json.FullName);

        //        throw new NotImplementedException();


        //var o1 = ExifRead(Id, "-s", file.FullName);
        //var o2 = ExifRead(Id, "", file.FullName);

        //var date = ExifRead(Id, $"-T -{ExifDefaultCreationTag}", file.FullName);
        //var rDate = ReadMediaDefaultCreationDate(file.FullName);

        //var allDates = ExifRead(Id, "-T -AllDates", file.FullName);
        //var dates = ReadAllDates(file.FullName);

        //var nDate = dates.FirstOrDefault();
        //var uDate = ReadMediaDefaultCreationDate(file.FullName);




        //var dates = new DateTime[][]
        //{
        //    //ReadAllDates(mediaFile.FullName),
        //    ReadAllDates(jsonFile.FullName),
        //}
        //.SelectMany()
        //.Distinct()
        //.ToArray();

        //if (dates.Any())
        //{

        //}

        //var jd = ExifRead(Id, "-a -s -json:all", jsonFile.FullName);
        //var fd = ExifRead(Id, "-s", jsonFile.FullName);
        //var dates 
        //return;

        //        var x = GenericReadTagValue(f2.FullName, "CreationTimeFormatted");

        //        /*
        //         -tagsfromfile "%d/%F.json" "-GPSAltitude<GeoDataAltitude" "-GPSLatitude<GeoDataLatitude"
        //"-GPSLatitudeRef<GeoDataLatitude" "-GPSLongitude<GeoDataLongitude" "-GPSLongitudeRef<GeoDataLongitude"
        //-Description -d %s "-Alldates<PhotoTakenTimeTimestamp"
        //        */


        //        var jd = ExifRead(Id, "-a -s -json:all", json.FullName);
        //        var fd = ExifRead(Id, "-s", file.FullName);

        //        var fileDateOrig = ReadMediaDefaultCreationDate(file.FullName).AddDays(1).AddMonths(1);
        //        TryUpdateMediaTargetedDateTime(file.FullName, fileDateOrig);

        //        var d2 = ReadMediaDefaultCreationDate(file.FullName);
        //        if (fileDateOrig == d2)
        //        {

        //        }
        //        else
        //        {

        //        }



        //        //var o1 = ExifRead(Id, "-s", file.FullName);
        //        //var o2 = ExifRead(Id, "", file.FullName);

        //        //var date = ExifRead(Id, $"-T -{ExifDefaultCreationTag}", file.FullName);
        //        //var rDate = ReadMediaDefaultCreationDate(file.FullName);

        //        //var allDates = ExifRead(Id, "-T -AllDates", file.FullName);
        //        //var dates = ReadAllDates(file.FullName);

        //        //var nDate = dates.FirstOrDefault();
        //        //TryUpdateMediaTargetedDateTime(file.FullName, nDate);
        //        //var uDate = ReadMediaDefaultCreationDate(file.FullName);

        //        file.Delete();
        throw new NotImplementedException();
    }
    //public bool TryReadMediaDefaultCreationDate(string path, out DateTime dateTime)
    //{
    //    dateTime = ReadMediaDefaultCreationDate(path);
    //    return DateHelper.IsValidDateTime(dateTime);
    //}
    public DateTime ReadMediaDefaultCreationDate(string path)
    {
        DateHelper.TryExtractMinimumValidDateTime(
            ExifRead(Id, $"-T -{ExifDefaultCreationTag}", path),
            out var dateTime);

        return dateTime;
    }
    public bool TryUpdateMediaTargetedDateTime(string path, DateTime dateTime)
    {
        var formatedDateTime = DateTimeFormat(dateTime);

        const char sperator = ' ';
        var args = new string(
            string.Join(sperator,
                ExifTargetedDateTimeTags.Select(i => $"\"-{i}={formatedDateTime}\""))
                .ToArray());

        return TryExifWrite(Id, args, path, AttemptToFixIncorrectOffsets, ClearBackupFilesOnComplete);
    }

    internal static void CleanUp()
    {
        if (Directory.Exists(TempDirectory))
            Directory.Delete(TempDirectory, true);
    }
    #endregion
}
