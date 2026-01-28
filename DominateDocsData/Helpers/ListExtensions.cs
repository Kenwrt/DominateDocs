namespace DominateDocsData.Helpers;

public static class ListExtensions
{
    /// <summary>
    /// Removes an item at index if the index is valid. Returns true if removed.
    /// </summary>
    public static bool RemoveAtSafe<T>(this List<T> list, int index)
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (index < 0 || index >= list.Count) return false;

        list.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the first item matching the predicate. Returns true if removed.
    /// </summary>
    public static bool RemoveWhere<T>(this List<T> list, Func<T, bool> predicate)
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        var index = list.FindIndex(x => predicate(x));
        if (index < 0) return false;

        list.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes all items matching the predicate. Returns the number removed.
    /// </summary>
    public static int RemoveAllWhere<T>(this List<T> list, Func<T, bool> predicate)
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        return list.RemoveAll(x => predicate(x));
    }

    /// <summary>
    /// Removes the first item whose id (selected by idSelector) equals the given id. Returns true if removed.
    /// </summary>
    public static bool RemoveById<T, TId>(this List<T> list, TId id, Func<T, TId> idSelector)
        where TId : notnull
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (idSelector is null) throw new ArgumentNullException(nameof(idSelector));

        var comparer = EqualityComparer<TId>.Default;
        var index = list.FindIndex(x => comparer.Equals(idSelector(x), id));
        if (index < 0) return false;

        list.RemoveAt(index);
        return true;
    }
}