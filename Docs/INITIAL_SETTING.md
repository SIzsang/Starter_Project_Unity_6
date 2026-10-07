# 아키텍처와 데이터 설계 기준

현재 구현의 책임·의존 방향·초기화·데이터 수명을 설명합니다. 진행 상태는 [로드맵](ROADMAP.md), API 예제는 [실행 기반 사용법](PHASE_2_RUNTIME.md), 확인된 결과는 [검증 현황](INTEGRATION_VALIDATION.md)을 따릅니다.

## 이 문서의 순서

- [계층과 의존 방향](#계층과-의존-방향)
- [실행 객체의 소유권](#실행-객체의-소유권)
- [초기화 순서](#초기화-순서)
- [데이터와 저장 경계](#데이터와-저장-경계)
- [객체 수명과 비동기](#객체-수명과-비동기)
- [교체 경계와 설계 원칙](#교체-경계와-설계-원칙)
- [관련 문서](#관련-문서)

## 계층과 의존 방향

| 계층 | 책임 | 참조 기준 |
| --- | --- | --- |
| Core | 앱 실행·설정·저장·씬 수명·입력 상황·Pause/Audio | UI·Editor·Game Layer·선택 모듈을 참조하지 않음 |
| UI | 화면 표시·Input System 어댑터·사용자 요청 | Core 사용 |
| Editor | 개발 진입·검사·Debug·빌드 | Editor 어셈블리에서 Core/UI 사용 |
| Modules/Pooling | 프리팹 대여·반납·소유 객체 수명 | Unity만 참조. 게임이 선택 연결 |
| Game Layer | 게임 규칙·콘텐츠·조작·저장 모델 | 필요한 Core/UI/Module 계약 사용 |

게임 코드가 JSON·UI·Pool 타입을 직접 쓰면 해당 asmdef 참조를 명시합니다. [에셋·어셈블리 구조](../Assets/PROJECT_STRUCTURE.md)

## 실행 객체의 소유권

```text
Boot / AppBootstrap
  └─ AppRoot
      ├─ AppBootstrapper → AppServices
      │                    ├─ Data / Settings / Game
      │                    └─ Input / Pause / Audio
      ├─ GameStateController
      ├─ AsyncLifetime
      └─ SceneFlow → 활성 SceneRoot → Game Layer
```

AppRoot는 앱 소유권과 외부 요청을 조정합니다. AppBootstrapper는 초기화 순서, AppServices는 타입별 서비스와 해제, SceneFlow는 씬 전환, SceneRoot는 씬 내부 조립·실행·종료를 담당합니다. 서비스 등록 컨테이너를 추가하지 않습니다.

## 초기화 순서

1. AppConfig·선택형 DataCatalog의 필수 값과 ID/참조를 검증합니다.
2. 실행용 Config 사본과 Definition 조회 목록을 준비합니다.
3. Settings·Game 저장 정보를 읽고 Input·Pause·Audio를 조립합니다.
4. Runtime Settings에 음량·화면 값을 적용합니다.
5. 활성 SceneRoot의 Initialize → Enter를 완료합니다.
6. Ready를 공개하고 Title 또는 개발용 Main 흐름으로 진입합니다.

Awake의 객체 간 순서에 기대지 않습니다. 필수 실패는 Failed로 전환해 진입을 막습니다. 현재 복구 방법은 설정 수정 후 Play/앱 재시작입니다.

## 데이터와 저장 경계

| 종류 | 표현 | 변경·저장 기준 |
| --- | --- | --- |
| 기본 설정·고정 콘텐츠 | ScriptableObject / DataDefinition | 공유 원본. 플레이 중 수정하지 않음 |
| 사용자 설정 | UserSettings + settings.json | 전체 슬롯 공유. 검증·시스템 적용·저장 |
| 실행 상태 | RuntimeSessionData | 플레이 중 변경. 원본 Definition과 저장 스냅샷 분리 |
| 게임 저장 | GameSession + JSON payload | 명시적 저장 시 Runtime 스냅샷. 게임이 필드·버전·규칙 결정 |

기본 슬롯은 3개이며 활성 진행은 하나입니다. 기존 슬롯 1 파일·저장 schemaVersion 2를 유지합니다. 파일당 기본 1 MiB, JSON 깊이 32, 메인 스레드의 동기 파일 I/O와 한 앱 소유권을 전제로 합니다. [설정·저장 계약](SETTINGS_AND_SAVE.md)

저장 실패는 작업 데이터와 기존 파일을 보호합니다. 미래 버전은 덮어쓰지 않으며 백업 복구는 명시적으로 확정합니다. Title 복귀는 이전 Scene의 Exit/Dispose가 끝난 뒤 Runtime과 세션을 해제하고 자동 저장하지 않습니다.

## 객체 수명과 비동기

앱 토큰은 실패/파괴, 씬 토큰은 Exit 시작/Dispose, 객체 토큰은 파괴 시 취소됩니다. Game Layer는 전달된 토큰을 사용하고 await 이후 결과 적용 전에 취소를 확인합니다.

정상 씬 전환은 Exit → Dispose를 거칩니다. Exit 대기는 이미 취소된 씬 토큰 대신 앱 토큰을 사용합니다. 강제 파괴·실패는 Dispose를 보장합니다. 취소할 수 없는 Unity 씬 로드는 끝까지 관찰합니다.

Pool 반납은 객체 파괴와 다릅니다. 재사용 객체의 비동기 결과는 씬 토큰과 PoolLease.IsValid를 함께 확인합니다.

## 교체 경계와 설계 원칙

ITextFileStore는 Begin 전에 주입하며 앱은 빌려 씁니다. IRuntimeSettings는 앱이 성공·실패 모두 소유하고 해제합니다. GamePayloadPolicy는 게임이 초기 payload·버전·검증을 공급합니다.

Singleton 남용·DI Container·전역 Event Bus·모든 타입의 인터페이스화는 도입하지 않습니다. 실제 반복 요구가 생긴 경계만 확장합니다. [설계 검토 기록](ARCHITECTURE_REVIEW.md)

## 관련 문서

- [실행 상태](GAME_STATE.md) · [SceneRoot·Runtime·입력·Audio](PHASE_2_RUNTIME.md)
- [개발 지원](PHASE_3_DEVELOPMENT.md) · [Pool](PHASE_4_MODULES.md)
- [초기 설계 제안 원문](Archive/INITIAL_SETTING_PROPOSAL.md)
