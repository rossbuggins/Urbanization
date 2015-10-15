using System.Collections.Generic;
using System.Linq;

namespace Mirage.Urbanization
{
    public static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => new HashSet<T>(enumerable);

        public static IEnumerable<IEnumerable<T>> GetBatched<T>(this IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            while (true)
            {
                var currentBatch = new List<T>();
                foreach (var iteration in Enumerable.Range(0, 150))
                {
                    if (enumerator.MoveNext())
                        currentBatch.Add(enumerator.Current);
                    else
                    {
                        yield return currentBatch;
                        yield break;
                    }
                }
                yield return currentBatch;
            }
        } 
    }
}