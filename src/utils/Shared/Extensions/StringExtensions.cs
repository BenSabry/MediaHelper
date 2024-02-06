using System.Text;

namespace Shared.Extensions;

public static class StringExtensions
{
    private const string NumbersAR = "٠١٢٣٤٥٦٧٨٩";
    private const string NumbersEN = "0123456789";

    public static string ReplaceArabicNumbers(this string text)
    {
        var sb = new StringBuilder(text);

        for (int i = 0; i < NumbersAR.Length; i++)
            sb.Replace(NumbersAR[i], NumbersEN[i]);

        return sb.ToString();
    }
    public static bool ContainsArabicNumbers(this string text)
    {
        return NumbersAR.Any(text.Contains);
    }
}
