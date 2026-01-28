namespace DominateDocsData.Helpers;

public static class EnumerableExtensions
{
    public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        var i = 0;
        foreach (var x in source)
        {
            if (predicate(x)) return i;
            i++;
        }
        return -1;
    }
}