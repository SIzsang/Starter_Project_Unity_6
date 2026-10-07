# Phase 4 선택형 모듈 요구 평가

기준: 2026-10-07, main / ab5635763eae9da15dcfa0a7e6bbc4bf7950c581 이후 작업 트리. Phase 2/3 완료 변경과 사용자 ProjectSettings 변경을 보존한다. Phase 4 후보는 필수 구현 순서가 아니다.

## 현재 코드의 반복 요구

| 후보 | 현재 증거 | 이번 판단 |
| --- | --- | --- |
| Localization 문자열/테이블 | UserSettings.language 저장·복원과 Title/Main의 en/ko 전환이 이미 있으나 문구는 코드에 고정된다. 두 화면과 이후 게임 UI에서 같은 문자열 조회·설정 연결이 필요하다. | 보류. 사용자 판단으로 필수적이지 않으며 실제 번역 콘텐츠 요구가 확인될 때 선정한다. |
| UI Navigation / Popup Stack | Title이 페이지·삭제 확인·취소 기본 선택을 관리하고 Screen/Pause가 자기 선택을 복구한다. 중첩 popup 사용처는 아직 없다. | 기존 화면의 소유권으로 현재 요구를 처리한다. 새 공통 스택을 미리 만들지 않는다. |
| Object Pool | 사용자가 Pool을 우선 필요로 지정했다. 현재 측정된 성능 문제를 가정하지 않으며 기본 Audio는 기존 Source를 재사용한다. | 선정. 사용자가 Pool을 우선 필요로 지정했다. Game Layer가 프리팹 reset 규칙을 소유하고 모듈은 대여/반납/소유자 수명만 관리한다. |
| Asset Provider / Addressables | 현재 Scene/직접 에셋 참조로 필요한 콘텐츠를 공급한다. 비동기 콘텐츠·메모리 해제·원격 배포 요구가 없다. | 보류. 새 패키지와 강제 로더를 추가하지 않는다. |
| Time Service | 현재 PauseService가 필요한 timeScale 캡처·중지·복구를 제공한다. 여러 시간 영역이나 중첩 배속이 없다. | 기존 경로 유지. |
| Input Rebinding / Device Detection | Gameplay/UI/Debug 컨텍스트와 키보드/게임패드 UI는 기존 어댑터로 연결된다. 사용자 키 저장·장치별 프롬프트 요구가 없다. | 보류. 실제 게임 액션·저장 호환·장치 전환 UX부터 결정해야 한다. |
| Platform 추상화 | 현재 Windows 예제와 기존 IRuntimeSettings로 화면·음량을 처리한다. 추가 스토어/플랫폼 SDK 요구가 없다. | 기존 경계 유지. |

## 선택 기준과 완료선

사용자 답변: Pool을 제외한 후보는 필수적이지 않으며, 현재 구현에서 필수적인 후보만 개발한다. 이번 범위는 후보 평가 → 선택형 프리팹 Pool → Architecture/Integration Review와 위험 기반 최소 검증으로 확정했다.

Core의 AppConfig/AppRoot/AppServices와 저장 형식은 선택형 모듈을 참조하지 않는다. 모듈을 연결하지 않은 프로젝트는 기존 실행 흐름을 유지한다. Pool은 씬/게임 객체가 소유하며 게임별 reset 규칙·스폰 시점·용량 결정은 Game Layer가 담당한다. Unity의 기존 ObjectPool을 사용하고 서비스 등록·Singleton·장르별 규칙·새 패키지는 추가하지 않는다.

이 문서는 요구 선정과 사용법을 기록한다. 실제 검증은 INTEGRATION_VALIDATION.md, 진행 상태는 ROADMAP.md와 Notion 11에서 관리한다. 정의되지 않은 Phase 5를 만들지 않는다.

## Prefab Pool 사용

Assets/02_Scripts/Modules/Pooling의 StarterProject.Pooling 어셈블리는 Unity만 참조한다. Core/UI/Editor에서 이 모듈을 참조하지 않는다. 게임 어셈블리에서 사용할 때 StarterProject.Pooling을 참조한다.

씬의 GameObject에 PrefabPool을 추가하고 Inspector에서 Prefab과 Max Retained를 지정한다. 코드 생성 시 첫 Rent/Prewarm 전에 Configure(prefab, maximumRetained)를 호출한다. 프리팹 하나당 Pool 하나를 명시적으로 소유한다. 씬 전역 Registry·Singleton·AppServices 등록은 없다.

