# Starter Project 상세 Summary — 새 게임 제작용 인계 기록

최종 점검: 2026-09-23 · 상태: **공통 기능 1~6단계 코드·자동 검증 완료, 7단계 현장 확인 진행 중, 템플릿 배포 전**

이 문서는 향후 새 프로젝트에서 이 프리셋을 사용할 때 다시 확인할 기준 기록이다. 대화나 모델의 임시 기억에 의존하지 않도록 프로젝트와 함께 버전 관리한다. 변경 후에는 검증 결과와 한계를 함께 갱신한다.

## 목표와 범위

- **목표:** 새 게임에서 초기화·설정·저장·씬 흐름을 다시 만들지 않고 게임별 콘텐츠와 규칙을 개발할 수 있는 Unity 스타터를 제공한다.
- **첫 검증 환경:** Unity 6000.3.16f1, URP 2D 17.3.0, uGUI, Input System 1.19.0, Windows PC. 현재 렌더링·화면 구성은 2D 시작점이며 모든 플랫폼·장르의 완성품은 아니다.
- **공통과 게임별 경계:** Boot, 설정, 파일 저장, 기본 씬 전환, 개발용 진입은 공통이다. 전투·인벤토리·캐릭터 성장·스테이지 규칙·사망·자동 저장 시점은 새 게임의 `Gameplay`가 정의한다.
- **현재 완료 판정:** 1~6단계 약 85%는 로드맵상 범위이며 배포 완성률이 아니다. 7단계의 GUI·기기 확인과 8단계의 템플릿 정리가 남아 있다.
- **예제 UI 범위:** PC와 가로형 모바일을 대상으로 Screen Space Overlay·1920×1080 Canvas Scaler와 안전 영역 맞춤을 사용한다. 기존 1280×720 예제 배치는 비례 환산한다. 모바일 실제 기기 검증과 게임별 HUD는 별개다. [반응형 UI 기준](RESPONSIVE_UI.md)

## Bootstrap Scene과 실행 흐름

| 요소 | 현재 역할 |
| --- | --- |
| `00_StartScene` / `AppBootstrap` | 빌드의 첫 씬. 유일한 `AppRoot`를 확보하고 `SO_AppConfig`로 초기화를 시작한다. |
| `SO_AppConfig` | Boot·Title·Main 경로와 사용자 설정 기본값. 세 씬의 활성화·순서·중복을 검증한 뒤 실행용 사본을 만든다. |
| `AppRoot` | `NotStarted → Initializing → Ready/Failed` 상태, 설정·저장 서비스의 수명, Title/Main 이동과 중복 전환 차단을 조정한다. |
| Boot `StarterScreen` | 준비 단계와 오류를 표시한다. `EventSystem`의 Input System UI 모듈을 사용한다. |

정상 실행은 `Boot → 설정·씬 검증 → 사용자 설정/저장 읽기 → 음량·화면 적용 → Title → New Game 또는 Continue → Main`이다. 초기화 실패는 Boot에 남아 원인을 표시하고 게임 진입을 막는다. 이미 준비된 루트로 Boot에 재진입하면 루트를 재사용해 Title로 돌아간다. 씬 전환 중에는 추가 요청과 버튼 입력을 차단한다.

`Tools > Starter Project > Validate Setup`은 현재 AppConfig·빌드 씬·입력 액션과 Boot의 AppBootstrap 참조, 활성 상태 표시 Canvas·Text, 단일 활성 EventSystem·UI 액션 및 세 씬의 Canvas 해상도 설정을 **씬을 변경하지 않고** 검사한다. Boot → Title → Main 왕복·중복 루트·실패 차단은 PlayMode 테스트에서 확인했다. GUI의 실제 표시·메뉴 클릭은 아직 사람이 직접 확인하지 않았으므로 시각적 완성으로 표시하지 않는다.

## 데이터와 저장 계약

