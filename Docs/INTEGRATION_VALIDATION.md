# 7단계 통합·Windows 빌드 검증

최종 점검: 2026-10-07. **v1.0.0의 공통 프리셋 구현·필수 회귀·Windows Player 확인·배포는 완료했다.** 이 문서는 날짜별 실행 기록이며 과거의 대기·미완료 표현은 당시 상태다. 최신 main의 추가 보완은 [제작 시작 전 점검](GAME_START_AUDIT.md), 플랫폼 지원 범위와 Computer Use 제한은 [배포 기준](RELEASE.md)을 따른다.

## 2026-10-07 Phase 4 Gate — 선택형 Pool

기준 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리. 사용자는 Pool을 우선 필요로 지정했고 다른 후보는 현재 필수적일 때만 개발하도록 했다. 이번 선정 범위는 프리팹 Pool이며 그 외 후보의 필수 요구는 확인되지 않아 보류했다.

- Unity 6000.3.16f1 관련 PlayMode **11/11 통과, 실패·건너뜀 0개**. Logs/Phase4PoolPlayMode.xml 및 .log.
- 확인: 준비 전 OnEnable 방지·위치/재사용, 다른 Pool/default/중복/이전 대여 반납 거절, prepare 오류 회수·재진입 거절, 서로 다른 Prewarm 재고·상한 초과 반납 파괴, 소유자 비활성화/재활성화, 외부 부모에 둔 활성 clone과 재고의 소유자 파괴 정리, 외부 Destroy 후 무효/교체, 준비 중 소유자 종료, Pool 씬 unload 후 다른 씬 부모의 clone 정리.
- 최종 구조 리뷰에서 테스트도 Pooling 모듈 내부 StarterProject.Pooling.Tests 어셈블리로 격리했다. 기존 PlayMode 테스트 asmdef는 시작 상태의 해시로 복구했다. 새 어셈블리의 컴파일·Test Runner 검색과 대표 활성화 검사 **1/1 통과**. Logs/Phase4AssemblyPlayMode.xml 및 .log. 이전 11개와 같은 Pool/테스트 소스이며 추가 고유 항목으로 집계하지 않는다.

Architecture Review: Unity ObjectPool 저장소를 재사용하고 씬/게임 객체 소유 PrefabPool만 추가했다. Core/UI/Editor/기존 테스트 어셈블리에 모듈 참조가 없다. 모듈과 내부 테스트를 함께 제외할 수 있다. AppRoot/AppServices에 서비스 등록·Singleton·새 Generic Framework/인터페이스/패키지를 추가하지 않았다. 원본 프리팹과 게임별 reset·스폰·용량 정책을 게임이 소유하고 Pool은 clone 대여/반납/종료를 맡는다.

Integration Review: PoolLease 세대가 한 번의 대여를 나타낸다. 이전 복사본·지연 반납이 새 대여를 건드리지 않는다. 준비 후 활성화하고 실패/소유자 종료에는 회수한다. OnDisable 전체 반납, OnDestroy 전체 정리와 외부 parent의 수명을 확인했다. SceneRoot.Exit의 ReturnAll과 씬 토큰/lease.IsValid 검사 연결은 사용 계약에 명시했다. 반납이 게임의 임의 Task를 자동 취소하는 것으로 설명하지 않는다.

원본 Editor 잠금 때문에 현재 소스/캐시 임시 복사본으로 필요한 범위만 실행했다. 첫 수명 검증 뒤 구조 리뷰의 테스트 어셈블리 변경에 대해 대표 1개만 최종 확인했다. Clean Clone 검증은 아니다. 최종 C#/asmdef **60개** 원본/검증 해시 일치와 시작 시 기존 스크립트/메타 **124개 전부 불변**을 Logs/Phase4SourceHashes.json 및 Phase4Preservation.json에 기록했다. 최초 검증 해시는 Phase4SourceHashesInitial.json에 보관했다.

.utmp/Phase4Validation-20261007 및 .utmp/Phase4AssemblyValidation-20261007은 소유권·절대 경로·Git/링크 부재·Unity 종료를 확인해 모두 삭제했다. 결과 XML/로그/해시·Phase4Cleanup.json만 보관한다. 이번 Work의 Localization 초안도 사용자 범위 수정에 맞춰 생성한 파일만 제거했다. 기존 사용자 ProjectSettings와 원본 Editor를 유지했다.

전체 회귀·새 Windows 빌드·GUI/실기기·실제 게임 부하/성능 측정은 수행하지 않았다. 사용자 지정 Phase 4 범위와 Gate 완료. 후속 Phase는 현재 로드맵에 없으며 실제 게임의 필수 요구가 생기면 다음 Work에서 작업을 선정한다. [Pool 사용법](PHASE_4_MODULES.md)

## 2026-10-07 Phase 3 Gate — 개발 지원 기반

기준 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리. 원본 Unity Editor가 열려 있어 현재 Assets·Packages·ProjectSettings·캐시를 복사한 임시 프로젝트에서 Unity 6000.3.16f1로 관련 범위만 실행했다. Clean Clone 검증이 아니다.

- EditMode **20/20 통과, 실패·건너뜀 0개**. 새 Diagnostics 3개·빌드 정책 7개·Scene/저장 정책 Validator 7개와 기존 Preview/예제 UI/사용자 UI 보존 검사 3개. Logs/Phase3EditMode.xml 및 .log.
- PlayMode **4/4 통과, 실패·건너뜀 0개**. StarterSmoke 1개, 초기화 실패 로그·GameState 소비자 예외 격리·정리 예외를 동반한 초기화 실패의 기존 경계 3개. Logs/Phase3PlayMode.xml 및 .log.
- Smoke는 실제 격리 파일로 Boot→Menu→New Game→Save→새 AppRoot→Continue→Load, 세션/슬롯/payload 복원, 명시적 저장 전 파일 미생성, 작업 JSON·저장 스냅샷 분리와 다른 파일 미생성을 확인했다. 대체 Runtime Settings를 사용하며 프로세스 재시작 검증은 아니다.
- .NET SDK와 설치된 Unity CoreModule 참조로 실제 Diagnostics 소스의 Development/QA/Release 세 심볼을 각각 컴파일·실행해 환경과 Debug/Info/Warning 기본값을 확인했다. Logs/Phase3BuildSymbols.log. Unity Player 빌드·실행으로 집계하지 않는다.

Architecture Review: StarterLog/BuildEnvironment는 진단 필터·컴파일 환경만 소유한다. Debug Window/Build Configuration/Validator는 Editor에 두고 기존 Setup에서 검사와 빌드 책임을 분리했다. AppRoot에는 기존 진단 호출을 연결하며 별도 서비스 등록·새 Singleton·DI/Logger 인터페이스·패키지를 추가하지 않았다. Core는 UI·Input System·Editor·Game Layer를 참조하지 않는다.

