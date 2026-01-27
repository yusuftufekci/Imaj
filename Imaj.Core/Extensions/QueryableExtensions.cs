using System;
using System.Linq;
using System.Linq.Expressions;

namespace Imaj.Core.Extensions
{
    /// <summary>
    /// IQueryable için yardımcı extension method'lar.
    /// Filter logic'i daha okunabilir hale getirir.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Koşul true ise Where filtresi uygular, değilse query'yi olduğu gibi döner.
        /// </summary>
        /// <example>
        /// query.WhereIf(!string.IsNullOrEmpty(filter.Name), x => x.Name.Contains(filter.Name))
        /// </example>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return condition ? query.Where(predicate) : query;
        }

        /// <summary>
        /// String değer boş değilse Where filtresi uygular.
        /// </summary>
        /// <example>
        /// query.WhereIfNotEmpty(filter.Code, x => x.Code.Contains(filter.Code))
        /// </example>
        public static IQueryable<T> WhereIfNotEmpty<T>(
            this IQueryable<T> query,
            string? value,
            Expression<Func<T, bool>> predicate)
        {
            return query.WhereIf(!string.IsNullOrWhiteSpace(value), predicate);
        }

        /// <summary>
        /// Nullable değer null değilse Where filtresi uygular.
        /// </summary>
        public static IQueryable<T> WhereIfHasValue<T, TValue>(
            this IQueryable<T> query,
            TValue? value,
            Expression<Func<T, bool>> predicate) where TValue : struct
        {
            return query.WhereIf(value.HasValue, predicate);
        }

        /// <summary>
        /// Sayfalama uygular (Skip/Take).
        /// </summary>
        /// <param name="page">Sayfa numarası (1-based)</param>
        /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
        public static IQueryable<T> Paginate<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
