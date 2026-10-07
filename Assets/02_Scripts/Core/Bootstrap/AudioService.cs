using System;
using UnityEngine;

namespace StarterProject
{
    /// <summary>앱이 소유하는 BGM/SFX/UI 채널입니다. Master volume은 기존 RuntimeSettings가 적용합니다.</summary>
    public sealed class AudioService : IDisposable
    {
        private readonly GameObject host;
        private bool paused;
        private bool disposed;
        public AudioSource BgmSource { get; }
        public AudioSource SfxSource { get; }
        public AudioSource UiSource { get; }

        internal AudioService(Transform parent)
        {
            host = new GameObject("App Audio");
            if (parent != null) host.transform.SetParent(parent, false);
            BgmSource = CreateSource("BGM");
            SfxSource = CreateSource("SFX");
            UiSource = CreateSource("UI");
        }

        private AudioSource CreateSource(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(host.transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }

        public bool PlayBgm(AudioClip clip, bool loop = true)
        {
            if (disposed || clip == null) return false;
            BgmSource.Stop();
            BgmSource.clip = clip; BgmSource.loop = loop; BgmSource.Play();
            if (paused) BgmSource.Pause();
            return true;
        }
        public void StopBgm() { if (!disposed) { BgmSource.Stop(); BgmSource.clip = null; } }
        public bool PlaySfx(AudioClip clip, float volume = 1)
        {
            if (disposed || paused || clip == null) return false;
            SfxSource.PlayOneShot(clip, Mathf.Clamp01(volume)); return true;
        }
        public bool PlayUI(AudioClip clip, float volume = 1)
        {
            if (disposed || clip == null) return false;
            UiSource.PlayOneShot(clip, Mathf.Clamp01(volume)); return true;
        }
        internal void SetPaused(bool value)
        {
            if (disposed || paused == value) return;
            paused = value;
            if (paused) { BgmSource.Pause(); SfxSource.Pause(); }
            else { BgmSource.UnPause(); SfxSource.UnPause(); }
        }
        internal void OnSceneExit() { if (!disposed) SfxSource.Stop(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            BgmSource.Stop(); SfxSource.Stop(); UiSource.Stop();
            UnityEngine.Object.Destroy(host);
        }
    }
}