Integration Review: Debug 명령의 AppRoot 상태·슬롯 경계와 입력 범위의 씬/앱/창 해제를 코드에서 확인했다. 테스트는 로그 필터·Console 심각도, 빌드 심볼·옵션과 전역 설정 보존, Preview Scene 검사·열린 씬 보존, 사용자 UI 허용과 저장 payload 훅 미실행을 확인했다. 기본 입력 어댑터를 쓰는 경우의 계약만 검사하고 선택형 예제 UI 검사는 분리했다.

C#/asmdef **54개**의 원본/검증 소스 SHA-256 일치를 Logs/Phase3SourceHashes.json에 기록했다. Work가 만든 .utmp/Phase3Validation-20261007 및 .utmp/Phase3Compile은 절대 경로·소유권·Git 부재·링크 부재·Unity 실행 종료를 확인해 삭제했다. Logs/Phase3Cleanup.json에 기록하고 결과 XML/로그/해시는 보관했다. 원본 Editor와 기존 사용자 ProjectSettings Cloud 변경을 유지했다.

전체 회귀·새 Windows 빌드·Clean Clone·Debug Menu GUI 직접 조작·물리 입력·실기기·오디오 청취는 수행하지 않았다. Phase 3와 Gate 완료. Phase 4 구현은 다음 Work에서 실제 반복 요구가 확인된 모듈부터 판단한다. [개발 지원 사용법](PHASE_3_DEVELOPMENT.md)

## 2026-10-07 Phase 2 Gate — 완료

기준은 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리, Unity 6000.3.16f1이다. 초기화·공용 서비스·씬/취소·Input System·저장 스냅샷 변경의 실제 위험을 확인하는 범위로 검사했다. 원본 Editor 잠금 때문에 Assets/Packages/ProjectSettings/Library 캐시의 임시 복사본에서 실행했으며 깨끗한 Git 복제 검증은 아니다.

- EditMode GameStateTests 11개와 Phase2DataInputTests 9개, **20/20 통과**, 실패·건너뜀 0개. Logs/Phase2EditMode.xml 및 로그.
- PlayMode 기존 Bootstrap 20개, 신규 Phase2Flow 5개, RuntimeFlow 중 변경 영향이 있는 7개를 실행해 **31개 통과·1개 실패**를 확인했다. 이전 GameState 테스트가 실제 Pause 단계에도 시간이 흐를 것으로 기대한 오류를 수정했다. Logs/Phase2PlayMode.xml 및 로그.
- 관련 7개 재검사(새 Pause 중 실패 복구 포함) **7/7 통과**, 실패·건너뜀 0개. 합성 AudioClip으로 BGM의 재생/일시정지/복귀 및 Pause 중 UI 채널 재생을 확인했다. Logs/Phase2PlayModeFinal.xml 및 로그.
- Integration Review에서 비동기 Exit보다 먼저 세션을 해제하던 순서를 보완하고 신규 Exit 검사와 관련 씬/Pause 9개를 실행해 **9/9 통과**, 실패·건너뜀 0개. Logs/Phase2ExitPlayMode.xml 및 로그.
- PlayMode는 **고유 34개 항목**을 실행과 재검사로 확인했으며 34/34 단일 실행으로 집계하지 않는다. 현재 미해결 실패 없음. 첫 컴파일의 새 PlayMode 테스트 Newtonsoft DLL 참조 누락은 테스트 asmdef에 기존 DLL 참조를 추가해 해결했다. 새 패키지는 추가하지 않았다.

Architecture Review: 초기화 순서는 AppBootstrapper, 타입별 서비스 소유권은 AppServices, 씬 생명주기는 SceneFlow/SceneRoot에 분리했다. AppRoot는 기존 외부 요청 API와 앱 수명·상태 연결을 유지한다. Core는 UI·Input System·Game Layer를 참조하지 않으며 저장 I/O와 게임별 규칙을 추가로 소유하지 않는다. 기존 저장소/RuntimeSettings 교체 경계만 사용하고 DI Container·Service Locator·추가 Singleton·전역 Event Bus를 만들지 않았다.

Integration Review: 설정/Definition 검증 → 서비스 조립/설정 적용 → Scene Initialize/Enter → Ready 순서를 확인했다. 앱/씬/객체 취소 후 결과 적용 차단, Runtime JSON과 저장 스냅샷 분리·실패 보존·Title/Continue, Exit 완료까지 데이터 유지, 범위 입력 복구·Player/UI 맵 전환·Loading 잠금, Pause의 시간/UI/Audio 및 실패/파괴 복구를 확인했다. 기존 키보드·게임패드 Submit, Escape 취소, 로딩 입력 차단과 Boot 왕복도 관련 범위에 포함했다. Master volume은 기존 RuntimeSettings → AudioListener.volume 경로를 재사용하며 중복 곱하지 않는 코드를 리뷰했다.

최종 C#/asmdef 47개가 검증 복사본과 일치함을 Logs/Phase2SourceHashes.json에 남겼다. Work가 만든 .utmp/Phase2Validation-20261007과 .utmp/Phase2Compile은 경로·소유권·실행 종료·링크 부재를 확인해 삭제했다. 결과 XML·로그·해시만 Logs에 보관한다. 기존 사용자 ProjectSettings 변경과 원본 Editor를 유지했다.

전체 회귀·Windows 새 빌드·GUI 시각·실기기·물리 입력·실제 오디오 청취는 수행하지 않았다. 게임별 장치 품질과 대규모 콘텐츠 요구는 별도 확인한다. Phase 2와 Gate를 완료하고 Phase 3 구현은 다음 Work에 맡긴다. [실제 사용법](PHASE_2_RUNTIME.md)
## 2026-10-07 Phase 2 — GameState 초기 작업 검증

기준 `main` / `ab5635763eae9da15dcfa0a7e6bbc4bf7950c581`의 작업 트리에 GameState를 추가했다. 원본 Editor가 열려 있어 현재 Assets·Packages·ProjectSettings와 로컬 캐시를 복사한 임시 검증 프로젝트에서 Unity **6000.3.16f1**을 실행했다. 깨끗한 Git 복제본 검증은 아니다. 테스트는 기존처럼 임시 저장소·대체 Runtime Settings를 사용한다.

