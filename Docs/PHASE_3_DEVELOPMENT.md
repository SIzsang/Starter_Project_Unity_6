# Phase 3 개발 지원 사용법

Logging, Debug Menu, Development/QA/Release, Project Validator, Smoke Test의 사용 계약이다. 현재 진행 상태는 [로드맵](ROADMAP.md), 실제 Gate 결과는 [검증 기록](INTEGRATION_VALIDATION.md)에 둔다.

## Logging

Core의 경량 정적 유틸리티를 메인 스레드에서 사용한다.

~~~csharp
StarterLog.Info(LogCategory.Game, "Gameplay entered", this);
StarterLog.Warning(LogCategory.Storage, "Save request rejected", this);
StarterLog.MinimumLevel = LogLevel.Warning;
StarterLog.EnabledCategories = LogCategory.App | LogCategory.Storage;
StarterLog.ResetDefaults();
~~~

레벨은 Debug / Info / Warning / Error / Off이며 최소 레벨 이상이고 활성 Category와 겹치는 메시지만 Console로 전달한다. Warning/Error는 Unity의 해당 심각도를 유지한다. None Category와 Off 레벨은 출력하지 않는다. Off 또는 Category None 설정은 오류도 숨기므로 진단이 필요하면 Reset log filters를 사용한다.

Category는 App, Bootstrap, Scene, Storage, Data, Input, Audio, UI, Lifetime, Developer, Build, Validation, Game이다. App 상태 변경·실패, 저장 결과, 소비자/정리 예외와 기존 UI 오류를 연결했다. JSON payload·비밀번호·토큰 등 사용자 데이터는 메시지에 넣지 않는다. 외부 게임 코드가 만든 메시지의 내용은 호출자가 관리한다.

Development 기본 Debug, QA 기본 Info, Release 기본 Warning. Editor는 Development다. SubsystemRegistration에서 기본값으로 돌아가므로 Domain Reload를 끈 새 Play에서도 이전 필터를 이어 쓰지 않는다. 디스크 로그·원격 전송·버퍼·Logger 서비스·전역 이벤트 버스는 추가하지 않았다.

## Editor Debug Menu

Tools > Starter Project > Debug Menu를 연다.

- Console: 현재 BuildEnvironment, 최소 레벨·Category 필터와 기본값 복구.
- Scene: 활성 빌드 씬을 Edit Mode에서 연다. 미저장 씬은 Unity의 저장 확인을 거친다.
- Application: App/Game 상태, 초기화 단계, 활성 씬 수명, 입력 상황, 세션/슬롯, 서비스 상태와 실패 이유.
- Play 명령: 빈 슬롯 New Game, 선택 슬롯 Continue, 활성 슬롯 Save, Pause/Resume, Return to Title.
- Debug 입력: 현재 씬 또는 앱 토큰에 연결한 범위다. 창 닫기·씬 종료·앱 교체 시 해제하여 기본 입력으로 복구한다.

런타임 명령은 기존 AppRoot API의 상태·전환·슬롯 정책을 통과한다. Play 중 씬 이동은 Title/Main의 기존 앱 경로를 사용한다. 게임별 임의 씬 강제 이동이나 저장 삭제 API는 제공하지 않는다.

Save는 현재 AppRoot의 저장소에 실제로 저장한다. 일반 Boot Play는 사용자 저장을 사용하고, Main 직접 Play는 기존 격리 테스트 저장소를 사용한다. 개발 실험은 Main 직접 Play 경로를 선택한다. 창은 Editor 어셈블리에만 포함되어 Player에는 들어가지 않는다.

## Windows Build Configuration

| 메뉴 | 환경/출력 폴더 | 옵션 | 기본 로그 |
| --- | --- | --- | --- |
| Build Windows Preview | Development / Builds/Windows | StrictMode, Development, AllowDebugging | Debug |
| Build Windows QA | QA / Builds/Windows-QA | StrictMode, ForceEnableAssertions | Info |
| Build Windows Release | Release / Builds/Windows-Release | StrictMode | Warning |

메뉴 위치는 Tools > Starter Project다. 실행 파일명은 기존 Product Name 기반 안전한 Windows 이름을 사용한다. 스크립트에서는 StarterBuildConfiguration.Build(AppBuildKind.QA) 등을 호출한다. 현재 활성 빌드 씬과 StandaloneWindows64를 사용한다.

