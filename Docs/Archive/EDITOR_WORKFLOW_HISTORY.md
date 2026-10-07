# Editor 실행·검증 도구

> 과거 기록입니다. 당시 제안·대기 상태·검증 결과를 보존하며 현재 사양은 [문서 안내](../INDEX.md)를 따릅니다.

상태: 2026-09-23 Unity 6000.3.16f1 복제본 자동 검증 통과. 실제 GUI 직접 조작과 메뉴 대화상자 확인은 7단계 통합 검증에서 이어간다.

2026-09-23 추가 확인: 원본 Unity Editor에서 `Tools > Starter Project > Validate Setup` 메뉴를 실제로 실행했고 Editor 로그에 `Setup validation passed`가 남았다. Main 씬 직접 Play와 Reset Test Data 대화상자 확인은 아직 수행하지 않았다. Main 씬을 열기 위한 Windows 파일 대화상자가 현재 자동화 도구의 입력 대상이 되지 않아 이번 결과에 포함하지 않는다.

## 현재 개발 지원 도구

Logging, Editor Debug Menu, Development/QA/Release 빌드, Preview Scene Validator와 격리 저장 Smoke는 [Phase 3 사용법](../PHASE_3_DEVELOPMENT.md)을 따른다. 아래 6단계 자동 검증 기록은 Phase 1 당시 결과다. 최신 결과는 [검증 기록](../INTEGRATION_VALIDATION.md)에 둔다.

## 실행 방법

- **일반 흐름:** Boot 씬을 열고 Play하면 설정·저장 정보를 준비한 뒤 Title로 진입한다.
- **Main 직접 작업:** AppConfig의 Main 씬을 열고 Play하면 Editor가 지정한 Boot 씬에서 시작한다. 공통 초기화를 마친 뒤 새 빈 세션을 생성하고 Main으로 이동한다. Title의 버튼 조작을 건너뛰지만 초기화·설정 검증과 씬 전환 잠금은 그대로 사용한다.
- **테스트 저장:** Main 직접 Play마다 `Library/StarterProject/PlaySessions/<세션 ID>`의 새 저장소를 사용한다. 일반 실행의 `Application.persistentDataPath/StarterData` 파일을 읽거나 덮어쓰지 않는다. 설정도 새 저장소에서 기본값으로 시작한다.

이 진입 방식은 개발용이며 빌드에는 포함되지 않는다. 현재 AppConfig에 등록된 **Main 씬만** 직접 Play 대상으로 삼는다. Title 직접 Play와 다른 임의 씬의 자동 Boot 경유는 제공하지 않는다.

## 메뉴

| 메뉴 | 동작 |
| --- | --- |
| `Tools > Starter Project > Validate Setup` | AppConfig와 기본 설정, 빌드 씬 순서·활성화, 모든 활성 씬 에셋·Missing Script, Boot 연결, SceneRoot·기본 입력·저장 정책 버전·예약 빌드 심볼을 검사한다. 편집 중인 씬은 교체하지 않으며 사용자 UI에 의존하지 않는다. |
| `Tools > Starter Project > Validate Example UI` | 기본 StarterScreen을 사용하는 경우 Boot·Title·Main의 예제 화면, 1920×1080 Overlay Canvas와 UI 입력 연결을 추가로 검사한다. Main 직접 Play·Windows Preview의 필수 조건은 아니다. |
| `Tools > Starter Project > Open Test Data Folder` | 마지막 Main 직접 Play의 개발용 저장 폴더를 연다. 마지막 세션이 없다면 개발용 세션 루트를 연다. |
| `Tools > Starter Project > Reset Test Data` | Play가 꺼진 상태에서 대상 경로와 백업 경로를 표시하고 확인받는다. 기존 `PlaySessions` 폴더를 같은 프로젝트의 `UserSettings/StarterProject/TestDataBackups` 아래로 이동한다. 원본이 이동하지 못하면 삭제로 우회하지 않는다. 백업은 Library 캐시를 지워도 남는다. |

