# 검증 현황과 확인 범위

기준일: 2026-10-07. 실제 수행한 종류·결과·한계만 기록합니다. 현재 사양은 [개요](PRESET_SUMMARY.md), 사용법은 [문서 안내](INDEX.md)를 따릅니다.

## 최신 결과 요약

| 범위 | 실제 결과 | 해석 |
| --- | --- | --- |
| Phase 2 | EditMode 20/20, PlayMode 고유 34개 실행·재검사 | 한 번의 34/34 실행으로 기록하지 않음 |
| Phase 3 | EditMode 20/20, PlayMode 4/4, 빌드 심볼 3종 컴파일·기본값 | 실제 Player 3종 빌드와 구분 |
| Phase 4 Pool | PlayMode 11/11, 독립 어셈블리 대표 1/1 | 대표 재검사는 고유 항목 수에 더하지 않음 |
| 소스 인계 정리 | Gate 당시 C#/asmdef 60개 원본/검증 해시 일치. 이번 문서 정리에서 스크립트/메타 139개가 Git HEAD와 일치 | Git 줄바꿈 정규화를 적용한 내용 비교. 새 Unity 실행 결과가 아님 |

각 Gate의 미해결 실패는 없습니다. 검증 복사본은 정리했고 XML/로그/해시는 로컬 Logs에 보관합니다. Logs는 Git 추적 대상이 아닙니다.

## 아직 수행하지 않은 최신 소스의 확인

새 Windows Player 빌드·GUI 직접 조작·물리 입력·오디오 청취·모바일 실기기·실제 게임 부하 측정은 최신 Phase 2~4에서 수행하지 않았습니다. 적용할 게임과 변경 위험에 필요한 범위만 확인합니다.

기존 v1.0.0 및 그 당시 Windows Player의 화면·프로세스 저장·합성 입력 결과는 해당 버전의 기록입니다. 기존 Release·Template 배포는 완료됐으며 이를 최신 소스의 새 Player 검사로 간주하지 않습니다.

## Phase Gate의 상세 근거

아래 Branch/Commit·작업 트리·임시 경로는 각 검증을 수행한 당시 기준입니다.

### 2026-10-07 Phase 2 Gate — 완료

기준은 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리, Unity 6000.3.16f1이다. 초기화·공용 서비스·씬/취소·Input System·저장 스냅샷 변경의 실제 위험을 확인하는 범위로 검사했다. 원본 Editor 잠금 때문에 Assets/Packages/ProjectSettings/Library 캐시의 임시 복사본에서 실행했으며 깨끗한 Git 복제 검증은 아니다.

- EditMode GameStateTests 11개와 Phase2DataInputTests 9개, **20/20 통과**, 실패·건너뜀 0개. Logs/Phase2EditMode.xml 및 로그.
- PlayMode 기존 Bootstrap 20개, 신규 Phase2Flow 5개, RuntimeFlow 중 변경 영향이 있는 7개를 실행해 **31개 통과·1개 실패**를 확인했다. 이전 GameState 테스트가 실제 Pause 단계에도 시간이 흐를 것으로 기대한 오류를 수정했다. Logs/Phase2PlayMode.xml 및 로그.
- 관련 7개 재검사(새 Pause 중 실패 복구 포함) **7/7 통과**, 실패·건너뜀 0개. 합성 AudioClip으로 BGM의 재생/일시정지/복귀 및 Pause 중 UI 채널 재생을 확인했다. Logs/Phase2PlayModeFinal.xml 및 로그.
- Integration Review에서 비동기 Exit보다 먼저 세션을 해제하던 순서를 보완하고 신규 Exit 검사와 관련 씬/Pause 9개를 실행해 **9/9 통과**, 실패·건너뜀 0개. Logs/Phase2ExitPlayMode.xml 및 로그.
- PlayMode는 **고유 34개 항목**을 실행과 재검사로 확인했으며 34/34 단일 실행으로 집계하지 않는다. 현재 미해결 실패 없음. 첫 컴파일의 새 PlayMode 테스트 Newtonsoft DLL 참조 누락은 테스트 asmdef에 기존 DLL 참조를 추가해 해결했다. 새 패키지는 추가하지 않았다.

Architecture Review: 초기화 순서는 AppBootstrapper, 타입별 서비스 소유권은 AppServices, 씬 생명주기는 SceneFlow/SceneRoot에 분리했다. AppRoot는 기존 외부 요청 API와 앱 수명·상태 연결을 유지한다. Core는 UI·Input System·Game Layer를 참조하지 않으며 저장 I/O와 게임별 규칙을 추가로 소유하지 않는다. 기존 저장소/RuntimeSettings 교체 경계만 사용하고 DI Container·Service Locator·추가 Singleton·전역 Event Bus를 만들지 않았다.

