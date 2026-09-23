# Starter Project — 템플릿 완성·복제 체크리스트

최종 수정: 2026-09-23 · 배포 준비: 미완료

## 배포 형태 제안

이 프로젝트에서 말하는 프리셋은 에셋뿐 아니라 패키지·프로젝트 설정·문서까지 포함하는 재사용 프로젝트다.
첫 배포 방식은 GitHub Template Repository를 추천한다. Unity Hub용 커스텀 템플릿 패키징은 사용 흐름이 안정된 뒤 별도로 검토한다.
GitHub에서는 저장소 설정의 Template repository 옵션으로 템플릿을 지정할 수 있다. [GitHub 공식 문서](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-template-repository)

원격 저장소의 Template 지정 여부는 배포 단계에서 확인한다. 현재 단계의 로컬 커밋과 최종 템플릿 배포는 구분한다.

## 현재 확인 완료

- [x] Assets / Packages / ProjectSettings 구조 확인
- [x] Unity 6000.3.16f1 및 URP 2D / Input System 패키지 확인
- [x] .gitignore의 Library / Temp / Logs / UserSettings / IDE 생성 파일 제외 규칙 확인
- [x] 에셋 직렬화 설정이 Force Text인 상태 확인
- [x] 오프라인 공통 기반 및 설계 우선 진행 방향 확인
- [x] 초기화·데이터 기준 초안 작성

위 항목은 파일·방향 확인이며 실행 테스트를 의미하지 않는다.

## 프리셋 구현 완료 조건

- [x] SO·JSON 분담, Boot 흐름, 첫 지원 플랫폼 확정
- [x] Boot → 준비 완료 → Title → 게임 진입 동작
- [x] Boot/Title/Main 씬이 필요한 순서로 빌드 목록에 포함
- [x] 설정 파일이 없는 첫 실행에서 기본값으로 정상 진입
- [x] 사용자 설정 저장 후 앱 재실행 시 복원 (Windows 검증 빌드의 별도 프로세스)
- [x] JSON 파손·필드 누락·범위 오류에 대한 정책 동작
- [x] 이전 저장 형식 변환 및 미래 버전 덮어쓰기 방지
- [ ] 저장 실패·중단 시 기존 파일 또는 백업 보존
- [x] 필수 참조 누락 시 원인을 표시하고 게임 진입 중단
- [x] Boot 중복 호출·씬 전환 연타에도 공통 객체와 구독 중복 없음
- [x] Play 종료·재시작 시 이전 실행 상태가 남지 않음 (복제본 자동 실행)
- [x] Main 씬에서 직접 Play하는 개발 경로 검증 (Boot 경유·세션 격리 자동 실행)
- [x] 런타임 코드에 UnityEditor 의존성 없음
- [x] 초기화·저장 실패 경로를 다루는 필요한 자동 테스트 통과
- [x] 확정된 음량·화면 설정의 시스템 적용과 저장 실패 시 기존 적용 값 보호
- [x] 씬 전환 중 로딩 표시·입력 차단과 키보드·게임패드 UI 연결 구현
- [x] Boot·Title·Main·로딩 예제 UI의 가로형 화면 안전 영역 맞춤 구현 (합성 해상도 자동 검증)
- [ ] 실제 모바일 가로 화면의 안전 영역·가독성·터치 조작 검증
- [x] 첫 지원 플랫폼의 실제 빌드에서 실행·저장·복원 확인 (Windows 자동 실행; GUI 현장 확인 대기)

## 템플릿 공개·버전 지정 전

