# Starter Project 상세 Summary — 새 게임 제작용 인계 기록

최종 점검: 2026-10-04 · 상태: **v1.0.0 배포 완료, 최신 main의 범용성·사용성·설계 계약 보완**

이 문서는 향후 새 프로젝트에서 이 프리셋을 사용할 때 다시 확인할 기준 기록이다. 대화나 모델의 임시 기억에 의존하지 않도록 프로젝트와 함께 버전 관리한다. 변경 후에는 검증 결과와 한계를 함께 갱신한다.

## 목표와 범위

- **목표:** 새 게임에서 초기화·설정·저장·씬 흐름을 다시 만들지 않고 게임별 콘텐츠와 규칙을 개발할 수 있는 Unity 스타터를 제공한다.
- **첫 검증 환경:** Unity 6000.3.16f1, URP 2D 17.3.0, uGUI, Input System 1.19.0, Windows PC. 현재 렌더링·화면 구성은 2D 시작점이며 모든 플랫폼·장르의 완성품은 아니다.
- **공통과 게임별 경계:** Boot, 설정, 파일 저장, 기본 씬 전환, 개발용 진입은 공통이다. 전투·인벤토리·캐릭터 성장·스테이지 규칙·사망·자동 저장 시점은 새 게임의 `Gameplay`가 정의한다.
- **현재 완료 판정:** 사용자 요청에 따라 새 게임 제작의 일반적인 오류를 막는 검증으로 범위를 정리했다. 구현·필수 회귀와 Windows Player 시작·저장·이어하기·1920×1080 표시 확인은 완료했다. v1.0.0 태그·소스 ZIP·GitHub Template으로 배포한다. 게임별 기기 품질·물리 입력은 별도 확인 범위다. [배포 기준](RELEASE.md)
- **예제 UI 범위:** PC와 가로형 모바일을 대상으로 Screen Space Overlay·1920×1080 Canvas Scaler와 안전 영역 맞춤을 사용한다. 기존 1280×720 예제 배치는 비례 환산한다. 배경·버튼 `Image`에 게임 스프라이트를 지정하고 Boot의 로딩 프리팹 슬롯으로 로딩 화면을 교체할 수 있다. 모바일 실제 기기 검증과 게임별 HUD는 별개다. [반응형 UI 기준](RESPONSIVE_UI.md)

## 후속 요구·설계 초안 — 2026-10-05

사용자가 저장·삭제 가능한 슬롯 3개와 일반적인 시작 메인 메뉴 배치를 요청했다. 현재 구현은 단일 슬롯이며 3슬롯 기능은 아직 적용하지 않았다. 최신 코드의 완료 상태와 이번 추가 요구의 설계 단계를 구분한다.

PC·가로형 모바일의 좌측 메뉴형·중앙 메뉴형·슬롯 우선형을 비교한 초안을 작성했다. 독립 진행 3개를 권장 가정으로 두고 빈 슬롯의 새 게임·저장된 슬롯의 이어하기·활성 슬롯 저장·확인 후 삭제 흐름을 제안한다. 기존 파일은 슬롯 1로 유지하며 삭제한 진행이 백업에서 재등장하지 않도록 슬롯의 복구 파일까지 처리할 계약을 정의했다. 슬롯 방식·배치 확정 뒤 런타임과 Unity 화면을 구현하고 해당 경계만 검증한다. [사례 조사·배치·저장 계약 초안](MAIN_MENU_AND_SAVE_SLOTS_DRAFT.md)

## 최신 완료 상태와 다음 프로젝트의 확인 항목

| 범위 | 현재 결과 |
| --- | --- |
| 공통 프리셋 구현 | Boot·설정·단일 슬롯 저장·복구·씬 흐름·개발 저장소 격리·UI 에셋 연결 완료 |
| 2026-10-04 1차 공통 사용 점검 | 추가 Gameplay 씬의 저장 허용, PC 창 크기 조절, 세 씬의 UI 입력과 활성 Boot 진입점 검사 보완. PlayMode 15/15·EditMode 16/16 통과 |
| 2026-10-04 후속 재사용성 검사 | 공통·예제 UI 검사 분리, 결과 알림, 사용자 진행률 Image·중첩 Panel 수정. EditMode 18개 항목(17개 통과 후 1개 재검사), PlayMode 27/27 통과 |
| 2026-10-04 코드 설계 점검 | 언어 식별자 일반화, 저장 검증 오류 계약 일치, SOLID·패턴·확장 계약 문서 보강. 관련 EditMode 57/57 통과 |
| 필수 회귀 | 깨끗한 복제본 EditMode 50/50, UI 확장 후 PlayMode 31/31, 입력 차단 보완의 해당 테스트 1/1 통과 |
| 실제 Windows Player | 별도 프로세스 저장·재실행 복원, 합성 UI 입력의 새 게임·저장·이어하기, 네이티브 1920×1080 Title·Main 표시 확인 |
| 배포 | 공개 GitHub Template, main, v1.0.0 태그와 Release의 ZIP·SHA-256·manifest 확인 |
| 추가 Windows 표시·설정 검증 | Codex 재시작 후 Player 실행 성공. 960×540 Title·Main 배치, 음량 적용, 창/전체화면 전환, 씬 전환과 단일 AppRoot 통과 |
| Computer Use 직접 조작 | Editor·Player 창 캡처는 재선택 후에도 시간 초과. 대기형 Player에 보낸 Enter가 Unity 입력 로그에 나타나지 않아 클릭·키보드 조작은 미확인 |
| 새 게임 대상 장치 | Android/iOS 빌드·실기기, 실제 터치·물리 키보드/게임패드·오디오 청취는 미검증 |

