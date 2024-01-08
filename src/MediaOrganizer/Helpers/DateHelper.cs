using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace MediaOrganizer.Helpers;
public static class DateHelper
{
    #region Fields-Static
    private const string RegexOptionalChar = "(.?)";
    private const int RegexTimeoutMilliseconds = 100;

    private static readonly Func<string, (bool Valid, DateTime Value)>[] Parsers = GetParsers();
    private static readonly Regex[] NumericsRegexes = GetNumericsRegexes();
    private static readonly Regex[] InvalidRegexes = GetInvalidRegexes();
    private static readonly (Regex regex, string format)[] FormatedRegexes = GetFormatedRegex();
    #endregion

    #region Behavior
    public static bool TryExtractMinimumValidDateTime(string s, out DateTime result)
    {
        var dates = ExtractAllPossibleDateTimes(s)
            .Where(IsValidDateTime);

        if (dates.Any())
        {
            result = dates.Min();
            return true;
        }

        result = default;
        return false;
    }
    public static DateTime[] ExtractAllPossibleDateTimes(string s)
    {
        if (MatchesInvalidRegex(s)) return [];

        return Parsers
            .Select(parser => parser(s))
            .Where(i => i.Valid)
            .Select(i => i.Value)
            .Distinct()
            .ToArray();
    }
    public static bool TryParseDateTime(string s, string format, out DateTime result)
    {
        return DateTime.TryParseExact(s, format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
    }
    public static bool IsValidDateTime(DateTime date)
    {
        //TODO make it more accurate
        return date < DateTime.Now && date.Year > 1900;
    }
    private static bool IsValidDateTime(long number)
    {
        if (number < DateTime.MinValue.Ticks || number > DateTime.MaxValue.Ticks)
            return false;

        try
        {
            var date = new DateTime(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        try
        {
            var date = DateTime.FromBinary(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        try
        {
            var date = DateTime.FromFileTimeUtc(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        return false;
    }

    private static Func<string, (bool Valid, DateTime Value)>[] GetParsers()
    {
        return [MatchFormatedRegex, MatchNumericRegex];
    }
    private static Regex[] GetNumericsRegexes()
    {
        return [CreateRegex(@"\d+")];
    }
    private static Regex[] GetInvalidRegexes()
    {
        return [CreateRegex(@"(^FB_IMG_)(\d+)"), CreateRegex(@"(^received_)(\d+)")];
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
    private static (bool, DateTime) MatchFormatedRegex(string s)
    {
        foreach (var i in FormatedRegexes)
        {
            var match = i.regex.Match(s);
            if (match.Success &&
                TryParseDateTime(match.Value,
                RecoverOptionalCharsInFormatUsingValue(i.format, match.Value),
                out var date))

                return (true, date);
        }

        return (false, default);
    }
    private static (bool, DateTime) MatchNumericRegex(string s)
    {
        while (TryMatchAnyRegex(s, NumericsRegexes, out Match match))
        {
            if (long.TryParse(match.Value, out var res) && IsValidDateTime(res))
                return (true, new DateTime(res));

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
        const string hour = @"(0[0-9]|1[0-9]|2[0-3])";
        const string dayNumber = @"(0[1-9]|[12][0-9]|3[01])";
        const string dayName = @"((?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?)";
        const string monthNumber = @"(?:0[1-9]|1[0-2])";
        const string monthName = @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)";
        const string year = @"((19|20)\d\d)";
        const string zone = @"([+-]\d{2}:\d{2})";

        var pairs = new (string Match, string Pattern, bool CaseSensitive)[]
        {
            ("YYYY", year, false),
            ("MMM", monthName, false),
            ("MM", monthNumber, true),
            ("DDD", dayName, false),
            ("DD", dayNumber, false),
            ("HH", hour, false),
            ("mm", minute, true),
            ("ss", minute, false),
            ("fff", millisecond, false),
            ("zzz", zone, false)
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

        //exceptional case
        if ("+-".Contains(sb[sb.Length - 4]))
            sb.Remove(sb.Length - 4, 1);

        return sb.ToString();
    }
    #endregion
}
