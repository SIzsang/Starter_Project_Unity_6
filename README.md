# Starter Project — Unity 6

새 게임을 시작할 때 복제해서 사용하는 Unity 공통 기반 프로젝트입니다.
현재 작업 범위는 **로드맵 1·2단계: 설계 기준과 Bootstrap → Title → Main**입니다.
템플릿 전체는 아직 완성되지 않았으며, 사용자 설정·게임 저장은 다음 단계입니다.

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
초기화가 완료되면 Title이 열리며, `Start Game`으로 Main에 진입합니다.
Main의 `Back to Title`로 돌아올 수 있습니다.
설정 에셋은 `Assets/04_Data/Config/SO_AppConfig.asset`입니다.

2026-09-09 코드 검토 후 PlayMode 10개·EditMode 3개 테스트가 통과했습니다. 검토 내용과 재현 방법은 [Bootstrap 사용 가이드](Docs/BOOTSTRAP.md)에 기록합니다.

Title/Main의 직접 Play 자동 지원은 6단계입니다. 현재는 Boot 실행을 안내하고 시작 버튼을 막습니다.

공통 코드는 장르에 독립적으로 설계하지만 현재 렌더링 구성은 **2D용**입니다.
3D 프로젝트에서 사용하려면 렌더러, 카메라, 조명과 관련 패키지를 별도로 검토해야 합니다.

## 문서

- [Bootstrap 사용 가이드](Docs/BOOTSTRAP.md): 실행·확장 위치와 검증 결과
- [개발 로드맵](Docs/ROADMAP.md): 8단계 작업 순서와 완료 기준
- [초기화·데이터 관리 설계](Docs/INITIAL_SETTING.md): 구현 기준, 데이터 분담과 오류 처리
- [템플릿 완성·복제 체크리스트](Docs/TEMPLATE_CHECKLIST.md): 완료 조건과 새 프로젝트에서 바꿀 항목
- [에셋 폴더 구조](Assets/PROJECT_STRUCTURE.md): 폴더별 책임과 이름 규칙
- [Notion — Starter Project](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50): 설계·결정 사항 정리. Git 문서와 자동 동기화되지는 않습니다.

## 저장소 사용

`Assets`와 `.meta`, `Packages`, `ProjectSettings`, 문서를 함께 버전 관리합니다.
`Library`, `Temp`, `Logs`, `UserSettings`, 자동 생성 IDE 파일은 제외합니다.
템플릿 배포와 버전 지정은 체크리스트의 검증을 마친 뒤 진행합니다.
