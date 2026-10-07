using System;
using UnityEngine;

namespace StarterProject.Pooling
{
    /// <summary>한 번의 대여만 나타냅니다. 복사·반납 후에는 재사용된 객체를 제어하지 않습니다.</summary>
    public readonly struct PoolLease : IDisposable
    {
        internal readonly PrefabPool Owner;
        internal readonly PooledInstance Item;
        internal readonly ulong Version;
        internal PoolLease(PrefabPool owner, PooledInstance item, ulong version)
        { Owner = owner; Item = item; Version = version; }

        public bool IsValid => Owner != null && Owner.IsCurrent(Item, Version);
        public GameObject GameObject => IsValid ? Item.gameObject : null;
        public bool TryReturn() => Owner != null && Owner.TryReturn(this);
        public void Dispose() => TryReturn();
    }
}