- EditMode `GameStateTests` **11/11 통과, 실패·건너뜀 0개**. 허용 경로·알림 횟수·중복/잘못된 값 거절·실패 종료 상태·알림 중 재진입을 확인했다. `Logs/GameStateEditMode.xml`·`.log`.
- PlayMode `BootstrapFlowTests` **20개 항목**을 실행해 최초 **19개 통과·1개 실패·건너뜀 0개**를 확인했다. 실패는 새 테스트 기대값 오류로, 실패한 루트를 Boot에서 재사용할 때 Gameplay를 기대하고 있었다. 실제 구현의 Failed 유지가 올바르므로 기대값을 수정했다. `Logs/GameStatePlayMode.xml`·`.log`.
- 수정한 `BootDisplaysFailureAndStaysOnBoot` 하나만 다시 실행해 **1/1 통과, 실패·건너뜀 0개**를 확인했다. 최초 통과 19개와 합쳐 고유 PlayMode 20개를 확인했으며 20/20 단일 실행으로 기록하지 않는다. `Logs/GameStatePlayModeFinal.xml`·`.log`.

Boot 초기화·Main 직접 진입·같은 Title/Main에서 초기화·Title/Main 왕복·중복 전환·실패 차단·루트 재사용/파괴, 상태 전환 알림·소비자 예외 격리·요청 재진입 차단을 확인했다. 기존 슬롯·설정 흐름의 Bootstrap 회귀도 포함된다. 현재 미해결 실패는 없다.

공용 상태 계약과 초기화·씬 전환을 변경하므로 관련 검증은 필요했다. 변경 위험을 확인하는 범위로 한정했으며 전체 회귀·새 Player 빌드·GUI·실기기 검증은 수행하지 않았다. **Pause는 상태 계약만 검증했으며 Time/Input/UI 제어는 후속 작업이다.** 사용법은 [GameState 가이드](GAME_STATE.md)를 따른다.

## 2026-10-05 독립 슬롯 3개·메인 메뉴 구현 검증

기준 소스 `5ac93ac` 이후 기본 3개 독립 슬롯, 활성 슬롯 저장, Title의 좌측 메인 메뉴·목적별 슬롯 선택·별도 삭제 확인을 구현했다. Unity **6000.3.16f1**에서 원본 프로젝트로 실행했으며 테스트 저장소는 임시 디렉터리와 대체 실행 설정을 사용해 사용자 저장을 건드리지 않았다.

- `StarterProject.Editor.StarterProjectSetup.RebuildTitleMenu` 성공. Title UI만 갱신하고 기존 씬 메타 GUID와 Boot/Main을 유지했다. 생성 단계의 예제 UI 구성 검사도 통과했다(`Logs/ThreeSlotTitleSetup.log`).
- 관련 EditMode **77/77 통과, 실패·건너뜀 0개**: `PersistenceTests` 49개, `SaveSlotTests` 8개, `StarterProjectSetupTests` 20개. 결과는 `Logs/ThreeSlotEditMode.xml`·`.log`에 있다.
- 관련 PlayMode **39개 항목**: `BootstrapFlowTests` 16개, `RuntimeFlowTests` 20개, `SaveSlotFlowTests` 3개. 최초 실행은 **35개 통과·4개 실패·건너뜀 0개**였다(`Logs/ThreeSlotPlayMode.xml`·`.log`). 실패 원인은 StarterScreen이 Title 메뉴의 OnEnable 순서에 따라 기존 즉시 진입 리스너를 함께 등록하는 문제였다.
- 메뉴 참조 존재로 입력 소유권을 정하고 기존 핸들러의 우회 진입도 차단했다. 실패 4개와 영향받는 메뉴 4개만 다시 실행해 **8/8 통과, 실패·건너뜀 0개**를 확인했다(`Logs/ThreeSlotInputPlayModeFinal.xml`·`.log`). 현재 미해결 실패는 없으며 이 8개를 신규 고유 테스트로 중복 집계하지 않는다.

검사한 범위는 슬롯별 세션·payload 격리와 재생성 후 복원, 기존 슬롯 1 파일·무인자 API 호환, 활성 슬롯 저장/삭제 차단, 삭제 capability 미지원, 잘못된 번호, 손상·미래 버전 슬롯 격리, 백업 복구, 정확한 백업·보존·임시 파일 삭제 및 다른 슬롯·설정 보존이다. 잠긴 주 파일·백업의 삭제 실패도 확인했다. 삭제는 여러 파일에 걸친 원자적 트랜잭션은 아니며 실패 시 일부 보조 파일이 이미 삭제되었을 수 있다.

UI에서는 New Game 클릭은 빈 슬롯 선택 화면까지만 열고 Continue는 선택한 저장 슬롯을 이어가는지, 모두 빈 상태의 Continue 잠금·이유 안내와 모두 찬 상태의 관리 연결, 확인 대상·마지막 저장 시각, Cancel 기본 선택과 취소/성공 뒤 같은 카드 복귀, 메인 메뉴 선택 복귀, 키보드·게임패드 Submit의 두 단계와 전환 중 잠금을 확인했다. 배치·Image·패널·버튼은 씬에서 교체할 수 있고 프레임마다 저장 파일을 읽지 않는다.

이 회차에는 새 검증 복제본·Player 빌드·Computer Use·실제 창 표시·모바일 실기기 검사를 추가하지 않았다. UI 구성과 합성 입력의 결과를 실제 기기 검증으로 간주하지 않는다. 기존 v1.0.0 태그·ZIP은 단일 슬롯 소스 그대로 유지하며 최신 사용법은 [설정·저장 가이드](SETTINGS_AND_SAVE.md)와 [Summary](PRESET_SUMMARY.md)에 기록했다.

## 2026-10-04 코드 재사용·SOLID·확장 계약 점검

`bfd1929` 기준 검토 후 언어 식별자의 en/ko 고정과 쓰기 검증 예외 처리의 불일치를 수정했다. 원본 프로젝트에서 Unity 6000.3.16f1의 `PersistenceTests`·`RuntimeSettingsTests`만 실행해 **57/57 통과, 실패·건너뜀 0개**를 확인했다. 신규 16개 사례와 해당 경계의 기존 회귀를 포함한다.

- 게임별 언어 식별자의 기본값·저장·재생성 후 복원, 빈 값·공백·제어 문자 거부와 파일 로드 시 기본값 복구, 최대 64자 경계
- 새 언어 식별자가 음량·화면 설정 적용을 막지 않는지 확인
- payload 검증의 FormatException·OverflowException은 false로 반환하고 기존 세션·주 파일을 보존하는지 확인
- 예상 데이터 오류가 아닌 InvalidOperationException은 숨기지 않는지 확인
- 기존 백업·버전 보호·파일 실패·실행 설정 복원 관련 회귀

결과는 `Logs/SolidContractsEditMode.xml`·`Logs/SolidContractsEditMode.log`에 기록했다. 테스트는 임시 저장소와 대체 실행 설정을 사용한다. 추가 검증 복제본·Player 빌드·GUI·모바일 실기기 검증은 수행하지 않았다. 수정하지 않은 씬 전환·UI 흐름의 PlayMode 검사를 반복하지 않았다. 코드의 책임·소유권·의도된 확장 한계는 [설계 검토](ARCHITECTURE_REVIEW.md)와 [Summary](PRESET_SUMMARY.md)에 반영했다.

