namespace Kudiyarov.Invest.Common.Extensions;

public static class EnumerableExtensions
{
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
    {
        var result = source as IReadOnlyCollection<T> ?? source.ToList();
        return result;
    }
}