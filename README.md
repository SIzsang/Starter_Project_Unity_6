# Starter Project — Unity 6

새 게임을 시작할 때 복제해서 사용하는 Unity 공통 기반 프로젝트입니다.
**로드맵 1~6단계: Bootstrap·설정·저장·공통 기능·Editor 개발 경로**를 구현하고 자동 검증했습니다.
7단계 통합 테스트와 Windows 빌드 검증을 진행 중입니다.

## 방향

- 오프라인 게임을 기본으로 하고, 온라인 기능은 필요한 게임에서 추가합니다.
- 게임마다 달라지는 규칙과 콘텐츠는 `Gameplay`에, 재사용할 기반 기능은 `Core`에 둡니다.
- 현재 에셋 폴더 구성을 유지하며, 실제 기능을 추가할 때 하위 폴더를 확장합니다.
- 기본값·에셋 참조는 SO, 사용자 설정·게임 저장은 JSON, 실행 상태는 일반 C# 객체로 분리합니다.

## 현재 환경

- Unity: `6000.3.16f1`
- Rendering: URP 2D (`17.3.0`)
- Input: Unity Input System (`1.19.0`)
- Test: Unity Test Framework (`1.6.0`)
- 첫 검증 대상: Windows PC / uGUI
- 시작 씬: `00_StartScene` → `01_Title` → `02_MainScene`

## 실행

`Assets/01_Scenes/Boot/00_StartScene.unity`를 열고 Play합니다.
초기화가 완료되면 Title이 열리며, `New Game`으로 Main에 진입합니다.
Main에서 `Save Game`으로 저장하고 `Back to Title` → `Continue`로 복원합니다.
설정 버튼으로 저장한 음량·전체화면 값은 즉시 시스템에 적용합니다.
방향키/WASD 또는 게임패드로 버튼을 선택하고 Enter/A로 실행합니다. 씬 전환 중에는 로딩 화면을 표시하고 입력을 막습니다.
언어 코드는 저장하며 번역 콘텐츠 연결은 개별 게임에서 추가합니다.
설정 에셋은 `Assets/04_Data/Config/SO_AppConfig.asset`입니다.

설정·저장 정책과 최신 검증 결과는 [3·4단계 사용 가이드](Docs/SETTINGS_AND_SAVE.md)에 기록합니다.

Main 씬을 열고 Play하면 개발용 저장 공간에서 Boot 초기화 후 Main에 진입합니다. 반복 진입과 저장소 분리는 복제본 자동 실행에서 확인했습니다. GUI에서의 직접 조작 확인은 7단계에 남겨 둡니다. Title 직접 Play는 Boot 실행을 안내합니다. 사용법과 검증 범위는 [Editor 작업 가이드](Docs/EDITOR_WORKFLOW.md)를 따릅니다.

공통 코드는 장르에 독립적으로 설계하지만 현재 렌더링 구성은 **2D용**입니다.
3D 프로젝트에서 사용하려면 렌더러, 카메라, 조명과 관련 패키지를 별도로 검토해야 합니다.

## 문서

- [Bootstrap 사용 가이드](Docs/BOOTSTRAP.md): 실행·확장 위치와 검증 결과
- [설정·게임 저장 사용 가이드](Docs/SETTINGS_AND_SAVE.md): 3·4단계 사용법·버전·복구·게임별 확장
- [공통 기능 사용 가이드](Docs/COMMON_SERVICES.md): 5단계 음량·화면 적용, 입력·로딩과 검증 범위
- [Editor 작업 가이드](Docs/EDITOR_WORKFLOW.md): 6단계 Main 직접 Play·설정 검사·테스트 데이터 백업과 검증 상태
- [로그라이크·로그라이트 사전 조사](Docs/ROGUELIKE_PLAY_FLOW_RESEARCH.md): 실제 게임의 런·영구 성장·중단 저장 비교와 6단계 개발 진입 제안
- [개발 로드맵](Docs/ROADMAP.md): 8단계 작업 순서와 완료 기준
- [초기화·데이터 관리 설계](Docs/INITIAL_SETTING.md): 구현 기준, 데이터 분담과 오류 처리
- [템플릿 완성·복제 체크리스트](Docs/TEMPLATE_CHECKLIST.md): 완료 조건과 새 프로젝트에서 바꿀 항목
- [에셋 폴더 구조](Assets/PROJECT_STRUCTURE.md): 폴더별 책임과 이름 규칙
- [Notion — Starter Project](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50): 설계·결정 사항 정리. Git 문서와 자동 동기화되지는 않습니다.

## 저장소 사용

`Assets`와 `.meta`, `Packages`, `ProjectSettings`, 문서를 함께 버전 관리합니다.
`Library`, `Temp`, `Logs`, `UserSettings`, 자동 생성 IDE 파일은 제외합니다.
템플릿 배포와 버전 지정은 체크리스트의 검증을 마친 뒤 진행합니다.
