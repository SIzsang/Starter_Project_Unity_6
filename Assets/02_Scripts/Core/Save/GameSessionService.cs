using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarterProject
{
    /// <summary>공통 저장 메타데이터와 게임별 JSON payload. 게임 규칙은 Core에 넣지 않습니다.</summary>
    public sealed class GameSession
    {
        public string SessionId { get; }
        public string CreatedUtc { get; }
        public string SavedUtc { get; }
        public string PayloadJson { get; }
        public GameSession(string sessionId, string createdUtc, string savedUtc, string payloadJson)
        { SessionId = sessionId; CreatedUtc = createdUtc; SavedUtc = savedUtc; PayloadJson = payloadJson; }
    }

    /// <summary>단일 슬롯 기반. 새 게임은 메모리에서 시작하며 기존 저장 교체는 명시적 요청만 허용합니다.</summary>
    public sealed class GameSessionService
    {
        public const string FileName = "save-slot-1.json";
        private readonly JsonRepository<GameSession> saveRepository;
        private readonly int payloadVersion;
        private readonly Action<string> payloadValidator;
        private LoadResult<GameSession> savedGameResult;
        public GameSession Current { get; private set; }
        public StorageStatus Status => savedGameResult.Status;
        public bool CanContinue => savedGameResult.HasValue;
        public bool RequiresReplacement => Current != null && savedGameResult.HasValue && savedGameResult.Value.SessionId != Current.SessionId;
        public string Message { get; private set; } = "";

        public GameSessionService(ITextFileStore fileStore, int payloadVersion = 1, Action<string> payloadValidator = null)
        {
            if (payloadVersion < 1) throw new ArgumentOutOfRangeException(nameof(payloadVersion));
            this.payloadVersion = payloadVersion;
            this.payloadValidator = payloadValidator;
            saveRepository = new JsonRepository<GameSession>(fileStore, FileName, Deserialize, Serialize);
            RefreshSave();
        }

        public void RefreshSave()
        {
            savedGameResult = saveRepository.Load();
            Message = savedGameResult.Message;
        }

        public void StartNew(string initialPayloadJson = "{}")
        {
            ValidatePayload(initialPayloadJson);
            Current = new GameSession(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.ToString("O"), "", initialPayloadJson);
            Message = "New game started. Existing save is unchanged.";
        }

        public bool TryContinue()
        {
            RefreshSave();
            if (!savedGameResult.HasValue) { Message = string.IsNullOrEmpty(savedGameResult.Message) ? "No saved game is available." : savedGameResult.Message; return false; }
            Current = savedGameResult.Value;
            Message = savedGameResult.Status == StorageStatus.Recovered ? savedGameResult.Message : "Saved game loaded.";
            return true;
        }

        public void EndSession() => Current = null;

        public bool TrySave(string payloadJson = null, bool replaceExisting = false)
        {
            if (Current == null) { Message = "Start or continue a game before saving."; return false; }
            RefreshSave();
            if (RequiresReplacement && !replaceExisting)
            { Message = "A different game is saved. Confirm Replace Save to overwrite it."; return false; }
            var sessionToSave = new GameSession(Current.SessionId, Current.CreatedUtc, DateTimeOffset.UtcNow.ToString("O"), payloadJson ?? Current.PayloadJson);
            if (!saveRepository.TrySave(sessionToSave, out var errorMessage)) { Message = errorMessage; return false; }
            Current = sessionToSave;
            savedGameResult = new LoadResult<GameSession>(StorageStatus.Loaded, sessionToSave);
            Message = "Game saved.";
            return true;
        }

        public bool TryRecoverBackup()
        {
            if (!saveRepository.TryRecoverBackup(out var errorMessage)) { Message = errorMessage; return false; }
            RefreshSave();
            Message = "Backup recovered. The damaged original was preserved.";
            return true;
        }

        private void ValidatePayload(string payloadJson)
        {
            JsonData.ParseObject(payloadJson);
            payloadValidator?.Invoke(payloadJson);
        }

        private GameSession Deserialize(string jsonText)
        {
            var jsonObject = JsonData.ParseObject(jsonText);
            var schemaVersion = JsonData.GetSchemaVersion(jsonObject, 2);
            // v1: 같은 메타데이터 + data 객체. v2: payload와 게임별 payloadVersion으로 분리.
            var savedPayloadVersion = schemaVersion == 1 ? 1 : GetRequiredInteger(jsonObject, "payloadVersion");
            if (savedPayloadVersion != payloadVersion)
                throw new UnsupportedDataVersionException("Game data version is incompatible. The save is protected.");
            var sessionId = GetRequiredString(jsonObject, "sessionId");
            if (!Guid.TryParseExact(sessionId, "N", out _)) throw new JsonException("Invalid session ID.");
            var createdUtc = GetRequiredString(jsonObject, "createdUtc");
            var savedUtc = GetRequiredString(jsonObject, "savedUtc");
            if (!DateTimeOffset.TryParseExact(createdUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var createdTime)
                || !DateTimeOffset.TryParseExact(savedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var savedTime)
                || savedTime < createdTime)
                throw new JsonException("Invalid save timestamps.");
            var payloadObject = jsonObject[schemaVersion == 1 ? "data" : "payload"] as JObject;
            if (payloadObject == null) throw new JsonException("Game payload must be an object.");
            var payloadJson = payloadObject.ToString();
            ValidatePayload(payloadJson);
            return new GameSession(sessionId, createdUtc, savedUtc, payloadJson);
        }

        private string Serialize(GameSession session)
        {
            ValidatePayload(session.PayloadJson);
            return new JObject { ["schemaVersion"] = 2, ["sessionId"] = session.SessionId,
                ["createdUtc"] = session.CreatedUtc, ["savedUtc"] = session.SavedUtc,
                ["payloadVersion"] = payloadVersion, ["payload"] = JsonData.ParseObject(session.PayloadJson) }.ToString();
        }

        private static string GetRequiredString(JObject jsonObject, string propertyName)
        {
            if (jsonObject[propertyName]?.Type != JTokenType.String) throw new JsonException("Missing or invalid " + propertyName);
            return jsonObject[propertyName].Value<string>();
        }
        private static int GetRequiredInteger(JObject jsonObject, string propertyName)
        {
            if (jsonObject[propertyName]?.Type != JTokenType.Integer) throw new JsonException("Missing or invalid " + propertyName);
            return jsonObject[propertyName].Value<int>();
        }
    }
}
