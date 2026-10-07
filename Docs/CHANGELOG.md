# 변경 이력

## 2026-10-07 — 소스 커밋 분리·현재 인계 기준 정리

- 완료 기반을 실행·진단 / Editor 개발 도구 / 선택형 Pool / 문서·인계로 분리해 로컬 커밋했다. 커밋 제목은 영어, 본문은 한국어를 사용했다.
- 개인 Unity Cloud 설정은 원본에 보존하고 커밋에서 제외했다. 신규 스크립트/폴더 메타 누락·중복 GUID와 최종 Gate 소스 60개 일치를 확인했다.
- README·Summary·새 게임 가이드에서 현재 사용 시작점을 명시했다. Phase 2·3와 선정 Phase 4 완료, Windows·URP 2D 게임 제작 착수 가능으로 정리했다.
- 후속 소스·문서 정리는 완료했다. 다음 권장 작업은 작은 Game Layer 적용 사례다. 새 Player 빌드·Unity 테스트·원격 Push·태그·Release는 수행하지 않았다.
- 현재 기능 소스 기준: 실행·진단 52688c0 / 개발 도구 91f413a / Pool a24ff7d. 기존 v1.0.0 태그·ZIP은 유지한다.

## 2026-10-07 — Phase 4 선택형 Pool 및 Gate

- 사용자 우선 요구인 프리팹 Pool 선정. 다른 선택형 후보는 현재 필수 요구가 없어 보류.
- Unity ObjectPool 기반 PrefabPool, 대여 세대별 PoolLease, 비활성 준비/오류 회수·Prewarm·반납 재고 상한·소유자 종료 정리.
- Pool 코드/테스트를 독립 모듈 폴더·어셈블리에 격리. 기존 스크립트/메타·Core/UI/Editor/테스트 참조 보존.
- 관련 수명 검증과 최종 어셈블리 대표 검사 통과. 임시 검증 환경 모두 정리.
- 선정 범위/Gate 완료. 후속 Phase는 실제 게임 요구를 확인해 선정한다. [사용법](PHASE_4_MODULES.md) · [검증](INTEGRATION_VALIDATION.md)


## 2026-10-07 — Phase 3 개발 지원 및 Gate

- Category/Level Console Logging과 환경 기본값·Play 시작 필터 복구.
- Editor Debug Menu의 상태/서비스 조회·기존 앱 명령·수명에 연결한 Debug 입력.
- Development/QA/Release의 Windows 옵션·출력 경로·Player 전용 심볼. 전역 Editor 설정 보존.
- 기존 Validate Setup의 전체 활성 씬/SceneRoot/입력/저장 정책/빌드 검사와 pre-build hook.
- 격리 실제 파일로 저장·새 앱 복원을 확인하는 최소 Smoke. 관련 위험 기반 Gate 검증과 임시 환경 정리 완료.
- Phase 3 완료. 다음 Work는 Phase 4 실제 반복 요구 평가/첫 모듈 선정. [사용법](PHASE_3_DEVELOPMENT.md) · [검증](INTEGRATION_VALIDATION.md)


## 2026-10-07 — Phase 2 실행 기반 및 Gate

- SceneRoot/SceneFlow 생명주기와 AppServices/AppBootstrapper의 공통 서비스 조립·수명 분리.
- AsyncLifetime 앱/씬/객체 취소, Definition/Runtime/저장 스냅샷 분리와 기존 설정 검사 경로의 ID·참조 검증.
- Input Context와 앱 전용 InputActionAsset 복제 어댑터, Pause 시간/입력/UI 복구, BGM/SFX/UI Audio Service 연결.
- 비동기 Exit 완료 뒤 세션을 해제하는 순서 보완. 저장 형식·기존 설정/슬롯 API 유지.
- Architecture / Integration Review 및 관련 최소 검증 완료, 임시 캐시 복사본·컴파일 산출물 삭제. [검증 근거](INTEGRATION_VALIDATION.md)
- Phase 2 완료 / Phase 3 Logging 대기. [사용법·게임 연결](PHASE_2_RUNTIME.md)

## 미배포 보완 — 2026-10-07

- Phase 2 GameState의 Boot/Menu/Loading/Gameplay/Pause/Failed 상태·전환 규칙·조회·변경 알림을 추가하고 기존 AppRoot 초기화·씬 전환·실패에 연결했다.
- 초기화용 AppState와 저장 형식은 유지한다. AppRoot는 실행 상태를 소유하며 외부에는 활성 세션의 Gameplay ↔ Pause 요청만 허용한다. 현재 Pause는 상태 계약이며 시간·입력·UI 조정은 후속 단계다.
- 관련 EditMode 11/11 통과. PlayMode 20개 중 테스트 기대값 오류 1개를 수정하고 해당 1개 재검사 통과를 확인했다. [검증 근거](INTEGRATION_VALIDATION.md) · [사용법](GAME_STATE.md). 다음 작업은 SceneRoot다.

## 미배포 보완 — 2026-10-05

