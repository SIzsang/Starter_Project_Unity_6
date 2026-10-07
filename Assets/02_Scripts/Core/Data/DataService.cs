using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarterProject
{
    /// <summary>저장 스냅샷과 분리된 가변 실행 데이터입니다. 게임별 필드는 JSON 객체에 둡니다.</summary>
    public sealed class RuntimeSessionData
    {
        public string SessionId { get; }
        public JObject Payload { get; }
        internal RuntimeSessionData(GameSession session)
        {
            SessionId = session.SessionId;
            Payload = JsonData.ParseObject(session.PayloadJson);
        }
        public string SnapshotPayload() => Payload.ToString(Formatting.None);
        internal void ApplySnapshot(string payload)
        {
            var snapshot = JsonData.ParseObject(payload);
            Payload.RemoveAll();
            foreach (var property in snapshot.Properties()) Payload.Add(property.Name, property.Value.DeepClone());
        }
    }

    /// <summary>Definition 조회와 세션 Runtime Data의 수명만 소유합니다. 저장 파일·게임 규칙을 소유하지 않습니다.</summary>
    public sealed class DataService
    {
        public IReadOnlyDictionary<string, DataDefinition> Definitions { get; }
        public RuntimeSessionData Runtime { get; private set; }

        public DataService(DataCatalog catalog = null)
        {
            Definitions = DataValidation.Validate(catalog);
        }
        public bool TryGetDefinition(string id, out DataDefinition definition)
        {
            definition = null;
            return id != null && Definitions.TryGetValue(id, out definition);
        }

        public void BeginSession(GameSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            Runtime = new RuntimeSessionData(session);
        }

        public void AcceptSavedSnapshot(GameSession session, bool replacePayload)
        {
            if (Runtime == null || Runtime.SessionId != session.SessionId) BeginSession(session);
            else if (replacePayload) Runtime.ApplySnapshot(session.PayloadJson);
        }

        public void EndSession() => Runtime = null;
    }
}