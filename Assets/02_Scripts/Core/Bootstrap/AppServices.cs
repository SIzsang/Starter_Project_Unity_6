using System;
using UnityEngine;

namespace StarterProject
{
    /// <summary>실행용 설정 사본입니다. 공유 AppConfig를 실행 상태로 사용하지 않습니다.</summary>
    public sealed class RuntimeAppConfig
    {
        public string BootScene { get; }
        public string TitleScene { get; }
        public string MainScene { get; }
        public DataCatalog DataCatalog { get; }
        private readonly UserSettings defaults;
        public UserSettings CreateDefaultSettings() => defaults.Copy();
        internal RuntimeAppConfig(AppConfig config)
        {
            BootScene = config.BootScene;
            TitleScene = config.TitleScene;
            MainScene = config.MainScene;
            DataCatalog = config.DataCatalog;
            defaults = config.CreateDefaultSettings();
        }
    }

    /// <summary>AppRoot가 소유하는 타입별 공통 서비스입니다. 검색·등록 컨테이너를 두지 않습니다.</summary>
    public sealed class AppServices : IDisposable
    {
        public RuntimeAppConfig Config { get; }
        public DataService Data { get; }
        public InputContextService Input { get; }
        public PauseService Pause { get; }
        public AudioService Audio { get; }
        public SettingsService Settings { get; }
        public GameSessionService Game { get; }
        internal IRuntimeSettings RuntimeSettings { get; }
        public bool IsDisposed { get; private set; }

        internal AppServices(RuntimeAppConfig config, ITextFileStore store, GamePayloadPolicy payload, IRuntimeSettings runtimeSettings, Transform audioParent)
        {
            Config = config;
            Data = new DataService(config.DataCatalog);
            Settings = new SettingsService(config.CreateDefaultSettings(), store);
            Game = new GameSessionService(store, payload != null ? payload.PayloadVersion : 1,
                payload != null ? (Action<string>)payload.ValidatePayload : null);
            RuntimeSettings = runtimeSettings ?? new UnityRuntimeSettings();
            Input = new InputContextService();
            Pause = new PauseService();
            Audio = new AudioService(audioParent);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            try { Audio.Dispose(); }
            finally
            {
                try { Pause.Dispose(); }
                finally
                {
                    try { Input.Dispose(); }
                    finally
                    {
                        try { RuntimeSettings.Dispose(); }
                        finally { Game.EndSession(); Data.EndSession(); }
                    }
                }
            }
        }
    }
}