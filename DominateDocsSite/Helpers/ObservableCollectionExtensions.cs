using System.Collections.ObjectModel;

namespace DominateDocsSite.Helpers;

public static class ObservableCollectionExtensions
{
    public static int RemoveWhere<T>(this ObservableCollection<T> source, Func<T, bool> predicate)
    {
        var removed = 0;
        for (int i = source.Count - 1; i >= 0; i--)
        {
            if (predicate(source[i]))
            {
                source.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    public static int FindIndex<T>(this ObservableCollection<T> src, Func<T, bool> pred)
    {
        var i = 0;
        foreach (var x in src) { if (pred(x)) return i; i++; }
        return -1;
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        if (source is null) return null;

        return new ObservableCollection<T>(source);
    }
}