Integration Review: 설정/Definition 검증 → 서비스 조립/설정 적용 → Scene Initialize/Enter → Ready 순서를 확인했다. 앱/씬/객체 취소 후 결과 적용 차단, Runtime JSON과 저장 스냅샷 분리·실패 보존·Title/Continue, Exit 완료까지 데이터 유지, 범위 입력 복구·Player/UI 맵 전환·Loading 잠금, Pause의 시간/UI/Audio 및 실패/파괴 복구를 확인했다. 기존 키보드·게임패드 Submit, Escape 취소, 로딩 입력 차단과 Boot 왕복도 관련 범위에 포함했다. Master volume은 기존 RuntimeSettings → AudioListener.volume 경로를 재사용하며 중복 곱하지 않는 코드를 리뷰했다.

최종 C#/asmdef 47개가 검증 복사본과 일치함을 Logs/Phase2SourceHashes.json에 남겼다. Work가 만든 .utmp/Phase2Validation-20261007과 .utmp/Phase2Compile은 경로·소유권·실행 종료·링크 부재를 확인해 삭제했다. 결과 XML·로그·해시만 Logs에 보관한다. 기존 사용자 ProjectSettings 변경과 원본 Editor를 유지했다.

전체 회귀·Windows 새 빌드·GUI 시각·실기기·물리 입력·실제 오디오 청취는 수행하지 않았다. 게임별 장치 품질과 대규모 콘텐츠 요구는 별도 확인한다. Phase 2와 Gate를 완료하고 후속 Phase 3·선정 Phase 4도 완료됐으며 현재 방향은 로드맵을 따른다. [실제 사용법](PHASE_2_RUNTIME.md)

### 2026-10-07 Phase 3 Gate — 개발 지원 기반

기준 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리. 원본 Unity Editor가 열려 있어 현재 Assets·Packages·ProjectSettings·캐시를 복사한 임시 프로젝트에서 Unity 6000.3.16f1로 관련 범위만 실행했다. Clean Clone 검증이 아니다.

- EditMode **20/20 통과, 실패·건너뜀 0개**. 새 Diagnostics 3개·빌드 정책 7개·Scene/저장 정책 Validator 7개와 기존 Preview/예제 UI/사용자 UI 보존 검사 3개. Logs/Phase3EditMode.xml 및 .log.
- PlayMode **4/4 통과, 실패·건너뜀 0개**. StarterSmoke 1개, 초기화 실패 로그·GameState 소비자 예외 격리·정리 예외를 동반한 초기화 실패의 기존 경계 3개. Logs/Phase3PlayMode.xml 및 .log.
- Smoke는 실제 격리 파일로 Boot→Menu→New Game→Save→새 AppRoot→Continue→Load, 세션/슬롯/payload 복원, 명시적 저장 전 파일 미생성, 작업 JSON·저장 스냅샷 분리와 다른 파일 미생성을 확인했다. 대체 Runtime Settings를 사용하며 프로세스 재시작 검증은 아니다.
- .NET SDK와 설치된 Unity CoreModule 참조로 실제 Diagnostics 소스의 Development/QA/Release 세 심볼을 각각 컴파일·실행해 환경과 Debug/Info/Warning 기본값을 확인했다. Logs/Phase3BuildSymbols.log. Unity Player 빌드·실행으로 집계하지 않는다.

Architecture Review: StarterLog/BuildEnvironment는 진단 필터·컴파일 환경만 소유한다. Debug Window/Build Configuration/Validator는 Editor에 두고 기존 Setup에서 검사와 빌드 책임을 분리했다. AppRoot에는 기존 진단 호출을 연결하며 별도 서비스 등록·새 Singleton·DI/Logger 인터페이스·패키지를 추가하지 않았다. Core는 UI·Input System·Editor·Game Layer를 참조하지 않는다.

Integration Review: Debug 명령의 AppRoot 상태·슬롯 경계와 입력 범위의 씬/앱/창 해제를 코드에서 확인했다. 테스트는 로그 필터·Console 심각도, 빌드 심볼·옵션과 전역 설정 보존, Preview Scene 검사·열린 씬 보존, 사용자 UI 허용과 저장 payload 훅 미실행을 확인했다. 기본 입력 어댑터를 쓰는 경우의 계약만 검사하고 선택형 예제 UI 검사는 분리했다.

C#/asmdef **54개**의 원본/검증 소스 SHA-256 일치를 Logs/Phase3SourceHashes.json에 기록했다. Work가 만든 .utmp/Phase3Validation-20261007 및 .utmp/Phase3Compile은 절대 경로·소유권·Git 부재·링크 부재·Unity 실행 종료를 확인해 삭제했다. Logs/Phase3Cleanup.json에 기록하고 결과 XML/로그/해시는 보관했다. 원본 Editor와 기존 사용자 ProjectSettings Cloud 변경을 유지했다.

전체 회귀·새 Windows 빌드·Clean Clone·Debug Menu GUI 직접 조작·물리 입력·실기기·오디오 청취는 수행하지 않았다. Phase 3와 Gate 완료. 후속 선정 Phase 4 Pool 범위도 완료됐으며 현재 방향은 로드맵을 따른다. [개발 지원 사용법](PHASE_3_DEVELOPMENT.md)

### 2026-10-07 Phase 4 Gate — 선택형 Pool