- [x] Product Name의 기존 Project_DE 이름을 `StarterProject`로 변경
- [ ] 추적 중인 Unity Cloud 프로젝트·조직 ID는 제거함. Unity Hub/Editor Services UI에서 연결 해제 상태 최종 확인
- [x] 회사명은 `StarterTemplate`, Standalone 앱 ID는 `com.startertemplate.starterproject`로 정리. 새 게임에서는 첫 실행 전 둘 다 교체
- [x] 공통 Core/UI/Editor의 `StarterProject.*` 어셈블리·네임스페이스는 유지. 새 게임의 Gameplay 코드는 게임 고유 네임스페이스에 둬 일괄 이름 변경으로 참조를 깨지 않음
- [x] 추적 중인 Assets/ProjectSettings에서 기존 게임 전용 이름·아이콘·콘텐츠·서버 주소 잔존 여부 점검. 게임 전용 이미지·오디오·프리팹·애니메이션 파일은 없음
- [x] 협업·멀티플레이 센터·Visual Scripting·2D 도구 등의 유지 필요성 검토
- [x] 현재 코드·씬에 참조가 없는 협업, 멀티플레이 센터, Visual Scripting 패키지를 복제본 테스트·빌드 후 제거. 2D 애니메이션·스프라이트 도구는 미래 게임 에셋 작업을 위해 유지
- [x] 추적 중인 Assets와 .meta의 쌍 검사. 새 저장 정책 스크립트의 .meta도 포함; 기존 씬·설정 에셋 연결은 깨끗한 복제본의 Unity 자동 테스트·빌드에서 확인. 이번 변경의 회귀 검증은 아래에 별도 보류
- [x] 실제 Git 목록 151개에서 Library/Temp/Logs/UserSettings/Builds/IDE 생성 파일이 추적되지 않음을 검사 (새 파일 추가 전 기준)
- [x] `3460339`의 새 Git 복제본에서 패키지 첫 복원·Unity 자동 실행·EditMode 46개·PlayMode 29개·Windows 개발 빌드 검증. **GUI 화면 직접 Play는 별도 미완료**
- [x] README의 실제 사용 절차와 지원 플랫폼 갱신; [새 게임 시작 가이드](NEW_GAME_SETUP.md)에 복제·게임 데이터 연결 절차 기록
- [x] [변경 이력](CHANGELOG.md)과 Unity 버전·알려진 제한 기록. 배포 태그는 아직 없음
- [ ] 새 `GamePayloadPolicy` 연결·Product Name 기반 빌드명의 Unity 회귀 검증 및 깨끗한 복제본 확인
- [ ] 검증이 끝난 상태를 커밋·푸시하고 템플릿 지정 및 버전 태그 생성

2026-09-10: EditMode 30개·PlayMode 14개 통과. 서비스·AppRoot 재생성 후 설정·게임 저장 복원과 쓰기 실패 시 기존 파일 보호를 확인했다. 전체 앱 프로세스 재실행·강제 종료 내구성·출시 검증 항목은 7단계에서 완료 처리한다. 상세 범위는 [3·4단계 가이드](SETTINGS_AND_SAVE.md)를 따른다.

2026-09-22: 5단계 시스템 설정·입력·로딩 기능을 추가했고 EditMode 44개·PlayMode 25개가 통과했다. 검증 결과와 범위는 [공통 기능 가이드](COMMON_SERVICES.md)를 따른다. 실제 Windows 창 전환·청취·물리 입력 장치 검증은 7단계에 남긴다.

2026-09-23: 범용 프리셋 검토 후 6단계 Editor 개발 도구를 구현했다. Unity 복제본에서 EditMode 46개·PlayMode 26개가 통과했다. Main 직접 Play 두 번씩을 도메인 재로드 켜짐·꺼짐에서 자동 실행해 별도 저장소·시작 씬 복원·일반 저장 보호를 확인했고, 재로드 꺼짐에서는 미저장 씬도 유지됐다. 자동 검증 복제본에서만 batchmode 차단 조건을 해제했다. 실제 GUI 조작과 테스트 데이터 메뉴 대화상자 확인은 7단계에서 수행한다. [Editor 작업 가이드](EDITOR_WORKFLOW.md)

2026-09-23: Windows x64 개발 빌드에서 별도 프로세스 재실행 뒤 설정과 게임 진행 복원·이어하기를 확인했다. 파손 설정·미래 버전 저장의 보호와 손상 원본을 보존하는 명시적 백업 복구도 검증했다. 저장 완료 직후 강제 종료, 임시 파일 flush 후 주 파일 교체 전 강제 종료에서도 이전 정상 저장이 복원됐다. 후자는 계측 복제본의 한 중단 시점이며 미완료 `.tmp`를 남긴다. 실제 GUI 화면·오디오·물리 입력 장치와 다른 중단 시점은 미검증이다. [통합·빌드 검증 기록](INTEGRATION_VALIDATION.md)

2026-09-23: SOLID 책임 경계 검토 후 설정 저장의 시스템 적용·JSON 기록 순서를 보완했다. 적용 실패는 새 파일을 남기지 않고, 저장 실패는 이전 실행 설정으로 되돌린다. EditMode 46개·PlayMode 27개와 Windows 빌드의 별도 프로세스 재실행을 확인했다. [설계 점검](ARCHITECTURE_REVIEW.md)

2026-09-23: PC와 가로형 모바일 예제 UI의 Canvas Render Mode·Scaler를 확인하고 콘텐츠를 안전 영역에 맞추도록 보완했다. PlayMode의 합성 화면 값 검증은 EditMode 46개·PlayMode 29개에 포함된다. 모바일 실기기 검증과 게임별 UI는 이후 단계다. [반응형 UI 기준](RESPONSIVE_UI.md)