~~~csharp
using StarterProject.Pooling;
using UnityEngine;

// 새 게임의 Game Layer에 둔다.
public sealed class PooledEffectExample : MonoBehaviour
{
    [SerializeField] private PrefabPool effects;
    private PoolLease effect;

    public void Prewarm() => effects.Prewarm(8); // Max Retained >= 8일 때 선택 사용

    public void Spawn(Vector3 position, Quaternion rotation)
    {
        effect.Dispose();
        effect = effects.Rent(position, rotation, transform,
            prepare: instance => { /* 게임별 상태 초기화. 아직 비활성이다. */ });
    }

    public void Despawn() => effect.Dispose();
    private void OnDisable() => Despawn();
}
~~~

Rent는 비활성 clone을 얻고 부모/월드 위치/회전과 프리팹 기준 localScale을 설정한 뒤 prepare를 호출하고 마지막에 활성화한다. 새 clone도 비활성 보관 부모 아래에서 생성하여 준비 전 OnEnable을 막는다. prepare는 동기 완료 콜백이다. async void로 전달하지 않는다. prepare가 실패하면 clone을 회수하고 호출자에게 예외를 전달한다. 게임별 상태·Animator/Particle/Physics 초기화는 prepare 또는 게임의 OnEnable/OnDisable이 소유한다.

PoolLease는 대여 세대를 포함한 작은 값 형식이다. IsValid가 false면 GameObject는 null이고 TryReturn은 false다. 이전 대여의 복사본·반납·지연 callback으로 다시 빌려준 객체를 회수하지 않는다. Raw GameObject만 저장하고 나중에 반납하는 API는 제공하지 않는다. prefab 원본과 PooledInstance 내부 컴포넌트를 게임 코드에서 수정하거나 제거하지 않는다.

ReturnAll로 현재 대여를 종료한다. 풀 컴포넌트/소유자를 비활성화해도 전체 반납하며 재활성화하면 재고를 사용할 수 있다. Pool 소유자 파괴 시 활성/비활성 clone을 모두 파괴한다. 대여 시 다른 씬의 외부 parent에 둔 clone도 같은 Pool 소유다. DontDestroyOnLoad를 자동으로 적용하지 않으며 앱 전역 Pool이 필요한 게임은 소유자를 직접 구성한다.

SceneRoot.Exit에서 게임 작업을 종료할 때 Pool.ReturnAll을 호출하고, await 결과 적용 전에는 기존 씬 토큰과 lease.IsValid를 확인한다. Pool 반납 자체는 게임의 임의 Task를 취소하지 않는다. destroyCancellationToken은 객체 파괴 때 취소되므로 재사용 반납을 나타내지 않는다.

Max Retained는 반납 후 보관 수의 상한이다. 동시에 대여할 수 있는 수의 제한이 아니다. 초과 반납은 파괴하고 Prewarm은 이 상한 내에서 비활성 재고를 준비한다. Default Capacity는 내부 컬렉션 용량이며 프리팹 생성 수를 뜻하지 않는다. 실제 활성 수 제한·스폰 시점·적절한 용량은 게임이 결정한다. ActiveCount/InactiveCount는 이 모듈이 추적하는 살아 있는 clone 수다.

모든 호출은 메인 스레드에서 수행한다. prepare/활성화/비활성화 callback 안에서 Pool 작업에 재진입하지 않는다. 준비 중 소유자가 종료되면 대여를 회수하고 OperationCanceledException을 반환한다. 게임이 clone을 직접 Destroy한 경우 대여는 무효가 되고 재고 파괴 시 다음 대여에서 제거/교체한다. 정상 종료는 Dispose/TryReturn 경로를 사용한다.

[Unity 6000.3 ObjectPool 문서](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html)의 기본 저장소·상한·정리 동작을 재사용한다. 이번 작업은 게임의 성능 수치를 측정한 최적화 결과로 집계하지 않는다.

테스트도 모듈 내부의 StarterProject.Pooling.Tests 어셈블리에 둔다. Pooling 폴더를 제외하면 해당 코드/메타/테스트 참조가 함께 빠지며 기존 프로젝트 어셈블리의 Pool 의존은 없다.

선정한 Pool 범위와 Gate를 완료했다. 다른 후보는 현재 필수 요구가 없어 보류했다. 다음 Phase는 로드맵에 정의되지 않았으며 다음 Work는 새 게임의 실제 요구/후속 작업 선정부터 시작한다.
