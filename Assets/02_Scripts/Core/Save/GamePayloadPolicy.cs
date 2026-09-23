using UnityEngine;

namespace StarterProject
{
    /// <summary>
    /// 게임별 저장 데이터의 버전·초기값·검증 규칙을 Core 수정 없이 연결하는 확장 지점입니다.
    /// 게임 프로젝트에서 파생 ScriptableObject를 만들고 Boot의 AppBootstrap에 할당합니다.
    /// </summary>
    public abstract class GamePayloadPolicy : ScriptableObject
    {
        /// <summary>게임 데이터 형식 버전입니다. 형식 변경 시 마이그레이션 정책과 함께 올립니다.</summary>
        public virtual int PayloadVersion => 1;

        /// <summary>새 게임과 Editor의 Main 직접 Play가 사용할 초기 JSON 객체입니다.</summary>
        public virtual string CreateInitialPayload() => "{}";

        /// <summary>저장 또는 불러오려는 JSON 객체의 게임별 필드를 검사합니다.</summary>
        public virtual void ValidatePayload(string payloadJson) { }
    }
}
