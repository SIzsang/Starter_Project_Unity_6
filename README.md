# Starter Project — Unity 6

새 게임을 시작할 때 복제해서 사용하는 Unity 공통 기반 프로젝트입니다.
Bootstrap·설정·저장·공통 기능·Editor 개발 경로와 UI 에셋 교체 경로를 구현했습니다.
필수 Unity 회귀 테스트와 Windows Player의 시작·저장·이어하기 흐름, 1920×1080 화면 표시를 확인했고 v1.0.0 배포를 준비했습니다. 검증 범위와 새 게임 인계 절차는 [v1.0.0 배포 가이드](Docs/RELEASE.md)에 있습니다.

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
- 첫 검증 대상: Windows PC / uGUI. 예제 UI는 1920×1080 설계 기준으로 PC와 가로형 모바일 해상도·안전 영역에 대응하도록 설정
- 예제 배경·버튼은 `Image.Source Image`로 게임 스프라이트를 교체하고, 로딩 화면은 Boot `StarterScreen`의 선택형 프리팹 슬롯으로 교체 가능
- 시작 씬: `00_StartScene` → `01_Title` → `02_MainScene`
- 템플릿 기본 식별자: Company `StarterTemplate`, Product `StarterProject`. 새 게임은 첫 Play·빌드 전에 고유한 이름과 앱 ID로 변경
- Windows Preview 빌드: `Tools > Starter Project > Build Windows Preview` → `Builds/Windows/<Product Name>.exe`

## 실행

`Assets/01_Scenes/Boot/00_StartScene.unity`를 열고 Play합니다.
초기화가 완료되면 Title이 열리며, `New Game`으로 Main에 진입합니다.
Main에서 `Save Game`으로 저장하고 `Back to Title` → `Continue`로 복원합니다.
설정 버튼으로 저장한 음량·전체화면 값은 즉시 시스템에 적용합니다.
방향키/WASD 또는 게임패드로 버튼을 선택하고 Enter/A로 실행합니다. 씬 전환 중에는 로딩 화면을 표시하고 입력을 막습니다.
언어 코드는 저장하며 번역 콘텐츠 연결은 개별 게임에서 추가합니다.
설정 에셋은 `Assets/04_Data/Config/SO_AppConfig.asset`입니다.

설정·저장 정책과 최신 검증 결과는 [3·4단계 사용 가이드](Docs/SETTINGS_AND_SAVE.md)에 기록합니다.

Main 씬을 열고 Play하면 개발용 저장 공간에서 Boot 초기화 후 Main에 진입합니다. 반복 진입과 저장소 분리는 복제본 자동 실행에서 확인했습니다. 직접 조작·기기별 품질 확인 범위는 배포 가이드에 구분합니다. Title 직접 Play는 Boot 실행을 안내합니다. 사용법과 검증 범위는 [Editor 작업 가이드](Docs/EDITOR_WORKFLOW.md)를 따릅니다.

공통 코드는 장르에 독립적으로 설계하지만 현재 렌더링 구성은 **2D용**입니다.
3D 프로젝트에서 사용하려면 렌더러, 카메라, 조명과 관련 패키지를 별도로 검토해야 합니다.

## 문서

- [Bootstrap 사용 가이드](Docs/BOOTSTRAP.md): 실행·확장 위치와 검증 결과
- [설정·게임 저장 사용 가이드](Docs/SETTINGS_AND_SAVE.md): 3·4단계 사용법·버전·복구·게임별 확장
- [공통 기능 사용 가이드](Docs/COMMON_SERVICES.md): 5단계 음량·화면 적용, 입력·로딩과 검증 범위
- [가로형 PC·모바일 UI](Docs/RESPONSIVE_UI.md): Render Mode·Canvas Scaler·안전 영역과 검증 한계
- [Editor 작업 가이드](Docs/EDITOR_WORKFLOW.md): 6단계 Main 직접 Play·설정 검사·테스트 데이터 백업과 검증 상태
- [통합·Windows 빌드 검증](Docs/INTEGRATION_VALIDATION.md): 7단계 빌드·프로세스 재실행·오류 보호 결과와 남은 GUI 확인
- [SOLID·패턴 점검](Docs/ARCHITECTURE_REVIEW.md): 공통 기반의 책임·의존 경계와 설정 저장 실패 보완
- [프리셋 상세 Summary](Docs/PRESET_SUMMARY.md): 확인된 기능·검증·한계와 새 게임 제작 시 인계 기준
- [새 게임 시작 가이드](Docs/NEW_GAME_SETUP.md): 복제 직후 식별자·씬 확인과 게임별 JSON 데이터 연결 절차
- [v1.0.0 배포 가이드](Docs/RELEASE.md): 새 프로젝트 생성, 검증 기준과 지원 범위
- [변경 이력](Docs/CHANGELOG.md): 템플릿의 주요 변경과 버전 상태
- [제작 가능 범위 브리핑](Docs/GAME_CAPABILITY_BRIEF.md): 현재 프리셋으로 시작할 수 있는 게임과 추가 구현 영역, 캐릭터 에셋 애니메이션 인계 조건
- [로그라이크·로그라이트 사전 조사](Docs/ROGUELIKE_PLAY_FLOW_RESEARCH.md): 실제 게임의 런·영구 성장·중단 저장 비교와 6단계 개발 진입 제안
- [개발 로드맵](Docs/ROADMAP.md): 8단계 작업 순서와 완료 기준
- [초기화·데이터 관리 설계](Docs/INITIAL_SETTING.md): 구현 기준, 데이터 분담과 오류 처리
- [템플릿 완성·복제 체크리스트](Docs/TEMPLATE_CHECKLIST.md): 완료 조건과 새 프로젝트에서 바꿀 항목
- [에셋 폴더 구조](Assets/PROJECT_STRUCTURE.md): 폴더별 책임과 이름 규칙
- [Notion — Starter Project](https://app.notion.com/p/3d4bbcd98529818d9753eb412e3c8a50): 설계·결정 사항 정리. Git 문서와 자동 동기화되지는 않습니다.

## 저장소 사용

`Assets`와 `.meta`, `Packages`, `ProjectSettings`, 문서를 함께 버전 관리합니다.
`Library`, `Temp`, `Logs`, `UserSettings`, 자동 생성 IDE 파일은 제외합니다.
사용하지 않는 Unity Version Control 협업, Multiplayer Center, Visual Scripting 패키지는 제거했습니다. 2D 애니메이션·스프라이트 가져오기 도구는 새 게임에서 프레임 에셋을 사용할 수 있도록 유지합니다.
버전 태그의 소스 또는 GitHub Template으로 새 프로젝트를 만듭니다. 원본 작업 폴더의 개인 Editor 상태를 복사하지 않고, 배포 소스의 중립 식별자로 시작합니다.
