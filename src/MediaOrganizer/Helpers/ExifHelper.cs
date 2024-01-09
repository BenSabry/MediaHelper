using System.Text;

namespace MediaOrganizer.Helpers;
public class ExifHelper
{
    #region Fields-Static
    private const string ExifTool = "exiftool.exe";
    private const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";
    private const char ExifTagsWriteSeparator = ' ';
    private const string ExifDefaultCreationTag = "CreateDate";
    private const string ExifReadAllDatesArgs = $"-T -AllDates -FileCreateDate -FileModifyDate -FileAccessDate -{ExifDefaultCreationTag}";

    private static readonly string[] ExifTargetedDateTimeTags = [ExifDefaultCreationTag, "FileCreateDate", "FileModifyDate"];
    private static readonly string ExifWriteAllDatesValuePlaceHolder = Guid.NewGuid().ToString();
    private static readonly string ExifWriteAllDatesArgs;
    private static readonly string[] SupportedMediaExtensions;

    private const bool IgnoreMinorErrorsAndWarnings = true;
    private static readonly object GlobalLock = new object();

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

        ExifWriteAllDatesArgs = new string(string.Join(ExifTagsWriteSeparator,
                ExifTargetedDateTimeTags.Select(i => $"\"-{i}={ExifWriteAllDatesValuePlaceHolder}\""))
                .ToArray());
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
        lock (GlobalLock)
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
    public DateTime[] ReadAllDates(string path)
    {
        const char Separator = '\t';
        return ExifRead(Id, ExifReadAllDatesArgs, path)
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Select(DateHelper.ExtractAllPossibleDateTimes)
            .SelectMany()
            .Distinct()
            .ToArray();
    }
    public DateTime[] ReadAllDatesFromJson(FileInfo jsonFile) => ReadAllDates(jsonFile.FullName);
    public bool TryUpdateMediaTargetedDateTime(string path, DateTime dateTime)
    {
        return TryExifWrite(Id, ExifWriteAllDatesArgs.Replace(ExifWriteAllDatesValuePlaceHolder, DateTimeFormat(dateTime)),
            path, AttemptToFixIncorrectOffsets, ClearBackupFilesOnComplete);
    }

    internal static void CleanUp()
    {
        lock (GlobalLock)
            if (Directory.Exists(TempDirectory))
                Directory.Delete(TempDirectory, true);
    }
    #endregion
}
