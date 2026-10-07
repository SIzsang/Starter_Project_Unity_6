using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarterProject.Pooling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    public sealed class PrefabPoolTests
    {
        private readonly List<GameObject> owners = new List<GameObject>();
        private GameObject template;

        [UnitySetUp] public IEnumerator Setup()
        {
            template = new GameObject("Pool test template");
            template.AddComponent<PoolActivationProbe>();
            PoolActivationProbe.Activations = 0;
            PoolActivationProbe.UnpreparedActivations = 0;
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var owner in owners) if (owner != null) Object.Destroy(owner);
            owners.Clear();
            if (template != null) Object.Destroy(template);
            yield return null;
        }
        private PrefabPool Create(int capacity = 3)
        {
            var owner = new GameObject("Pool test owner");
            owners.Add(owner);
            var pool = owner.AddComponent<PrefabPool>();
            pool.Configure(template, capacity);
            return pool;
        }
        private static PoolLease Rent(PrefabPool pool, Transform parent = null) =>
            pool.Rent(new Vector3(2, 3, 4), Quaternion.Euler(0, 0, 30), parent,
                go => go.GetComponent<PoolActivationProbe>().Prepared = true);

        [UnityTest] public IEnumerator ReusesCloneAndPreparesBeforeActivation()
        {
            var pool = Create();
            var first = Rent(pool);
            var clone = first.GameObject;
            Assert.That(clone.activeInHierarchy, Is.True);
            Assert.That(clone.transform.position, Is.EqualTo(new Vector3(2, 3, 4)));
            Assert.That(PoolActivationProbe.Activations, Is.EqualTo(1));
            Assert.That(PoolActivationProbe.UnpreparedActivations, Is.Zero);
            Assert.That(first.TryReturn(), Is.True);
            Assert.That(clone.activeInHierarchy, Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            var next = Rent(pool);
            Assert.That(next.GameObject, Is.SameAs(clone));
            Assert.That(first.GameObject, Is.Null);
            Assert.That(next.IsValid, Is.True);
            next.Dispose();
            yield return null;
        }

        [UnityTest] public IEnumerator RejectsForeignDefaultDuplicateAndStaleLease()
        {
            var pool = Create();
            var other = Create();
            var lease = Rent(pool);
            var copy = lease;
            Assert.That(other.TryReturn(lease), Is.False);
            Assert.That(pool.TryReturn(default), Is.False);
            Assert.That(lease.TryReturn(), Is.True);
            Assert.That(copy.TryReturn(), Is.False);
            var reused = Rent(pool);
            Assert.That(copy.TryReturn(), Is.False);
            Assert.That(reused.IsValid, Is.True);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            copy.Dispose();
            Assert.That(reused.IsValid, Is.True);
            reused.Dispose();
            yield return null;
        }

        [UnityTest] public IEnumerator FailedPreparationReturnsCloneAndAllowsNextRent()
        {
            var pool = Create();
            Assert.That(() => pool.Rent(Vector3.zero, Quaternion.identity, prepare: go =>
            {
                throw new InvalidOperationException("Game reset failed.");
            }), Throws.InvalidOperationException.With.Message.EqualTo("Game reset failed."));
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            Assert.That(PoolActivationProbe.Activations, Is.Zero);
            var lease = Rent(pool);
            Assert.That(lease.IsValid, Is.True);
            lease.Dispose();
            yield return null;
        }

        [UnityTest] public IEnumerator PrewarmCreatesDistinctInactiveInventoryWithoutActivatingPrefab()
        {
            var pool = Create(3);
            pool.Prewarm(3);
            Assert.That(pool.InactiveCount, Is.EqualTo(3));
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(PoolActivationProbe.Activations, Is.Zero);
            pool.Prewarm(2);
            var first = Rent(pool);
            var second = Rent(pool);
            var third = Rent(pool);
            Assert.That(first.GameObject, Is.Not.SameAs(second.GameObject));
            Assert.That(second.GameObject, Is.Not.SameAs(third.GameObject));
            Assert.That(first.GameObject, Is.Not.SameAs(third.GameObject));
            pool.ReturnAll();
            Assert.That(pool.InactiveCount, Is.EqualTo(3));
            Assert.That(first.IsValid || second.IsValid || third.IsValid, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator RetainedCapacityDestroysExcessReturnsWithoutLimitingActiveRentals()
        {
            var pool = Create(1);
            var first = Rent(pool);
            var second = Rent(pool);
            var excess = second.GameObject;
            Assert.That(pool.ActiveCount, Is.EqualTo(2));
            first.Dispose();
            second.Dispose();
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            yield return null;
            Assert.That(excess == null, Is.True);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest] public IEnumerator DisablingOwnerReturnsExternalChildrenAndReenableCanReuse()
        {
            var pool = Create();
            var outside = new GameObject("External parent");
            owners.Add(outside);
            var lease = Rent(pool, outside.transform);
            var clone = lease.GameObject;
            pool.enabled = false;
            Assert.That(lease.IsValid, Is.False);
            Assert.That(clone.activeInHierarchy, Is.False);
            Assert.That(clone.transform.IsChildOf(pool.transform), Is.True);
            Assert.That(() => Rent(pool), Throws.InvalidOperationException);
            pool.enabled = true;
            var resumed = Rent(pool);
            Assert.That(resumed.GameObject, Is.SameAs(clone));
            Assert.That(lease.TryReturn(), Is.False);
            resumed.Dispose();
            yield return null;
        }

        [UnityTest] public IEnumerator DestroyingOwnerDestroysRentedOutsideAndInactiveObjects()
        {
            var pool = Create();
            var outside = new GameObject("External parent");
            owners.Add(outside);
            var active = Rent(pool, outside.transform);
            var retained = Rent(pool);
            var activeClone = active.GameObject;
            var inactiveClone = retained.GameObject;
            retained.Dispose();
            Object.Destroy(pool.gameObject);
            yield return null;
            Assert.That(activeClone == null && inactiveClone == null, Is.True);
            Assert.That(active.IsValid || retained.IsValid, Is.False);
            Assert.That(outside != null, Is.True);
        }

        [UnityTest] public IEnumerator ExternallyDestroyedActiveAndInactiveClonesAreNotReused()
        {
            var pool = Create();
            var first = Rent(pool);
            Object.Destroy(first.GameObject);
            yield return null;
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.TryReturn(), Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);
            var retained = Rent(pool);
            var clone = retained.GameObject;
            retained.Dispose();
            Object.Destroy(clone);
            yield return null;
            Assert.That(pool.InactiveCount, Is.Zero);
            var replacement = Rent(pool);
            Assert.That(replacement.GameObject != null, Is.True);
            replacement.Dispose();
        }

        [UnityTest] public IEnumerator OwnerDisabledDuringPreparationCancelsRentalAndRecovers()
        {
            var pool = Create();
            Assert.That(() => pool.Rent(Vector3.zero, Quaternion.identity, prepare: go => pool.enabled = false),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            pool.enabled = true;
            var lease = Rent(pool);
            Assert.That(lease.IsValid, Is.True);
            lease.Dispose();
            yield return null;
        }

        [UnityTest] public IEnumerator UnloadingPoolSceneDestroysCloneParentedInSurvivingScene()
        {
            var scene = SceneManager.CreateScene("PoolScene-" + Guid.NewGuid().ToString("N"));
            var pool = Create();
            SceneManager.MoveGameObjectToScene(pool.gameObject, scene);
            var outside = new GameObject("Surviving parent");
            owners.Add(outside);
            var lease = Rent(pool, outside.transform);
            var clone = lease.GameObject;
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
            Assert.That(clone == null, Is.True);
            Assert.That(lease.IsValid, Is.False);
            Assert.That(outside != null, Is.True);
        }

        [UnityTest] public IEnumerator ConfigurationAndReentrantPreparationAreRejectedSafely()
        {
            var pool = Create(1);
            Assert.That(() => pool.Configure(template, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            pool.Prewarm(1);
            Assert.That(() => pool.Configure(template), Throws.InvalidOperationException);
            Assert.That(() => pool.Prewarm(2), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => pool.Rent(Vector3.zero, Quaternion.identity, prepare: go => Rent(pool)),
                Throws.InvalidOperationException.With.Message.Contains("reenter"));
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            var lease = Rent(pool);
            lease.Dispose();
            yield return null;
        }
    }

    public sealed class PoolActivationProbe : MonoBehaviour
    {
        public static int Activations;
        public static int UnpreparedActivations;
        public bool Prepared;
        private void OnEnable()
        {
            Activations++;
            if (!Prepared) UnpreparedActivations++;
        }
    }
}
