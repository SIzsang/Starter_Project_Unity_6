using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

    /// <summary>슬롯 목록용 읽기 전용 정보입니다. 게임별 진행률이나 표시용 콘텐츠를 요구하지 않습니다.</summary>
    public sealed class SaveSlotInfo
    {
        public int SlotId { get; }
        public StorageStatus Status { get; }
        public bool CanContinue { get; }
        public bool IsEmpty => Status == StorageStatus.Missing;
        public string SavedUtc { get; }
        public string Message { get; }
        internal SaveSlotInfo(int slotId, LoadResult<GameSession> result)
        {
            SlotId = slotId; Status = result.Status; CanContinue = result.HasValue;
            SavedUtc = result.Value?.SavedUtc ?? ""; Message = result.Message;
        }
    }

    /// <summary>슬롯별 독립 진행과 하나의 활성 세션을 관리합니다. 새 게임은 첫 명시적 저장 전까지 메모리에만 존재합니다.</summary>
    public sealed class GameSessionService
    {
        public const string FileName = "save-slot-1.json";
        public const int DefaultSlotCount = 3;
        private readonly JsonRepository<GameSession>[] saveRepositories;
        private readonly LoadResult<GameSession>[] savedGameResults;
        private readonly IDeleteSaveFileStore deletionStore;
        private readonly int payloadVersion;
        private readonly Action<string> payloadValidator;
        private IReadOnlyList<SaveSlotInfo> slots;
        public GameSession Current { get; private set; }
        public int? CurrentSlotId { get; private set; }
        public int SlotCount => saveRepositories.Length;
        public IReadOnlyList<SaveSlotInfo> Slots => slots;
        public bool CanDelete => deletionStore != null;
        // 기존 단일 슬롯 API는 활성 슬롯 또는 슬롯 1의 상태를 가리킵니다.
        public StorageStatus Status => savedGameResults[(CurrentSlotId ?? 1) - 1].Status;
        public bool CanContinue => savedGameResults[(CurrentSlotId ?? 1) - 1].HasValue;
        public bool AnyCanContinue
        {
            get { foreach (var result in savedGameResults) if (result.HasValue) return true; return false; }
        }
        public bool RequiresReplacement => Current != null && savedGameResults[CurrentSlotId.Value - 1].HasValue
            && savedGameResults[CurrentSlotId.Value - 1].Value.SessionId != Current.SessionId;
        public string Message { get; private set; } = "";

        public GameSessionService(ITextFileStore fileStore, int payloadVersion = 1, Action<string> payloadValidator = null,
            int slotCount = DefaultSlotCount, IDeleteSaveFileStore deletionStore = null)
        {
            if (fileStore == null) throw new ArgumentNullException(nameof(fileStore));
            if (payloadVersion < 1) throw new ArgumentOutOfRangeException(nameof(payloadVersion));
            if (slotCount < 1 || slotCount > 10) throw new ArgumentOutOfRangeException(nameof(slotCount));
            this.payloadVersion = payloadVersion;
            this.payloadValidator = payloadValidator;
            this.deletionStore = deletionStore ?? fileStore as IDeleteSaveFileStore;
            saveRepositories = new JsonRepository<GameSession>[slotCount];
            savedGameResults = new LoadResult<GameSession>[slotCount];
            for (var index = 0; index < slotCount; index++)
                saveRepositories[index] = new JsonRepository<GameSession>(fileStore, GetFileName(index + 1), Deserialize, Serialize);
            RefreshSave();
        }

        private static string GetFileName(int slotId) => "save-slot-" + slotId + ".json";
        private int GetSlotIndex(int slotId)
        {
            if (slotId < 1 || slotId > SlotCount) throw new ArgumentOutOfRangeException(nameof(slotId));
            return slotId - 1;
        }
        public SaveSlotInfo GetSlot(int slotId) => slots[GetSlotIndex(slotId)];

        public void RefreshSave()
        {
            var snapshot = new SaveSlotInfo[SlotCount];
            var messages = new List<string>();
            for (var index = 0; index < SlotCount; index++)
            {
                savedGameResults[index] = saveRepositories[index].Load();
                snapshot[index] = new SaveSlotInfo(index + 1, savedGameResults[index]);
                if (!string.IsNullOrEmpty(savedGameResults[index].Message))
                    messages.Add("Slot " + (index + 1) + ": " + savedGameResults[index].Message);
            }
            slots = Array.AsReadOnly(snapshot);
            Message = string.Join("\n", messages);
        }

        /// <summary>기존 호출은 슬롯 1을 사용합니다. 슬롯의 기존 진행은 명시적 저장 교체 전까지 유지합니다.</summary>
        public void StartNew(string initialPayloadJson = "{}") => StartNew(1, initialPayloadJson);
        public void StartNew(int slotId, string initialPayloadJson = "{}")
        {
            GetSlotIndex(slotId);
            ValidatePayload(initialPayloadJson);
            Current = new GameSession(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.ToString("O"), "", initialPayloadJson);
            CurrentSlotId = slotId;
            Message = "New game started in slot " + slotId + ". Existing saves are unchanged.";
        }

        public bool TryContinue() => TryContinue(1);
        public bool TryContinue(int slotId)
        {
            var index = GetSlotIndex(slotId);
            RefreshSave();
            var savedGame = savedGameResults[index];
            if (!savedGame.HasValue)
            { Message = string.IsNullOrEmpty(savedGame.Message) ? "No saved game is available in this slot." : savedGame.Message; return false; }
            Current = savedGame.Value;
            CurrentSlotId = slotId;
            Message = savedGame.Status == StorageStatus.Recovered ? savedGame.Message : "Saved game loaded from slot " + slotId + ".";
            return true;
        }

        public void EndSession() { Current = null; CurrentSlotId = null; }

        /// <summary>현재 진행을 활성 슬롯에 저장합니다. 다른 슬롯에는 쓰지 않습니다.</summary>
        public bool TrySave(string payloadJson = null, bool replaceExisting = false)
        {
            if (Current == null) { Message = "Start or continue a game before saving."; return false; }
            RefreshSave();
            if (RequiresReplacement && !replaceExisting)
            { Message = "A different game is saved. Confirm Replace Save to overwrite it."; return false; }
            var index = CurrentSlotId.Value - 1;
            var sessionToSave = new GameSession(Current.SessionId, Current.CreatedUtc, DateTimeOffset.UtcNow.ToString("O"), payloadJson ?? Current.PayloadJson);
            if (!saveRepositories[index].TrySave(sessionToSave, out var errorMessage)) { Message = errorMessage; return false; }
            Current = sessionToSave;
            RefreshSave();
            Message = "Game saved in slot " + CurrentSlotId.Value + ".";
            return true;
        }

        public bool TryRecoverBackup() => TryRecoverBackup(CurrentSlotId ?? 1);
        public bool TryRecoverBackup(int slotId)
        {
            var index = GetSlotIndex(slotId);
            if (!saveRepositories[index].TryRecoverBackup(out var errorMessage)) { Message = errorMessage; return false; }
            RefreshSave();
            Message = "Backup recovered for slot " + slotId + ". The damaged original was preserved.";
            return true;
        }

        /// <summary>주 파일과 복구용 파일을 삭제합니다. 활성 세션의 슬롯은 먼저 종료해야 합니다.</summary>
        public bool TryDelete(int slotId)
        {
            GetSlotIndex(slotId);
            if (CurrentSlotId == slotId) { Message = "End the active game before deleting its slot."; return false; }
            if (deletionStore == null) { Message = "This storage does not support slot deletion."; return false; }
            try { deletionStore.DeleteSaveFiles(GetFileName(slotId)); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                || exception is ArgumentException || exception is NotSupportedException)
            {
                RefreshSave();
                Message = "Could not delete slot " + slotId + ". " + exception.Message;
                return false;
            }
            RefreshSave();
            if (!GetSlot(slotId).IsEmpty) { Message = "The slot could not be verified as empty."; return false; }
            Message = "Slot " + slotId + " deleted.";
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
