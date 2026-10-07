using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace StarterProject
{
    /// <summary>Config 검증 → 실행용 사본 → Settings/Save → 시스템 적용 순서를 명시적으로 조립합니다.</summary>
    public static class AppBootstrapper
    {
        /// <remarks>전달받은 runtimeSettings는 성공·실패 모두 이 경계가 소유하며 store의 수명은 주입자가 소유합니다.</remarks>
        public static AppServices Initialize(AppConfig config, ITextFileStore store, GamePayloadPolicy payload,
            IRuntimeSettings runtimeSettings, Action<string> report = null, CancellationToken token = default, Transform audioParent = null)
        {
            AppServices services = null;
            try
            {
                if (config == null) throw new InvalidOperationException("AppBootstrap requires an AppConfig asset.");
                token.ThrowIfCancellationRequested();
                config.Validate();
                var snapshot = new RuntimeAppConfig(config);
                report?.Invoke("Loading user settings and save information");
                token.ThrowIfCancellationRequested();
                store = store ?? new JsonFileStore(Path.Combine(Application.persistentDataPath, "StarterData"));
                services = new AppServices(snapshot, store, payload, runtimeSettings, audioParent);
                report?.Invoke("Applying audio and display settings");
                token.ThrowIfCancellationRequested();
                services.RuntimeSettings.Apply(services.Settings.Current);
                token.ThrowIfCancellationRequested();
                return services;
            }
            catch
            {
                try
                {
                    if (services != null) services.Dispose();
                    else runtimeSettings?.Dispose();
                }
                catch (Exception error) { StarterLog.Warning(LogCategory.Bootstrap, $"Startup cleanup failed: {error.Message}"); }
                throw;
            }
        }
    }
}