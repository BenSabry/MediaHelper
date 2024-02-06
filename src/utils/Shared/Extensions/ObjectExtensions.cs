using System.Reflection;

namespace Shared.Extensions;

public static class GenericExtensions
{
    public static T Clone<T>(this T o, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        => (T)ObjectExtensions.CloneObject(o, flags);
}
public static class ObjectExtensions
{
    public static void Dispose(this object? o)
    {
        if (o is null) return;
        else if (o is string) return;
        else if (o is IDisposable disposable)
        {
            disposable.Dispose();
            return;
        }

        var t = o.GetType();

        if (typeof(IEnumerable<>).IsAssignableFrom(t))
            Parallel.ForEach((IEnumerable<object>)o, Dispose);

        else
        {
            Parallel.ForEach(t.GetProperties(), i => { Dispose(i.GetValue(o)); });
            Parallel.ForEach(t.GetFields(), i => { Dispose(i.GetValue(o)); });
        }
    }

    internal static object? CloneObject(in object? o, BindingFlags flags)
    {
        if (o is null) return o;
        else if (o is string) return o;

        var t = o.GetType();
        if (t.IsValueType) return o;

        if (typeof(IEnumerable<>).IsAssignableFrom(t))
            return ((IEnumerable<object>)o).Select(item => CloneObject(item, flags));

        var copy = Activator.CreateInstance(t, true);

        foreach (var i in t.GetProperties()) i.SetValue(copy, CloneObject(i.GetValue(o), flags));
        foreach (var i in t.GetFields()) i.SetValue(copy, CloneObject(i.GetValue(o), flags));

        return copy;
    }
}