2026-09-23: `966f33f`의 새 Git 복제본에서 Unity 6000.3.16f1의 패키지·에셋 첫 가져오기, EditMode 46개·PlayMode 29개와 Windows 개발 빌드를 확인했다. 이어 검증 복제본에서 미사용 패키지 3개를 제거하고 템플릿 회사명·제품명·앱 ID 및 로컬 Cloud 식별자를 정리한 뒤 같은 테스트·빌드를 다시 통과했다. 원본 Editor의 `Validate Setup` 메뉴도 실제 GUI 경로로 실행해 로그의 통과 메시지를 확인했다. 이 시점에는 변경 후 새 최종 복제본 검증과 Main 직접 Play·메뉴 대화상자·화면/입력·모바일 실기기·Unity Hub의 Cloud 연결 표시 확인이 남아 있었다. [통합 검증](INTEGRATION_VALIDATION.md)

2026-09-23 최종 복제 재검증: 정리 내용을 담은 `3460339`를 새 폴더로 다시 복제해, 비어 있는 Library에서 Unity가 패키지를 복원한 뒤 EditMode 46개·PlayMode 29개와 Windows 개발 빌드(169,600,344 bytes)가 통과했다. 추적 중인 Cloud ID는 비어 있고 제거한 선택 패키지는 다시 설치되지 않았다. GUI 창·장치 품질과 Unity Hub 연결 표시를 확인해야 배포 완료로 판정한다.

2026-09-23 배포 준비 구현: 새 게임이 Core를 수정하지 않고 초기 JSON·payload 버전·검증 규칙을 연결하도록 `GamePayloadPolicy` 확장 지점을 추가했다. Main의 `TrySaveGame(payloadJson)`으로 실제 게임 상태를 전달한다. Windows Preview 파일명은 Product Name을 따른다. 에셋·meta 쌍과 추적 생성 파일, 게임 전용 잔존물 목록을 정적으로 점검하고 새 게임 시작 가이드·변경 이력을 작성했다. **이 새 코드의 Unity 회귀 검증은 제작 작업을 마친 뒤 진행한다.** GUI·실기기 검증은 사용자 요청에 따라 미룬다.

검증 착수 기록: Unity 명령줄 EditMode 테스트가 테스트 시작 전 Licensing Client IPC 연결 실패와 `com.unity.editor.headless` 라이선스 오류로 진행하지 못해 중단했다. 복제본의 생성된 C# 프로젝트로 Core 단독 `dotnet build`는 오류 0개로 통과했지만 Unity 패키지 참조 경고가 있어 Unity Test Runner 결과를 대체하지 않는다. 라이선스 연결이 정상화되면 회귀 테스트·빌드를 한 번에 재개한다. [통합 검증 기록](INTEGRATION_VALIDATION.md)

## 새 게임을 만들 때

1. 검증된 템플릿 버전에서 새 저장소를 만든다.
2. 새 프로젝트 폴더를 Unity Hub에 추가하고 문서에 명시된 Unity 버전으로 연다.
3. **첫 Play·빌드 전에** Product Name, 회사명, 앱 식별자, 아이콘을 새 게임에 맞춘다. 게임별 코드의 네임스페이스를 정하되 공통 `StarterProject.*` 어셈블리는 유지한다. 기본 제품 식별자를 공유한 채 실행하면 다른 복제 게임과 저장 경로가 겹칠 수 있다.
4. 필요한 경우 새 Unity Cloud 프로젝트를 연결한다.
5. 이전 게임과 저장 경로가 겹치지 않는지 실제 경로를 확인한다.
6. 플랫폼, 화면 방향·해상도, 입력 장치, 렌더러, 품질 설정을 선택한다.
7. 기본 SO, 시작 씬과 게임별 초기화·저장 데이터 모델을 구성한다.
8. 첫 실행·저장·재실행·빌드를 확인하고 새 프로젝트의 기준 커밋을 남긴다.

## 이후 유지보수

공통 기반 수정은 Starter Project에서 검증한 후 버전을 남긴다. 새 게임에는 사용한 템플릿 버전을 기록한다.
템플릿에서 만들어진 기존 게임에 이후 변경이 자동 적용된다고 가정하지 않는다. 필요한 수정만 게임별 영향과 저장 호환성을 확인해 반영한다.
여러 게임에서 같은 코드를 반복해서 동기화하게 될 때 공통 기능의 UPM 패키지 분리를 검토한다.