## 2026-10-04 범용성·사용성 후속 재검토

기준 커밋 `8da41b6`의 공통 검사와 예제 화면 결합, 저장 결과 알림 누락, 사용자 로딩 Image의 진행률, 전체 화면 Panel의 안전 영역 적용을 보완했다. 관련 EditMode·PlayMode 검사와 범위는 [재검토 보고서](REUSABILITY_REVIEW.md)를 따른다. 새 Player 빌드와 Computer Use·실기기 검증은 추가하지 않았다.

## 2026-10-04 1차 범용 템플릿 사용 경계 점검

캐릭터 능력치·스테이지 진행·전투 등 게임별 구현은 제외하고 원본 프로젝트에서 관련 테스트만 실행했다. Main을 언로드한 추가 씬의 저장·Continue와 Title·전환 저장 차단을 포함한 `RuntimeFlowTests` **15/15**, 세 씬의 UI 입력과 Boot 초기화 진입점 누락·비활성·중복을 다루는 `StarterProjectSetupTests` **16/16**이 통과했다. 모두 실패·건너뜀 0개이며 테스트 저장소는 사용자 저장과 분리됐다.

증거는 `Logs/GameStartAuditPlayMode.xml`·`.log`와 `Logs/GameStartAuditEditModeFinal.xml`·`.log`다. 새 검증 복제본·시험용 Player 빌드는 생성하지 않았다. PC `resizableWindow` 활성화는 설정으로 확인했으며 직접 창 조절·물리 입력·모바일 실기기 검증은 추가하지 않았다. 코드 보완은 최신 main에 기록하며 v1.0.0 태그·ZIP은 유지한다.

## 검증 환경

- Unity 6000.3.16f1, Windows x64 Development 빌드. 검증 복제본 `Logs/Stage34Validation`에서 실행했다.
- 복제본의 `productName`만 `StarterProjectStage7Validation`으로 바꿔 사용자 저장 경로를 원본과 분리했다. 원본 프로젝트 설정과 커밋에는 반영하지 않았다.
- 빌드 내부 흐름 검증용 `Stage7PlayerSmoke.cs`는 복제본에만 넣었다. 결과 빌드와 로그는 Git에서 제외되는 `Logs` 아래에 있다.
- 6단계 기준점은 로컬 커밋 `335cd3b`다. 원본 작업 트리에 7단계 런타임 검증 코드를 추가하지 않았다.

## 자동 검증 결과

| 항목 | 결과 | 근거 |
| --- | --- | --- |
| 전체 Unity 테스트 | EditMode 46개·PlayMode 26개 통과, 실패·건너뜀 0개 | `Logs/Stage6EditModeFinal.xml`, `Logs/Stage6PlayModeFinal.xml` |
| Windows x64 개발 빌드 | 성공, Unity BuildReport 172,158,828 bytes | `Logs/Stage7WindowsBuild.log` |
| 실행 파일 시작 | 그래픽 출력 없는 빌드 프로세스가 Unity Player를 초기화 | 복제본 `Builds/Windows/Stage7PlayerStart.log` |
| 정상 흐름, 첫 프로세스 | Boot → Title → New Game → Main, 설정과 게임 payload 저장 | 복제본 `Builds/Windows/Stage7PlayerSmokeResult.txt`의 `CREATE_PASS` |
| 정상 흐름, 재실행 프로세스 | 설정값·게임 세션과 payload 복원, Continue → Main → Title, AppRoot 1개 | 같은 결과 파일의 `RESUME_PASS` |
| 오류 흐름, 새 프로세스 | 파손 설정은 `Invalid`, 미래 버전 게임 저장은 `UnsupportedVersion`; Continue와 덮어쓰기를 막고 두 원본 파일을 보존 | 같은 결과 파일의 `ERROR_PASS`, `Stage7PlayerVerifyErrors.log` |
| 백업 복구, 새 프로세스 | 파손된 설정·게임 주 파일에서 정상 백업을 `Recovered`로 표시하고 명시적 복구 후 Continue 성공; 손상 원본은 별도 `.preserved-*` 파일로 보존 | 같은 결과 파일의 `BACKUP_PASS`, `Stage7PlayerVerifyBackup.log` |
| 저장 완료 직후 강제 종료 | 저장 API가 성공한 직후 Player를 강제 종료하고 다음 프로세스에서 같은 세션·payload로 Continue 성공 | 같은 결과 파일의 `KILL_AFTER_SAVE_READY`, `KILL_RECOVERY_PASS` |
| 저장 도중 강제 종료 | 복제본에만 넣은 지연 지점에서 임시 파일 기록·flush 후, 주 파일 교체 전에 Player를 강제 종료했다. 다음 프로세스에서 이전 정상 저장으로 Continue 성공했고 주 파일·`.bak`은 이전 payload를 유지했다 | 같은 결과 파일의 `INTERRUPT_BEFORE_REPLACE`, `INTERRUPT_RECOVERY_PASS`, `Stage7PlayerVerifyInterruptedWrite.log` |
| 그래픽 장치 경로 | Direct3D 11, NVIDIA GeForce GTX 1660 SUPER에서 오류 흐름 검증 프로세스 정상 종료 | 복제본 `Builds/Windows/Stage7PlayerGraphics.log` |
| 설계 보완 후 회귀 | 설정 플랫폼 적용·저장 순서 변경 후 EditMode 46개·PlayMode 27개 통과. 새 Windows 개발 빌드의 다른 두 Player 프로세스에서 첫 저장·재실행 Continue 성공 | `Logs/SolidEditMode.xml`, `Logs/SolidPlayMode.xml`, `Logs/SolidWindowsBuild.log`, 복제본 `Builds/Windows/SolidPlayerCreate.log`·`SolidPlayerResume.log` |
| Boot 구성 보완 후 회귀 | `Validate Setup`이 Boot 상태 표시 Canvas·Text와 EventSystem·UI 액션을 확인한다. EditMode 46개·PlayMode 27개 통과, 새 Windows 빌드에서 별도 프로세스 저장·재실행 Continue 성공 | `Logs/BootstrapEditModeFinal.xml`, `Logs/BootstrapPlayModeFinal.xml`, `Logs/BootstrapWindowsBuild.log`, 복제본 `Builds/Windows/BootstrapPlayerCreate.log`·`BootstrapPlayerResume.log` |
| 가로형 반응형 UI 후 회귀 | Boot·Title·Main과 로딩 화면의 안전 영역 맞춤, 모바일 가로 좌·우 방향 허용. 합성 가로 화면·PC 창 크기 테스트를 포함해 EditMode 46개·PlayMode 29개 통과. Windows 개발 빌드 172,177,793 bytes, 별도 프로세스 첫 저장·이어하기 성공 | `Logs/ResponsiveEditModeValidated.xml`, `Logs/ResponsivePlayModeValidated.xml`, `Logs/ResponsiveWindowsBuildValidated.log`, 복제본 `Builds/Windows/ResponsivePlayerCreate.log`·`ResponsivePlayerResume.log` |
| 1920×1080 디자인 좌표 이관 후 회귀 | 세 예제 씬과 동적 로딩 UI의 디자인 좌표를 1920×1080으로 통일. EditMode 46개·PlayMode 29개 통과. 격리 제품명의 Windows 개발 빌드 172,177,785 bytes와 서로 다른 헤드리스 Player 프로세스의 첫 저장·이어하기 성공 | `Logs/FullHdMigratedEditMode.xml`, `Logs/FullHdFinalPlayMode.xml`, `Logs/FullHdIsolatedWindowsBuild.log`, 복제본 `Builds/Windows/FullHdIsolatedcreate.log`·`FullHdIsolatedresume.log` |
| 원본 Editor 메뉴 | 열린 Unity Editor에서 `Tools > Starter Project > Validate Setup`을 실제 메뉴 키보드 경로로 실행해 성공 로그 확인. Main 직접 Play·대화상자·화면 시각 검증은 별개 | `C:/Users/ausqk/AppData/Local/Unity/Editor/Editor.log`의 `Setup validation passed` |
| 깨끗한 Git 복제본 | `966f33f`를 새 폴더에 복제하고 기존 Library 없이 패키지·에셋을 첫 가져오기. EditMode 46개·PlayMode 29개 통과, Windows 개발 빌드 172,168,120 bytes 성공 | `Logs/TemplateCleanEditMode.xml`, `Logs/TemplateCleanPlayMode.xml`, `Logs/TemplateCleanWindowsBuild.log` |
| 템플릿 정리 복제본 | 미사용 패키지 3개 제거 후 EditMode 46개·PlayMode 29개와 Windows 빌드(169,600,783 bytes) 통과. 중립 회사명·제품명·앱 ID와 비활성 Cloud ID 제거 후 EditMode 46개·PlayMode 29개 및 Windows 빌드(169,600,343 bytes) 재통과. 이 검증은 복제본에서 수행했으며 원본 변경 후 새 최종 복제본 검증은 별개 | `Logs/PackageTrimEditMode.xml`, `Logs/PackageTrimPlayMode.xml`, `Logs/PackageTrimWindowsBuild.log`, `Logs/TemplateIdentityEditMode.xml`, `Logs/TemplateIdentityPlayMode.xml`, `Logs/TemplateIdentityWindowsBuild.log` |
| 템플릿 저장 경로 스모크 | 일회성 검증 코드와 `StarterProjectTemplateSmoke` 제품명을 복제본에만 추가했다. 서로 다른 헤드리스 Windows Player 프로세스에서 첫 저장·재실행 Continue가 각각 CREATE_PASS·RESUME_PASS로 종료. 실제 게임별 저장 모델은 검증 대상이 아님 | `Logs/TemplateSmokeWindowsBuild.log`, 복제본 `Builds/Windows/Stage7PlayerSmokeResult.txt`·`Templatecreate.log`·`Templateresume.log` |
| 정리 후 최종 새 Git 복제본 | `3460339`를 비어 있는 새 폴더로 복제. 첫 패키지·에셋 가져오기부터 EditMode 46개·PlayMode 29개 통과, Windows 개발 빌드 169,600,344 bytes 성공. Cloud 프로젝트·조직 ID 공란과 선택 패키지 3개 제거 상태 유지 | `Logs/TemplateFinalEditMode.xml`, `Logs/TemplateFinalPlayMode.xml`, `Logs/TemplateFinalWindowsBuild.log` |

