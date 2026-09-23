# 공통 기반 설계 점검 — SOLID와 적용 패턴

2026-09-23 기준. 이 프로젝트의 목적은 장르 규칙을 정하지 않은 Unity 게임 제작용 스타터다. 설계 패턴 하나를 전체에 강제하지 않고 변경 이유가 다른 경계에만 작은 추상화를 둔다.

| 원칙 | 현재 판단과 적용 |
| --- | --- |
| 단일 책임 | `AppBootstrap`은 진입, `AppRoot`는 앱 흐름 조정, `SettingsService`와 `GameSessionService`는 각 데이터 정책, `JsonFileStore`는 파일 교체, UI 컴포넌트는 표시·입력을 맡는다. `AppRoot`는 여러 서비스를 **조정**하므로 저장 형식이나 화면 구현을 직접 소유하지 않는다. |
| 개방·폐쇄 | `ITextFileStore`, `IRuntimeSettings`, 게임 payload 검증 함수를 바꿔 저장 위치·플랫폼 적용·게임별 데이터 규칙을 교체할 수 있다. 단일 슬롯과 `en`/`ko` 예제 UI는 시작 구현이며 모든 게임의 저장·현지화 정책으로 간주하지 않는다. |
| 리스코프 치환 | 대체 저장소는 읽기·쓰기 실패를 보고하고 기존 파일을 보존해야 한다. 대체 실행 설정 구현은 실패 시 직전 적용 상태의 복원을 시도하고 복구 실패도 예외로 알려야 한다. 이 계약을 실패 주입 테스트로 확인한다. |
| 인터페이스 분리 | `ITextFileStore`는 텍스트 읽기·원자적 쓰기, `IRuntimeSettings`는 설정 적용·수명 해제만 노출한다. 게임별 큰 서비스 인터페이스나 범용 이벤트 버스는 두지 않는다. |
| 의존성 역전 | Core의 저장·플랫폼 적용 정책은 작은 인터페이스에 의존한다. `AppRoot`는 기본 구현을 조립하는 진입점이고, UI는 Core 상태를 구독한다. Core에서 UI·Editor 어셈블리를 참조하지 않는다. |

## 선택한 패턴과 변경 이유

- **Composition Root / Application Controller:** `AppRoot`에서 공통 서비스의 생성·수명·씬 전환을 조정한다. Unity 씬 수명 경계가 한곳에 있어 추가 DI 컨테이너 없이도 교체와 테스트가 가능하다.
- **Repository + Adapter:** `JsonRepository<T>`가 버전·복구 정책을 담당하고 `ITextFileStore`가 디스크 접근을 격리한다. `IRuntimeSettings`가 Unity 전역 음량·화면 API를 격리한다. 실제 저장 형식과 게임 payload는 별개로 확장한다.
- **Observer:** `SettingsService.Changed`와 `AppRoot.StateChanged`로 설정 적용과 화면 갱신을 연결한다. 해제 시 구독을 제거한다.
- **보상 가능한 설정 변경:** 이전에는 JSON 저장 후 플랫폼 적용이 실패하면 새 설정 파일만 남았다. 이제 `AppRoot.TrySaveSettings`가 값을 검증하고 플랫폼에 먼저 적용한 뒤 저장한다. 저장이 실패하면 이전 실행 설정을 다시 적용하며, 플랫폼 적용이 실패하면 새 JSON을 쓰지 않고 앱을 실패 상태로 전환한다. `SettingsService.Reload()`·백업 복구의 변경 알림은 기존처럼 실행 설정에 반영된다. 파일 저장과 화면 장치는 단일 원자적 트랜잭션이 아니므로 보상 적용마저 실패하면 앱을 실패 상태로 전환한다.

장르별 런·사망·영구 성장·자동 저장, 다중 슬롯, 번역 콘텐츠와 Player 입력 맵은 게임별 확장 영역이다. 이를 공통 Core의 전략 인터페이스로 미리 만들면 사용하지 않는 추상화와 설정 비용이 늘어난다.

## 검증과 다음 단계

Unity 6000.3.16f1 격리 복제본에서 EditMode 46개·PlayMode 27개가 모두 통과했다. 플랫폼 적용 실패 시 이전 JSON 불변, 디스크 쓰기 실패 시 이전 실행 값 복원을 포함한다. 변경된 Core로 Windows x64 Development 빌드가 성공했고, 별도 Player 프로세스에서 첫 실행 저장과 재실행 이어하기가 통과했다. 로그는 Git 제외 `Logs/SolidEditMode.xml`, `Logs/SolidPlayMode.xml`, `Logs/SolidWindowsBuild.log`, 복제본 `Builds/Windows/SolidPlayerCreate.log`·`SolidPlayerResume.log`에 있다.

7단계의 남은 확인은 원본 Editor GUI 직접 Play·메뉴와 Windows의 실제 화면·오디오·물리 입력이다. 저장 중 강제 종료 실험에서 남은 `.tmp`는 이후 정리 정책을 검토한다. 자동 검증이나 빌드 성공만으로 이 현장 확인을 완료 처리하지 않는다.
