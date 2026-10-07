# 과거 기록 — BOOTSTRAP

> 과거 기록입니다. 당시 제안·대기 상태·검증 결과를 보존하며 현재 사양은 [문서 안내](../INDEX.md)를 따릅니다.

## 검증

2026-09-09: Unity 6000.3.16f1에서 **PlayMode 10개 + EditMode 3개 통과 / 실패·건너뜀 0개**.

- Boot → Title → Main → Title 및 실제 UI 클릭 핸들러 연결
- 중복 전환 요청 거절, 씬 이동 시 AppRoot 유지와 단일 UI·EventSystem 확인
- Boot 재진입 시 준비된 AppRoot 재사용
- AppConfig 누락 시 실패 처리 및 다음 씬 진입 차단
- 빌드 목록에 없는 Main 씬 설정 실패
- 중복 AppRoot의 시작 차단 및 초기화 중 파괴·취소
- Boot 미경유 Main 실행 시 버튼 차단·안내
- Boot 실패 메시지 표시, 중복 씬 역할과 Boot 순서 오류 검사
- 검증 이후 SO 변경에도 실행 경로 보존
- 같은 화면에서 루트 생성·실패·제거 및 화면 재활성화 반영
- 미저장 씬 보호, 누락 입력 모듈 보정, 비활성 EventSystem 재사용·반복 호출

당시 테스트 결과는 `Logs/StarterPlayModeTests.xml`, `Logs/StarterEditModeTests.xml`에 기록했다. 그래픽 출력 없는 batchmode로 진행했고 Title/Main의 당시 화면 배치는 2026-09-07에 확인했다. 이후 2026-09-23에 예제 UI를 1920×1080 디자인 좌표로 옮겼으며 현재 기준은 아래의 UI 절을 따른다.

2026-09-09 Windows x64 개발 빌드 생성 성공: `Builds/Windows/StarterProject.exe`. 실제 플레이 흐름은 Editor PlayMode에서 검증했으며, 배포 빌드의 장기 실행·다양한 기기 검증은 로드맵 7단계다.

Unity의 `Window > General > Test Runner`에서 PlayMode의 `StarterProject.PlayModeTests`와 EditMode의 `StarterProject.EditModeTests`를 실행한다. EditMode 씬 테스트는 미저장 씬이 있으면 건너뛰므로 먼저 저장한다.
Windows 개발 빌드는 `Tools > Starter Project > Build Windows Preview`에서 만들며 결과는 `Builds/Windows`에 생성된다.

## 현재 범위

JSON 설정·게임 저장 기반은 3·4단계에서 추가했다. 상세 검증·사용법은 [설정·게임 저장 가이드](../SETTINGS_AND_SAVE.md)를 따른다. 5단계 음량·화면 적용과 입력·로딩 연결은 [공통 기능 가이드](../COMMON_SERVICES.md)에 기록한다. 실제 게임 콘텐츠는 개별 게임에서 추가한다.
Main 직접 Play의 개발용 Boot 경유를 6단계에서 구현하고 복제본 자동 실행으로 확인했다. Title 직접 Play는 Boot 실행을 안내한다. 검증 범위와 테스트 데이터 경로는 [Editor 작업 가이드](../EDITOR_WORKFLOW.md)를 따른다.
폴더·씬 이름과 GUID는 유지하며, 기존 제품명·Cloud 연결 정리는 8단계에 남긴다.

## 2026-09-09 코드 검토·보완

- UI가 초기화·실패 상태에서 매 프레임 동일한 문자열을 생성하던 처리를 상태 변경 시에만 실행하도록 수정했다. 프레임당 상태 비교는 유지하며, 성능 향상률을 별도로 측정한 것은 아니다.
- AppConfig 검증과 실행 경로 복사를 같은 프레임에 수행한다. 검증 직후 SO가 수정되더라도 실행에는 검증한 경로를 사용한다.
- 예제 생성 도구는 Play 중이거나 미저장 씬이 있으면 수정을 시작하지 않는다. 필수 UI 액션과 씬 경로도 먼저 검사한다.
- 기존 EventSystem에 Input System UI 모듈이 없으면 추가하고, 비활성 상태를 보정하며 기존 다른 입력 모듈은 비활성화한다. 반복 호출 시 중복 생성하지 않는다.
- 씬을 여는 동안 설정 에셋이 해제될 수 있으므로 경로는 미리 복사하고 에셋 참조는 씬을 연 뒤 다시 가져온다.
- 자동 테스트의 스크린샷 저장을 제거해 그래픽 장치·이미지 파일 생성에 의존하지 않도록 했다. 화면 배치는 필요할 때 별도로 확인한다.

`CreateExampleAssets`는 기존 예제 씬을 생성·보정하는 도구다. 사용 전 씬을 저장해야 하며, 기존 화면의 입력·Bootstrap 연결과 설정·저장 예제 버튼 배치를 수정할 수 있다. 새 프로젝트에서 일반적인 Play를 시작할 때마다 호출할 필요는 없다.

## 2026-09-23 Boot 씬 구성 재검증

`Validate Setup`에 Boot의 활성 `StarterScreen`·Canvas·상태 Text와 단일 활성 EventSystem·UI 액션 검사를 추가했다. 누락된 AppBootstrap 참조뿐 아니라 준비·오류 화면 자체가 표시되지 않는 구성도 편집 시점에 발견한다. Unity 6000.3.16f1 격리 복제본에서 EditMode 46개·PlayMode 27개 전체 통과. 실제 GUI 화면의 시각적 확인은 7단계에 남겨 두며, 현재 상태·향후 새 게임 인계 기준은 [프리셋 상세 Summary](../PRESET_SUMMARY.md)에 기록한다.
같은 Boot 씬과 Core로 Windows x64 Development 빌드를 만들고 검증용 새 저장 경로에서 서로 다른 Player 프로세스의 첫 저장·재실행 이어하기도 확인했다. 숨겨진 그래픽 Player의 캡처는 검은 화면이라 시각 검증으로 사용하지 않는다.

## 가로형 PC·모바일 UI

Boot·Title·Main과 로딩 오버레이의 Canvas는 `Screen Space - Overlay`, `Scale With Screen Size`, 기준 1920×1080, 가로·세로 Match 0.5를 사용한다. 기존 1280×720 예제 배치는 1920 기준으로 비례 환산한다. 화면 전체 배경은 가장자리까지 채우고 조작 콘텐츠는 안전 영역 안에 맞춘다. 모바일은 가로 좌·우 방향만 허용한다. 적용 범위·기기 검증 한계는 [반응형 UI 가이드](../RESPONSIVE_UI.md)에 기록한다.