[Template](https://github.com/SIzsang/Starter_Project_Unity_6) · [Release](https://github.com/SIzsang/Starter_Project_Unity_6/releases/tag/v1.0.0). 배포 소스 기준은 `53db0d56dcb693b0c1a65088583de715d8b95038`이며 ZIP은 156개 파일, 243,977 bytes다. SHA-256은 `B32A4F070EAD6CC555C00722560F087B3B54A29C1890F54FE1C9D44B57F8D4FA`다. GitHub 업로드 파일의 digest가 로컬 해시와 같음을 확인했다. 배포 후 인계 문서 보완은 main에 기록하며 v1.0.0 태그와 ZIP을 바꾸지 않는다.

새 프로젝트는 **식별자 변경 → Validate Setup → 게임별 payload·콘텐츠 연결 → 대상 플랫폼의 첫 실행·저장·재실행·빌드** 순서로 시작한다. 예제 배경·버튼에 스프라이트를 넣고 로딩 프리팹을 연결할 수 있다. 캐릭터 프레임 에셋의 Walk/Attack/Hurt Clip·Animator는 새 게임의 콘텐츠 작업에서 구성한다. 1920×1080은 UI 설계 좌표이며 최대 해상도가 아니다. 화면 크기와 안전 영역에 따른 비례 축소 기반은 구현·자동 검증했고, 최종 에셋의 가독성과 조작 크기는 대상 기기에서 확인한다.

### 범용성·사용성 후속 재검토 — 2026-10-04

SOL 6.1 작업의 기준 커밋 `8da41b6`을 검토해 사용자 UI 교체와 이벤트 기반 표시에서 생기는 네 가지 문제를 보완했다. `Validate Setup`은 공통 AppConfig·씬·Boot 진입점만 확인하고, 기본 화면은 별도 `Validate Example UI`로 확인한다. 사용자 UI를 사용하는 Main 직접 Play와 Windows Preview는 예제 Canvas·입력 에셋을 강제하지 않는다.

저장 성공·실패와 이어하기 실패는 확정된 상태·메시지를 `StateChanged`로 알린다. 일반 로딩 Image는 Track 자식의 가로 막대로 정규화하고 Filled Image는 배치·방향을 유지하며 채움 값만 바꾼다. 전체 화면 조작 Panel은 자식과 함께 안전 영역에 들어가며, 장식 자식이 있는 배경은 Canvas의 `StarterCanvasLayout > Full Screen Roots`에 명시할 수 있다.

검토 근거와 이번 필수 검증 결과는 [범용성·사용성 재검토](REUSABILITY_REVIEW.md)에 기록한다. 게임별 데이터·콘텐츠는 추가하지 않았다. 최신 main에 적용하며 v1.0.0 ZIP은 이전 소스로 유지한다.

### 1차 범용 템플릿 사용 점검 — 2026-10-04

게임별 캐릭터·능력치·스테이지 진행·전투·자동 저장 규칙은 이번 점검에서 제외했다. 공통 기반에서 Main 이외의 씬으로 이동한 활성 세션의 저장을 허용하고 Boot·Title·전환 중 차단을 유지했다. PC 창 크기 조절을 활성화했으며 Validate Setup은 세 예제 씬의 단일 활성 EventSystem·필수 UI 액션과 Boot의 단일 활성 AppBootstrap을 확인한다.

원본 프로젝트의 격리된 테스트 저장소로 관련 PlayMode **15/15**, EditMode **16/16**을 통과했다. 최신 main에서 사용할 수 있고 기존 v1.0.0 태그·ZIP에는 들어 있지 않으므로, 보완된 템플릿으로 새 프로젝트를 만들 때는 최신 main을 사용한다. 새 Player 빌드·직접 GUI 조작·모바일 실기기 검증은 이번 회차에 추가하지 않았다.

사용 전에 회사명·제품명·앱 식별자를 지정하고 설정·씬 이동 시 경로와 빌드 목록을 갱신한다. 새 asmdef는 Core와 사용하는 DLL을 참조해야 한다. 현재 입력은 새 Input System, 예제 문구는 uGUI Text이며 TMP 전환은 표시 코드의 참조도 변경해야 한다. Main 이외의 씬은 Editor 직접 Play 초기화 대상이 아니다. 자세한 조건·검증 증거는 [제작 시작 전 점검](GAME_START_AUDIT.md)과 [새 게임 시작 가이드](NEW_GAME_SETUP.md)를 따른다.

### 재시작 후 GUI 검증 결과 — 2026-10-02

- **실행 제한 해소:** 대상 Player의 허용 설정을 좁게 추가하고 Codex를 재시작한 뒤 Computer Use로 실행했다. 이전 앱 권한 거부는 당시 시도 기록이며 현재 실행 장애가 아니다.
- **실제 Windows 그래픽 결과:** 검증 복제본의 일회성 시나리오가 Title에서 음량 버튼의 Unity 이벤트를 호출하고 `AudioListener.volume`·저장 설정의 일치를 확인했다. 전체화면 해제, 실제 960×540 창 크기 변경, Main 이동 후 크기 유지와 전체화면 복귀를 통과했다. 전체화면 복귀 때 렌더링 크기는 960×540이었으며 1920×1080 복귀를 검사한 것은 아니다.
- **화면 확인:** 초기 1920×1080 Title과 960×540 Title·Main·전체화면 Main을 네이티브 렌더링 이미지로 기록했다. 960×540 이미지에서 문구·버튼 배치를 확인했고 모든 활성 버튼의 경계가 화면 안에 있음을 검사했다. 전환 후 AppRoot는 1개였고 시나리오에 런타임 오류가 없었다. `Logs/FinalHandoffValidation/result.txt`는 `PASS`다.
- **직접 입력의 한계:** 자동 이벤트 호출과 구분하기 위해 입력을 주입하거나 버튼을 호출하지 않는 관찰용 Player를 따로 빌드·실행했다. 포커스가 있고 Start Game이 선택된 Ready 상태에서 Computer Use로 Enter를 보냈으나 키·클릭 로그와 Main 전환이 없었다. Windows 창 캡처는 Editor·Player에서 각각 재선택 후에도 시간 초과됐다. 따라서 OS 마우스·키보드 전달을 통과로 기록하지 않으며 원인도 제품 코드로 단정하지 않는다.
- **증거와 배포 경계:** `Logs/FinalHandoffValidation/`의 로그·이미지와 `Logs/FinalComputerUseInputValidation/events.log`, `Logs/FinalComputerUseInputWindowsBuild.log`를 보관한다. 관찰용 빌드는 169,610,584 bytes였다. 임시 코드는 검증 복제본에만 사용했고 원본 런타임·v1.0.0 ZIP은 변경하지 않았다. 관찰 소스·Player 로그도 증거 폴더에 복사했다. 이후 사용자의 테스트 자료 정리 요청에 따라 시험용 빌드·임시 코드·저장 폴더는 일반 삭제로 정리했다. 앞선 강제 삭제 거부 기록은 당시 결과이며 최신 정리 상태는 아래와 `Logs/TestMaterialCleanup-20261002.json`을 따른다.

### 사용이 끝난 테스트 자료 정리 — 2026-10-02

- 검증 폴더 5개(`NamingValidation`, `Stage34Validation`, `TemplateCloneValidation`, `TemplateFinalValidation`, `TemplateLatestValidation`)의 불필요한 캐시·실행 파일·소스 복사본·임시 진단을 정리했다. 현재 실행 중인 Editor가 원본 프로젝트를 사용하고 있음을 확인한 뒤 삭제했다.
- 필요한 복제본 로그·결과와 Player 로그는 `Logs/ArchivedValidation/`에 **46개 파일, 약 0.91MB**로 보관했다. 복제본에서 복사한 44개 증거 파일은 삭제 전에 원본과 SHA-256 일치를 확인했다. 기존 문서가 참조한 `Logs/<검증 폴더>/Logs/...` 또는 `Builds/Windows/<로그>`는 이제 `Logs/ArchivedValidation/<검증 폴더>/...`에서 확인한다.
- 시험용 제품명 `StarterProjectGuiScenario20261002`, `StarterProjectTemplateProbe`, `StarterProjectTemplateSmoke`의 저장 폴더 3개와 임시 커밋 본문·테스트 백업·실행 상태·GUI 권한 안내 도구 21개를 삭제했다. 실제 프로젝트의 기본 사용자 설정과 최종 테스트 XML·GUI 이미지·로그 및 `Builds/Template` 배포 파일은 보관했다.
- 파일 크기 합산과 새 증거 보관분을 기준으로 **약 9.303GB**를 정리했다. ZIP SHA-256은 기존 배포 값과 일치한다. 원본 프로젝트의 개인 Cloud 연결 변경은 정리 대상에 포함하지 않았다.
- **남은 정리 제한:** 검증 폴더 3곳의 숨김 `.git` 메타데이터만 약 **1.768MB** 남았다. 해당 메타데이터의 강제 삭제는 자동 승인 검사에서 `blocked by policy`로 거절됐으며 상세 사유는 제공되지 않았다. 빈 `.git` 폴더도 남아 있다. 로그·캐시·빌드가 남았다는 의미는 아니며, 원본 저장소의 `.git`과 별개의 시험용 메타데이터다.

## Bootstrap Scene과 실행 흐름

| 요소 | 현재 역할 |
| --- | --- |
| `00_StartScene` / `AppBootstrap` | 빌드의 첫 씬. 유일한 `AppRoot`를 확보하고 `SO_AppConfig`로 초기화를 시작한다. |
| `SO_AppConfig` | Boot·Title·Main 경로와 사용자 설정 기본값. 세 씬의 활성화·순서·중복을 검증한 뒤 실행용 사본을 만든다. |
| `AppRoot` | `NotStarted → Initializing → Ready/Failed` 상태, 설정·저장 서비스의 수명, Title/Main 이동과 중복 전환 차단을 조정한다. |
| Boot `StarterScreen` | 준비 단계와 오류를 표시한다. `EventSystem`의 Input System UI 모듈을 사용한다. |

정상 실행은 `Boot → 설정·씬 검증 → 사용자 설정/저장 읽기 → 음량·화면 적용 → Title → New Game 또는 Continue → Main`이다. 초기화 실패는 Boot에 남아 원인을 표시하고 게임 진입을 막는다. 이미 준비된 루트로 Boot에 재진입하면 루트를 재사용해 Title로 돌아간다. 씬 전환 중에는 추가 요청과 버튼 입력을 차단한다.

`Tools > Starter Project > Validate Setup`은 AppConfig·빌드 씬·Boot의 단일 활성 AppBootstrap·설정 참조를 **씬을 변경하지 않고** 검사한다. 기본 StarterScreen의 상태 표시 Canvas·Text, 세 씬의 EventSystem·UI 액션·Canvas 해상도는 별도 `Validate Example UI` 메뉴에서 검사한다. Boot → Title → Main 왕복·중복 루트·실패 차단은 PlayMode 테스트에서 확인했다. 원본 Editor의 Validate Setup 메뉴와 Boot Play → Title → 정지 후 Boot 복귀를 확인했고, Windows Player의 네이티브 Title·Main 화면도 확인했다. Computer Use의 창 캡처·물리 입력 전달과 Main 직접 Play의 GUI 확인에는 제한이 남아 있다.

## 코드 재사용 구조·SOLID·디자인 패턴 — 2026-10-04

검토 기준은 `bfd1929`와 이번 보완이다. 공통 초기화·설정·저장·씬 전환을 재사용하기에 책임 분리는 적절하다. 실제 확장 장애였던 Core의 en/ko 제한과 저장 검증 예외 처리의 불일치를 수정했다. 모든 장르 요구를 수정 없이 수용한다는 뜻은 아니며, 현재 확장 계약 밖의 기능은 아래 경계를 기준으로 설계한다. [상세 SOLID·패턴 검토](ARCHITECTURE_REVIEW.md)

### 의존 방향과 책임

`새 게임 Gameplay / UI → Core`, `Editor → Core / UI` 방향이다. Core는 프로젝트의 UI·Editor를 참조하지 않으며, Unity API와 Newtonsoft.Json에는 의존한다. Unity 밖에서 그대로 쓰는 순수 .NET 라이브러리는 아니다. 예제 UI 어셈블리 자체를 삭제하면 Editor의 예제 생성 코드 참조도 함께 정리해야 한다.

| 구성요소 | 맡는 일 | 새 게임에서 넣을 것 |
| --- | --- | --- |
| AppBootstrap / AppConfig | Boot 진입·씬 경로·기본 설정 | 새 게임의 씬과 기본값 |
| AppRoot | 서비스 조립·수명·준비 상태·씬 요청·설정 적용/저장 조정 | 공통 진입점으로 사용. 전투·성장 규칙은 넣지 않음 |
| SettingsService / UserSettings | 옵션 스냅샷·검증·JSON 형식 | 기존 필드 값. 새 옵션 필드는 복사·검증·직렬화·버전 정책까지 함께 확장 |
| GameSessionService / GamePayloadPolicy | 공통 세션 메타데이터·payload 규칙 | 별도 Gameplay 모델과 파생 정책 |
| JsonRepository / JsonFileStore | 보호·백업 선택·명시적 복구 / 실제 파일 교체 | 필요할 때 계약에 맞는 ITextFileStore 구현 |
| StarterScreen / LoadingOverlay / CanvasLayout | 예제 입력·상태 표시·전환 화면·안전 영역 | 게임 에셋과 자체 UI. 화면 타입을 Core에 추가하지 않음 |

### 원칙과 실제 적용 패턴

- **S:** 진입·요청 조정·데이터 정책·파일 IO·표시의 변경 이유를 분리했다. AppRoot는 앱 흐름의 조정자이며 게임 규칙의 저장소로 확대하지 않는다.
- **O:** 저장소·실행 설정·payload 정책을 교체할 수 있다. 언어도 Core 변경 없이 게임별 식별자를 저장한다. 새 옵션 스키마·다중 슬롯까지 자동 확장되는 구조는 아니다.
- **L:** 대체 구현은 원본 파일 보호, 실행 설정 복원·해제, 순수 검증 계약을 지켜야 한다. 기본 구현과 테스트 대체 구현을 확인했으며 임의의 외부 구현까지 보증하지 않는다.
- **I:** ITextFileStore와 IRuntimeSettings는 현재 소비자에게 필요한 작은 경계다. 현재 요구에 추가 인터페이스 분할은 필요하지 않다.
- **D:** 서비스는 저장소 추상화에 의존하고, AppRoot가 기본 구현을 조립한다. UI·Editor 의존이 Core로 역류하지 않는다.

적용 구조는 AppRoot의 **Composition Root·Application Controller**, payload/변환 delegate의 **Strategy**, 플랫폼·파일 경계의 **Adapter**, 보호·복구를 묶은 제한된 **Repository**, 상태 이벤트의 **Observer**다. AppRoot.Instance는 중복을 제거하는 단일 Unity 실행 인스턴스다. AppState enum은 상태 모델이며 GoF State 구현은 아니다. 불변 세션·방어적 복사는 소유권 규칙이며 Memento·Prototype으로 분류하지 않는다. 현재 규모에서는 별도 DI 컨테이너·이벤트 버스·상태 클래스 계층을 추가하지 않는다.

### 교체 시점·소유권·호출 규칙

| 확장 경로 | 연결 시점·수명 | 지켜야 할 계약 |
| --- | --- | --- |
| ConfigureStorage | Begin 전 교체 가능. 외부 자원의 정리는 주입자 책임 | 없는 파일만 null. 검증·쓰기 실패 시 기존 주 파일 보호. 정상 교체는 .bak, 복구 교체는 기존 .bak과 원본 별도 보존 |
| ConfigureRuntimeSettings | Begin 전 1회. AppRoot가 Dispose | 실패 시 직전 실행 상태 복원 시도, Dispose 시 최초 상태 복원·중복 해제 안전성 |
| GamePayloadPolicy | Boot Inspector 또는 Begin 전 ConfigureGamePayload | Inspector 또는 수동 연결 중 한 경로만 사용. 양수 payload 버전·JSON 객체 초기값·순수 검증. 실행 중 정책 에셋 변경 금지 |
| 사용자 UI / Gameplay | AppRoot가 준비된 뒤 Try* API 사용 | Settings.Current는 복사본, Game.Current는 읽기 전용. 저수준 서비스 호출은 씬 차단·설정 적용 조정을 우회할 수 있음 |

일반 Player의 주입은 Boot의 단일 활성 연결 컴포넌트 Awake에서 AppRoot를 확보하고 Configure*를 호출하는 방식으로 구성한다. AppBootstrap.Start가 Begin을 호출한다. 기존 루트가 NotStarted일 때만 구성하고, Boot 재진입의 Ready 루트에는 Configure*를 다시 호출하지 않는다. 여러 Awake의 상대 순서에 의존하지 않는다. Editor의 Main 직접 Play는 저장소를 개발용 격리 경로로 교체하므로 일반 Player의 사용자 저장소 동작을 확인하는 경로로 사용하지 않는다.

모든 앱 API는 Unity 메인 스레드에서 사용한다. StateChanged·Changed는 동기 호출이며 구독자는 해제와 자체 예외 처리를 책임진다. 씬 이동 Try*의 true는 요청 접수다. 실제 준비는 Ready·!IsTransitioning·목적 씬을 함께 확인한다. 저장 Try*는 동기 결과를 반환하며 성공 여부는 bool로 판단한다. StorageStatus는 파일을 읽어 판정한 상태다. Loaded 상태에서도 새 payload 저장은 실패할 수 있으므로 마지막 작업의 성공 여부와 구분한다. 안내 문자열을 프로그램 분기 조건으로 해석하지 않는다.

검증기는 읽기·저장 과정에서 반복 호출될 수 있다. JSON 데이터 오류는 JsonException·InvalidDataException·FormatException·OverflowException으로 알릴 수 있으며, 나머지 코드 결함까지 저장 실패로 숨기지 않는다. 재화 차감이나 상태 갱신은 검증기 밖의 Gameplay가 담당한다.

### 이번 보완의 범위

언어는 1~64자이며 공백·제어 문자를 포함하지 않는 식별자로 저장한다. `ja`, `fr`, `zh-Hans`, `pt-BR`, 게임 전용 식별자를 지원하며 등록된 언어 목록·번역 존재 여부는 게임이 결정한다. 기본 UI는 en/ko 토글을 유지한다. 기존 settings schemaVersion 1은 호환되며 번역 기능을 구현한 것은 아니다.

현재 전제는 단일 앱·세션·저장 슬롯, Boot/Title/Main 흐름, 동기 저장, 파일당 1MiB·JSON 깊이 32다. 다중 프로필·클라우드·Addressables·payload 마이그레이션은 요구가 정해질 때 확장한다. 관련 저장·설정 EditMode **57/57 통과, 실패·건너뜀 0개**를 확인했다. 새 언어의 기본값·저장·복원과 잘못된 식별자, 길이 경계, 변환 예외의 저장 실패·원본 보호를 검사했다. GUI·추가 Player 빌드·모바일 실기기는 이번 변경의 검증 범위에 포함하지 않았다. 로그와 상세 범위는 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 남긴다.

## 데이터와 저장 계약

- `UserSettings`: SO 기본값에서 복사하고 사용자 JSON에 음량·전체화면·언어 코드를 저장한다. 저장 시 플랫폼 적용을 먼저 확인하고 JSON 저장 실패에는 이전 실행 설정으로 복원한다.
- `GameSessionService`: 단일 저장 슬롯에 세션 ID·시각·버전과 **게임별 JSON 객체 payload**를 기록한다. 새 게임은 기존 저장을 즉시 지우지 않는다. 다른 세션으로 기존 저장을 바꾸려면 확인이 필요하다.
- `JsonRepository<T>` / `JsonFileStore`: 임시 파일을 기록·검증한 뒤 교체하며 `.bak`을 남긴다. 파손 시 정상 백업을 표시하고 사용자의 명시적 복구 전에는 원본을 바꾸지 않는다. 미래 버전 파일은 덮어쓰지 않는다.
- 저장 크기는 **파일 전체 최대 1MiB**다. 현재 저장은 동기식이므로 대량 월드 데이터나 긴 이력에는 저장 형식·성능 재검토가 필요하다.
- 현재 기본 `Save Game` 버튼은 게임 오브젝트의 상태를 자동 수집하지 않고 `GameSessionService.Current.PayloadJson`을 저장한다. 새 게임의 `Gameplay`에서 실제 상태를 JSON 객체로 만들고 저장 버튼/체크포인트와 연결해야 한다. `Continue` 후에는 현재 payload를 새 게임의 타입 모델로 복원한다.
- Boot의 `AppBootstrap`에 게임별 `GamePayloadPolicy` 에셋을 선택적으로 연결하면 Core 수정 없이 초기 JSON·payload 버전·유효성 검사 규칙을 지정할 수 있다. 활성 세션의 Main·추가 Gameplay 씬에서 `AppRoot.TrySaveGame(payloadJson)`으로 현재 게임 상태를 전달한다. 미연결 상태는 기존 빈 `{}`·버전 1 동작을 유지한다. 새 정책의 저장·거부·Continue 복원은 Unity PlayMode에서 확인했다. [새 게임 시작 가이드](NEW_GAME_SETUP.md)

**향후 계획된 약 100명의 캐릭터 능력치와 스테이지 진행:** 프리셋 Core에 고정 필드를 추가하지 않는다. 새 게임에서는 캐릭터 ID와 변화하는 능력치·성장 상태, 스테이지 ID와 진행 단계만 payload에 넣고 고정 캐릭터 정의는 게임 데이터에서 조회한다. 100명 × 1KiB면 약 100KiB, 5KiB면 약 500KiB로 현재 상한 안에 있지만, 실제 JSON 직렬화 크기를 측정한다. 인벤토리·전투 로그·월드 오브젝트 이력을 누적하면 1MiB에 가까워질 수 있다. 이 수치는 설계 예시이며 실제 게임 스키마가 확정된 것은 아니다.

## 개발 경로와 사용법

1. 일반 Play는 Boot 씬에서 시작한다. Title의 New Game/Continue로 Main에 들어간다.
2. Editor에서 Main 씬을 직접 Play하면 Boot 초기화를 거친 뒤 **새 빈 개발 세션**으로 Main에 진입한다. 각 Play의 저장소는 `Library/StarterProject/PlaySessions/<sessionId>`로 분리되어 일반 사용자 저장을 건드리지 않는다.
3. `Validate Setup`으로 설정을 확인하고, 개발 세션 파일은 `Open Test Data Folder`에서 확인한다. `Reset Test Data`는 확인 대화상자와 백업 이동을 거치며 취소할 수 있다.
4. 새 게임의 `Gameplay`에 캐릭터/스테이지 타입 모델과 직렬화·복원 코드를 둔다. 현재 단일 슬롯과 JSON payload 계약을 유지할지 게임 요구사항에 맞춰 결정한다.

## 날짜별 검증 기록

아래의 대기·미완료 표현은 각 작업 당시의 기록이다. 현재 완료 상태는 위 표를 따른다. 상세 날짜·로그·실행 범위는 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 보관한다.

- Unity 6000.3.16f1의 `6a31e97` 새 Git 복제본에서 **EditMode 50개·PlayMode 30개 통과, 실패·건너뜀 0개**. 빈 Library에서 패키지·에셋을 가져온 뒤 실행했다. 결과: `Logs/TemplateLatestEditMode.xml`, `Logs/TemplateLatestPlayMode.xml`.
- Main 직접 Play 반복은 도메인 재로드 켜짐·꺼짐의 자동 실행에서 Boot 경유, 세션 격리, 시작 씬 복원과 일반 저장 보호를 확인했다. 자동 실행용 batchmode 예외는 복제본에만 넣었고 원본 코드에는 없다.
- Windows x64 Development 빌드에서 서로 다른 Player 프로세스의 첫 실행 저장·재실행 복원·이어하기, 파손/미래 버전 보호, 명시적 백업 복구, 저장 완료 직후 강제 종료와 계측 복제본의 교체 전 강제 종료를 확인했다. 계측 중단 후 미완료 `.tmp`가 남으며 전원 손실·모든 중단 시점을 증명하지 않는다.
- Boot 구성 검사 보완 후 새 Windows x64 Development 빌드도 성공했고, 검증용 제품명 `StarterProjectBootstrapValidation`의 다른 두 Player 프로세스에서 `CREATE_PASS`·`RESUME_PASS`를 확인했다. 로그는 `Logs/BootstrapWindowsBuild.log`와 복제본 `Builds/Windows/BootstrapPlayerCreate.log`·`BootstrapPlayerResume.log`다.
- 숨겨진 그래픽 Player에서 Boot 캡처를 두 번 시도했으나 결과가 검은 화면이라 레이아웃 증거로 사용하지 않는다. 실제 GUI 시각 검증은 남아 있다.
- 가로형 UI 레이아웃 보완 후 복제본에서 EditMode 46개·PlayMode 29개가 통과했다. 안전 영역·해상도 변경은 합성 화면 값으로 자동 검사했으며 모바일 실기기 시각·터치 확인은 남아 있다.
- 변경 후 Windows 개발 빌드도 성공했고 검증용 별도 저장 경로의 두 Player 프로세스에서 첫 저장·재실행 이어하기를 통과했다. Android·iOS 빌드 모듈은 설치되지 않아 모바일 패키지 실행은 확인하지 않았다. [검증 기록](INTEGRATION_VALIDATION.md)
- 1920×1080 좌표 이관 후 Windows 개발 빌드를 다시 만들고 `StarterProjectFullHdValidation` 저장 경로에서 서로 다른 두 헤드리스 Player 프로세스의 `CREATE_PASS`·`RESUME_PASS`를 확인했다. 이 실행은 화면 배치의 시각적 검증을 대신하지 않는다.
- 템플릿 패키지·식별 정보 정리 시험 복제본에서 EditMode 46개·PlayMode 29개, Windows 개발 빌드와 별도 헤드리스 Player 프로세스의 새 저장·이어하기를 확인했다. 저장 위치는 검증 전용 `StarterProjectTemplateSmoke` 제품명으로 격리했다. 이어 `3460339`의 **새 Git 복제본**을 비어 있는 Library에서 열어 EditMode 46개·PlayMode 29개와 Windows 빌드(169,600,344 bytes)를 재확인했다. 원본 Editor의 Main 직접 Play와 화면 조작은 아직 남아 있다.
- **남은 7단계:** 원본 Editor의 GUI Main 직접 Play와 메뉴 확인·취소, 실제 Windows 창/전체화면·오디오 청취·키보드/게임패드 조작, 다른 중단 시점과 임시 파일 정리 정책. 자동 테스트와 빌드 성공을 이 현장 검증으로 대체하지 않는다.
- **8단계 진행:** 추적 중인 `Project_DE`·`DefaultCompany`와 Cloud 프로젝트·조직 ID를 중립 템플릿 값 또는 빈 값으로 바꾸고, 현재 코드·씬에서 사용하지 않는 협업·Multiplayer Center·Visual Scripting 패키지를 제거했다. 2D 애니메이션 도구는 유지한다. 변경 후 깨끗한 복제본의 자동 PlayMode·빌드는 통과했다. Unity Hub/Editor Services의 연결 표시와 GUI 화면·장치 확인, 버전·템플릿 지정은 남아 있다. 원격 푸시·태그·공개는 아직 수행하지 않았다.
- **8단계 후속 제작:** Product Name에 따라 Windows Preview 파일명을 정하고, 게임별 저장 정책의 선택적 연결, 새 게임 시작 가이드와 변경 이력을 추가했다. 추적된 에셋·meta 쌍과 생성 파일 제외, 게임 전용 콘텐츠·서버 주소 부재를 정적 점검했다. 후속 코드는 깨끗한 Git 복제본의 Unity EditMode·PlayMode 및 Windows 개발 빌드를 통과했다. Product Name 변경 빌드에서 이름이 바뀐 실행 파일도 확인했다. GUI Computer Use 검증은 사용자 요청에 따라 미뤘다.
- **2026-09-24 Console 수정:** 새 PlayMode 테스트의 문자열 구문 오류를 고쳤다. 원본 프로젝트의 생성된 EditMode·PlayMode C# 프로젝트는 각각 경고·오류 0개로 빌드됐다. 별도 복제본의 Unity Test Runner는 라이선스 클라이언트 연결 실패로 시작하지 못했으므로 이 C# 컴파일을 런타임 동작 검증으로 간주하지 않는다.
- **2026-09-24 후속 Unity 검증:** 권한이 허용된 명령줄 실행에서 라이선스가 연결됐고 테스트 명령에서 조기 종료를 일으키는 `-quit`을 제거했다. 최신 소스 복제본과 새 Git 복제본에서 각각 EditMode 50개·PlayMode 30개 및 Windows 빌드를 통과했다. 새 복제본의 Product Name 변경 빌드도 성공했다. 자동 실행 결과는 GUI 배치·실제 입력·모바일 기기의 확인을 대체하지 않는다. [검증 기록](INTEGRATION_VALIDATION.md)

- **2026-10-02 UI 교체·재검증:** 예제 씬은 `Image` 스프라이트를 직접 교체할 수 있고 Boot의 선택형 로딩 프리팹 슬롯으로 로딩 화면을 바꿀 수 있다. 사용자 프리팹에 필요한 동적 문구·진행률 참조를 연결하며, 런타임이 투명 입력 차단막을 추가한다. 최신 UI 소스를 반영한 Unity PlayMode 31/31, 입력 차단 보완 후 관련 테스트 1/1과 Windows 개발 빌드가 성공했다. 실제 에셋을 넣은 화면·터치 가독성은 미검증이다. [UI 기준](RESPONSIVE_UI.md)

- **2026-10-02 원본 Editor 부분 확인:** Boot에서 Play하여 Title 로드, Play 종료 후 Boot 복귀를 확인했다. Computer Use의 Unity 창 캡처가 반복 시간 초과되고 GameView 내부 버튼이 접근성에 표시되지 않아 Title→Main 조작·Main 직접 Play·시각 확인은 완료하지 못했다. 별도 Windows Player 재빌드는 성공했지만 Computer Use 앱 실행 승인 대기가 시간 초과돼 Player 화면은 열지 못했다. [검증 기록](INTEGRATION_VALIDATION.md)

- **2026-10-02 v1.0.0 인계:** 네이티브 Windows Player Title·Main 화면과 합성 키 입력의 New Game→Save→Title→Continue, 같은 저장 세션 복원을 확인했다. main·GitHub Template·v1.0.0 태그로 기준 소스를 고정한다. 임시 테스트 코드와 Cloud ID는 포함하지 않는다. [배포 가이드](RELEASE.md)

## 새 프로젝트 인계 기준

후속 범용성·설계 보완을 포함하려면 최신 main의 GitHub Template을 기준으로 새 저장소를 만든다. v1.0.0 태그·ZIP은 이전 배포 소스이며 이번 수정은 포함하지 않는다. 복제 직후 Product Name·Company Name·앱 식별자와 저장 경로 충돌 여부, Cloud 연결, 플랫폼·렌더러·입력 요구사항을 새 게임에 맞춘다. `SO_AppConfig`와 씬 구성을 검사하고 Unity Test Runner·첫 실행·저장·재실행·Windows 빌드를 다시 확인한다. 어떤 프리셋 버전에서 출발했는지 새 게임의 기록에 남긴다. 향후 게임에서 바꾼 Core를 이 원본 프리셋에 자동으로 역반영하지 않는다.

세부 기록: [새 게임 시작 가이드](NEW_GAME_SETUP.md), [제작 가능 범위](GAME_CAPABILITY_BRIEF.md), [Bootstrap 사용 가이드](BOOTSTRAP.md), [설정·저장](SETTINGS_AND_SAVE.md), [공통 기능](COMMON_SERVICES.md), [Editor 개발 경로](EDITOR_WORKFLOW.md), [SOLID 점검](ARCHITECTURE_REVIEW.md), [통합 검증](INTEGRATION_VALIDATION.md), [로드맵](ROADMAP.md), [템플릿 체크리스트](TEMPLATE_CHECKLIST.md), [변경 이력](CHANGELOG.md).
