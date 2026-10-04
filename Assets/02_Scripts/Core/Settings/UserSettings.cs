using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace StarterProject
{
    /// <summary>SO에서 복사해 사용하는 사용자 설정입니다. 실행 시스템 적용은 AppRoot가 연결합니다.</summary>
    [Serializable]
    public sealed class UserSettings
    {
        public const int MaxLanguageIdentifierLength = 64;
        [Range(0, 1)] public float masterVolume = 1;
        public bool fullscreen = true;
        /// <summary>게임이 해석하는 언어 식별자입니다. 실제 지원 언어와 번역 콘텐츠는 게임에서 정합니다.</summary>
        public string language = "en";
        public UserSettings Copy() => new UserSettings { masterVolume = masterVolume, fullscreen = fullscreen, language = language };

        /// <summary>식별자의 저장 가능한 형태만 확인하며 언어 등록 목록이나 BCP 47 규칙을 강제하지 않습니다.</summary>
        public static bool IsValidLanguageIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxLanguageIdentifierLength) return false;
            foreach (var character in value)
                if (char.IsWhiteSpace(character) || char.IsControl(character)) return false;
            return true;
        }

        public void Validate()
        {
            if (float.IsNaN(masterVolume) || float.IsInfinity(masterVolume) || masterVolume < 0 || masterVolume > 1)
                throw new ArgumentException("Master volume must be between 0 and 1.");
            if (!IsValidLanguageIdentifier(language))
                throw new ArgumentException("Language identifier must contain 1 to 64 characters without whitespace or control characters.");
        }
    }

    /// <summary>기본값 복사, 부분 JSON 병합, 값 검증, 변경 확정 저장을 담당합니다.</summary>
    public sealed class SettingsService
    {
        public const string FileName = "settings.json";
        private readonly UserSettings defaultSettings;
        private readonly JsonRepository<UserSettings> settingsRepository;
        private UserSettings currentSettings;
        public UserSettings Current => currentSettings.Copy();
        public StorageStatus Status { get; private set; }
        public string Message { get; private set; } = "";
        /// <summary>읽기·저장·복구로 현재 설정이 확정된 뒤 알립니다. 저장 실패에는 발생하지 않습니다.</summary>
        public event Action Changed;
        public bool CanSave => Status != StorageStatus.UnsupportedVersion && Status != StorageStatus.IoError && Status != StorageStatus.Recovered;

        public SettingsService(UserSettings defaultSettings, ITextFileStore fileStore)
        {
            defaultSettings.Validate();
            this.defaultSettings = defaultSettings.Copy();
            settingsRepository = new JsonRepository<UserSettings>(fileStore, FileName, Deserialize, Serialize);
            Reload();
        }

        public void Reload()
        {
            Message = "";
            var loadResult = settingsRepository.Load();
            currentSettings = loadResult.Value ?? defaultSettings.Copy();
            Status = loadResult.Status;
            if (!string.IsNullOrEmpty(loadResult.Message)) Message = loadResult.Message;
            Changed?.Invoke();
        }

        private UserSettings Deserialize(string jsonText)
        {
            var jsonObject = JsonData.ParseObject(jsonText);
            JsonData.GetSchemaVersion(jsonObject, 1);
            var settings = defaultSettings.Copy();
            var hasInvalidFields = false;
            var volumeToken = jsonObject["masterVolume"];
            if (volumeToken != null)
            {
                if ((volumeToken.Type == JTokenType.Float || volumeToken.Type == JTokenType.Integer)
                    && double.TryParse(volumeToken.ToString(Newtonsoft.Json.Formatting.None), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var volume)
                    && !double.IsNaN(volume) && volume >= 0 && volume <= 1)
                    settings.masterVolume = (float)volume;
                else hasInvalidFields = true;
            }
            var fullscreenToken = jsonObject["fullscreen"];
            if (fullscreenToken != null)
            {
                if (fullscreenToken.Type == JTokenType.Boolean) settings.fullscreen = fullscreenToken.Value<bool>();
                else hasInvalidFields = true;
            }
            var languageToken = jsonObject["language"];
            if (languageToken != null)
            {
                if (languageToken.Type == JTokenType.String && UserSettings.IsValidLanguageIdentifier(languageToken.Value<string>()))
                    settings.language = languageToken.Value<string>();
                else hasInvalidFields = true;
            }
            if (hasInvalidFields) Message = "Invalid setting fields were restored to defaults.";
            return settings;
        }

        private static string Serialize(UserSettings settings)
        {
            settings.Validate();
            return new JObject { ["schemaVersion"] = 1, ["masterVolume"] = settings.masterVolume,
                ["fullscreen"] = settings.fullscreen, ["language"] = settings.language }.ToString();
        }

        public bool TryApplyAndSave(UserSettings settings)
        {
            if (settings == null) { Message = "Settings are missing."; return false; }
            var settingsSnapshot = settings.Copy();
            if (!settingsRepository.TrySave(settingsSnapshot, out var errorMessage, allowOverwriteInvalidFile: true))
            { Message = errorMessage; return false; }
            currentSettings = settingsSnapshot;
            Status = StorageStatus.Loaded;
            Message = "Settings saved.";
            Changed?.Invoke();
            return true;
        }

        public bool TryRecoverBackup()
        {
            if (!settingsRepository.TryRecoverBackup(out var errorMessage)) { Message = errorMessage; return false; }
            Reload();
            Message = "Settings backup recovered.";
            return true;
        }
    }
}
