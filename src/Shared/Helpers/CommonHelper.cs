using System.Text;

namespace Shared.Helpers;
public static class CommonHelper
{
    #region Properties
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

    public static void RetryIfFails(Action action, int retryCount = 50, int delayOfRetry = 10)
    {
        while (retryCount-- > 0)
            try
            {
                action();
                return;
            }
            catch (Exception)
            {
                Task.Delay(delayOfRetry).Wait();
            }
    }
    #endregion
}
