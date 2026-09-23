# 새 게임 프로젝트 시작 가이드

기준: Unity `6000.3.16f1`, Windows PC와 URP 2D 예제. 이 저장소는 Boot·설정·저장·기본 씬 흐름을 재사용하는 **프로젝트 템플릿**이다. 전투·캐릭터·스테이지·게임별 UI는 복제한 게임에서 만든다. 검증이 끝난 버전으로 새 저장소를 만들고, 사용한 커밋 또는 태그를 새 게임의 문서에 기록한다.

## 첫 Play 전에

1. `Assets`, 각 `.meta`, `Packages`, `ProjectSettings`가 포함된 새 저장소를 Unity Hub에 추가하고 지정된 Editor 버전으로 연다. `Library`, `Temp`, `Logs`, `Builds`, `UserSettings`는 복사하지 않는다.
2. **Project Settings > Player**에서 Company Name, Product Name, 플랫폼별 Application Identifier, 아이콘을 게임 고유 값으로 바꾼다. 기본 `StarterTemplate` / `StarterProject`로 여러 게임을 실행하면 `Application.persistentDataPath`가 겹칠 수 있다.
3. Unity Cloud가 필요하다면 새 게임의 프로젝트를 연결한다. 이 템플릿에 추적된 Cloud 프로젝트·조직 ID는 비어 있다. Editor Services와 Unity Hub의 실제 연결 표시도 확인한다.
4. `Tools > Starter Project > Validate Setup`을 실행한다. `00_StartScene` → `01_Title` → `02_MainScene`이 빌드 씬 앞에 있고 `SO_AppConfig`가 세 씬을 가리키는지 확인한다. 처음에는 Boot에서 Play한다.
5. 예제 UI는 **Screen Space Overlay + Scale With Screen Size, 1920×1080, Match 0.5**를 사용한다. 1920×1080은 최대 해상도가 아니라 디자인 기준이다. 현재 모바일 방향은 가로이며 실제 기기·터치·노치 확인은 새 게임에서도 필요하다.

## 게임별 저장 데이터 연결

Core는 게임 내용을 모르는 단일 슬롯 파일 저장을 제공한다. `Gameplay` 어셈블리에서 `GamePayloadPolicy` 파생 ScriptableObject를 만들고, **Boot 씬의 `AppBootstrap` 컴포넌트에 에셋을 연결**한다. 정책을 연결하지 않으면 현재 예제대로 payloadVersion 1과 빈 `{}` 객체를 사용한다. 정책은 새 게임과 Editor의 Main 직접 Play에도 같은 초기 JSON을 제공한다.

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

실제 진행 상태를 저장할 때 게임별 코드가 JSON 객체를 만든 뒤 **Main 씬에서** `AppRoot.Instance.TrySaveGame(payloadJson)`을 호출한다. `Continue` 뒤에는 `AppRoot.Instance.Game.Current.PayloadJson`을 게임 모델로 역직렬화한다. 단일 슬롯에 이미 다른 세션이 있으면 교체 확인을 받은 뒤 `TrySaveGame(payloadJson, replaceExisting: true)`를 호출한다. 예제 Main의 `Save Game` 버튼은 현재 payload를 다시 저장할 뿐, 캐릭터나 스테이지 객체를 자동 수집하지 않는다. 게임별 저장 버튼·체크포인트에서 새 JSON을 전달하도록 연결한다.

정적 캐릭터 정의·스프라이트는 에셋으로 보관하고 저장 payload에는 캐릭터 ID, 달라진 능력치·성장 상태, 스테이지 진행처럼 **변하는 값**을 넣는다. 약 100명 규모도 필드 크기에 따라 1MiB 안에 들어갈 수 있지만 실제 UTF-8 JSON 파일 전체 크기를 측정해야 한다. 파일 크기 상한은 1MiB, JSON 깊이 상한은 32다. 저장은 동기식이며 단일 슬롯·단일 프로세스 소유를 전제로 한다. 게임 데이터 형식을 바꿀 때는 `PayloadVersion`을 올리고 이전 저장의 변환·보호 정책을 게임에서 설계한다. 버전만 올리면 이전 파일은 보호되지만 이어하기는 비활성화된다. [상세 저장 계약](SETTINGS_AND_SAVE.md)

## 게임 제작 시작점

- `Assets/02_Scripts/Gameplay`에 게임 고유 네임스페이스와 로직을 둔다. `StarterProject.Core`·`StarterProject.UI`를 이름만 바꾸지 않는다. Core의 공개 API로 연결하고, 공통 기반 변경은 별도 검증한다.
- Boot·Title·Main의 예제 UI는 교체 가능한 시작 화면이다. 장르별 메뉴, HUD, 접근성, 터치 배치는 게임 요구에 맞춰 만든다. 프레임별 Walk·Attack·Hurt 에셋의 Sprite/Animation Clip/Animator 제작도 **새 게임 프로젝트에서** 진행한다.
- 3D 게임은 렌더러·카메라·조명·패키지를 다시 구성한다. Android/iOS 실기기 빌드와 물리 장치·해상도 검증은 현재 프리셋의 Windows 자동 검증에 포함되지 않는다.
- Windows 미리보기는 `Tools > Starter Project > Build Windows Preview`에서 만들고 `Builds/Windows/<Product Name>.exe`로 저장된다. Windows 파일명에 쓸 수 없는 문자는 `_`로 바뀐다.

첫 게임 데이터 저장·앱 재실행 후 이어하기, 실패·백업 복구, 대상 플랫폼 빌드, 실제 화면·입력을 게임별로 검증하고 기준 커밋을 남긴다. 템플릿의 이후 수정은 이미 복제한 게임에 자동 적용되지 않는다.
