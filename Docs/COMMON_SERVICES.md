# 공통 기능 사용 가이드 — 5단계

## 실행과 적용 범위

`00_StartScene`에서 Play한다. Boot가 사용자 설정을 읽은 뒤 음량·화면에 적용하고 Title로 이동한다. Title/Main의 설정 버튼은 새 값을 검증·시스템 적용한 뒤 저장한다. 저장 실패 시 이전 시스템 값을 다시 적용하며, 백업 복구나 `SettingsService.Reload()`로 확정된 설정도 반영한다.

- `Volume`: 전체 음량 0~100%. `AudioListener.volume`을 사용하므로 일반 AudioSource의 소리에 적용한다. 예제에 배경음이나 효과음 콘텐츠를 추가하지는 않는다. 실제 청취 확인은 AudioSource에 클립을 연결해 수행한다.
- `Fullscreen`: 켜짐은 테두리 없는 전체화면(`FullScreenWindow`), 꺼짐은 창 모드(`Windowed`). 해상도·주사율·품질 설정은 변경하지 않는다. Editor의 Game View로는 실제 Windows 창 전환을 검증할 수 없으므로 빌드 검증 항목으로 남긴다.
- `Language`: 예제 버튼은 `en`/`ko`를 전환한다. Core는 공백·제어 문자가 없는 1~64자의 언어 식별자를 저장하므로 게임별 언어를 추가할 수 있다. 번역 테이블·글꼴·지원 언어 선택은 개별 게임에서 연결한다. 현재 예제 UI는 영어다.
- 방향키/WASD·게임패드로 버튼을 선택하고 Enter/A로 실행한다. 덮어쓰기 확인 중 Escape/B는 확인을 취소한다. 씬 전환·실패 중에는 버튼 입력을 차단한다.

기존 Input System 액션 에셋의 `UI` 맵과 `InputSystemUIInputModule`을 사용한다. 조작키 재설정·게임별 Player 맵 활성화·입력 감도는 이번 최소 UI 입력 범위에 포함하지 않는다.

## 수명과 실패 처리

`AppRoot`가 `IRuntimeSettings`를 소유한다. 기본 구현 `UnityRuntimeSettings`는 같은 화면 모드 요청을 반복하지 않으며, 루트 파괴나 초기화 실패 시 생성 당시 전역 음량·화면 모드로 복원한다. 이는 Play 반복 실행에서 이전 실행의 전역 설정을 남기지 않기 위한 처리다.

`SettingsService.Changed`는 현재 값이 확정된 뒤 발생한다. `AppRoot`는 Reload·백업 복구의 알림으로 시스템 설정을 적용하며 해제 시 구독을 제거한다. 화면 버튼의 설정 저장은 `AppRoot`가 검증 → 시스템 적용 → JSON 저장을 조정한다. 플랫폼 적용에서 예외가 나면 새 JSON을 쓰지 않고 앱을 실패 상태로 바꾸며 초기 시스템 설정을 복원한다. JSON 저장이 실패하면 이전 실행 설정으로 되돌린다. 이 보상 적용마저 실패하면 앱을 실패 상태로 전환한다.

테스트와 플랫폼별 구현은 `Begin` 전에 `ConfigureRuntimeSettings`로 대체 구현을 전달한다. 루트가 `Dispose`를 호출하므로 동일 인스턴스를 다른 루트와 공유하지 않는다. Core는 UI·Input System에 의존하지 않는다.

## 로딩과 입력

`AppRoot.IsTransitioning`과 `LoadingProgress`가 씬 전환 상태를 제공한다. 전환 시작을 같은 프레임에 `StateChanged`로 전달해 버튼을 잠근다. 진행률은 Unity 로드 작업의 값이며 로드 완료·씬 활성화까지 끝난 뒤에만 100%가 된다. 초기화 단계는 수치 대신 단계 문구로 표시한다.

`StarterLoadingOverlay`는 AppRoot 아래 단 하나의 Canvas로 생성되어 씬 전환 중에도 유지된다. 화면 위에 로딩 문구·진행률을 표시하고 포인터 입력을 막는다. 새 씬의 준비가 끝나면 숨기고 UI 선택을 복원한다. 실패 시에는 로딩 화면을 숨겨 기존 오류 안내를 볼 수 있게 한다.

`StarterScreen`은 활성화 시 구독하고 비활성화 시 해제한다. 첫 진입 또는 선택 항목 소실 시 유효한 버튼을 선택해 마우스 없이도 사용할 수 있게 한다. 런타임 Canvas는 루트와 함께 제거되며 씬·프리팹에 별도로 복제할 필요가 없다.

## 검증

2026-09-22, Unity 6000.3.16f1: **EditMode 44개 + PlayMode 25개 통과, 실패·건너뜀 0개**. 기존 설정·저장·Bootstrap 테스트를 포함한다. 열려 있는 원본 Editor를 유지하기 위해 최신 소스를 반영한 `Logs/Stage34Validation` 복제 프로젝트에서 실행했다. 결과는 `Logs/Stage5EditMode.xml`, `Logs/Stage5PlayModeFinal.xml`이다.

- 음량·화면 매핑, 잘못된 설정 보호, 비동기 화면 전환의 중복 요청 방지, 적용 실패 복구·Dispose
- 저장 설정의 Ready 이전 적용, 저장 성공·실패, 직접 Reload·백업 복구, 구독 해제
- 플랫폼 적용과 정리 동시 실패 시 원래 오류 유지·게임 진입 차단
- 전환 시작 즉시 버튼 잠금, 로딩 화면 단일성·진행률 범위, 이미 Title인 초기화와 중복 루트 정리
- 실제 Input System 이벤트를 통한 키보드 Enter·게임패드 South 입력, Escape 덮어쓰기 취소·UI 선택 복원

자동 테스트는 임시 저장 폴더와 대체 시스템 설정 구현을 사용해 실제 사용자 저장·창 설정과 분리한다. 음량·화면 매핑과 전역 상태 복구는 설정 적용 경계의 단위 테스트로 검증한다.

최초 PlayMode 실행에서 포커스가 없는 batchmode의 키보드 이벤트가 Editor 쪽으로 전달되어 2개 테스트가 실패했다. 설치된 Input System의 자체 테스트 방식에 맞춰 테스트 동안만 입력 라우팅·백그라운드 처리를 조정하고 종료 시 원복했다. 버튼 함수를 직접 호출하는 우회 없이 입력 이벤트 경로를 유지한 최종 실행에서 전부 통과했다. 프로젝트의 일반 입력 설정은 변경하지 않았다.

Windows 플레이어의 전체화면 전환, 실제 오디오 장치 청취, 물리 게임패드 조작과 앱 프로세스 재실행은 로드맵 7단계의 실제 실행 검증에 남긴다.

2026-09-23 설계 점검 후 설정 저장의 플랫폼 적용 순서를 보완했다. 격리 복제본에서 EditMode 46개·PlayMode 27개 통과, Windows 개발 빌드와 별도 프로세스 저장·재실행 이어하기 통과. 상세 책임 경계와 미검증 범위는 [SOLID·패턴 점검](ARCHITECTURE_REVIEW.md)과 [통합 검증 기록](INTEGRATION_VALIDATION.md)을 따른다.

## API 참고

- [Unity AudioListener.volume](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioListener-volume.html)
- [Unity Screen.fullScreenMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-fullScreenMode.html)
- [Unity AsyncOperation.progress](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-progress.html)
