# Editor 개발 워크플로

일반 실행, Main 직접 Play, 개발 저장소와 메뉴의 사용 순서입니다. 상세 옵션은 [개발 지원 사용법](PHASE_3_DEVELOPMENT.md)에 둡니다.

## 실행 경로 선택

| 상황 | 경로 | 저장소 |
| --- | --- | --- |
| 사용자 흐름 확인 | Boot에서 Play → Title → 슬롯 선택 | Application.persistentDataPath/StarterData |
| 게임 씬 반복 개발 | 설정된 Main에서 직접 Play → Boot 경유 → 새 Main 세션 | Library/StarterProject/PlaySessions/<세션 ID> |
| Title만 직접 Play | Boot 실행 안내 | 일반 Boot 경로 사용 |

Main 직접 Play는 공통 초기화를 우회하지 않습니다. 첫 설정과 빈 세션을 격리 저장소에서 준비하고 Play 종료 시 Editor 시작 씬을 복구합니다. 추가 Gameplay 씬이 자동으로 같은 개발 진입을 제공한다고 가정하지 않습니다.

## 메뉴 찾기

모든 메뉴는 `Tools > Starter Project` 아래에 있습니다.

| 메뉴 | 용도 |
| --- | --- |
| Validate Setup | 설정·활성 씬·SceneRoot·입력·저장 정책·빌드 심볼 검사 |
| Validate Example UI | 기본 예제 화면 구성의 선택 검사 |
| Open Test Data Folder | 개발용 테스트 경로 열기 |
| Reset Test Data | 확인 후 개발 데이터 백업·초기화 |
| Debug Menu | 상태·서비스·로그 조회와 기존 앱 명령 |
| Build Windows Preview | Development / Builds/Windows |
| Build Windows QA | QA / Builds/Windows-QA |
| Build Windows Release | Release / Builds/Windows-Release |

## 검사와 편집 내용 보존

Validate Setup은 저장된 씬을 Preview Scene으로 확인하고 닫습니다. 열린 씬과 dirty 상태는 보존하며, 실제 편집 결과를 확인하려면 먼저 저장합니다. 선택형 DataCatalog의 ID·참조 검증도 포함합니다.

사용자 UI에 예제 Canvas를 강제하지 않습니다. GamePayloadPolicy의 초기 payload 생성/검증 훅이나 실제 저장 읽기·쓰기는 실행하지 않습니다. 일반 Build Pipeline에도 pre-build 검사를 연결합니다.

## Debug·로그·빌드 사용

Debug 명령은 기존 AppRoot의 준비·전환·슬롯 정책을 통과합니다. Save는 현재 저장소에 쓰므로 개발 실험은 격리된 Main 직접 Play에서 합니다. 창 닫기·씬 종료·앱 교체 시 Debug 입력 범위를 해제합니다.

빌드 환경은 Player 전용 심볼로 전달합니다. 예약 심볼을 전역 Player Settings에 넣지 않습니다. Play 중이거나 미저장 씬이 있으면 빌드를 시작하지 않습니다. [로그·옵션·Smoke API](PHASE_3_DEVELOPMENT.md)

## 확인 범위

Main 직접 Play의 Boot 경유·저장 격리·반복 진입은 기존 자동 실행에서 확인했습니다. GUI 직접 조작, Reset Test Data의 대화상자, 실제 입력·Audio와 새 Player의 확인은 적용 환경에 맞춰 진행합니다.

검증을 매 작업의 기본 절차로 반복하지 않습니다. [최신 검증](INTEGRATION_VALIDATION.md) · [이전 개발 도구 기록](Archive/EDITOR_WORKFLOW_HISTORY.md)