- `UserSettings`: SO 기본값에서 복사하고 사용자 JSON에 음량·전체화면·언어 코드를 저장한다. 저장 시 플랫폼 적용을 먼저 확인하고 JSON 저장 실패에는 이전 실행 설정으로 복원한다.
- `GameSessionService`: 단일 저장 슬롯에 세션 ID·시각·버전과 **게임별 JSON 객체 payload**를 기록한다. 새 게임은 기존 저장을 즉시 지우지 않는다. 다른 세션으로 기존 저장을 바꾸려면 확인이 필요하다.
- `JsonRepository<T>` / `JsonFileStore`: 임시 파일을 기록·검증한 뒤 교체하며 `.bak`을 남긴다. 파손 시 정상 백업을 표시하고 사용자의 명시적 복구 전에는 원본을 바꾸지 않는다. 미래 버전 파일은 덮어쓰지 않는다.
- 저장 크기는 **파일 전체 최대 1MiB**다. 현재 저장은 동기식이므로 대량 월드 데이터나 긴 이력에는 저장 형식·성능 재검토가 필요하다.
- 현재 기본 `Save Game` 버튼은 게임 오브젝트의 상태를 자동 수집하지 않고 `GameSessionService.Current.PayloadJson`을 저장한다. 새 게임의 `Gameplay`에서 실제 상태를 JSON 객체로 만들고 저장 버튼/체크포인트와 연결해야 한다. `Continue` 후에는 현재 payload를 새 게임의 타입 모델로 복원한다.

**향후 계획된 약 100명의 캐릭터 능력치와 스테이지 진행:** 프리셋 Core에 고정 필드를 추가하지 않는다. 새 게임에서는 캐릭터 ID와 변화하는 능력치·성장 상태, 스테이지 ID와 진행 단계만 payload에 넣고 고정 캐릭터 정의는 게임 데이터에서 조회한다. 100명 × 1KiB면 약 100KiB, 5KiB면 약 500KiB로 현재 상한 안에 있지만, 실제 JSON 직렬화 크기를 측정한다. 인벤토리·전투 로그·월드 오브젝트 이력을 누적하면 1MiB에 가까워질 수 있다. 이 수치는 설계 예시이며 실제 게임 스키마가 확정된 것은 아니다.

## 개발 경로와 사용법

1. 일반 Play는 Boot 씬에서 시작한다. Title의 New Game/Continue로 Main에 들어간다.
2. Editor에서 Main 씬을 직접 Play하면 Boot 초기화를 거친 뒤 **새 빈 개발 세션**으로 Main에 진입한다. 각 Play의 저장소는 `Library/StarterProject/PlaySessions/<sessionId>`로 분리되어 일반 사용자 저장을 건드리지 않는다.
3. `Validate Setup`으로 설정을 확인하고, 개발 세션 파일은 `Open Test Data Folder`에서 확인한다. `Reset Test Data`는 확인 대화상자와 백업 이동을 거치며 취소할 수 있다.
4. 새 게임의 `Gameplay`에 캐릭터/스테이지 타입 모델과 직렬화·복원 코드를 둔다. 현재 단일 슬롯과 JSON payload 계약을 유지할지 게임 요구사항에 맞춰 결정한다.

## 검증된 결과와 한계