`CREATE_PASS`와 `RESUME_PASS`는 **서로 다른 Windows Player 프로세스**에서 생성했다. 저장 위치는 검증 전용 제품명 아래의 `Application.persistentDataPath/StarterData`다. `ERROR_PASS`는 파손 파일 준비와 검증을 또 다른 두 프로세스로 실행했고, Direct3D 11 경로에서도 한 번 더 확인했다. 이어 별도 두 프로세스로 정상 백업을 준비·복구해 `BACKUP_PASS`를 확인했다. 완료 직후 강제 종료와 재실행도 다른 프로세스에서 확인했다. 저장 도중 종료 실험에서는 복제본의 `JsonFileStore`에만 임시 파일 flush 직후 일시 정지 지점을 넣고 외부에서 해당 Player를 종료했다. 남은 `.tmp` 파일은 교체되지 않은 새 payload를 담고 있으며 정상 주 파일·백업은 이전 payload를 유지했다. 원본 템플릿 코드에는 이 지연 지점이나 일회성 런타임 검사 코드를 넣지 않았다. 이 검증용 빌드는 배포물로 사용하지 않는다.

## 2026-09-23 당시 남은 확인

1. 원본 Editor GUI에서 Main 직접 Play 두 번, 미저장 씬·기존 시작 씬 설정과 메뉴 확인·취소를 눈으로 확인한다. `Validate Setup` 메뉴는 원본 Editor에서 실행해 통과했지만 Main 씬 열기용 Windows 파일 대화상자는 자동화 도구가 입력 대상으로 잡지 못했다. 복제본 자동 검증과 구분한다.
2. 잘못된 AppConfig·입력 참조의 Console 안내, Reset Test Data의 백업 경로·파일과 취소 동작을 GUI에서 확인한다.
3. Windows 창/전체화면 전환, 실제 오디오 출력, 키보드·게임패드 선택·확인·취소를 화면과 장치에서 확인한다.
4. 일반 GUI 실행에서 저장 도중 강제 종료와 재시작을 확인한다. 현재 자동 검증은 계측 복제본의 **임시 파일 flush 후·주 파일 교체 전** 한 지점을 다루며, 전원 손실·파일 시스템별 동작이나 교체 도중의 모든 중단 시점을 증명하지 않는다. 이 실험에서 중단된 `.tmp` 파일은 남으므로 장기 사용 시 정리 정책은 후속 검토가 필요하다.
5. 실제 가로형 모바일 기기에서 노치·안전 영역, 긴 문구의 가독성, 터치 목표 크기와 좌·우 회전을 확인한다. 현재 설치에는 Android·iOS 빌드 모듈이 없어 모바일 패키지 검증을 수행하지 않았다. [UI 기준](RESPONSIVE_UI.md)

