# 범용성·사용성 재검토 — 2026-10-04

> **검토 기록:** 2026-10-04 당시의 재사용성 보완입니다. 현재 사용 절차는 [새 게임 가이드](NEW_GAME_SETUP.md), 최신 결과는 [검증 현황](INTEGRATION_VALIDATION.md)을 따릅니다.

검토 기준은 SOL 6.1 작업이 반영된 `8da41b6`이다. 범용 스타터의 초기화·저장·UI 교체·편집기 작업 흐름만 검토하며 캐릭터 능력치, 스테이지 진행, 전투 등 게임별 구현은 제외했다.

## 결론과 보완

Core가 게임 데이터나 예제 UI에 의존하지 않는 방향과 추가 씬 저장·PC 창 크기 조절 보완은 적절하다. 다만 다른 게임에서 UI를 교체하거나 이벤트로 상태를 표시할 때 드러나는 네 가지 문제를 발견해 수정했다.

| 문제 | 실제 사용 시 영향 | 수정 |
| --- | --- | --- |
| 공통 검사에 예제 UI 강제 | StarterScreen·기본 UI를 교체하면 Main 직접 Play와 Build Windows Preview가 실패 | Validate Setup은 AppConfig·빌드 씬·활성 Boot 진입점만 검사. 예제 화면은 별도 Validate Example UI 메뉴로 검사 |
| 저장 결과 알림 누락 | StateChanged만 구독하는 사용자 UI가 저장 성공·실패 또는 이어하기 실패 결과를 표시하지 못함 | 결과 상태와 StorageMessage를 확정한 뒤 알림. 성공한 이어하기는 기존 씬 전환 알림 사용 |
| 진행률 Image의 앵커 가정 | 일반 Image를 연결하면 0%에서도 폭이 남고, Filled Image는 채움 값이 바뀌지 않음 | 일반 막대는 Track 자식으로 왼쪽부터 채우도록 앵커·오프셋 정규화. Filled는 작성한 배치·방향을 유지하고 fillAmount 갱신 |
| 전체 화면 Panel의 안전 영역 누락 | Panel 안에 묶은 버튼·문구가 배경으로 분류돼 노치 영역을 침범 | 자식이 있는 컨테이너와 Selectable을 콘텐츠로 이동. 장식 자식이 있는 배경은 Full Screen Roots로 명시 |

기본 UI는 기존 1920×1080·Overlay·가로 화면 설정을 사용한다. 사용자 정의 UI의 Render Mode·기준 해상도·입력 구현은 공통 초기화 검사의 조건이 아니다. 예제 UI를 계속 사용하는 경우에만 해당 예제의 화면·입력 계약을 별도로 검사한다.

## 새 프로젝트에서 사용하는 방법

1. `Tools > Starter Project > Validate Setup`: 모든 프로젝트에서 공통 초기화 구성을 확인한다. Main 직접 Play와 Windows Preview도 이 검사를 사용한다.
2. `Tools > Starter Project > Validate Example UI`: 기본 StarterScreen을 사용하는 동안 화면과 Input System UI 연결을 확인한다. 사용자 UI로 전면 교체한 뒤에는 해당 UI에 맞는 검사·연결을 사용한다.
3. 저장 결과 표시 UI는 `AppRoot.StateChanged`를 구독하고 `StorageMessage`·`Game`의 확정된 상태를 읽는다. 구독한 컴포넌트가 비활성화되거나 파괴될 때 구독을 해제한다.
4. 로딩 프리팹의 일반 Progress Fill은 Track 아래에 둔다. 가로 폭은 진행률에 따라 자동 조절된다. 원형·세로·다른 방향의 표시는 Image Type을 Filled로 지정하고 Fill Method·Origin을 설정한다.
5. 자식이 있는 전체 화면 배경을 유지하려면 Canvas에 `StarterCanvasLayout`을 미리 추가하고 `Full Screen Roots`에 해당 Canvas 직속 배경을 연결한다. 조작 Panel은 지정하지 않는다. 실행 중 추가되는 조작 UI는 `ContentRoot` 아래에 둔다.

회사명·제품명·앱 식별자 변경, AppConfig·씬 경로 갱신, 새 어셈블리의 명시적 참조는 여전히 새 프로젝트의 기본 설정이다. 단일 저장 슬롯, Main 중심 개발용 진입, uGUI 예제·새 Input System은 현재 제공 범위이며 모든 장르의 완성 프레임워크를 의미하지 않는다.

## 검증 기록

Unity 6000.3.16f1에서 관련 검사만 실행했다.

- **EditMode 18개 항목 통과:** 첫 실행은 17개 통과·1개 실패였다. 임시 Boot 씬을 생성할 때 NewScene이 테스트 AppConfig 참조를 해제한 것이 원인이어서 에셋을 다시 불러오도록 테스트 준비를 수정했다. 실패한 항목만 재실행해 1/1 통과했고 건너뜀은 없었다. 예제 UI가 없는 씬의 공통 검사 통과, 별도 예제 검사와 기존 초기화·입력 검사 보호를 확인했다.
- **PlayMode 27/27 통과:** RuntimeFlowTests 17개와 StarterCanvasLayoutTests 10개를 실행했다. 저장 성공·실패·이어하기 실패의 확정된 상태 알림, 일반·Filled 진행률 0/50/100%, 중첩 Panel의 안전 영역과 명시적 배경 보존, 기존 저장·로딩·입력 흐름을 확인했다. 실패·건너뜀 0개다.
- 결과: `Logs/ReuseReviewEditMode.xml`(최초 실행), `Logs/ReuseReviewEditModeFinal.xml`(실패 항목 재검사), `Logs/ReuseReviewPlayMode.xml`. 각 XML과 같은 이름의 `.log`에 실행 로그가 있다.

검사에서 만든 임시 씬·설정 에셋은 정리하고 기존 빌드 씬 목록을 복원했다. 테스트 저장소는 사용자 저장과 분리했다. 이전 전체 테스트 수와 이번 선택 검사 수를 합산하지 않는다.

새 Windows Player 빌드·Computer Use·모바일 실기기 검사는 이번 범위에 추가하지 않는다. 원본 프로젝트의 개인 Cloud 연결 변경은 커밋에서 제외한다. 보완 소스는 최신 main에 반영하며 기존 v1.0.0 태그·ZIP은 변경하지 않는다.