- 기본 저장을 독립 슬롯 3개와 하나의 활성 세션으로 확장했다. 현재 슬롯으로만 저장하며 기존 슬롯 1 파일·JSON 형식과 공통 settings.json은 유지한다.
- 슬롯 상태의 읽기 전용 스냅샷과 명시적 시작·이어하기·복구·삭제 API를 추가했다. 슬롯 없는 시작·이어하기는 슬롯 1 호환 경로로 남긴다. 서비스 slotCount는 1~10이며 기본 AppRoot·예제 UI는 3개다.
- 읽기·쓰기 계약과 분리한 `IDeleteSaveFileStore`를 추가했다. 선택 슬롯의 정확한 복구·보존·임시 파일을 먼저, 주 파일을 마지막에 삭제하고 오류를 숨기지 않는다. 미지원 대체 저장소는 읽기·저장을 유지하면서 삭제만 거절한다.
- 좌측 메인 메뉴에 New Game / Continue / Options / Credits / PC Quit를 두고 빈 슬롯 시작과 저장 슬롯 이어하기를 분리했다. Title의 별도 관리 화면에서 삭제 확인·취소·복구를 제공한다.
- 게임별 캐릭터·스테이지·플레이 시간 필드는 추가하지 않았다. Summary·사용 가이드·설계 계약을 현재 구현에 맞췄으며 조사·화면 초안은 이력으로 보관한다.

**구현·필수 자동 검증 완료.** 관련 EditMode 77/77 통과. PlayMode 39개 항목 중 초기 실패 4개에서 Title의 기존·신규 입력 리스너 중복 연결을 수정했고, 관련 8/8 재검사 통과를 확인했다. [실행 근거](INTEGRATION_VALIDATION.md). 기존 v1.0.0 태그·ZIP은 바꾸지 않는다. [사용법·API·삭제 계약](SETTINGS_AND_SAVE.md) · [Summary](PRESET_SUMMARY.md)

## 미배포 보완 — 2026-10-04

코드 구조 재검토에서 Core의 언어 en/ko 제한을 제거하고 게임별 식별자의 저장·복원을 허용했다. 저장 검증의 FormatException·OverflowException을 읽기와 같은 데이터 오류로 처리해 기존 저장·세션을 보호한다. 저장소·payload의 수명·반복 검증 계약과 Summary의 SOLID·적용 패턴·확장 경계를 보강했다. 기존 설정 형식과 예제 언어 토글은 유지한다. [설계 검토](ARCHITECTURE_REVIEW.md)

후속 범용성·사용성 검토에서는 예제 UI 교체가 Main 직접 Play·미리보기 빌드를 막던 결합을 해소했다. 공통 `Validate Setup`과 `Validate Example UI`를 분리하고 저장 결과·이어하기 실패의 상태 알림, 사용자 로딩 Image의 진행률, 전체 화면 조작 Panel의 안전 영역을 보완했다. [재검토 결과](REUSABILITY_REVIEW.md)

- 활성 게임 세션의 저장을 Main 이외의 Gameplay 씬에서도 허용했다. Boot·Title·전환 중 저장 차단과 기존 저장 보호는 유지한다.
- PC 창의 크기 조절을 허용하고 JSON 코드를 위한 어셈블리 참조 안내를 추가했다.
- Validate Setup이 Boot뿐 아니라 Title·Main의 단일 EventSystem과 필수 포인터·선택 액션을 검사하도록 보완했다.
- Boot의 비활성·중복 AppBootstrap을 설정 검사에서 거부해 초기화가 시작되지 않는 구성을 발견할 수 있게 했다.
- 캐릭터·진행 같은 게임별 데이터 검사는 제외하고 공통 저장·초기화·UI 입력 경계를 점검했다. 검증 결과는 [제작 시작 전 점검](GAME_START_AUDIT.md)에 기록한다.
- 이번 보완은 최신 main의 소스이며 기존 v1.0.0 태그·ZIP을 변경하지 않는다. [제작 시작 전 점검](GAME_START_AUDIT.md)

## v1.0.0 — 2026-10-02

- 범용 Boot → Title → Main, 설정·단일 슬롯 JSON 저장·복구, 입력·로딩, Editor Main 직접 Play와 1920×1080 기준 가로형 반응형 예제 UI를 구성했다.
- 게임별 `GamePayloadPolicy`를 Boot에 선택적으로 연결해 초기 JSON·payload 버전·검증 규칙을 Core 수정 없이 지정할 수 있게 했다. Main에서는 `AppRoot.TrySaveGame(payloadJson)`으로 실제 게임 상태를 전달한다.
- Windows Preview 실행 파일명이 복제한 게임의 Product Name을 따르도록 바꾸고, 새 게임 설정 절차와 템플릿 인계 범위를 문서화했다.
- 게임/조직 Cloud ID와 불필요한 선택 패키지를 제거하고 기본 Company/Product/Application Identifier를 중립 값으로 정리했다.
- 2026-09-24: 저장 정책 PlayMode 테스트의 문자열 구문 오류를 고쳤다. Unity 6000.3.16f1의 최신 Git 복제본에서 EditMode 50개·PlayMode 30개가 모두 통과했고 Windows 개발 빌드가 성공했다. 검증 복제본의 Product Name 변경 후 해당 이름의 실행 파일도 생성됐다.
- 2026-10-02: 예제 씬의 `Image`에 게임 스프라이트를 연결하는 절차를 명시하고, Boot `StarterScreen`에 선택형 로딩 UI 프리팹 슬롯을 추가했다. 사용자 프리팹에도 전환 중 입력 차단을 자동 적용한다. PlayMode 31개와 Windows 개발 빌드가 통과했고, 입력 차단 보완 후 관련 테스트 1개를 다시 통과했다.

첫 배포 버전은 **v1.0.0**이다. 기본 브랜치 main과 GitHub Template, 버전 태그의 소스 ZIP을 제공한다. 필수 검증 결과와 모바일·물리 장치의 미검증 범위는 [배포 가이드](RELEASE.md)에 기록한다. 이전 단계별 작업 날짜와 검증 범위는 [로드맵](ROADMAP.md)과 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 있다.
