using UnityEngine;

namespace StarterProject.Pooling
{
    /// <summary>Pool이 만든 clone의 소유권·대여 세대입니다. 게임에서는 PoolLease를 사용합니다.</summary>
    [AddComponentMenu(""), DisallowMultipleComponent]
    public sealed class PooledInstance : MonoBehaviour
    {
        internal PrefabPool Owner;
        internal ulong Version;
        internal bool IsRented;
        private void OnDestroy()
        {
            if (Owner != null) Owner.Forget(this);
        }
    }
}
