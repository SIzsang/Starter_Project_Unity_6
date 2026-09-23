using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("StarterProject.EditModeTests")]

namespace StarterProject
{
    /// <summary>설정을 실행 환경에 적용하고 소유권 해제 시 이전 상태를 복원합니다. Apply 실패 시 직전 상태 복원을 시도하고 실패를 예외로 알립니다.</summary>
    public interface IRuntimeSettings : IDisposable
    {
        void Apply(UserSettings settings);
    }

    /// <summary>실제 화면을 전환하지 않고 적용 정책을 검증하기 위한 최소 플랫폼 경계입니다.</summary>
    internal interface IRuntimeSettingsBackend
    {
        float MasterVolume { get; set; }
        FullScreenMode FullScreenMode { get; set; }
    }

    /// <summary>전체 음량과 화면 모드를 적용하고 생성 당시의 Unity 전역 상태를 보관합니다.</summary>
    public sealed class UnityRuntimeSettings : IRuntimeSettings
    {
        private readonly IRuntimeSettingsBackend backend;
        private readonly float originalVolume;
        private readonly FullScreenMode originalScreenMode;
        private FullScreenMode requestedScreenMode;
        private bool screenModeKnown = true;
        private bool disposed;

        public UnityRuntimeSettings() : this(new UnityBackend()) { }

        internal UnityRuntimeSettings(IRuntimeSettingsBackend backend)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            originalVolume = backend.MasterVolume;
            originalScreenMode = backend.FullScreenMode;
            requestedScreenMode = originalScreenMode;
        }

        /// <summary>모든 필드를 먼저 검증하며 적용 실패 시 직전 실행 상태를 복원합니다.</summary>
        public void Apply(UserSettings settings)
        {
            if (disposed) throw new ObjectDisposedException(nameof(UnityRuntimeSettings));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            var screenMode = settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            var previousVolume = backend.MasterVolume;
            var previousScreenMode = requestedScreenMode;
            try
            {
                if (previousVolume != settings.masterVolume) backend.MasterVolume = settings.masterVolume;
                // 화면 전환이 아직 반영되지 않았어도 동일 요청을 프레임마다 반복하지 않습니다.
                if (!screenModeKnown || requestedScreenMode != screenMode)
                {
                    screenModeKnown = false;
                    backend.FullScreenMode = screenMode;
                }
                requestedScreenMode = screenMode;
                screenModeKnown = true;
            }
            catch (Exception applyError)
            {
                try { Restore(previousVolume, previousScreenMode); }
                catch (Exception restoreError)
                {
                    throw new AggregateException("Runtime settings could not be applied or restored.", applyError, restoreError);
                }
                throw;
            }
        }

        /// <summary>대기 중인 화면 요청도 원래 모드로 되돌리고 중복 해제를 무시합니다.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Restore(originalVolume, originalScreenMode);
        }

        /// <summary>한 값의 복구가 실패해도 나머지 전역 상태의 복구를 시도합니다.</summary>
        private void Restore(float volume, FullScreenMode screenMode)
        {
            Exception volumeError = null;
            try { backend.MasterVolume = volume; }
            catch (Exception error) { volumeError = error; }
            try
            {
                screenModeKnown = false;
                backend.FullScreenMode = screenMode;
                requestedScreenMode = screenMode;
                screenModeKnown = true;
            }
            catch (Exception screenError)
            {
                if (volumeError != null)
                    throw new AggregateException("Runtime volume and screen mode could not be restored.", volumeError, screenError);
                throw;
            }
            if (volumeError != null) throw volumeError;
        }

        /// <summary>음량은 AudioListener 전체 음량, 화면은 명시적인 FullScreenMode로 연결합니다.</summary>
        private sealed class UnityBackend : IRuntimeSettingsBackend
        {
            public float MasterVolume { get => AudioListener.volume; set => AudioListener.volume = value; }
            public FullScreenMode FullScreenMode { get => Screen.fullScreenMode; set => Screen.fullScreenMode = value; }
        }
    }
}
