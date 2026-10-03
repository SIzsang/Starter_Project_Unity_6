# 변경 이력

## 미배포 보완 — 2026-10-04

- 활성 게임 세션의 저장을 Main 이외의 Gameplay 씬에서도 허용했다. Boot·Title·전환 중 저장 차단과 기존 저장 보호는 유지한다.
- PC 창의 크기 조절을 허용하고 JSON 코드를 위한 어셈블리 참조 안내를 추가했다.
- Validate Setup이 Boot뿐 아니라 Title·Main의 단일 EventSystem과 필수 포인터·선택 액션을 검사하도록 보완했다.
- Boot의 비활성·중복 AppBootstrap을 설정 검사에서 거부해 초기화가 시작되지 않는 구성을 발견할 수 있게 했다.
- 캐릭터·진행 같은 게임별 데이터 검사는 제외하고 공통 저장·초기화·UI 입력 경계를 점검했다. 검증 결과는 [제작 시작 전 점검](GAME_START_AUDIT.md)에 기록한다.
- 이번 보완은 최신 main의 소스이며 기존 v1.0.0 태그·ZIP을 변경하지 않는다. [제작 시작 전 점검](GAME_START_AUDIT.md)

## v1.0.0 — 2026-10-02

- 범용 Boot → Title → Main, 설정·단일 슬롯 JSON 저장·복구, 입력·로딩, Editor Main 직접 Play와 1920×1080 기준 가로형 반응형 예제 UI를 구성했다.
- 게임별 `GamePayloadPolicy`를 Boot에 선택적으로 연결해 초기 JSON·payload 버전·검증 규칙을 Core 수정 없이 지정할 수 있게 했다. Main에서는 `AppRoot.TrySaveGame(payloadJson)`으로 실제 게임 상태를 전달한다.
- Windows Preview 실행 파일명이 복제한 게임의 Product Name을 따르도록 바꾸고, 새 게임 설정 절차와 템플릿 인계 범위를 문서화했다.
- 게임/조직 Cloud ID와 불필요한 선택 패키지를 제거하고 기본 Company/Product/Application Identifier를 중립 값으로 정리했다.
- 2026-09-24: 저장 정책 PlayMode 테스트의 문자열 구문 오류를 고쳤다. Unity 6000.3.16f1의 최신 Git 복제본에서 EditMode 50개·PlayMode 30개가 모두 통과했고 Windows 개발 빌드가 성공했다. 검증 복제본의 Product Name 변경 후 해당 이름의 실행 파일도 생성됐다.
- 2026-10-02: 예제 씬의 `Image`에 게임 스프라이트를 연결하는 절차를 명시하고, Boot `StarterScreen`에 선택형 로딩 UI 프리팹 슬롯을 추가했다. 사용자 프리팹에도 전환 중 입력 차단을 자동 적용한다. PlayMode 31개와 Windows 개발 빌드가 통과했고, 입력 차단 보완 후 관련 테스트 1개를 다시 통과했다.

첫 배포 버전은 **v1.0.0**이다. 기본 브랜치 main과 GitHub Template, 버전 태그의 소스 ZIP을 제공한다. 필수 검증 결과와 모바일·물리 장치의 미검증 범위는 [배포 가이드](RELEASE.md)에 기록한다. 이전 단계별 작업 날짜와 검증 범위는 [로드맵](ROADMAP.md)과 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 있다.