Main 직접 Play가 시작될 때 기존 `EditorSceneManager.playModeStartScene` 값을 기억해 두고 Play 종료 시 복원한다. 저장된 씬 에셋이나 빌드 씬 목록은 변경하지 않는다. 필수 설정 검사에 실패하면 Play 진입을 중단하고 오류를 Console에 남긴다. AppConfig·씬 목록은 공통 검사 결과에 맞춰 수정한다. 기본 화면의 입력 연결은 Validate Example UI로 확인한다. 예제 생성 도구는 초기 예제 구성용이므로 사용자 UI를 교체한 뒤 일괄 보정용으로 실행하지 않는다.

## 범용성 경계

공통 코드가 제공하는 것은 초기화 완료 후 Title 또는 Main의 빈 세션으로 가는 진입 선택, 파일 저장 경계, 검증과 데이터 관리다. ‘런’, 사망, 영구 성장, 자동 저장 시점, 체크포인트나 프로필은 게임별 `Gameplay`가 정한다. [장르 사례 조사](../ROGUELIKE_PLAY_FLOW_RESEARCH.md)는 게임 설계 참고 자료다.

여기서 범용성은 **장르 규칙을 강제하지 않고 새 게임에서 공통 기반을 재사용할 수 있다는 뜻**이다. 현재 제공하는 화면·렌더링·첫 검증 환경은 Windows PC, uGUI, URP 2D다. 3D 렌더러, 다른 플랫폼과 UI 방식은 복제한 게임에서 별도로 구성·검증해야 한다.

`AppRoot.ConfigureStartupDestination`은 `Begin` 전에만 호출할 수 있다. Editor 도구는 Boot의 `AppBootstrap`이 `Begin`을 호출하기 전에 `ConfigureStorage`와 이 진입 선택을 전달한다. 일반 실행에서는 이 선택을 전달하지 않아 Title 흐름을 유지한다.

## 검증 상태와 완료 조건

Unity 복제본의 전체 Test Runner 결과는 **EditMode 46개·PlayMode 26개 통과, 실패·건너뜀 0개**다. 결과 파일은 `Logs/Stage6EditModeFinal.xml`과 `Logs/Stage6PlayModeFinal.xml`이며 생성 폴더라 Git에는 포함하지 않는다. 설정 검사·세션 경로 검사와 AppRoot의 Boot → Main 새 세션 테스트를 포함한다.

일회성 Editor 자동 검증에서는 Main 씬에서 Play를 연속 두 번 시작해 Boot → Main, 서로 다른 세션 ID·저장 폴더, Play 종료 후 기존 `playModeStartScene` 복원, 일반 저장 파일 불변을 확인했다. Domain Reload 켜짐·꺼짐 모두 통과했고, 꺼짐 설정에서는 미저장 Main 씬의 편집 객체와 dirty 상태도 보존됐다. 이 자동 검증은 Test Runner와 충돌하지 않도록 **검증 복제본에서만** `Application.isBatchMode` 차단 조건을 잠시 해제해 실행했다. 원본 코드와 커밋에는 이 변경이나 일회성 스크립트를 포함하지 않는다. 따라서 사람이 GUI에서 Play 버튼·메뉴 대화상자를 직접 조작한 결과로 간주하지 않는다.

7단계에서 남은 현장 확인은 GUI의 Main 직접 Play, 잘못된 AppConfig·입력 참조에서 진입 중단 안내, Reset Test Data의 확인·취소·백업 파일, 그리고 실제 Windows 빌드의 실행·재시작이다. 전체 앱 종료·재실행 내구성과 실제 Windows 플레이 확인도 [로드맵](../ROADMAP.md)의 7단계에서 수행한다.

## Phase 2 데이터 검사

AppConfig의 선택형 Data Catalog를 지정하면 기존 Validate Setup → AppConfig.Validate에서 Definition ID 누락·중복·필수 참조·게임별 추가 규칙을 확인한다. 게임별 Validate 훅은 원본 변경·I/O 없이 반복 호출 가능해야 한다. 서비스 등록 컨테이너나 별도 새 검사 메뉴를 추가하지 않는다. [Definition·Runtime 사용법](../PHASE_2_RUNTIME.md)
