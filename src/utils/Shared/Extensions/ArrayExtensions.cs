namespace Shared.Extensions;

public static class ArrayExtensions
{
    public static T[] Fill<T>(this T[] array) where T : new()
    {
        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < array.Length; i++)
            array[i] = new T();

        return array;
    }
}
