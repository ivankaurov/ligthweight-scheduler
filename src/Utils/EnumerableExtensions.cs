// -----------------------------------------------------------------------
// <copyright file="EnumerableExtensions.cs" company="Intermedia">
//   Copyright © Intermedia.net, Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lightweight.Scheduler.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source) =>
            source.AsTResult<IEnumerable<T>, IReadOnlyCollection<T>>(s => s.ToList());

        public static ICollection<T> AsCollection<T>(this IEnumerable<T> source) =>
            source.AsTResult<IEnumerable<T>, ICollection<T>>(s => s.ToList());

        internal static TResult AsTResult<TSource, TResult>(this TSource source, Func<TSource, TResult> converter)
            where TResult : TSource =>
            source is TResult res ? res : converter(source);
    }
}