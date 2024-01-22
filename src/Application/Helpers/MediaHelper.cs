namespace Application.Helpers;
public static class MediaHelper
{
    #region Fields-Static
    private static string[] SupportedMediaExtensions => ["jpg", "jpeg", "gif", "tif", "tiff", "bmp", "aac", "m4a", "mp3", "wav", "wma", "ac3", "dts", "aif", "aiff", "asf", "flac", "adp", "dsf", "dff", "l16", "l24", "ogg", "oga", "mpg", "mpeg", "vob", "mp4", "m4v", "avi", "mov", "qt", "mts", "m2ts", "mkv"];
    #endregion

    #region Behavior-Static
    public static bool IsSupportedMediaFile(FileInfo file) => SupportedMediaExtensions.Contains(file.Extension.ToLower());
    public static bool IsSupportedMediaFile(string fileName) => SupportedMediaExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    #endregion
}
