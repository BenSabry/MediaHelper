namespace Shared.Extensions;

public static class CharExtensions
{
    public static bool IsNumber(this char c) => c >= '0' && c <= '9';
    public static bool IsLetter(this char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    public static bool NotNumberOrLetter(this char c) => !IsNumber(c) && !IsLetter(c);
}
