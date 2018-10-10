namespace Lightweight.Scheduler.Abstractions.Identities
{
    using System;
    using System.Collections.Generic;

    public sealed class DefaultEqualityComparer<T> : IEqualityComparer<T>
    {
        public static readonly DefaultEqualityComparer<T> Instance = new DefaultEqualityComparer<T>();

        private static readonly bool IsEquatable = typeof(IEquatable<T>).IsAssignableFrom(typeof(T));

        private DefaultEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return IsEquatable
                ? ((IEquatable<T>)x).Equals(y)
                : x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
