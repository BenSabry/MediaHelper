using System.Text;

namespace MediaOrganizer.Helpers;
public static class CommonHelper
{
    #region Properties
    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public static readonly string ToolsDirectory = Path.Combine(BaseDirectory, "Tools");
    public static readonly string TempDirectory = Path.Combine(BaseDirectory, "Temp");

    private static readonly char[] separator = ['\r', '\n'];
    #endregion

    #region Behavior
    public static string FormatNumberToLength(int number, int length)
    {
        return FormatToLength(number.ToString(), length, '0');
    }
    public static string FormatToLength(string text, int length, char separator)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder();
        for (int i = 0; i < length - text.Length; i++)
            builder.Append(separator);

        return builder.Append(text).ToString();
    }
    public static string[] SplitStringLines(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? [] : text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim()).ToArray();
    }
    #endregion
}
