# 변경 이력

## Unreleased — 2026-09-23

- 범용 Boot → Title → Main, 설정·단일 슬롯 JSON 저장·복구, 입력·로딩, Editor Main 직접 Play와 1920×1080 기준 가로형 반응형 예제 UI를 구성했다.
- 게임별 `GamePayloadPolicy`를 Boot에 선택적으로 연결해 초기 JSON·payload 버전·검증 규칙을 Core 수정 없이 지정할 수 있게 했다. Main에서는 `AppRoot.TrySaveGame(payloadJson)`으로 실제 게임 상태를 전달한다.
- Windows Preview 실행 파일명이 복제한 게임의 Product Name을 따르도록 바꾸고, 새 게임 설정 절차와 템플릿 인계 범위를 문서화했다.
- 게임/조직 Cloud ID와 불필요한 선택 패키지를 제거하고 기본 Company/Product/Application Identifier를 중립 값으로 정리했다.
- 2026-09-24: 저장 정책 PlayMode 테스트의 문자열 구문 오류를 고쳤다. Unity 6000.3.16f1의 최신 Git 복제본에서 EditMode 50개·PlayMode 30개가 모두 통과했고 Windows 개발 빌드가 성공했다. 검증 복제본의 Product Name 변경 후 해당 이름의 실행 파일도 생성됐다.

이 항목은 **배포 태그가 아니다**. 남은 GUI·모바일·장치 확인 및 원격 템플릿 설정을 마친 뒤 버전을 확정한다. 이전 단계별 작업 날짜와 검증 범위는 [로드맵](ROADMAP.md)과 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 있다.
