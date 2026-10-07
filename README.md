# Starter Project — Unity 6

새 Unity 게임에서 반복하는 초기화·설정·저장·씬·입력 기반을 재사용하는 프로젝트 템플릿입니다.

**현재 상태:** Phase 1 실행 골격, Phase 2 실행 기반, Phase 3 개발 지원, Phase 4의 선정 Pool 범위와 Gate를 완료했습니다. Windows·URP 2D 게임 제작을 시작할 수 있습니다. 게임 규칙과 콘텐츠는 새 프로젝트의 Game Layer에서 연결합니다.

## 처음 읽는 순서

1. [프리셋 개요](Docs/PRESET_SUMMARY.md): 제공 기능, 현재 단계, 사용 범위
2. [새 게임 시작](Docs/NEW_GAME_SETUP.md): 복제·식별자·씬·게임 데이터 연결
3. [문서 안내](Docs/INDEX.md): 목적에 맞는 사용법과 검증 문서 찾기

## 환경과 시작 씬

| 항목 | 기준 |
| --- | --- |
| Unity | 6000.3.16f1 |
| 렌더링 | URP 2D 17.3.0 |
| 입력·UI | Input System 1.19.0, uGUI |
| JSON | Newtonsoft JSON 3.2.2 |
| 첫 사용 환경 | Windows PC |
| 씬 흐름 | 00_StartScene → 01_Title → 02_MainScene |
| UI 디자인 기준 | 1920×1080, Overlay Canvas, Match 0.5 |

1920×1080은 UI 디자인 좌표입니다. 실제 출력의 최대 해상도를 뜻하지 않습니다. 모바일 Safe Area 예제는 제공하며 실기기 품질은 적용할 게임에서 확인합니다.

## 프리셋 받기

[GitHub Template](https://github.com/SIzsang/Starter_Project_Unity_6)의 현재 main에서 `Use this template`으로 새 저장소를 만듭니다. 시작에 사용한 커밋을 새 게임 문서에 기록합니다.

현재 기반은 실행·진단 `52688c0`, 개발 도구 `91f413a`, Pool `a24ff7d`를 포함합니다. [v1.0.0 Release](https://github.com/SIzsang/Starter_Project_Unity_6/releases/tag/v1.0.0)는 이전 단일 슬롯 버전입니다. 해당 ZIP과 현재 main의 기능 범위를 구분합니다.

## 첫 실행

1. Company Name·Product Name·Application Identifier를 게임 고유 값으로 바꿉니다.
2. `Tools > Starter Project > Validate Setup`을 실행합니다.
3. `Assets/01_Scenes/Boot/00_StartScene.unity`를 열고 Play합니다.
4. Title에서 빈 슬롯으로 New Game을 시작합니다.
5. Main의 Save Game → Back to Title → Continue로 저장 흐름을 사용합니다.

게임별 SceneRoot·Runtime·payload·입력과 HUD를 연결하는 방법은 [새 게임 시작 가이드](Docs/NEW_GAME_SETUP.md)를 따릅니다. 기본 설정 에셋은 `Assets/04_Data/Config/SO_AppConfig.asset`입니다.

## 기능별 문서

| 목적 | 문서 |
| --- | --- |
| 실행·초기화 | [Bootstrap](Docs/BOOTSTRAP.md) |
| 저장·설정·복구 | [설정과 저장](Docs/SETTINGS_AND_SAVE.md) |
| 씬 수명·Runtime·입력·Pause·Audio | [Phase 2 실행 기반](Docs/PHASE_2_RUNTIME.md) |
| 로그·Debug·빌드·Validator·Smoke | [Phase 3 개발 지원](Docs/PHASE_3_DEVELOPMENT.md) |
| 선택형 프리팹 재사용 | [Phase 4 Pool](Docs/PHASE_4_MODULES.md) |
| Editor 반복 작업 | [Editor 워크플로](Docs/EDITOR_WORKFLOW.md) |
| 실제 확인 범위 | [검증 현황](Docs/INTEGRATION_VALIDATION.md) |
| 앞으로의 작업 | [로드맵](Docs/ROADMAP.md) · [후속 계획](Docs/NEXT_STEPS.md) |

## 구조와 관리 기준

공통 코드는 Core, 입력/UI 어댑터는 UI, 개발 도구는 Editor, 선택형 기능은 Modules, 게임 규칙은 Gameplay에 둡니다. [설계 기준](Docs/INITIAL_SETTING.md) · [에셋 구조](Assets/PROJECT_STRUCTURE.md)

Assets와 .meta, Packages, ProjectSettings, 문서를 버전 관리합니다. Library·Temp·Logs·Builds·UserSettings와 IDE 생성 파일은 제외합니다. 개인 Cloud 연결 정보는 템플릿 소스에 넣지 않습니다.

설계와 현재 작업은 [Notion — Starter Project](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50)에서도 확인합니다. Git 문서와 Notion은 역할에 맞춰 관리합니다.