기준 main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리. 사용자는 Pool을 우선 필요로 지정했고 다른 후보는 현재 필수적일 때만 개발하도록 했다. 이번 선정 범위는 프리팹 Pool이며 그 외 후보의 필수 요구는 확인되지 않아 보류했다.

- Unity 6000.3.16f1 관련 PlayMode **11/11 통과, 실패·건너뜀 0개**. Logs/Phase4PoolPlayMode.xml 및 .log.
- 확인: 준비 전 OnEnable 방지·위치/재사용, 다른 Pool/default/중복/이전 대여 반납 거절, prepare 오류 회수·재진입 거절, 서로 다른 Prewarm 재고·상한 초과 반납 파괴, 소유자 비활성화/재활성화, 외부 부모에 둔 활성 clone과 재고의 소유자 파괴 정리, 외부 Destroy 후 무효/교체, 준비 중 소유자 종료, Pool 씬 unload 후 다른 씬 부모의 clone 정리.
- 최종 구조 리뷰에서 테스트도 Pooling 모듈 내부 StarterProject.Pooling.Tests 어셈블리로 격리했다. 기존 PlayMode 테스트 asmdef는 시작 상태의 해시로 복구했다. 새 어셈블리의 컴파일·Test Runner 검색과 대표 활성화 검사 **1/1 통과**. Logs/Phase4AssemblyPlayMode.xml 및 .log. 이전 11개와 같은 Pool/테스트 소스이며 추가 고유 항목으로 집계하지 않는다.

Architecture Review: Unity ObjectPool 저장소를 재사용하고 씬/게임 객체 소유 PrefabPool만 추가했다. Core/UI/Editor/기존 테스트 어셈블리에 모듈 참조가 없다. 모듈과 내부 테스트를 함께 제외할 수 있다. AppRoot/AppServices에 서비스 등록·Singleton·새 Generic Framework/인터페이스/패키지를 추가하지 않았다. 원본 프리팹과 게임별 reset·스폰·용량 정책을 게임이 소유하고 Pool은 clone 대여/반납/종료를 맡는다.

Integration Review: PoolLease 세대가 한 번의 대여를 나타낸다. 이전 복사본·지연 반납이 새 대여를 건드리지 않는다. 준비 후 활성화하고 실패/소유자 종료에는 회수한다. OnDisable 전체 반납, OnDestroy 전체 정리와 외부 parent의 수명을 확인했다. SceneRoot.Exit의 ReturnAll과 씬 토큰/lease.IsValid 검사 연결은 사용 계약에 명시했다. 반납이 게임의 임의 Task를 자동 취소하는 것으로 설명하지 않는다.

원본 Editor 잠금 때문에 현재 소스/캐시 임시 복사본으로 필요한 범위만 실행했다. 첫 수명 검증 뒤 구조 리뷰의 테스트 어셈블리 변경에 대해 대표 1개만 최종 확인했다. Clean Clone 검증은 아니다. 최종 C#/asmdef **60개** 원본/검증 해시 일치와 시작 시 기존 스크립트/메타 **124개 전부 불변**을 Logs/Phase4SourceHashes.json 및 Phase4Preservation.json에 기록했다. 최초 검증 해시는 Phase4SourceHashesInitial.json에 보관했다.

.utmp/Phase4Validation-20261007 및 .utmp/Phase4AssemblyValidation-20261007은 소유권·절대 경로·Git/링크 부재·Unity 종료를 확인해 모두 삭제했다. 결과 XML/로그/해시·Phase4Cleanup.json만 보관한다. 이번 Work의 Localization 초안도 사용자 범위 수정에 맞춰 생성한 파일만 제거했다. 기존 사용자 ProjectSettings와 원본 Editor를 유지했다.

전체 회귀·새 Windows 빌드·GUI/실기기·실제 게임 부하/성능 측정은 수행하지 않았다. 사용자 지정 Phase 4 범위와 Gate 완료. 후속 Phase는 현재 로드맵에 없으며 실제 게임의 필수 요구가 생기면 다음 Work에서 작업을 선정한다. [Pool 사용법](PHASE_4_MODULES.md)

## 과거 실행·빌드·문제 기록

[이전 검증 기록](Archive/VALIDATION_HISTORY.md)에서 GameState 초기 검사, 3슬롯·메뉴, v1.0.0 Windows Player, 입력 자동화 한계와 테스트 자료 정리를 확인합니다. [기존 Release 기준](RELEASE.md)

### 2026-10-02 Codex 재시작 후 GUI 검증

기존 링크의 상세 기록은 [당시 GUI 검증](Archive/VALIDATION_HISTORY.md#2026-10-02-codex-재시작-후-gui-검증)으로 이동했습니다. 네이티브 화면·Unity 이벤트 결과와 OS 물리 입력 결과를 구분합니다.

## 검증 판단 기준

문서·배치 정리는 실행 동작을 바꾸지 않아 Unity 테스트·빌드를 반복하지 않습니다. 저장·초기화·수명·공용 API·어셈블리·입력 등의 중요한 위험이 생기면 관련 경로의 최소 검증을 선택합니다. 자동 테스트·컴파일·빌드·실기기는 각각 확인하는 범위가 다릅니다.
