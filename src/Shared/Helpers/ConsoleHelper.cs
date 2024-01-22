namespace Shared.Helpers;

public static class ConsoleHelper
{
    public static string GenerateProgressBar(long index, long total, long width)
    {
        if (total == default)
        {
            index++;
            total++;
        }
        if (index > total)
            total = index;

        var perc = Math.Round(index / (decimal)total * 100, 2);
        var percText = $"[ {perc}% ";

        if (width < percText.Length + 2)
            return $"{percText}]";

        var done = Math.Max(((width - percText.Length) / (decimal)100 * perc) - 2, default);
        var remain = width - percText.Length - (int)done;

        return $"{percText}{new string('-', (int)done)}{new string(' ', (int)remain - 1)}]";
    }
}
