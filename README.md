# Starter Project — Unity 6

새 게임을 시작할 때 복제해서 사용하는 Unity 공통 기반 프로젝트입니다.
현재 단계는 **초기 설계**이며, 실행 가능한 초기화 프레임워크나 배포용 템플릿은 아직 완성되지 않았습니다.

## 방향

- 오프라인 게임을 기본으로 하고, 온라인 기능은 필요한 게임에서 추가합니다.
- 게임마다 달라지는 규칙과 콘텐츠는 `Gameplay`에, 재사용할 기반 기능은 `Core`에 둡니다.
- 현재 에셋 폴더 구성을 유지하며, 실제 기능을 추가할 때 하위 폴더를 확장합니다.
- 초기화 흐름과 데이터 관리 기준을 먼저 검토한 뒤 구현합니다.

## 현재 환경

- Unity: `6000.3.16f1`
- Rendering: URP 2D (`17.3.0`)
- Input: Unity Input System (`1.19.0`)
- Test: Unity Test Framework (`1.6.0`)
- 시작 씬 현황: 빌드 목록에는 `02_MainScene`만 등록되어 있습니다. Boot 흐름은 아직 구현 전입니다.

공통 코드는 장르에 독립적으로 설계하지만 현재 렌더링 구성은 **2D용**입니다.
3D 프로젝트에서 사용하려면 렌더러, 카메라, 조명과 관련 패키지를 별도로 검토해야 합니다.

## 문서

- [초기화·데이터 관리 설계](Docs/INITIAL_SETTING.md): 추천안, 대안, 오류 처리와 구현 순서
- [템플릿 완성·복제 체크리스트](Docs/TEMPLATE_CHECKLIST.md): 완료 조건과 새 프로젝트에서 바꿀 항목
- [에셋 폴더 구조](Assets/PROJECT_STRUCTURE.md): 폴더별 책임과 이름 규칙
- [Notion — Starter Project](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50): 설계·결정 사항 정리. Git 문서와 자동 동기화되지는 않습니다.

## 저장소 사용

`Assets`와 `.meta`, `Packages`, `ProjectSettings`, 문서를 함께 버전 관리합니다.
`Library`, `Temp`, `Logs`, `UserSettings`, 자동 생성 IDE 파일은 제외합니다.
템플릿 배포와 버전 지정은 체크리스트의 검증을 마친 뒤 진행합니다.