첫 지원 환경의 일반 실행과 화면 품질까지 확인한 뒤 7단계를 완료로 기록한다. 현재 자동 검증은 Windows 빌드의 시작·저장·재실행 복원 및 주요 오류 보호가 동작한다는 범위로 해석한다.

설정 저장의 플랫폼 적용 실패가 새 JSON을 남기지 않도록 보완한 근거와 SOLID 책임 경계는 [설계 점검](ARCHITECTURE_REVIEW.md)에 기록한다. 회귀 빌드에서는 검증 복제본의 `productName`을 `StarterProjectSolidValidation`으로 바꿔 이전 Stage7 저장과 경로를 분리했다. 검증 스크립트와 제품명 변경은 원본 프로젝트에 포함하지 않는다.

Boot 회귀 빌드는 복제본에서만 `productName`을 `StarterProjectBootstrapValidation`으로 바꿔 새 경로에서 실행했다. 숨겨진 그래픽 Player의 Boot 캡처 파일은 검은 화면이어서 화면 배치·가독성의 증거가 아니다. 일회성 캡처 코드와 Boot 지연 코드는 검증 복제본에만 있다.

1920×1080 회귀 빌드는 복제본의 `productName`을 `StarterProjectFullHdValidation`으로 바꿔 이전 테스트 저장과 분리했다. 그래픽 Player 초기화가 정체되어 해당 시도는 결과로 세지 않았고, 헤드리스 모드에서 제한 시간을 둔 새 게임·이어하기 검증을 완료했다. 디자인 좌표·안전 영역의 시각적 품질은 원본 Editor GUI와 실제 기기에서 계속 확인해야 한다.

새 Git 복제본은 Unity 첫 실행·테스트·빌드까지 통과했다. Unity가 첫 가져오기와 빌드 중 일부 URP 설정을 재직렬화하고 기본 `ProjectSettings/SceneTemplateSettings.json`을 생성했으므로, 이 자동 생성 변경을 기능 수정으로 복사하지 않았다. 패키지·식별 정보 실험은 이 복제본에서 이어 했고, 원본 코드·씬 참조를 직접 변경하지 않았다.
템플릿 저장 스모크의 제품명 변경과 검증 스크립트도 Git 제외 복제본에만 있다. 검증 저장은 `Application.persistentDataPath`의 `StarterTemplate/StarterProjectTemplateSmoke` 아래로 격리해 기본 템플릿의 저장 공간과 분리했다.

2026-09-23 후속 저장 정책·빌드명 변경: 구현 완료 뒤 복제본에서 Unity 명령줄 EditMode 회귀 검증을 시도했다. 테스트가 시작되기 전에 Licensing Client IPC 연결이 거부되고 `com.unity.editor.headless` 라이선스 오류가 발생해 중단했다. 새 테스트의 통과 수는 **0개가 아니라 미실행**이며 PlayMode와 새 Windows 빌드도 시작하지 않았다. 복제본의 생성된 C# 프로젝트에서 Core 단독 `dotnet build`는 오류 0개로 끝났으나 Unity 패키지 참조 경고 2개가 있어 Unity 테스트·빌드 완료 증거가 아니다. 사용자 요청에 따라 Computer Use GUI 검증은 진행하지 않았다.

2026-09-24 Console 오류 수정: 원본 Editor 로그의 최신 C# 오류는 `BootstrapFlowTests.cs`의 누락된 문자열 닫는 따옴표에 따른 `CS1010`·`CS1003`·`CS1026`이었다. 수정 후 원본에서 생성된 `StarterProject.PlayModeTests.csproj`와 `StarterProject.EditModeTests.csproj`를 `dotnet build --no-restore`로 각각 컴파일해 모두 경고·오류 0개를 확인했다. 열린 Unity Editor의 Console 표시는 다음 에셋 새로고침 전까지 이전 오류를 남길 수 있다. 별도 복제본 Unity 명령줄 테스트는 Licensing Client IPC 연결 실패가 반복되어 테스트 시작 전 중단했다. 이번 소스의 Unity Test Runner 통과나 Windows Player 빌드 성공은 아직 주장하지 않는다.

## 2026-09-24 최신 코드·깨끗한 Git 복제본 회귀 검증

권한이 허용된 명령줄 경로에서 Unity 6000.3.16f1의 Licensing Client 연결이 성공했다. `-runTests`에 `-quit`을 함께 전달하면 스크립트 컴파일 뒤 테스트 시작 전에 종료되어, Test Runner 실행에서는 `-quit`을 제거했다. 이전 라이선스 실패와 조기 종료는 **미실행 시도**로 남기고 아래 통과 결과와 구분한다.

| 대상 | 결과 | 증거 |
| --- | --- | --- |
| 최신 추적 소스가 원본과 SHA-256 일치하는 기존 검증 복제본 | EditMode 50/50·PlayMode 30/30, Windows 개발 빌드 성공 | `Logs/TemplateFinalValidation/Logs/PostFixEditMode.xml`, `PostFixPlayMode.xml`, `PostFixWindowsBuild.log` |
| `6a31e97`의 새 Git 복제본, 빈 Library에서 패키지·에셋 첫 가져오기 | EditMode 50/50·PlayMode 30/30, 실패·건너뜀 0개. Windows 개발 빌드 성공(169,602,028 bytes) | `Logs/TemplateLatestEditMode.xml`, `Logs/TemplateLatestPlayMode.xml`, `Logs/TemplateLatestWindowsBuild.log` |
| 새 복제본의 Product Name만 `StarterProjectTemplateProbe`로 변경 | 추가 Windows 개발 빌드 성공(169,602,070 bytes), `StarterProjectTemplateProbe.exe` 생성 | `Logs/TemplateLatestProductNameBuild.log` |

원본의 추적 파일 155개에 Library·Temp·Logs·UserSettings·Builds·IDE 생성 파일은 없고, 추적 에셋과 `.meta`의 누락 쌍도 없다. 원본 Editor 로그에는 오류 수정 후 새 C# 컴파일 실패가 없었다. Unity가 검증 복제본에서 재직렬화한 URP·프로젝트 설정과 생성 파일은 원본으로 복사하지 않았다. 테스트 빌드 산출물은 검증 후 정리하고 로그·XML은 `Logs/`에 남긴다. GUI Main 직접 Play, 화면·오디오·실제 입력, 모바일 기기, Unity Hub Cloud 연결 표시와 원격 배포는 계속 별도 확인 사항이다.

## 2026-10-02 UI 에셋 교체 경로 검증

예제 씬의 배경·버튼 `Image`는 Inspector에서 스프라이트를 연결할 수 있다. 로딩 화면은 Boot `StarterScreen`의 선택형 `StarterLoadingOverlay` 프리팹 슬롯으로 교체 가능하게 했고, 사용자 프리팹에서도 전환 중 포인터 입력 차단이 유지되도록 투명 막을 생성한다. 기존 기본 로딩 화면 경로는 그대로 유지한다.