- Unity 6000.3.16f1 격리 복제본의 **최신 EditMode 46개·PlayMode 29개 통과, 실패·건너뜀 0개**. 1920×1080 디자인 좌표 이관 후 결과: `Logs/FullHdMigratedEditMode.xml`, `Logs/FullHdFinalPlayMode.xml`.
- Main 직접 Play 반복은 도메인 재로드 켜짐·꺼짐의 자동 실행에서 Boot 경유, 세션 격리, 시작 씬 복원과 일반 저장 보호를 확인했다. 자동 실행용 batchmode 예외는 복제본에만 넣었고 원본 코드에는 없다.
- Windows x64 Development 빌드에서 서로 다른 Player 프로세스의 첫 실행 저장·재실행 복원·이어하기, 파손/미래 버전 보호, 명시적 백업 복구, 저장 완료 직후 강제 종료와 계측 복제본의 교체 전 강제 종료를 확인했다. 계측 중단 후 미완료 `.tmp`가 남으며 전원 손실·모든 중단 시점을 증명하지 않는다.
- Boot 구성 검사 보완 후 새 Windows x64 Development 빌드도 성공했고, 검증용 제품명 `StarterProjectBootstrapValidation`의 다른 두 Player 프로세스에서 `CREATE_PASS`·`RESUME_PASS`를 확인했다. 로그는 `Logs/BootstrapWindowsBuild.log`와 복제본 `Builds/Windows/BootstrapPlayerCreate.log`·`BootstrapPlayerResume.log`다.
- 숨겨진 그래픽 Player에서 Boot 캡처를 두 번 시도했으나 결과가 검은 화면이라 레이아웃 증거로 사용하지 않는다. 실제 GUI 시각 검증은 남아 있다.
- 가로형 UI 레이아웃 보완 후 복제본에서 EditMode 46개·PlayMode 29개가 통과했다. 안전 영역·해상도 변경은 합성 화면 값으로 자동 검사했으며 모바일 실기기 시각·터치 확인은 남아 있다.
- 변경 후 Windows 개발 빌드도 성공했고 검증용 별도 저장 경로의 두 Player 프로세스에서 첫 저장·재실행 이어하기를 통과했다. Android·iOS 빌드 모듈은 설치되지 않아 모바일 패키지 실행은 확인하지 않았다. [검증 기록](INTEGRATION_VALIDATION.md)
- 1920×1080 좌표 이관 후 Windows 개발 빌드를 다시 만들고 `StarterProjectFullHdValidation` 저장 경로에서 서로 다른 두 헤드리스 Player 프로세스의 `CREATE_PASS`·`RESUME_PASS`를 확인했다. 이 실행은 화면 배치의 시각적 검증을 대신하지 않는다.
- 템플릿 패키지·식별 정보 정리 시험 복제본에서 EditMode 46개·PlayMode 29개, Windows 개발 빌드와 별도 헤드리스 Player 프로세스의 새 저장·이어하기를 확인했다. 저장 위치는 검증 전용 `StarterProjectTemplateSmoke` 제품명으로 격리했다. 이어 `3460339`의 **새 Git 복제본**을 비어 있는 Library에서 열어 EditMode 46개·PlayMode 29개와 Windows 빌드(169,600,344 bytes)를 재확인했다. 원본 Editor의 Main 직접 Play와 화면 조작은 아직 남아 있다.
- **남은 7단계:** 원본 Editor의 GUI Main 직접 Play와 메뉴 확인·취소, 실제 Windows 창/전체화면·오디오 청취·키보드/게임패드 조작, 다른 중단 시점과 임시 파일 정리 정책. 자동 테스트와 빌드 성공을 이 현장 검증으로 대체하지 않는다.
- **8단계 진행:** 추적 중인 `Project_DE`·`DefaultCompany`와 Cloud 프로젝트·조직 ID를 중립 템플릿 값 또는 빈 값으로 바꾸고, 현재 코드·씬에서 사용하지 않는 협업·Multiplayer Center·Visual Scripting 패키지를 제거했다. 2D 애니메이션 도구는 유지한다. 변경 후 깨끗한 복제본의 자동 PlayMode·빌드는 통과했다. Unity Hub/Editor Services의 연결 표시와 GUI 화면·장치 확인, 버전·템플릿 지정은 남아 있다. 원격 푸시·태그·공개는 아직 수행하지 않았다.

## 새 프로젝트 인계 기준

이 프리셋이 최종 검증·버전 지정된 뒤 새 저장소를 만든다. 복제 직후 Product Name·Company Name·앱 식별자와 저장 경로 충돌 여부, Cloud 연결, 플랫폼·렌더러·입력 요구사항을 새 게임에 맞춘다. `SO_AppConfig`와 씬 구성을 검사하고 Unity Test Runner·첫 실행·저장·재실행·Windows 빌드를 다시 확인한다. 어떤 프리셋 버전에서 출발했는지 새 게임의 기록에 남긴다. 향후 게임에서 바꾼 Core를 이 원본 프리셋에 자동으로 역반영하지 않는다.

세부 기록: [제작 가능 범위](GAME_CAPABILITY_BRIEF.md), [Bootstrap 사용 가이드](BOOTSTRAP.md), [설정·저장](SETTINGS_AND_SAVE.md), [공통 기능](COMMON_SERVICES.md), [Editor 개발 경로](EDITOR_WORKFLOW.md), [SOLID 점검](ARCHITECTURE_REVIEW.md), [통합 검증](INTEGRATION_VALIDATION.md), [로드맵](ROADMAP.md), [템플릿 체크리스트](TEMPLATE_CHECKLIST.md).
