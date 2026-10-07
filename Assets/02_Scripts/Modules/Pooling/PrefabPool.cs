using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace StarterProject.Pooling
{
    /// <summary>씬/게임 객체가 소유하는 프리팹 하나의 Pool입니다. 메인 스레드에서만 사용합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class PrefabPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int maxRetained = 32;
        private ObjectPool<PooledInstance> pool;
        private Transform storage;
        private readonly HashSet<PooledInstance> rented = new HashSet<PooledInstance>();
        private readonly HashSet<PooledInstance> inactive = new HashSet<PooledInstance>();
        private bool changing;
        private bool returningAll;
        private bool disposed;
        public int ActiveCount => rented.Count;
        public int InactiveCount => inactive.Count;

        /// <summary>첫 Rent/Prewarm 전에만 설정합니다. Inspector에서도 연결할 수 있습니다.</summary>
        public void Configure(GameObject source, int maximumRetained = 32)
        {
            if (pool != null || disposed) throw new InvalidOperationException("Configure Pool before first use.");
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maximumRetained < 1) throw new ArgumentOutOfRangeException(nameof(maximumRetained));
            prefab = source;
            maxRetained = maximumRetained;
        }

        /// <summary>위치와 게임 준비를 비활성 상태에서 확정한 뒤 활성화합니다.</summary>
        public PoolLease Rent(Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> prepare = null)
        {
            RequireAvailable();
            changing = true;
            PooledInstance item = null;
            try
            {
                item = Take();
                if (parent != null && (parent == item.transform || parent.IsChildOf(item.transform)))
                    throw new ArgumentException("A pooled object cannot be parented to itself or its descendants.", nameof(parent));
                item.Version++;
                if (item.Version == 0) item.Version++;
                item.IsRented = true;
                rented.Add(item);
                var lease = new PoolLease(this, item, item.Version);
                item.transform.SetParent(parent != null ? parent : transform, false);
                item.transform.SetPositionAndRotation(position, rotation);
                item.transform.localScale = prefab.transform.localScale;
                prepare?.Invoke(item.gameObject);
                if (!lease.IsValid || !isActiveAndEnabled)
                    throw new OperationCanceledException("Pool ended during preparation.");
                item.gameObject.SetActive(true);
                if (!lease.IsValid || !isActiveAndEnabled)
                    throw new OperationCanceledException("Pool ended during activation.");
                return lease;
            }
            catch
            {
                if (item != null)
                {
                    if (item.IsRented) Release(item);
                    else if (item.Owner == this && !inactive.Contains(item)) pool.Release(item);
                }
                throw;
            }
            finally { changing = false; }
        }

        public bool TryReturn(PoolLease lease)
        {
            if (changing || lease.Owner != this || !IsCurrent(lease.Item, lease.Version)) return false;
            changing = true;
            try { Release(lease.Item); return true; }
            finally { changing = false; }
        }

        internal bool IsCurrent(PooledInstance item, ulong version) =>
            !disposed && item != null && item.Owner == this && item.IsRented && item.Version == version;

        /// <summary>필요하면 비활성 재고를 미리 만듭니다. maxRetained는 동시 대여 수가 아니라 반납 재고 상한입니다.</summary>
        public void Prewarm(int count)
        {
            RequireAvailable();
            if (count < 0 || count > maxRetained) throw new ArgumentOutOfRangeException(nameof(count));
            if (inactive.Count >= count) return;
            changing = true;
            var items = new List<PooledInstance>(count);
            try { for (var i = 0; i < count; i++) items.Add(Take()); }
            finally
            {
                foreach (var item in items) if (item != null) pool.Release(item);
                changing = false;
            }
        }

        /// <summary>SceneRoot.Exit 등에서 현재 대여를 모두 종료할 수 있습니다.</summary>
        public void ReturnAll()
        {
            if (disposed || returningAll) return;
            returningAll = true;
            var previous = changing;
            changing = true;
            try
            {
                foreach (var item in new List<PooledInstance>(rented))
                    if (item != null && item.IsRented) Release(item);
            }
            finally { changing = previous; returningAll = false; }
        }

        private void RequireAvailable()
        {
            if (disposed || !isActiveAndEnabled) throw new InvalidOperationException("Pool owner must be active.");
            if (changing) throw new InvalidOperationException("Pool callbacks cannot reenter pool operations.");
            if (pool != null) return;
            if (prefab == null || maxRetained < 1) throw new InvalidOperationException("Pool requires a prefab and positive retained capacity.");
            if (transform == prefab.transform || transform.IsChildOf(prefab.transform))
                throw new InvalidOperationException("Pool cannot use its owner or ancestor as the prefab.");
            if (prefab.GetComponent<PooledInstance>()?.Owner != null)
                throw new InvalidOperationException("Pool cannot use another pooled clone as the prefab.");
            var host = new GameObject("Inactive Pool Items");
            host.SetActive(false);
            host.transform.SetParent(transform, false);
            storage = host.transform;
            pool = new ObjectPool<PooledInstance>(Create, null, Store, Discard, true, Math.Min(8, maxRetained), maxRetained);
        }

        private PooledInstance Take()
        {
            PooledInstance item;
            do { item = pool.Get(); } while (item == null); // 외부에서 파괴한 재고를 재사용하지 않습니다.
            inactive.Remove(item);
            return item;
        }
        private PooledInstance Create()
        {
            var clone = Instantiate(prefab, storage, false);
            clone.SetActive(false);
            var item = clone.GetComponent<PooledInstance>() ?? clone.AddComponent<PooledInstance>();
            item.Owner = this;
            return item;
        }
        private void Release(PooledInstance item)
        {
            item.IsRented = false;
            rented.Remove(item); // OnDisable에서 반납이 겹쳐도 이전 Lease는 무효입니다.
            pool.Release(item);
        }
        private void Store(PooledInstance item)
        {
            if (item == null) return;
            item.IsRented = false;
            item.gameObject.SetActive(false);
            if (item == null) return;
            item.transform.SetParent(storage, false);
            inactive.Add(item);
        }
        private void Discard(PooledInstance item)
        {
            if (item == null) return;
            Forget(item);
            item.Owner = null;
            item.IsRented = false;
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
        internal void Forget(PooledInstance item) { rented.Remove(item); inactive.Remove(item); }
        private void OnDisable() => ReturnAll();
        private void OnDestroy()
        {
            disposed = true;
            foreach (var item in new List<PooledInstance>(rented)) Discard(item);
            rented.Clear();
            pool?.Dispose();
            pool = null;
            inactive.Clear();
            if (storage != null) Destroy(storage.gameObject);
        }
    }
}
