# 새 게임 프로젝트 시작 가이드

현재 main으로 새 게임을 만들고 공통 기반을 게임 코드에 연결하는 순서입니다. Windows·URP 2D가 현재 시작점입니다. [프리셋 개요](PRESET_SUMMARY.md) · [문서 안내](INDEX.md)

GitHub Template의 현재 main 또는 선택한 소스 커밋에서 시작하고 기준 커밋을 기록합니다. 기존 v1.0.0 ZIP은 이전 단일 슬롯 버전입니다. 게임 규칙·콘텐츠는 새 프로젝트의 Gameplay에서 구현합니다.

## 이 문서의 순서

- [첫 Play 전에](#첫-play-전에)
- [Phase 2 게임 실행 연결](#phase-2-게임-실행-연결)
- [게임별 저장 데이터 연결](#게임별-저장-데이터-연결)
- [슬롯·메뉴 교체 시](#슬롯메뉴-교체-시)
- [예제 UI에 게임 에셋 적용](#예제-ui에-게임-에셋-적용)
- [선택형 Pool 연결](#선택형-pool-연결)
- [개발 지원 연결](#개발-지원-연결)
- [게임 제작 시작점](#게임-제작-시작점)

## 첫 Play 전에

1. `Assets`, 각 `.meta`, `Packages`, `ProjectSettings`가 포함된 새 저장소를 Unity Hub에 추가하고 지정된 Editor 버전으로 연다. `Library`, `Temp`, `Logs`, `Builds`, `UserSettings`는 복사하지 않는다.
2. **Project Settings > Player**에서 Company Name, Product Name, 플랫폼별 Application Identifier, 아이콘을 게임 고유 값으로 바꾼다. 기본 `StarterTemplate` / `StarterProject`로 여러 게임을 실행하면 `Application.persistentDataPath`가 겹칠 수 있다.
3. Unity Cloud가 필요하다면 새 게임의 프로젝트를 연결한다. 이 템플릿에 추적된 Cloud 프로젝트·조직 ID는 비어 있다. Editor Services와 Unity Hub의 실제 연결 표시도 확인한다.
4. `Tools > Starter Project > Validate Setup`을 실행한다. `00_StartScene` → `01_Title` → `02_MainScene`이 빌드 씬 앞에 있고 `SO_AppConfig`가 세 씬을 가리키는지 확인한다. 기본 StarterScreen·StarterTitleMenu UI를 유지한다면 `Tools > Starter Project > Validate Example UI`도 실행한다. 사용자 UI로 교체해도 Main 직접 Play와 Windows Preview는 공통 초기화 검사만 사용한다. 처음에는 Boot에서 Play한다.
5. 예제 UI는 **Screen Space Overlay + Scale With Screen Size, 1920×1080, Match 0.5**를 사용한다. 1920×1080은 최대 해상도가 아니라 디자인 기준이다. 현재 모바일 방향은 가로이며 실제 기기·터치·노치 확인은 새 게임에서도 필요하다.

## Phase 2 게임 실행 연결

Gameplay 씬에 활성 SceneRoot 파생 컴포넌트 하나를 두고 Initialize/Enter/Exit/Dispose 훅에 게임 조립·실행·정리를 연결한다. 씬별 비동기는 LifetimeToken을 사용하고 await 이후 취소를 확인한다. Definition SO와 변경 가능한 Runtime을 분리하고, 선택형 DataCatalog를 AppConfig에 지정하면 기존 Validate Setup에서 ID·참조를 검사한다.

저장은 root.Data.Runtime.Payload를 변경한 뒤 root.TrySaveGame()으로 명시적 스냅샷을 만든다. 기본 UI를 교체할 때는 입력 어댑터를 생성하고 그 RuntimeActions를 게임 입력에도 사용한다. 게임별 Pause UI는 root.TryChangeGameState와 GameStateChanged를 사용한다. BGM/SFX/UI는 root.Audio에 요청한다. [전체 계약·예제](PHASE_2_RUNTIME.md)

게임 코드가 StarterInputContext 등 기본 UI 타입을 직접 사용하면 게임 asmdef에 StarterProject.UI 참조도 추가한다. Pool을 사용하면 StarterProject.Pooling을 선택적으로 참조한다. Core는 해당 선택 의존성을 가지지 않는다.

## 게임별 저장 데이터 연결

Core는 게임 내용을 모르는 기본 3개 독립 슬롯 파일 저장과 하나의 활성 세션을 제공한다. `Gameplay` 어셈블리에서 `GamePayloadPolicy` 파생 ScriptableObject를 만들고, **Boot 씬의 `AppBootstrap` 컴포넌트에 에셋을 연결**한다. 정책을 연결하지 않으면 현재 예제대로 payloadVersion 1과 빈 `{}` 객체를 사용한다. 정책은 새 게임과 Editor의 Main 직접 Play에도 같은 초기 JSON을 제공한다.

JSON 패키지를 사용하는 게임 코드를 추가할 때 `Assets/02_Scripts/Gameplay/MyGame.Gameplay.asmdef`를 다음처럼 만든다. `StarterProject.Core`를 참조하고 `Newtonsoft.Json.dll`을 명시적으로 연결한다. 다른 패키지나 DLL을 사용하는 게임 코드는 해당 참조도 추가한다.

```json
{
  "name": "MyGame.Gameplay",
  "rootNamespace": "MyGame",
  "references": ["StarterProject.Core", "Unity.InputSystem"],
  "overrideReferences": true,
  "precompiledReferences": ["Newtonsoft.Json.dll"]
}
```

```csharp
using System.IO;
using Newtonsoft.Json.Linq;
using StarterProject;
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/Save Payload Policy")]
public sealed class MyGamePayloadPolicy : GamePayloadPolicy
{
    public override int PayloadVersion => 1;
    public override string CreateInitialPayload() => "{\"stage\":1,\"characters\":{}}";

    public override void ValidatePayload(string payloadJson)
    {
        var data = JObject.Parse(payloadJson);
        if (data["stage"]?.Type != JTokenType.Integer || data["characters"]?.Type != JTokenType.Object)
            throw new InvalidDataException("Stage and characters are required.");
    }
}
```

실제 진행 상태를 저장할 때 게임별 코드가 JSON 객체를 만든 뒤 `AppRoot.Instance.TrySaveGame(payloadJson)`을 호출한다. **활성 게임 세션이 있으면 Main과 추가 Gameplay 씬에서 저장할 수 있다.** Boot·Title·AppRoot의 씬 전환 중에는 저장하지 않는다. `Continue` 뒤에는 Main에서 `AppRoot.Instance.Game.Current.PayloadJson`을 게임 모델로 역직렬화한다. 저장은 `Game.CurrentSlotId`의 활성 슬롯으로 제한한다. 새 Title 메뉴는 `TryStartNewGame(slotId)`로 빈 슬롯만 시작한다. 기존 호환 API로 만든 새 세션이 활성 슬롯의 다른 저장을 대체해야 한다면 확인을 받은 뒤 `TrySaveGame(payloadJson, replaceExisting: true)`를 호출한다. 예제 Main의 `Save Game` 버튼은 현재 payload를 다시 저장할 뿐, 캐릭터나 스테이지 객체를 자동 수집하지 않는다. 게임별 저장 버튼·체크포인트에서 새 JSON을 전달하도록 연결하고 실패 결과도 처리한다.

정적 캐릭터 정의·스프라이트는 에셋으로 보관하고 저장 payload에는 캐릭터 ID, 달라진 능력치·성장 상태, 스테이지 진행처럼 **변하는 값**을 넣는다. 약 100명 규모도 필드 크기에 따라 1MiB 안에 들어갈 수 있지만 실제 UTF-8 JSON 파일 전체 크기를 측정해야 한다. 파일 크기 상한은 1MiB, JSON 깊이 상한은 32다. 저장은 동기식이며 하나의 활성 세션·단일 프로세스 소유를 전제로 한다. 슬롯별 파일 전체에 각각 1MiB 상한을 적용한다. 게임 데이터 형식을 바꿀 때는 `PayloadVersion`을 올리고 이전 저장의 변환·보호 정책을 게임에서 설계한다. 버전만 올리면 이전 파일은 보호되지만 이어하기는 비활성화된다. [상세 저장 계약](SETTINGS_AND_SAVE.md)

## 슬롯·메뉴 교체 시

기존 `save-slot-1.json`은 그대로 슬롯 1로 읽고 슬롯 2·3은 독립 파일로 쓴다. `Game.Slots`/`GetSlot(id)`의 불변 정보로 사용자 UI를 표시하고 새 게임·이어하기·삭제·복구는 AppRoot의 슬롯 인자 API로 요청한다. 삭제를 지원하는 저장소는 `IDeleteSaveFileStore`도 구현해야 하며, 지원하지 않으면 메뉴의 삭제를 사용하지 않는다. 서비스 생성자의 slotCount는 1~10이지만 AppRoot 기본 구성과 예제 UI는 3개이므로 개수를 바꾸면 조립·화면 참조·검사도 함께 맞춘다. 상태·소유권·하위 호환 API의 정확한 의미는 [저장 계약](SETTINGS_AND_SAVE.md)을 따른다.

## 예제 UI에 게임 에셋 적용

Boot·Title·Main 씬의 `Starter UI`에서 배경과 버튼 `Image`의 `Source Image`를 게임 스프라이트로 교체한다. Boot/Main의 `StarterScreen`, Title의 `StarterTitleMenu`에 연결된 버튼·상태 `Text`와 `EventSystem`·Input System UI 모듈은 유지한다. Title은 좌측 메뉴와 페이지별 슬롯·관리·설정·제작진·삭제 확인 참조를 씬에 보관한다. 로딩 화면도 바꿀 경우 `StarterLoadingOverlay` 프리팹의 문구·진행률 참조를 연결하고 Boot의 `StarterScreen > Loading Overlay Prefab` 슬롯에 넣는다. 프리팹을 지정하지 않으면 기본 로딩 UI가 유지된다. 화면 전체 배경과 조작 콘텐츠의 안전 영역 위치, 글꼴과 버튼 터치 크기는 [UI 기준](RESPONSIVE_UI.md)을 따른다.

## 선택형 Pool 연결

반복 생성하는 게임 프리팹에는 [Prefab Pool 사용법](PHASE_4_MODULES.md)을 적용한다. Pooling 모듈과 테스트는 독립 폴더/어셈블리에 있어 필요할 때만 게임에서 참조한다. 프리팹 reset·스폰·재고 상한은 게임이 결정한다. SceneRoot.Exit에서 ReturnAll로 대여를 종료하고 비동기 결과 적용 전에는 씬 토큰과 PoolLease.IsValid를 확인한다. 다른 선택형 후보는 게임의 필수 요구가 확인될 때 선정한다.

## 개발 지원 연결

[Phase 3 사용법](PHASE_3_DEVELOPMENT.md)에서 Debug Menu, 로그 필터, 빌드 환경과 강화된 Validate Setup을 확인한다. 예약 빌드 심볼은 전역 Player Settings에 넣지 않고 전용 빌드 명령을 사용한다. 기본 StarterSmoke의 씬 경로·payload 표식을 새 게임의 계약에 맞춰 갱신한 뒤 격리 저장으로 실행→저장→새 앱→불러오기 흐름을 확인한다. 실제 플랫폼/장치 출시는 게임별로 검증한다.

## 게임 제작 시작점

- `Assets/02_Scripts/Gameplay`에 게임 고유 네임스페이스와 로직을 둔다. `StarterProject.Core`·`StarterProject.UI`를 이름만 바꾸지 않는다. Core의 공개 API로 연결하고, 공통 기반 변경은 별도 검증한다.
- Boot·Title·Main의 예제 UI는 교체 가능한 시작 화면이다. 장르별 메뉴, HUD, 접근성, 터치 배치는 게임 요구에 맞춰 만든다. 프레임별 Walk·Attack·Hurt 에셋의 Sprite/Animation Clip/Animator 제작도 **새 게임 프로젝트에서** 진행한다.
- 3D 게임은 렌더러·카메라·조명·패키지를 다시 구성한다. Android/iOS 실기기 빌드와 물리 장치·해상도 검증은 현재 프리셋의 Windows 자동 검증에 포함되지 않는다.
- Windows 미리보기는 `Tools > Starter Project > Build Windows Preview`에서 만들고 `Builds/Windows/<Product Name>.exe`로 저장된다. Windows 파일명에 쓸 수 없는 문자는 `_`로 바뀐다.

첫 게임 데이터 저장·앱 재실행 후 이어하기, 실패·백업 복구, 대상 플랫폼 빌드, 실제 화면·입력을 게임별로 검증하고 기준 커밋을 남긴다. 템플릿의 이후 수정은 이미 복제한 게임에 자동 적용되지 않는다.
