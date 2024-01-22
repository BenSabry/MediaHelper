namespace Shared.Extensions;

public static class StringExtensions
{
    public static string ReplaceArabicNumbers(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return text
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9');
    }
}
