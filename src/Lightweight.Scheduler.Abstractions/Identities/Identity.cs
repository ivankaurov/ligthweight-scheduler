namespace Lightweight.Scheduler.Abstractions.Identities
{
    using System;
    using System.Collections.Generic;

    public class Identity<TStorageKey> : IIdentity<TStorageKey>, IEquatable<IIdentity<TStorageKey>>
    {
        private readonly IEqualityComparer<TStorageKey> equalityComparer;

        public Identity(TStorageKey id)
            : this(id, DefaultEqualityComparer<TStorageKey>.Instance)
        {
        }

        public Identity(TStorageKey id, IEqualityComparer<TStorageKey> equalityComparer)
        {
            this.equalityComparer = equalityComparer ?? throw new ArgumentNullException(nameof(equalityComparer));
            this.Id = id;
        }

        public TStorageKey Id { get; }

        public bool Equals(IIdentity<TStorageKey> other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return this.equalityComparer.Equals(this.Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IIdentity<TStorageKey>);
        }

        public override int GetHashCode()
        {
            return this.equalityComparer.GetHashCode(this.Id);
        }

        public override string ToString()
        {
            return $"{{{this.Id}}}";
        }
    }
}