최신 UI·테스트 소스를 기존 검증 복제본에 해시 일치로 반영한 뒤 Unity 6000.3.16f1 PlayMode **31/31**을 통과했다(`Logs/ReleaseCandidatePlayMode.xml`). 입력 차단 보완 후 해당 신규 테스트 **1/1**을 다시 통과했다(`Logs/LoadingPrefabPlayMode.xml`). 변경 직후 Windows 개발 빌드와 최종 입력 차단 보완 후 빌드도 성공했다(`Logs/ReleaseCandidateWindowsBuild.log`, `Logs/LoadingPrefabFinalWindowsBuild.log`). 이 빌드가 실행된 복제본의 Product Name은 검증용 `StarterProjectTemplateProbe`이며 원본 프로젝트의 제품명·Cloud ID 변경은 포함하지 않았다. EditMode 코드는 이번에 변경하지 않아 기존 깨끗한 복제본의 50/50 결과를 유지한다. 실제 화면 시각·터치·물리 입력 검증은 수행하지 않았다.

## 2026-10-02 원본 Editor Computer Use 부분 검증

원본 Unity 6000.3.16f1 Editor에서 저장된 `00_StartScene` 상태로 Play를 시작했고, Editor 창 제목과 로그에서 `01_Title` 로드를 확인했다. Play를 종료하자 `00_StartScene`으로 돌아왔다. 해당 실행 구간의 Editor 로그에는 예외·오류가 없었다. 파일 열기 대화상자는 취소했고 씬 파일을 수정하지 않았다.

Unity 창의 화면 캡처가 반복해서 `window capture timed out: timed out waiting on channel` 오류로 실패했다. 접근성 정보에는 Editor의 GameView 창만 있고 게임 안의 버튼·텍스트가 노출되지 않았다. Title의 키보드 입력으로 Main 전환을 확인하지 못했으며, Main 직접 Play·실제 화면 배치·클릭·터치·오디오 검증은 완료하지 않았다. 기존 자동 테스트와 Windows 빌드 결과의 범위를 넘겨 GUI 검증 완료로 판정하지 않는다.

추가로 같은 검증 복제본의 Windows Player를 다시 빌드했으며 성공했다(169,603,386 bytes, `Logs/ComputerUseWindowsBuild.log`). Computer Use에서 이 Player를 실행하려던 호출은 `Computer Use app approval timed out`으로 끝났고, Player 창이 없음을 확인했다. 따라서 Player GUI 실행 결과는 없다. 임시 빌드 산출물은 정리했다.

## 2026-10-02 Windows Player 네이티브 화면·UI 흐름 확인

Computer Use로 검증 Player를 실행했고, 창이 늦게 생성된 뒤 목록에서 확인됐다. Windows 창 캡처는 계속 시간 초과됐으므로, Git에서 제외된 검증 복제본에만 임시 진단 코드를 넣어 게임의 실제 렌더링 이미지를 생성했다. `Logs/ComputerUseNativeValidation/frame_02_01_Title_1920x1080.png`와 `Logs/ComputerUseNativeScenario/frame_03_02_MainScene_1920x1080.png`에서 Title·Main 문구·버튼 배치를 확인했다.

초기 진단 Player는 포커스가 있고 New Game이 선택된 Ready 상태였지만, Computer Use로 보낸 Enter·F8은 Unity 키 입력 로그에 나타나지 않았다. 물리 입력을 확인한 결과로 처리하지 않는다. 별도 Player 시나리오는 Unity 입력 시스템에 합성 키 상태를 주입해 New Game → Save Game → Back to Title → Continue를 실행했고, 저장 후 같은 SessionId로 Main에 복귀하는 조건을 통과했다. 임시 시나리오는 테스트용으로 background input을 허용했다.

이어진 설정 테스트는 아래 방향키의 선택 대상을 Volume으로 가정한 임시 테스트 자체의 오류로 중단됐다. 실제 선택은 Fullscreen이었다. 보정한 추가 시나리오 빌드는 성공했으나 `StarterProjectGuiScenario20261002` 실행에서 `Computer Use was not approved to use starterprojectguiscenario20261002` 응답을 받았다. 따라서 추가 화면 모드·960×540 네이티브 화면 검증은 완료하지 않았다. 제품 코드 실패로 기록하지 않는다.

빌드 로그: `Logs/ComputerUseRetryWindowsBuild.log`, `Logs/ComputerUseNativeWindowsBuild.log`, `Logs/ComputerUseNativeScenarioBuild.log`, `Logs/ComputerUseNativeScenarioFinalBuild.log`. 실제 입력 진단과 합성 입력 시나리오 기록은 각각 `Logs/ComputerUseNativeValidation/events.log`, `Logs/ComputerUseNativeScenario/events.log`에 있다. 원본 런타임 코드는 `61b5355` 이후 변경하지 않았다. [v1.0.0 배포 범위](RELEASE.md)

## 2026-10-02 배포 후 마무리

- GitHub Template 설정과 공개 v1.0.0 Release를 확인했다. 소스 ZIP·SHA-256·manifest의 업로드 상태·크기·digest를 검증했다. ZIP은 소스 커밋 `53db0d5`, 156개 파일이며 Cloud 식별자·Library·검증 코드·사용자 저장을 포함하지 않는다.
- 기존 테스트 빌드·임시 진단 코드·시험용 저장 폴더를 정리했고 Player 로그와 네이티브 화면·테스트 XML은 보관했다. 기록은 `Logs/ReleaseCleanup-v1.0.0.json`에 있다.
- 추가 마무리 요청 후 Unity Editor 창을 새로 선택해 캡처·복구를 각각 한 번 시도했지만 `FrameArrived timed out: timed out waiting on channel`와 `window capture timed out: timed out waiting on channel`로 실패했다. 이전 좌표나 접근성 인덱스로 클릭하지 않았다.
- 남은 화면 모드·960×540 확인을 위해 v1.0.0 ZIP의 소스를 기존 격리 복제본에 다시 반영하고 Product Name만 `StarterProjectGuiScenario20261002`로 분리했다. 임시 진단은 설정 버튼의 Unity 이벤트·실제 플랫폼 반영, 960×540 Title/Main 화면과 버튼 경계, 단일 AppRoot를 확인하도록 구성했다. Windows 개발 빌드는 성공했다(169,614,335 bytes, `Logs/FinalHandoffWindowsBuild.log`).
- 해당 Player의 Computer Use 실행은 다시 `Computer Use was not approved to use starterprojectguiscenario20261002`로 거부됐다. 따라서 이 추가 시나리오는 **미실행**이며 화면 모드·960×540 네이티브 표시를 새 통과 결과로 추가하지 않는다. 임시 진단은 원본과 배포 ZIP에 반영하지 않았다. 원본 런타임 코드는 변경하지 않았다.

