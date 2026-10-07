# 공통 기능 사용 가이드

기본 설정·화면·로딩·입력의 연결 지점과 기능별 사용 문서를 안내합니다. 씬·Runtime·입력·Pause·Audio API는 [실행 기반](PHASE_2_RUNTIME.md)에 둡니다.

## 이 문서의 순서

- [기능 찾기](#기능-찾기)
- [실행과 적용 범위](#실행과-적용-범위)
- [수명과 실패 처리](#수명과-실패-처리)
- [로딩과 입력](#로딩과-입력)
- [진단 로그](#진단-로그)
- [선택형 프리팹 Pool](#선택형-프리팹-pool)
- [API 참고](#api-참고)
- [검증 문서](#검증-문서)

## 기능 찾기

| 목적 | 상세 가이드 |
| --- | --- |
| 저장·슬롯·복구 | [설정과 저장](SETTINGS_AND_SAVE.md) |
| 실행 상태·수명·입력·Audio | [Phase 2](PHASE_2_RUNTIME.md) |
| 화면·이미지·로딩 프리팹 | [반응형 UI](RESPONSIVE_UI.md) |
| 로그·Debug·빌드 | [Phase 3](PHASE_3_DEVELOPMENT.md) |
| 프리팹 대여·반납 | [Pool](PHASE_4_MODULES.md) |

## 실행과 적용 범위

`00_StartScene`에서 Play한다. Boot가 사용자 설정을 읽은 뒤 음량·화면에 적용하고 Title로 이동한다. Title의 Options 화면과 Main의 설정 버튼은 새 값을 검증·시스템 적용한 뒤 저장한다. 설정은 기본 3개 슬롯 전체가 공유한다. 저장 실패 시 이전 시스템 값을 다시 적용하며, 백업 복구나 `SettingsService.Reload()`로 확정된 설정도 반영한다.

- `Volume`: 전체 음량 0~100%. `AudioListener.volume`을 사용하므로 일반 AudioSource의 소리에 적용한다. 예제에 배경음이나 효과음 콘텐츠를 추가하지는 않는다. 실제 청취 확인은 AudioSource에 클립을 연결해 수행한다.
- `Fullscreen`: 켜짐은 테두리 없는 전체화면(`FullScreenWindow`), 꺼짐은 창 모드(`Windowed`). 해상도·주사율·품질 설정은 변경하지 않는다. Editor의 Game View로는 실제 Windows 창 전환을 검증할 수 없으므로 빌드 검증 항목으로 남긴다.
- `Language`: 예제 버튼은 `en`/`ko`를 전환한다. Core는 공백·제어 문자가 없는 1~64자의 언어 식별자를 저장하므로 게임별 언어를 추가할 수 있다. 번역 테이블·글꼴·지원 언어 선택은 개별 게임에서 연결한다. 현재 예제 UI는 영어다.
- Title의 좌측 New Game / Continue는 각각 빈 슬롯과 저장 슬롯 선택 화면을 연다. 슬롯 관리에서 삭제·백업 복구를 수행하며 삭제 확인의 기본 포커스는 Cancel이다. Options / Credits는 별도 화면이고 Quit는 PC에 표시한다.
- 방향키/WASD·게임패드로 버튼을 선택하고 Enter/A로 실행한다. Escape/B는 확인창을 취소하거나 이전 화면으로 돌아간다. 씬 전환·실패·삭제 확인 뒤쪽에는 버튼 입력을 차단한다.

기존 Input System 액션 에셋의 `UI` 맵과 `InputSystemUIInputModule`을 사용한다. Phase 2의 StarterInputContext는 실행용 Asset 복제본의 Player/UI 맵을 컨텍스트에 따라 전환한다. 게임 입력은 RuntimeActions를 사용한다. 조작키 재설정·입력 감도는 후속 모듈 또는 Game Layer에서 연결한다.

## 수명과 실패 처리

`AppRoot`가 `IRuntimeSettings`를 소유한다. 기본 구현 `UnityRuntimeSettings`는 같은 화면 모드 요청을 반복하지 않으며, 루트 파괴나 초기화 실패 시 생성 당시 전역 음량·화면 모드로 복원한다. 이는 Play 반복 실행에서 이전 실행의 전역 설정을 남기지 않기 위한 처리다.

`SettingsService.Changed`는 현재 값이 확정된 뒤 발생한다. `AppRoot`는 Reload·백업 복구의 알림으로 시스템 설정을 적용하며 해제 시 구독을 제거한다. 화면 버튼의 설정 저장은 `AppRoot`가 검증 → 시스템 적용 → JSON 저장을 조정한다. 플랫폼 적용에서 예외가 나면 새 JSON을 쓰지 않고 앱을 실패 상태로 바꾸며 초기 시스템 설정을 복원한다. JSON 저장이 실패하면 이전 실행 설정으로 되돌린다. 이 보상 적용마저 실패하면 앱을 실패 상태로 전환한다.

테스트와 플랫폼별 구현은 `Begin` 전에 `ConfigureRuntimeSettings`로 대체 구현을 전달한다. 루트가 `Dispose`를 호출하므로 동일 인스턴스를 다른 루트와 공유하지 않는다. Core는 UI·Input System에 의존하지 않는다.

## 로딩과 입력

`AppRoot.IsTransitioning`과 `LoadingProgress`가 씬 전환 상태를 제공한다. 전환 시작을 같은 프레임에 `StateChanged`로 전달해 버튼을 잠근다. 진행률은 Unity 로드 작업의 값이며 로드 완료·씬 활성화까지 끝난 뒤에만 100%가 된다. 초기화 단계는 수치 대신 단계 문구로 표시한다.

`StarterLoadingOverlay`는 AppRoot 아래 단 하나의 Canvas로 생성되어 씬 전환 중에도 유지된다. 화면 위에 로딩 문구·진행률을 표시하고 포인터 입력을 막는다. 새 씬의 준비가 끝나면 숨기고 UI 선택을 복원한다. 실패 시에는 로딩 화면을 숨겨 기존 오류 안내를 볼 수 있게 한다.

Boot/Main의 `StarterScreen`과 Title의 `StarterTitleMenu`는 활성화 시 구독하고 비활성화 시 해제한다. 첫 진입 또는 선택 항목 소실 시 유효한 버튼을 선택해 마우스 없이도 사용할 수 있게 한다. 런타임 Canvas는 루트와 함께 제거되며 씬·프리팹에 별도로 복제할 필요가 없다.

## 진단 로그

Core의 StarterLog.Info/Warning/Error에 LogCategory와 메시지를 전달한다. Level/Category 필터, 빌드별 기본값과 Editor 조작은 [Phase 3 개발 지원](PHASE_3_DEVELOPMENT.md)을 따른다. 게임 코드의 payload·비밀값은 메시지에 넣지 않는다.

## 선택형 프리팹 Pool

씬/게임 객체가 소유하는 PrefabPool과 대여별 PoolLease로 재사용·반납을 관리한다. Core/AppServices에 등록하지 않는다. 게임별 reset·스폰·용량 정책과 연결 방법은 [선택형 모듈 사용법](PHASE_4_MODULES.md)을 따른다.

## API 참고

- [Unity AudioListener.volume](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioListener-volume.html)
- [Unity Screen.fullScreenMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-fullScreenMode.html)
- [Unity AsyncOperation.progress](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-progress.html)

## 검증 문서

현재 결과는 [검증 현황](INTEGRATION_VALIDATION.md), 당시의 상세 실행 과정은 [이전 공통 기능 검증](Archive/COMMON_SERVICES_VALIDATION.md)을 따릅니다.
