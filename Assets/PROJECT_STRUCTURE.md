# Starter Project Asset Structure

`Starter Project`는 여러 게임에서 재사용할 수 있도록 에셋 종류 중심의 단순한 폴더 구조를 사용합니다.
존재하지 않는 기능을 빈 폴더로 미리 세분화하지 않고, 실제 파일이 생길 때 필요한 하위 폴더를 추가합니다.

## Current structure

```text
Assets
├─ 01_Scenes
│  ├─ Boot
│  ├─ Main
│  └─ Test
├─ 02_Scripts
│  ├─ Core
│  │  ├─ Bootstrap
│  │  ├─ Persistence
│  │  ├─ Settings
│  │  ├─ Save
│  │  └─ Manager
│  ├─ Gameplay
│  ├─ UI
│  ├─ Editor
│  └─ Tests
├─ 03_Prefabs
├─ 04_Data
│  └─ Config
├─ 05_Art
│  └─ Textures
├─ 06_Animations
├─ 07_Audio
├─ 08_UI
├─ 09_VFX
├─ 10_Materials
├─ 11_Shaders
├─ 12_Fonts
├─ 13_Input
├─ 14_Settings
│  ├─ Rendering
│  └─ SceneTemplates
├─ 15_Localization
├─ 90_ThirdParty
└─ 99_Development
```

## Folder responsibilities

- `01_Scenes`: 실행 씬과 테스트 씬
- `02_Scripts/Core`: 게임 콘텐츠에 종속되지 않는 초기화, 저장, 이벤트 등의 기반 코드
- `02_Scripts/Gameplay`: 캐릭터, 전투, 퍼즐, 스테이지 등 개별 게임의 실제 규칙
- `02_Scripts/UI`: UI 동작을 담당하는 C# 코드
- `02_Scripts/Editor`: Unity Editor 전용 코드
- `02_Scripts/Tests`: 자동화 테스트 코드
- `03_Prefabs`: 재사용하는 GameObject 프리팹
- `04_Data`: ScriptableObject 등 실제 게임 데이터 에셋
- `05_Art`: 스프라이트, 텍스처와 기타 원본 아트
- `06_Animations`: Animation Clip, Animator Controller, Timeline
- `07_Audio`: BGM, SFX, Audio Mixer
- `08_UI`: UXML, USS, UI 이미지 등 UI 에셋
- `09_VFX`: 파티클과 시각 효과 에셋
- `10_Materials`: 머티리얼과 물리 머티리얼
- `11_Shaders`: Shader와 Shader Graph
- `12_Fonts`: 폰트와 TextMesh Pro 폰트 에셋
- `13_Input`: Input System 액션 에셋
- `14_Settings`: 렌더링 및 프로젝트용 Unity 설정 에셋
- `15_Localization`: 문자열 테이블과 현지화 에셋
- `90_ThirdParty`: 외부 제작 에셋 원본
- `99_Development`: 프로토타입과 개발 전용 임시 에셋

## Growth rules

- 하위 폴더는 해당 기능의 실제 파일이 생길 때 추가합니다.
- `Gameplay` 아래 도메인 구조는 구현 과정에서 결정합니다.
- `Scripts/UI`에는 코드만, `08_UI`에는 UI 에셋만 둡니다.
- `Scripts`에는 데이터 타입 정의를, `04_Data`에는 실제 ScriptableObject 인스턴스를 둡니다.
- 외부 에셋은 `90_ThirdParty`에 원본 상태로 보관하고 직접 수정하지 않습니다.
- Addressables를 도입하더라도 에셋을 별도 보관소로 옮기지 않고 기존 위치에서 Group과 Label로 관리합니다.
- 실제 자동 테스트와 의존성 분리를 위해 Core / UI / Editor / PlayModeTests / EditModeTests에 최소 Assembly Definition(`asmdef`)을 사용합니다. Core는 UI·Editor 패키지에 의존하지 않습니다.
- `Core/Manager`는 현재 존재하는 폴더입니다. 모든 기능을 Manager로 만들거나 전역 Singleton으로 두어야 한다는 의미는 아닙니다.
- `Sprites` 등의 하위 폴더는 실제 에셋이 생길 때 추가합니다.
- 초기화와 데이터 관리의 제안 기준은 [초기 세팅 설계](../Docs/INITIAL_SETTING.md)를 참고합니다.

## Naming guideline

- C# 타입과 파일: `PascalCase`
- Scene: 현재 `00_StartScene`, `01_Title`, `02_MainScene`을 유지합니다. 숫자는 이름 정렬용이며 실행 순서는 코드와 빌드 씬 목록으로 정합니다.
- Prefab: `PF_Category_Name`
- ScriptableObject 에셋: `SO_Category_Name`
- Sprite: `SPR_Category_Name`
- Animation: `ANIM_Target_Action`
- Audio: `BGM_Name`, `SFX_Category_Name`