새 게임의 플랫폼·장치 확인과 도구의 실행 권한 제한을 프리셋의 일반 기능 검증과 구분한다. 물리 키보드/게임패드·오디오 청취·모바일 실기기 결과는 아직 없다.

## 2026-10-02 Codex 재시작 후 GUI 검증

Computer Use 플러그인 26.930.21537에서 대상 Player 실행을 다시 시도했다. 허용 설정 반영·Codex 재시작 후 앱 권한 거부 없이 Player가 실행됐다. 첫 시나리오는 약 8초에 검사를 마치고 자동 종료하여 `launch_app`에는 대상 창을 찾지 못했다는 응답이 남았지만, 새 결과 파일·타임스탬프·이미지에서 실제 실행과 완료를 확인했다. 같은 실행을 중복으로 시작하지 않았다.

| 회차 | 실행과 관찰 | 판정·증거 |
| --- | --- | --- |
| 1 | Title 준비·AppRoot 1개, 음량 버튼의 Unity 이벤트 호출 후 설정값·AudioListener 반영, 전체화면 해제 | 통과. `Logs/FinalHandoffValidation/events.log` |
| 1 | 실제 Windows 창을 960×540으로 변경하고 Title → Main 이동, 화면 크기 유지·활성 버튼 경계 검사 | 통과. 같은 폴더의 `result.txt`가 PASS, Title·Main PNG 기록 |
| 1 | Main에서 전체화면 버튼 이벤트 호출 후 전체화면 상태·설정 반영, 버튼 경계·단일 AppRoot 재확인 | 통과. 전체화면 렌더링은 960×540. 1920×1080 복귀를 검증하지 않음 |
| 2 | 네이티브 960×540 Title·Main·전체화면 Main 이미지를 직접 열어 문구와 버튼 배치 확인 | 잘림·겹침 없이 표시. `title_960x540_960x540.png`, `main_960x540_960x540.png`, `main_fullscreen_960x540.png` |
| 2–3 | 원본 Editor와 대기형 Player의 Windows 창 캡처, 각각 새 창 선택 후 한 번 재시도 | 미완료. `FrameArrived timed out: timed out waiting on channel` / `window capture timed out: timed out waiting on channel` |
| 3 | 입력 주입·버튼 이벤트 호출 없는 관찰용 Player 실행, 접근성 포커스 확인 후 Computer Use Enter | 미확인. Ready·포커스 있음·Start Game 선택 상태였으나 Unity 키·클릭 로그와 Main 전환 없음 |

1회차 버튼 호출은 `Button.onClick.Invoke()`이며 실제 OS 클릭이 아니다. 크기 변경은 임시 시나리오의 `Screen.SetResolution(960, 540, Windowed)`다. PNG는 Unity `ScreenCapture`로 기록한 실제 게임 렌더링이며 Windows 창 캡처 결과와 구분한다. 시나리오의 런타임 오류 기록은 없었다.

3회차 관찰용 빌드는 성공했다(169,610,584 bytes, `Logs/FinalComputerUseInputWindowsBuild.log`). `Logs/FinalComputerUseInputValidation/events.log`에는 씬·선택·화면 크기·포커스·실제 수신한 키와 버튼 이벤트만 관찰하도록 구성했다. 자동 입력이나 UI 동작은 실행하지 않았다. Computer Use Enter의 수신을 확인하지 못했으므로 반복 입력·추측 좌표 클릭으로 통과 판정을 만들지 않았다. 대상 Player를 닫고 로그·화면·관찰 소스를 보관했다. 사용이 끝난 시험용 빌드·임시 소스·시험용 저장의 삭제는 경로 확인 후 명시적 대상 경로로도 시도했지만 자동 승인 검사에서 `blocked by policy`로 거절됐다. 상세 사유는 제공되지 않아 삭제를 멈췄으며 이번 산출물은 남아 있다. `Logs/FinalComputerUseCleanup-20261002.json`에 정리 보류 상태를 기록한다.

이 회차에서 제품 런타임 코드는 변경하지 않았다. Summary·배포 기준만 결과에 맞게 보완하고 v1.0.0 태그·ZIP은 유지한다. OS 마우스·키보드, 물리 게임패드·오디오 청취·모바일 실기기는 별도 미확인이다.

## 2026-10-02 후속 테스트 자료 정리

사용자가 기존 시험용 자료 중 불필요한 자료의 삭제를 요청했다. 검증 폴더 5개의 범위·용량·링크 유무, 원본 Editor와 자식 프로세스만 실행 중인 상태, 관리형 worktree 첨부가 없는 상태를 확인했다. 각 삭제 경로는 프로젝트의 `Logs` 안이거나 특정 시험용 제품명의 저장 폴더였다.

검증 복제본의 필요한 로그·결과 44개를 `Logs/ArchivedValidation/<검증 폴더>/`에 복사하고 SHA-256 일치를 확인했다. Player 로그·결과 2개도 같은 보관 폴더의 `PlayerData/` 아래에 추가했다. 합계 46개, 911,320 bytes다. 이전 기록의 복제본 내부 로그 경로는 이 보관 폴더에서 같은 상대 경로로 확인한다. 최종 GUI 화면·입력 기록과 루트의 테스트 XML·빌드 로그는 기존 경로에 있다.

일반 삭제 방식으로 검증 복제본의 캐시·빌드·소스 복사본·임시 진단을 정리했다. 시험용 저장 폴더 3개와 임시 커밋 본문·백업·상태·안내 파일 21개도 삭제했다. 새 증거 보관분을 제외한 파일 크기 기준 정리 용량은 약 9.303GB다. `Builds/Template/StarterProject-v1.0.0.zip`의 SHA-256은 `B32A4F070EAD6CC555C00722560F087B3B54A29C1890F54FE1C9D44B57F8D4FA`로 유지됐고 실제 프로젝트 사용자 설정과 소스는 정리 대상에서 제외했다.

검증 폴더 `TemplateCloneValidation`, `TemplateFinalValidation`, `TemplateLatestValidation`에는 숨김 `.git`만 남아 있다. 합계 1,768,408 bytes이며 첫 폴더의 `.git`은 비어 있다. 해당 메타데이터의 강제 삭제 명령은 자동 승인 검사에서 `blocked by policy`로 거절돼 중단했다. 상세 사유는 제공되지 않았다. 원본 저장소의 `.git`을 삭제한 것은 아니다. 최신 정리 결과·보관 위치·잔여 경로는 `Logs/TestMaterialCleanup-20261002.json`에 기록했다.