한 Player에 STARTER_BUILD_DEVELOPMENT / STARTER_BUILD_QA / STARTER_BUILD_RELEASE 중 한 심볼을 BuildPlayerOptions.extraScriptingDefines로 전달한다. 전역 PlayerSettings defines와 EditorBuildSettings는 수정하지 않는다. 예약 심볼을 Standalone Player Settings에 넣으면 검사에서 거절하고, 둘 이상 컴파일하면 명시적인 오류가 난다. 게임 전용 심볼은 그대로 사용할 수 있다.

BuildEnvironment.Current로 게임의 개발 진단 수준을 조회한다. 명시적 심볼 없는 일반 Unity 빌드는 DEVELOPMENT_BUILD이면 Development, 그 외는 Release다. QA는 디버거 연결 없는 빌드에 assertions를 포함한다. Release는 별도 심볼로 구분하고 기본 로그를 낮춘다. 게임별 보안·스토어·플랫폼 정책은 게임에서 정한다.

Play 중이거나 열린 씬이 미저장 상태이면 빌드를 시작하지 않는다. 기존 Validate Setup과 pre-build hook이 공통 실행 구성을 검사한다. [Unity 6000.3 extraScriptingDefines 계약](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPlayerOptions-extraScriptingDefines.html)

## Project Validator

기존 Validate Setup을 확장하며 검사 중 씬·에셋·사용자 저장을 수정하지 않는다.

- AppConfig 기본값·필수 Boot/Title/Main 참조·순서와 선택형 DataCatalog의 기존 검증.
- 모든 활성 빌드 씬의 에셋 존재·중복 경로·Missing Script.
- Boot의 단일 활성 AppBootstrap과 Config 연결.
- 씬당 최대 한 개의 활성 SceneRoot, 올바른 InitialInputContext, 중복 authored AppRoot 소유자.
- 기본 Starter UI/입력 어댑터를 사용하는 씬의 EventSystem·InputSystemUIInputModule·UI 액션 계약. 명시적인 Gameplay SceneRoot와 기본 어댑터를 함께 사용하면 Player 맵도 검사.
- 선택형 GamePayloadPolicy 참조·양수 PayloadVersion. 초기 payload 생성/게임 검증 훅이나 기존 사용자 저장 읽기/쓰기는 수행하지 않음.
- 전역 Standalone defines의 예약 빌드 심볼 충돌.

저장된 씬 에셋을 Preview Scene으로 열고 finally에서 닫는다. 편집 중인 열린 씬과 dirty 상태를 유지한다. 검사하는 대상은 저장된 버전이므로 편집 결과를 검증할 때 먼저 저장한다. 사용자 UI가 예제 컴포넌트를 쓰지 않으면 예제 Canvas·입력 모듈을 강제하지 않는다. Validate Example UI는 계속 별도 선택 검사다.

서비스는 AppRoot/AppServices의 코드 조립을 사용한다. 검사 도구에서 새 서비스 등록 컨테이너나 게임별 의존 그래프를 만들지 않는다.

## 최소 Smoke Test

Unity Test Runner의 PlayMode에서 StarterSmokeTests.BootMenuNewGameSaveAndFreshAppLoad 한 개를 실행한다. Category는 StarterSmoke다.

격리된 Application.temporaryCachePath/StarterSmoke-<GUID>에 실제 JsonFileStore를 만들고 Boot → Menu → 슬롯 2 New Game → Save → AppRoot 파괴/새 생성 → Boot → Continue → Load를 확인한다. 첫 New Game만으로 파일을 만들지 않는 조건, 작업 JSON과 저장 스냅샷 분리, 같은 세션/슬롯/payload 복원, 슬롯 1·설정 파일 미생성도 검사한다. 종료 시 자체 테스트 저장 폴더를 지운다. Runtime Settings는 테스트 대체 구현을 사용한다.

이 테스트는 기본 예제 씬/JSON payload용이다. 새 게임에서 씬을 옮기거나 GamePayloadPolicy를 연결하면 테스트의 경로·payload 표식을 게임 계약에 맞춘다. 실제 프로세스 재시작·물리 입력·GUI·오디오·전체 회귀를 대신하지 않는다. 전체 테스트와 빌드를 매 기능마다 반복할 의무는 없으며 변경의 실제 위험에 맞춰 선택한다.
