# 문서 안내

문서의 목적을 구분해 필요한 것부터 읽습니다. 현재 상태는 개요/로드맵, 사용 방법은 기능 가이드, 실제 결과는 검증 기록을 기준으로 합니다.

## 처음 시작할 때

1. [프리셋 개요](PRESET_SUMMARY.md) — 현재 단계·제공 기능·확인 범위
2. [새 게임 시작](NEW_GAME_SETUP.md) — 복제부터 게임 데이터·입력 연결까지
3. [복제·인계 체크리스트](TEMPLATE_CHECKLIST.md) — 새 게임에서 바꿀 값과 확인 항목

## 목적별 사용 가이드

| 질문 | 문서 |
| --- | --- |
| 앱은 어디서 시작되는가? | [Bootstrap](BOOTSTRAP.md) |
| 설정·저장·삭제·복구는 어떻게 쓰는가? | [설정과 저장](SETTINGS_AND_SAVE.md) |
| 씬·서비스·Runtime·취소·입력·Pause·Audio는 누가 소유하는가? | [Phase 2 실행 기반](PHASE_2_RUNTIME.md) · [GameState](GAME_STATE.md) |
| 기본 화면·로딩·설정은 어떻게 연결되는가? | [공통 기능](COMMON_SERVICES.md) |
| UI 이미지·로딩 프리팹·화면 크기는 어떻게 바꾸는가? | [반응형 UI](RESPONSIVE_UI.md) |
| 로그·Debug·빌드·Validator·Smoke는 어떻게 쓰는가? | [Phase 3 개발 지원](PHASE_3_DEVELOPMENT.md) · [Editor 워크플로](EDITOR_WORKFLOW.md) |
| Pool은 어떻게 선택해서 사용하는가? | [Phase 4 Pool](PHASE_4_MODULES.md) |

## 방향·설계·검증

| 문서 | 담당 내용 |
| --- | --- |
| [로드맵](ROADMAP.md) | 현재 Phase와 다음 방향 |
| [후속 계획](NEXT_STEPS.md) | 다음 Work의 범위·완료 기준 |
| [설계 기준](INITIAL_SETTING.md) | 현재 책임·데이터·수명 경계 |
| [에셋 구조](../Assets/PROJECT_STRUCTURE.md) | 실제 폴더·어셈블리·명명 |
| [게임 제작 범위](GAME_CAPABILITY_BRIEF.md) | 제공 기반과 게임별 구현 |
| [검증 현황](INTEGRATION_VALIDATION.md) | 실제 실행 종류·결과·한계 |
| [변경 이력](CHANGELOG.md) | 날짜별 의미 있는 변경 |

## 버전·검토·조사 기록

아래 문서는 특정 시점의 판단과 근거입니다. 현재 사용법은 위 가이드를 우선합니다.

| 문서 | 기록 범위 |
| --- | --- |
| [v1.0.0 배포](RELEASE.md) | 이전 단일 슬롯 버전의 배포·검증 |
| [제작 시작 전 점검](GAME_START_AUDIT.md) | 2026-10-04 당시 공통 문제·제약 |
| [재사용성 검토](REUSABILITY_REVIEW.md) | 2026-10-04 UI·설정 검사 보완 |
| [설계 검토](ARCHITECTURE_REVIEW.md) | 2026-10-04/05 SOLID·확장 계약 |
| [메뉴·슬롯 초안](MAIN_MENU_AND_SAVE_SLOTS_DRAFT.md) | 최초 조사·설계와 이후 구현 구분 |
| [로그라이크 조사](ROGUELIKE_PLAY_FLOW_RESEARCH.md) | 장르별 흐름 참고·공통 적용 경계 |
| [과거 기록 보관함](Archive/README.md) | 이전 로드맵·초안·인계·검증 원문 |

## 문서 관리 기준

문서 하나에 현재 사양·과거 로그·다음 작업을 모두 누적하지 않습니다. 제목은 문서 목적, 대제목은 주제, 소제목은 해당 주제의 사용 절차나 계약을 나타냅니다.

현재 안내를 바꾸면 관련 사용법만 갱신합니다. 구현 이력은 CHANGELOG, 검증 근거는 INTEGRATION_VALIDATION, 이전 상세 서술은 Archive에 남깁니다. 실행하지 않은 검사·기기·빌드를 완료로 표시하지 않습니다.

[Notion 문서 허브](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50)는 같은 역할 기준으로 관리합니다. Work는 00 로드맵 → 11 체크포인트 → Git/실제 코드를 확인합니다.
