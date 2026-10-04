using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace StarterProject.Tests
{
    /// <summary>실제 창을 변경하지 않고 실행 설정의 적용, 중복 방지, 실패 및 수명 정책을 검증합니다.</summary>
    public sealed class RuntimeSettingsTests
    {
        [TestCase(0f, false, FullScreenMode.Windowed)]
        [TestCase(0.5f, true, FullScreenMode.FullScreenWindow)]
        [TestCase(1f, false, FullScreenMode.Windowed)]
        public void ApplyMapsVolumeAndFullscreen(float volume, bool fullscreen, FullScreenMode screenMode)
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                runtime.Apply(new UserSettings { masterVolume = volume, fullscreen = fullscreen });
                Assert.That(backend.MasterVolume, Is.EqualTo(volume));
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { screenMode }));
            }
        }

        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidVolumeCannotPartiallyChangeRuntime(float volume)
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                Assert.Throws<ArgumentException>(() => runtime.Apply(new UserSettings { masterVolume = volume, fullscreen = false }));
                Assert.That(backend.VolumeWrites, Is.Zero);
                Assert.That(backend.ScreenRequests, Is.Empty);
            }
        }

        [Test]
        public void MissingSettingsAndInvalidLanguageCannotChangeRuntime()
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                Assert.Throws<ArgumentNullException>(() => runtime.Apply(null));
                Assert.Throws<ArgumentException>(() => runtime.Apply(new UserSettings { masterVolume = 0, language = " " }));
                Assert.That(backend.VolumeWrites, Is.Zero);
                Assert.That(backend.ScreenRequests, Is.Empty);
            }
        }

        [TestCase("ja")]
        [TestCase("game:pirate")]
        public void GameLanguageIdentifierDoesNotBlockAudioAndDisplaySettings(string language)
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                runtime.Apply(new UserSettings { masterVolume = 0, fullscreen = false, language = language });
                Assert.That(backend.MasterVolume, Is.Zero);
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { FullScreenMode.Windowed }));
            }
        }

        [Test]
        public void RepeatedApplyDoesNotRepeatPendingScreenRequest()
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                var settings = new UserSettings { masterVolume = 0.5f, fullscreen = false };
                runtime.Apply(settings);
                runtime.Apply(settings.Copy());
                settings.language = "ko";
                runtime.Apply(settings);
                Assert.That(backend.VolumeWrites, Is.EqualTo(1));
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { FullScreenMode.Windowed }));
                settings.fullscreen = true;
                runtime.Apply(settings);
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow }));
            }
        }

        [Test]
        public void ScreenFailureRollsBackVolumeAndCanBeRetried()
        {
            var backend = new FakeBackend { ScreenFailuresRemaining = 1 };
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                var settings = new UserSettings { masterVolume = 0, fullscreen = false };
                Assert.Throws<InvalidOperationException>(() => runtime.Apply(settings));
                Assert.That(backend.MasterVolume, Is.EqualTo(0.75f));
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { FullScreenMode.Windowed, FullScreenMode.ExclusiveFullScreen }));
                runtime.Apply(settings);
                Assert.That(backend.MasterVolume, Is.Zero);
                Assert.That(backend.ScreenRequests[2], Is.EqualTo(FullScreenMode.Windowed));
            }
        }

        [Test]
        public void FailedChangeRestoresPreviousPendingModeInsteadOfStaleObservedMode()
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                runtime.Apply(new UserSettings { masterVolume = 0.5f, fullscreen = false });
                backend.ScreenFailuresRemaining = 1;
                Assert.Throws<InvalidOperationException>(() => runtime.Apply(new UserSettings { masterVolume = 0, fullscreen = true }));
                Assert.That(backend.MasterVolume, Is.EqualTo(0.5f));
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[]
                    { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed }));
            }
        }

        [Test]
        public void FailedRollbackReportsBothErrorsAndNextApplyReassertsScreenMode()
        {
            var backend = new FakeBackend();
            using (var runtime = new UnityRuntimeSettings(backend))
            {
                runtime.Apply(new UserSettings { fullscreen = false });
                backend.ScreenFailuresRemaining = 2;
                var error = Assert.Throws<AggregateException>(() => runtime.Apply(new UserSettings { fullscreen = true }));
                Assert.That(error.InnerExceptions.Count, Is.EqualTo(2));
                runtime.Apply(new UserSettings { fullscreen = false });
                Assert.That(backend.ScreenRequests, Is.EqualTo(new[]
                    { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed, FullScreenMode.Windowed }));
            }
        }

        [Test]
        public void DisposeRestoresOriginalValuesOnceAndRejectsFurtherApply()
        {
            var backend = new FakeBackend();
            var runtime = new UnityRuntimeSettings(backend);
            runtime.Apply(new UserSettings { masterVolume = 0, fullscreen = false });
            runtime.Dispose();
            runtime.Dispose();
            Assert.That(backend.MasterVolume, Is.EqualTo(0.75f));
            Assert.That(backend.VolumeWrites, Is.EqualTo(2));
            Assert.That(backend.ScreenRequests, Is.EqualTo(new[] { FullScreenMode.Windowed, FullScreenMode.ExclusiveFullScreen }));
            Assert.Throws<ObjectDisposedException>(() => runtime.Apply(new UserSettings()));
        }

        [Test]
        public void DisposeStillRestoresScreenWhenVolumeRestorationFails()
        {
            var backend = new FakeBackend();
            var runtime = new UnityRuntimeSettings(backend);
            runtime.Apply(new UserSettings { masterVolume = 0, fullscreen = false });
            backend.FailNextVolumeWrite = true;
            Assert.Throws<InvalidOperationException>(() => runtime.Dispose());
            Assert.That(backend.ScreenRequests[1], Is.EqualTo(FullScreenMode.ExclusiveFullScreen));
        }

        /// <summary>화면 실측값을 초기값으로 유지해 아직 반영되지 않은 Unity 화면 전환을 재현합니다.</summary>
        private sealed class FakeBackend : IRuntimeSettingsBackend
        {
            private float volume = 0.75f;
            public int VolumeWrites { get; private set; }
            public readonly List<FullScreenMode> ScreenRequests = new List<FullScreenMode>();
            public bool FailNextVolumeWrite;
            public int ScreenFailuresRemaining;
            public float MasterVolume
            {
                get => volume;
                set
                {
                    VolumeWrites++;
                    if (FailNextVolumeWrite) { FailNextVolumeWrite = false; throw new InvalidOperationException("Volume rejected."); }
                    volume = value;
                }
            }
            public FullScreenMode FullScreenMode
            {
                get => FullScreenMode.ExclusiveFullScreen;
                set
                {
                    ScreenRequests.Add(value);
                    if (ScreenFailuresRemaining > 0) { ScreenFailuresRemaining--; throw new InvalidOperationException("Screen rejected."); }
                }
            }
        }
    }
}
