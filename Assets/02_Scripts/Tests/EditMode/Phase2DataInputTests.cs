using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    public sealed class Phase2DataInputTests
    {
        private readonly List<Object> assets = new List<Object>();
        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); assets.Add(asset); return asset;
        }
        private static void Field(object owner, string name, object value) =>
            owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private DataDefinition Definition(string id, params string[] references)
        {
            var definition = Asset<DataDefinition>(); Field(definition, "id", id); Field(definition, "references", references); return definition;
        }
        private DataCatalog Catalog(params DataDefinition[] definitions)
        {
            var catalog = Asset<DataCatalog>(); Field(catalog, "definitions", definitions); return catalog;
        }
        [TearDown] public void Cleanup() { foreach (var asset in assets) Object.DestroyImmediate(asset); assets.Clear(); }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("two words")]
        [TestCase("bad\nline")]
        public void InvalidIdsAreRejected(string id) =>
            Assert.Throws<InvalidOperationException>(() => DataValidation.Validate(Catalog(Definition(id))));

        [Test] public void DuplicateMissingDefinitionAndMissingReferenceAreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => DataValidation.Validate(Catalog(Definition("same"), Definition("same"))));
            Assert.Throws<InvalidOperationException>(() => DataValidation.Validate(Catalog((DataDefinition)null)));
            Assert.Throws<InvalidOperationException>(() => DataValidation.Validate(Catalog(Definition("source", "missing"))));
        }
        [Test] public void CatalogLookupIsReadOnlyAndRuntimeDoesNotMutateSavedSession()
        {
            var first = Definition("first", "second"); var second = Definition("second");
            var data = new DataService(Catalog(first, second));
            Assert.That(data.TryGetDefinition("first", out var found), Is.True);
            Assert.That(found, Is.SameAs(first));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, DataDefinition>)data.Definitions).Clear());
            var game = new GameSessionService(new MemoryStore());
            game.StartNew("{\"stage\":1}");
            var savedSnapshot = game.Current.PayloadJson;
            data.BeginSession(game.Current);
            data.Runtime.Payload["stage"] = 9;
            Assert.That(game.Current.PayloadJson, Is.EqualTo(savedSnapshot));
            Assert.That(JObject.Parse(data.Runtime.SnapshotPayload())["stage"].Value<int>(), Is.EqualTo(9));
            Assert.That(first.Id, Is.EqualTo("first"));
            data.EndSession(); Assert.That(data.Runtime, Is.Null);
        }
        [Test] public void InvalidCatalogFailsBeforeStorageAndDisposesInjectedRuntime()
        {
            var config = Asset<AppConfig>(); Field(config, "dataCatalog", Catalog(Definition("")));
            var store = new MemoryStore(); var runtime = new RecordingSettings();
            Assert.Throws<InvalidOperationException>(() => AppBootstrapper.Initialize(config, store, null, runtime));
            Assert.That(store.Reads, Is.Zero); Assert.That(runtime.Applies, Is.Zero); Assert.That(runtime.Disposed, Is.True);
        }
        [Test] public void ContextRestoresAfterOutOfOrderDisposalAndLifetimeCancellation()
        {
            using var input = new InputContextService();
            input.SetState(InputContext.Gameplay);
            var first = input.Push(InputContext.UI);
            using var lifetime = new CancellationTokenSource();
            var second = input.Push(InputContext.Debug, lifetime.Token);
            first.Dispose(); Assert.That(input.Current, Is.EqualTo(InputContext.Debug));
            lifetime.Cancel(); Assert.That(input.Current, Is.EqualTo(InputContext.Gameplay));
            second.Dispose();
            using var cancelled = input.Push(InputContext.UI, lifetime.Token);
            Assert.That(input.Current, Is.EqualTo(InputContext.Gameplay));
        }
        [Test] public void ForcedContextsSuspendAndRestoreScopeAndDisposeEndsInput()
        {
            var input = new InputContextService();
            input.SetState(InputContext.Gameplay);
            using var scope = input.Push(InputContext.Debug);
            input.SetState(InputContext.Gameplay, InputContext.UI);
            Assert.That(input.Current, Is.EqualTo(InputContext.UI));
            input.SetState(InputContext.Gameplay, InputContext.None);
            Assert.That(input.Current, Is.EqualTo(InputContext.None));
            input.SetState(InputContext.Gameplay);
            Assert.That(input.Current, Is.EqualTo(InputContext.Debug));
            input.Dispose(); Assert.That(input.Current, Is.EqualTo(InputContext.None));
        }
        private sealed class RecordingSettings : IRuntimeSettings
        {
            public int Applies; public bool Disposed;
            public void Apply(UserSettings settings) => Applies++;
            public void Dispose() => Disposed = true;
        }
        private sealed class MemoryStore : ITextFileStore
        {
            public int Reads;
            public string ReadAllText(string fileName) { Reads++; return null; }
            public void WriteAllText(string fileName, string content, Func<string, bool> validate, bool preserve) => throw new NotSupportedException();
        }
    }
}