using Shared.Extensions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Helpers;
public static class DateHelper
{
    //TODO: try to make it IoC compatable
    #region Fields-Static
    private const string RegexOptionalChar = "(.?)";
    private const int RegexTimeoutMilliseconds = 100;

    private static readonly Regex[] NumericsRegexes = GetNumericsRegexes();
    private static readonly Regex[] InvalidRegexes = GetInvalidRegexes();
    private static readonly (Regex regex, string format)[] FormatedRegexes = GetFormatedRegex();
    #endregion

    #region Behavior-Public
    public static DateTime[] ExtractPossibleDateTimes(string s)
    {
        if (MatchesInvalidRegex(s)) return [];

        var formated = MatchFormatedRegex(s);
        if (formated.Valid)
            return [formated.DateTime];

        //var numeric = MatchNumericRegex(s);
        //if (numeric.Valid)
        //    return [numeric.DateTime];

        return [];
    }

    public static DateTime ParseDateTime(string s, string format)
    {
        return DateTime.ParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
    public static bool TryParseDateTime(string s, string format, out DateTime result)
    {
        return DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    public static bool TryParseDateTimeUsingGeneratedRegex(string s, string format, out DateTime dateTime)
    {
        var regex = new string[] { format }
            .Select(AddOptionalCharBetweenFormatParts)
            .Select(GenerateRegexPatternByDateFormat)
            .Select(CreateRegex)
            .First();

        var match = regex.Match(s);
        if (match.Success)
        {
            var clearFormat = RecoverOptionalCharsInFormatUsingValue(format, match.Value);
            if (TryParseDateTime(s, clearFormat, out dateTime))
                return true;
        }

        dateTime = default;
        return false;
    }
    public static bool TryParseDateTimeFromNumber(string s, out DateTime dateTime)
    {
        if (double.TryParse(s, out var dResult) &&
            (TryParseDateTimeFromJavaTimeStamp(dResult, out dateTime)
            || TryParseDateTimeFromUnixTimeStamp(dResult, out dateTime)))
            return true;

        //if (long.TryParse(s, out var lResult) && (
        //    TryParseDateTimeFromTicks(lResult, out dateTime)
        //    || TryParseDateTimeFromBinary(lResult, out dateTime)
        //    || TryParseDateTimeFromFileTimeUtc(lResult, out dateTime)))
        //    return true;

        dateTime = default;
        return false;
    }

    public static DateTime ParseDateTimeFromUnixTimeStamp(double timestamp)
    {
        return DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
    }
    public static DateTime ParseDateTimeFromJavaTimeStamp(double timestamp)
    {
        return DateTime.UnixEpoch.AddMilliseconds(timestamp).ToLocalTime();
    }

    public static bool TryParseDateTimeFromUnixTimeStamp(double timestamp, out DateTime value)
    {
        try
        {
            value = ParseDateTimeFromUnixTimeStamp(timestamp);
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }
    public static bool TryParseDateTimeFromJavaTimeStamp(double timestamp, out DateTime value)
    {
        try
        {
            value = ParseDateTimeFromJavaTimeStamp(timestamp);
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }
    public static bool TryParseDateTimeFromTicks(long ticks, out DateTime value)
    {
        try
        {
            value = new DateTime(ticks);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    public static bool TryParseDateTimeFromBinary(long binary, out DateTime value)
    {
        try
        {
            value = DateTime.FromBinary(binary);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    public static bool TryParseDateTimeFromFileTimeUtc(long fileTime, out DateTime value)
    {
        try
        {
            value = DateTime.FromFileTimeUtc(fileTime);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    #endregion

    #region Behavior-Private
    private static bool IsAcceptableDateTime(DateTime date)
    {
        //TODO make it more accurate
        return date < DateTime.Now && date.Year > 1900;
    }

    //TODO: extract regex related methods to separate class
    private static Regex[] GetNumericsRegexes()
    {
        return [CreateRegex(@"\d{4,}")];
    }
    private static Regex[] GetInvalidRegexes()
    {
        //return [CreateRegex(@"(^FB_IMG_)(\d+)"), CreateRegex(@"(^received_)(\d+)")];
        return Array.Empty<Regex>();
    }
    private static (Regex regex, string format)[] GetFormatedRegex()
    {
        return new string[]
            {
                "yyyyMMddHHmmsszzz",
                "yyyyMMddHHmmss",
                "ddMMMYYYYHHmmss",
                "yyyyMMdd",
            }
            .Select(AddOptionalCharBetweenFormatParts)
            .Select(i => (CreateRegex(GenerateRegexPatternByDateFormat(i)), i))
            .ToArray();
    }
    private static Regex CreateRegex(string s)
    {
        return new Regex(s, RegexOptions.Compiled,
            new TimeSpan(0, 0, 0, 0, RegexTimeoutMilliseconds));
    }
    private static bool MatchesInvalidRegex(string s)
    {
        return TryMatchAnyRegex(s, InvalidRegexes, out var _);
    }
    private static (bool Valid, DateTime DateTime) MatchFormatedRegex(string s)
    {
        foreach (var i in FormatedRegexes)
        {
            var match = i.regex.Match(s);
            if (match.Success &&
                TryParseDateTime(match.Value, RecoverOptionalCharsInFormatUsingValue(i.format, match.Value), out var dateTime)
                && IsAcceptableDateTime(dateTime))

                return (true, dateTime);
        }

        return (false, default);
    }
    private static (bool Valid, DateTime DateTime) MatchNumericRegex(string s)
    {
        while (TryMatchAnyRegex(s, NumericsRegexes, out Match match))
        {
            if (TryParseDateTimeFromNumber(match.Value, out var dateTime)
                && IsAcceptableDateTime(dateTime))
                return (true, dateTime);

            s = s.Replace(match.Value, string.Empty, StringComparison.Ordinal);
        }

        return (false, default);
    }
    private static bool TryMatchAnyRegex(string s, Regex[] regexes, out Match result)
    {
        foreach (var regex in regexes)
        {
            result = regex.Match(s);
            if (result.Success)
                return true;
        }

        result = Match.Empty;
        return false;
    }

    private static string GenerateRegexPatternByDateFormat(string format)
    {
        const string millisecond = @"(\\d{3})";
        const string minute = @"(0[0-9]|[1-5][0-9])";
        const string hour24 = @"(0[0-9]|1[0-9]|2[0-3])";
        const string hour12 = @"([0-9]|0[0-9]|1[0-2])";
        const string dayNumber = @"([1-9]|0[1-9]|[12][0-9]|3[01])";
        const string dayName = @"((?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?)";
        const string monthNumber = @"(?:0[1-9]|1[0-2])";
        const string monthName = @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)";
        const string year = @"((19|20)\d\d)";
        const string zone = @"([+-]\d{2}:\d{2})";
        const string clock = @"(AM|PM|am|pm)";

        var pairs = new (string Match, string Pattern, bool CaseSensitive)[]
        {
            //process days first because it overwites \d in regex
            ("DD", dayNumber, false),
            ("d", dayNumber, false),

            ("YYYY", year, false),
            ("MMM", monthName, false),
            ("MM", monthNumber, true),
            ("DDD", dayName, false),
            ("HH", hour24, false),
            ("h", hour12, false),
            ("mm", minute, true),
            ("ss", minute, false),
            ("fff", millisecond, false),
            ("zzz", zone, false),
            ("tt", clock, false)
        };

        foreach (var item in pairs)
            format = format.Replace(item.Match, item.Pattern, item.CaseSensitive
                ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        return format;
    }
    private static string AddOptionalCharBetweenFormatParts(string s)
    {
        var sb = new StringBuilder(s);

        int l = 0, r;
        while (l < sb.Length)
        {
            r = l;
            while (r < sb.Length)
                if (sb[l] == sb[r]) r++;
                else break;

            if (r == sb.Length)
                break;

            sb.Insert(r, RegexOptionalChar);
            l = r + RegexOptionalChar.Length;
        }

        //special cases
        const string specials = ", -\'_";
        foreach (var c in specials)
            sb.Replace($"{RegexOptionalChar}{c}", RegexOptionalChar);

        return sb.ToString();
    }
    private static string RecoverOptionalCharsInFormatUsingValue(string format, string value)
    {
        var sb = new StringBuilder(format);

        int left = 0, right;
        while (left < sb.Length)
        {
            //search (regex optional char)
            right = 0;
            while (left + right < sb.Length && right < RegexOptionalChar.Length)
                if (sb[left + right] == RegexOptionalChar[right]) right++;
                else break;

            //means (regex optional char) found
            if (right == RegexOptionalChar.Length)
            {
                sb.Remove(left, RegexOptionalChar.Length);
                if (value[left].NotNumberOrLetter())
                    sb.Insert(left, value[left]);
            }

            left++;
        }

        //special cases
        if ("+-".Contains(sb[sb.Length - 4]))
            sb.Remove(sb.Length - 4, 1);

        return sb.ToString();
    }
    #endregion
}
