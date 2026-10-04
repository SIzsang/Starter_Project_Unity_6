# 7단계 통합·Windows 빌드 검증

최종 점검: 2026-10-04. **v1.0.0의 공통 프리셋 구현·필수 회귀·Windows Player 확인·배포는 완료했다.** 이 문서는 날짜별 실행 기록이며 과거의 대기·미완료 표현은 당시 상태다. 최신 main의 추가 보완은 [제작 시작 전 점검](GAME_START_AUDIT.md), 플랫폼 지원 범위와 Computer Use 제한은 [배포 기준](RELEASE.md)을 따른다.

## 2026-10-04 범용성·사용성 후속 재검토

기준 커밋 `8da41b6`의 공통 검사와 예제 화면 결합, 저장 결과 알림 누락, 사용자 로딩 Image의 진행률, 전체 화면 Panel의 안전 영역 적용을 보완했다. 관련 EditMode·PlayMode 검사와 범위는 [재검토 보고서](REUSABILITY_REVIEW.md)를 따른다. 새 Player 빌드와 Computer Use·실기기 검증은 추가하지 않았다.

## 2026-10-04 1차 범용 템플릿 사용 경계 점검

캐릭터 능력치·스테이지 진행·전투 등 게임별 구현은 제외하고 원본 프로젝트에서 관련 테스트만 실행했다. Main을 언로드한 추가 씬의 저장·Continue와 Title·전환 저장 차단을 포함한 `RuntimeFlowTests` **15/15**, 세 씬의 UI 입력과 Boot 초기화 진입점 누락·비활성·중복을 다루는 `StarterProjectSetupTests` **16/16**이 통과했다. 모두 실패·건너뜀 0개이며 테스트 저장소는 사용자 저장과 분리됐다.

증거는 `Logs/GameStartAuditPlayMode.xml`·`.log`와 `Logs/GameStartAuditEditModeFinal.xml`·`.log`다. 새 검증 복제본·시험용 Player 빌드는 생성하지 않았다. PC `resizableWindow` 활성화는 설정으로 확인했으며 직접 창 조절·물리 입력·모바일 실기기 검증은 추가하지 않았다. 코드 보완은 최신 main에 기록하며 v1.0.0 태그·ZIP은 유지한다.

## 검증 환경

- Unity 6000.3.16f1, Windows x64 Development 빌드. 검증 복제본 `Logs/Stage34Validation`에서 실행했다.
- 복제본의 `productName`만 `StarterProjectStage7Validation`으로 바꿔 사용자 저장 경로를 원본과 분리했다. 원본 프로젝트 설정과 커밋에는 반영하지 않았다.
- 빌드 내부 흐름 검증용 `Stage7PlayerSmoke.cs`는 복제본에만 넣었다. 결과 빌드와 로그는 Git에서 제외되는 `Logs` 아래에 있다.
- 6단계 기준점은 로컬 커밋 `335cd3b`다. 원본 작업 트리에 7단계 런타임 검증 코드를 추가하지 않았다.

## 자동 검증 결과

| 항목 | 결과 | 근거 |
| --- | --- | --- |
| 전체 Unity 테스트 | EditMode 46개·PlayMode 26개 통과, 실패·건너뜀 0개 | `Logs/Stage6EditModeFinal.xml`, `Logs/Stage6PlayModeFinal.xml` |
| Windows x64 개발 빌드 | 성공, Unity BuildReport 172,158,828 bytes | `Logs/Stage7WindowsBuild.log` |
| 실행 파일 시작 | 그래픽 출력 없는 빌드 프로세스가 Unity Player를 초기화 | 복제본 `Builds/Windows/Stage7PlayerStart.log` |
| 정상 흐름, 첫 프로세스 | Boot → Title → New Game → Main, 설정과 게임 payload 저장 | 복제본 `Builds/Windows/Stage7PlayerSmokeResult.txt`의 `CREATE_PASS` |
| 정상 흐름, 재실행 프로세스 | 설정값·게임 세션과 payload 복원, Continue → Main → Title, AppRoot 1개 | 같은 결과 파일의 `RESUME_PASS` |
| 오류 흐름, 새 프로세스 | 파손 설정은 `Invalid`, 미래 버전 게임 저장은 `UnsupportedVersion`; Continue와 덮어쓰기를 막고 두 원본 파일을 보존 | 같은 결과 파일의 `ERROR_PASS`, `Stage7PlayerVerifyErrors.log` |
| 백업 복구, 새 프로세스 | 파손된 설정·게임 주 파일에서 정상 백업을 `Recovered`로 표시하고 명시적 복구 후 Continue 성공; 손상 원본은 별도 `.preserved-*` 파일로 보존 | 같은 결과 파일의 `BACKUP_PASS`, `Stage7PlayerVerifyBackup.log` |
| 저장 완료 직후 강제 종료 | 저장 API가 성공한 직후 Player를 강제 종료하고 다음 프로세스에서 같은 세션·payload로 Continue 성공 | 같은 결과 파일의 `KILL_AFTER_SAVE_READY`, `KILL_RECOVERY_PASS` |
| 저장 도중 강제 종료 | 복제본에만 넣은 지연 지점에서 임시 파일 기록·flush 후, 주 파일 교체 전에 Player를 강제 종료했다. 다음 프로세스에서 이전 정상 저장으로 Continue 성공했고 주 파일·`.bak`은 이전 payload를 유지했다 | 같은 결과 파일의 `INTERRUPT_BEFORE_REPLACE`, `INTERRUPT_RECOVERY_PASS`, `Stage7PlayerVerifyInterruptedWrite.log` |
| 그래픽 장치 경로 | Direct3D 11, NVIDIA GeForce GTX 1660 SUPER에서 오류 흐름 검증 프로세스 정상 종료 | 복제본 `Builds/Windows/Stage7PlayerGraphics.log` |
| 설계 보완 후 회귀 | 설정 플랫폼 적용·저장 순서 변경 후 EditMode 46개·PlayMode 27개 통과. 새 Windows 개발 빌드의 다른 두 Player 프로세스에서 첫 저장·재실행 Continue 성공 | `Logs/SolidEditMode.xml`, `Logs/SolidPlayMode.xml`, `Logs/SolidWindowsBuild.log`, 복제본 `Builds/Windows/SolidPlayerCreate.log`·`SolidPlayerResume.log` |
| Boot 구성 보완 후 회귀 | `Validate Setup`이 Boot 상태 표시 Canvas·Text와 EventSystem·UI 액션을 확인한다. EditMode 46개·PlayMode 27개 통과, 새 Windows 빌드에서 별도 프로세스 저장·재실행 Continue 성공 | `Logs/BootstrapEditModeFinal.xml`, `Logs/BootstrapPlayModeFinal.xml`, `Logs/BootstrapWindowsBuild.log`, 복제본 `Builds/Windows/BootstrapPlayerCreate.log`·`BootstrapPlayerResume.log` |
| 가로형 반응형 UI 후 회귀 | Boot·Title·Main과 로딩 화면의 안전 영역 맞춤, 모바일 가로 좌·우 방향 허용. 합성 가로 화면·PC 창 크기 테스트를 포함해 EditMode 46개·PlayMode 29개 통과. Windows 개발 빌드 172,177,793 bytes, 별도 프로세스 첫 저장·이어하기 성공 | `Logs/ResponsiveEditModeValidated.xml`, `Logs/ResponsivePlayModeValidated.xml`, `Logs/ResponsiveWindowsBuildValidated.log`, 복제본 `Builds/Windows/ResponsivePlayerCreate.log`·`ResponsivePlayerResume.log` |
| 1920×1080 디자인 좌표 이관 후 회귀 | 세 예제 씬과 동적 로딩 UI의 디자인 좌표를 1920×1080으로 통일. EditMode 46개·PlayMode 29개 통과. 격리 제품명의 Windows 개발 빌드 172,177,785 bytes와 서로 다른 헤드리스 Player 프로세스의 첫 저장·이어하기 성공 | `Logs/FullHdMigratedEditMode.xml`, `Logs/FullHdFinalPlayMode.xml`, `Logs/FullHdIsolatedWindowsBuild.log`, 복제본 `Builds/Windows/FullHdIsolatedcreate.log`·`FullHdIsolatedresume.log` |
| 원본 Editor 메뉴 | 열린 Unity Editor에서 `Tools > Starter Project > Validate Setup`을 실제 메뉴 키보드 경로로 실행해 성공 로그 확인. Main 직접 Play·대화상자·화면 시각 검증은 별개 | `C:/Users/ausqk/AppData/Local/Unity/Editor/Editor.log`의 `Setup validation passed` |
| 깨끗한 Git 복제본 | `966f33f`를 새 폴더에 복제하고 기존 Library 없이 패키지·에셋을 첫 가져오기. EditMode 46개·PlayMode 29개 통과, Windows 개발 빌드 172,168,120 bytes 성공 | `Logs/TemplateCleanEditMode.xml`, `Logs/TemplateCleanPlayMode.xml`, `Logs/TemplateCleanWindowsBuild.log` |
| 템플릿 정리 복제본 | 미사용 패키지 3개 제거 후 EditMode 46개·PlayMode 29개와 Windows 빌드(169,600,783 bytes) 통과. 중립 회사명·제품명·앱 ID와 비활성 Cloud ID 제거 후 EditMode 46개·PlayMode 29개 및 Windows 빌드(169,600,343 bytes) 재통과. 이 검증은 복제본에서 수행했으며 원본 변경 후 새 최종 복제본 검증은 별개 | `Logs/PackageTrimEditMode.xml`, `Logs/PackageTrimPlayMode.xml`, `Logs/PackageTrimWindowsBuild.log`, `Logs/TemplateIdentityEditMode.xml`, `Logs/TemplateIdentityPlayMode.xml`, `Logs/TemplateIdentityWindowsBuild.log` |
| 템플릿 저장 경로 스모크 | 일회성 검증 코드와 `StarterProjectTemplateSmoke` 제품명을 복제본에만 추가했다. 서로 다른 헤드리스 Windows Player 프로세스에서 첫 저장·재실행 Continue가 각각 CREATE_PASS·RESUME_PASS로 종료. 실제 게임별 저장 모델은 검증 대상이 아님 | `Logs/TemplateSmokeWindowsBuild.log`, 복제본 `Builds/Windows/Stage7PlayerSmokeResult.txt`·`Templatecreate.log`·`Templateresume.log` |
| 정리 후 최종 새 Git 복제본 | `3460339`를 비어 있는 새 폴더로 복제. 첫 패키지·에셋 가져오기부터 EditMode 46개·PlayMode 29개 통과, Windows 개발 빌드 169,600,344 bytes 성공. Cloud 프로젝트·조직 ID 공란과 선택 패키지 3개 제거 상태 유지 | `Logs/TemplateFinalEditMode.xml`, `Logs/TemplateFinalPlayMode.xml`, `Logs/TemplateFinalWindowsBuild.log` |

`CREATE_PASS`와 `RESUME_PASS`는 **서로 다른 Windows Player 프로세스**에서 생성했다. 저장 위치는 검증 전용 제품명 아래의 `Application.persistentDataPath/StarterData`다. `ERROR_PASS`는 파손 파일 준비와 검증을 또 다른 두 프로세스로 실행했고, Direct3D 11 경로에서도 한 번 더 확인했다. 이어 별도 두 프로세스로 정상 백업을 준비·복구해 `BACKUP_PASS`를 확인했다. 완료 직후 강제 종료와 재실행도 다른 프로세스에서 확인했다. 저장 도중 종료 실험에서는 복제본의 `JsonFileStore`에만 임시 파일 flush 직후 일시 정지 지점을 넣고 외부에서 해당 Player를 종료했다. 남은 `.tmp` 파일은 교체되지 않은 새 payload를 담고 있으며 정상 주 파일·백업은 이전 payload를 유지했다. 원본 템플릿 코드에는 이 지연 지점이나 일회성 런타임 검사 코드를 넣지 않았다. 이 검증용 빌드는 배포물로 사용하지 않는다.

## 2026-09-23 당시 남은 확인

1. 원본 Editor GUI에서 Main 직접 Play 두 번, 미저장 씬·기존 시작 씬 설정과 메뉴 확인·취소를 눈으로 확인한다. `Validate Setup` 메뉴는 원본 Editor에서 실행해 통과했지만 Main 씬 열기용 Windows 파일 대화상자는 자동화 도구가 입력 대상으로 잡지 못했다. 복제본 자동 검증과 구분한다.
2. 잘못된 AppConfig·입력 참조의 Console 안내, Reset Test Data의 백업 경로·파일과 취소 동작을 GUI에서 확인한다.
3. Windows 창/전체화면 전환, 실제 오디오 출력, 키보드·게임패드 선택·확인·취소를 화면과 장치에서 확인한다.
4. 일반 GUI 실행에서 저장 도중 강제 종료와 재시작을 확인한다. 현재 자동 검증은 계측 복제본의 **임시 파일 flush 후·주 파일 교체 전** 한 지점을 다루며, 전원 손실·파일 시스템별 동작이나 교체 도중의 모든 중단 시점을 증명하지 않는다. 이 실험에서 중단된 `.tmp` 파일은 남으므로 장기 사용 시 정리 정책은 후속 검토가 필요하다.
5. 실제 가로형 모바일 기기에서 노치·안전 영역, 긴 문구의 가독성, 터치 목표 크기와 좌·우 회전을 확인한다. 현재 설치에는 Android·iOS 빌드 모듈이 없어 모바일 패키지 검증을 수행하지 않았다. [UI 기준](RESPONSIVE_UI.md)

첫 지원 환경의 일반 실행과 화면 품질까지 확인한 뒤 7단계를 완료로 기록한다. 현재 자동 검증은 Windows 빌드의 시작·저장·재실행 복원 및 주요 오류 보호가 동작한다는 범위로 해석한다.

설정 저장의 플랫폼 적용 실패가 새 JSON을 남기지 않도록 보완한 근거와 SOLID 책임 경계는 [설계 점검](ARCHITECTURE_REVIEW.md)에 기록한다. 회귀 빌드에서는 검증 복제본의 `productName`을 `StarterProjectSolidValidation`으로 바꿔 이전 Stage7 저장과 경로를 분리했다. 검증 스크립트와 제품명 변경은 원본 프로젝트에 포함하지 않는다.

Boot 회귀 빌드는 복제본에서만 `productName`을 `StarterProjectBootstrapValidation`으로 바꿔 새 경로에서 실행했다. 숨겨진 그래픽 Player의 Boot 캡처 파일은 검은 화면이어서 화면 배치·가독성의 증거가 아니다. 일회성 캡처 코드와 Boot 지연 코드는 검증 복제본에만 있다.

1920×1080 회귀 빌드는 복제본의 `productName`을 `StarterProjectFullHdValidation`으로 바꿔 이전 테스트 저장과 분리했다. 그래픽 Player 초기화가 정체되어 해당 시도는 결과로 세지 않았고, 헤드리스 모드에서 제한 시간을 둔 새 게임·이어하기 검증을 완료했다. 디자인 좌표·안전 영역의 시각적 품질은 원본 Editor GUI와 실제 기기에서 계속 확인해야 한다.

새 Git 복제본은 Unity 첫 실행·테스트·빌드까지 통과했다. Unity가 첫 가져오기와 빌드 중 일부 URP 설정을 재직렬화하고 기본 `ProjectSettings/SceneTemplateSettings.json`을 생성했으므로, 이 자동 생성 변경을 기능 수정으로 복사하지 않았다. 패키지·식별 정보 실험은 이 복제본에서 이어 했고, 원본 코드·씬 참조를 직접 변경하지 않았다.
템플릿 저장 스모크의 제품명 변경과 검증 스크립트도 Git 제외 복제본에만 있다. 검증 저장은 `Application.persistentDataPath`의 `StarterTemplate/StarterProjectTemplateSmoke` 아래로 격리해 기본 템플릿의 저장 공간과 분리했다.

2026-09-23 후속 저장 정책·빌드명 변경: 구현 완료 뒤 복제본에서 Unity 명령줄 EditMode 회귀 검증을 시도했다. 테스트가 시작되기 전에 Licensing Client IPC 연결이 거부되고 `com.unity.editor.headless` 라이선스 오류가 발생해 중단했다. 새 테스트의 통과 수는 **0개가 아니라 미실행**이며 PlayMode와 새 Windows 빌드도 시작하지 않았다. 복제본의 생성된 C# 프로젝트에서 Core 단독 `dotnet build`는 오류 0개로 끝났으나 Unity 패키지 참조 경고 2개가 있어 Unity 테스트·빌드 완료 증거가 아니다. 사용자 요청에 따라 Computer Use GUI 검증은 진행하지 않았다.

2026-09-24 Console 오류 수정: 원본 Editor 로그의 최신 C# 오류는 `BootstrapFlowTests.cs`의 누락된 문자열 닫는 따옴표에 따른 `CS1010`·`CS1003`·`CS1026`이었다. 수정 후 원본에서 생성된 `StarterProject.PlayModeTests.csproj`와 `StarterProject.EditModeTests.csproj`를 `dotnet build --no-restore`로 각각 컴파일해 모두 경고·오류 0개를 확인했다. 열린 Unity Editor의 Console 표시는 다음 에셋 새로고침 전까지 이전 오류를 남길 수 있다. 별도 복제본 Unity 명령줄 테스트는 Licensing Client IPC 연결 실패가 반복되어 테스트 시작 전 중단했다. 이번 소스의 Unity Test Runner 통과나 Windows Player 빌드 성공은 아직 주장하지 않는다.

## 2026-09-24 최신 코드·깨끗한 Git 복제본 회귀 검증

권한이 허용된 명령줄 경로에서 Unity 6000.3.16f1의 Licensing Client 연결이 성공했다. `-runTests`에 `-quit`을 함께 전달하면 스크립트 컴파일 뒤 테스트 시작 전에 종료되어, Test Runner 실행에서는 `-quit`을 제거했다. 이전 라이선스 실패와 조기 종료는 **미실행 시도**로 남기고 아래 통과 결과와 구분한다.

| 대상 | 결과 | 증거 |
| --- | --- | --- |
| 최신 추적 소스가 원본과 SHA-256 일치하는 기존 검증 복제본 | EditMode 50/50·PlayMode 30/30, Windows 개발 빌드 성공 | `Logs/TemplateFinalValidation/Logs/PostFixEditMode.xml`, `PostFixPlayMode.xml`, `PostFixWindowsBuild.log` |
| `6a31e97`의 새 Git 복제본, 빈 Library에서 패키지·에셋 첫 가져오기 | EditMode 50/50·PlayMode 30/30, 실패·건너뜀 0개. Windows 개발 빌드 성공(169,602,028 bytes) | `Logs/TemplateLatestEditMode.xml`, `Logs/TemplateLatestPlayMode.xml`, `Logs/TemplateLatestWindowsBuild.log` |
| 새 복제본의 Product Name만 `StarterProjectTemplateProbe`로 변경 | 추가 Windows 개발 빌드 성공(169,602,070 bytes), `StarterProjectTemplateProbe.exe` 생성 | `Logs/TemplateLatestProductNameBuild.log` |

원본의 추적 파일 155개에 Library·Temp·Logs·UserSettings·Builds·IDE 생성 파일은 없고, 추적 에셋과 `.meta`의 누락 쌍도 없다. 원본 Editor 로그에는 오류 수정 후 새 C# 컴파일 실패가 없었다. Unity가 검증 복제본에서 재직렬화한 URP·프로젝트 설정과 생성 파일은 원본으로 복사하지 않았다. 테스트 빌드 산출물은 검증 후 정리하고 로그·XML은 `Logs/`에 남긴다. GUI Main 직접 Play, 화면·오디오·실제 입력, 모바일 기기, Unity Hub Cloud 연결 표시와 원격 배포는 계속 별도 확인 사항이다.

## 2026-10-02 UI 에셋 교체 경로 검증

예제 씬의 배경·버튼 `Image`는 Inspector에서 스프라이트를 연결할 수 있다. 로딩 화면은 Boot `StarterScreen`의 선택형 `StarterLoadingOverlay` 프리팹 슬롯으로 교체 가능하게 했고, 사용자 프리팹에서도 전환 중 포인터 입력 차단이 유지되도록 투명 막을 생성한다. 기존 기본 로딩 화면 경로는 그대로 유지한다.

최신 UI·테스트 소스를 기존 검증 복제본에 해시 일치로 반영한 뒤 Unity 6000.3.16f1 PlayMode **31/31**을 통과했다(`Logs/ReleaseCandidatePlayMode.xml`). 입력 차단 보완 후 해당 신규 테스트 **1/1**을 다시 통과했다(`Logs/LoadingPrefabPlayMode.xml`). 변경 직후 Windows 개발 빌드와 최종 입력 차단 보완 후 빌드도 성공했다(`Logs/ReleaseCandidateWindowsBuild.log`, `Logs/LoadingPrefabFinalWindowsBuild.log`). 이 빌드가 실행된 복제본의 Product Name은 검증용 `StarterProjectTemplateProbe`이며 원본 프로젝트의 제품명·Cloud ID 변경은 포함하지 않았다. EditMode 코드는 이번에 변경하지 않아 기존 깨끗한 복제본의 50/50 결과를 유지한다. 실제 화면 시각·터치·물리 입력 검증은 수행하지 않았다.

## 2026-10-02 원본 Editor Computer Use 부분 검증

원본 Unity 6000.3.16f1 Editor에서 저장된 `00_StartScene` 상태로 Play를 시작했고, Editor 창 제목과 로그에서 `01_Title` 로드를 확인했다. Play를 종료하자 `00_StartScene`으로 돌아왔다. 해당 실행 구간의 Editor 로그에는 예외·오류가 없었다. 파일 열기 대화상자는 취소했고 씬 파일을 수정하지 않았다.

Unity 창의 화면 캡처가 반복해서 `window capture timed out: timed out waiting on channel` 오류로 실패했다. 접근성 정보에는 Editor의 GameView 창만 있고 게임 안의 버튼·텍스트가 노출되지 않았다. Title의 키보드 입력으로 Main 전환을 확인하지 못했으며, Main 직접 Play·실제 화면 배치·클릭·터치·오디오 검증은 완료하지 않았다. 기존 자동 테스트와 Windows 빌드 결과의 범위를 넘겨 GUI 검증 완료로 판정하지 않는다.

추가로 같은 검증 복제본의 Windows Player를 다시 빌드했으며 성공했다(169,603,386 bytes, `Logs/ComputerUseWindowsBuild.log`). Computer Use에서 이 Player를 실행하려던 호출은 `Computer Use app approval timed out`으로 끝났고, Player 창이 없음을 확인했다. 따라서 Player GUI 실행 결과는 없다. 임시 빌드 산출물은 정리했다.

## 2026-10-02 Windows Player 네이티브 화면·UI 흐름 확인

Computer Use로 검증 Player를 실행했고, 창이 늦게 생성된 뒤 목록에서 확인됐다. Windows 창 캡처는 계속 시간 초과됐으므로, Git에서 제외된 검증 복제본에만 임시 진단 코드를 넣어 게임의 실제 렌더링 이미지를 생성했다. `Logs/ComputerUseNativeValidation/frame_02_01_Title_1920x1080.png`와 `Logs/ComputerUseNativeScenario/frame_03_02_MainScene_1920x1080.png`에서 Title·Main 문구·버튼 배치를 확인했다.

초기 진단 Player는 포커스가 있고 New Game이 선택된 Ready 상태였지만, Computer Use로 보낸 Enter·F8은 Unity 키 입력 로그에 나타나지 않았다. 물리 입력을 확인한 결과로 처리하지 않는다. 별도 Player 시나리오는 Unity 입력 시스템에 합성 키 상태를 주입해 New Game → Save Game → Back to Title → Continue를 실행했고, 저장 후 같은 SessionId로 Main에 복귀하는 조건을 통과했다. 임시 시나리오는 테스트용으로 background input을 허용했다.

이어진 설정 테스트는 아래 방향키의 선택 대상을 Volume으로 가정한 임시 테스트 자체의 오류로 중단됐다. 실제 선택은 Fullscreen이었다. 보정한 추가 시나리오 빌드는 성공했으나 `StarterProjectGuiScenario20261002` 실행에서 `Computer Use was not approved to use starterprojectguiscenario20261002` 응답을 받았다. 따라서 추가 화면 모드·960×540 네이티브 화면 검증은 완료하지 않았다. 제품 코드 실패로 기록하지 않는다.

빌드 로그: `Logs/ComputerUseRetryWindowsBuild.log`, `Logs/ComputerUseNativeWindowsBuild.log`, `Logs/ComputerUseNativeScenarioBuild.log`, `Logs/ComputerUseNativeScenarioFinalBuild.log`. 실제 입력 진단과 합성 입력 시나리오 기록은 각각 `Logs/ComputerUseNativeValidation/events.log`, `Logs/ComputerUseNativeScenario/events.log`에 있다. 원본 런타임 코드는 `61b5355` 이후 변경하지 않았다. [v1.0.0 배포 범위](RELEASE.md)

## 2026-10-02 배포 후 마무리

- GitHub Template 설정과 공개 v1.0.0 Release를 확인했다. 소스 ZIP·SHA-256·manifest의 업로드 상태·크기·digest를 검증했다. ZIP은 소스 커밋 `53db0d5`, 156개 파일이며 Cloud 식별자·Library·검증 코드·사용자 저장을 포함하지 않는다.
- 기존 테스트 빌드·임시 진단 코드·시험용 저장 폴더를 정리했고 Player 로그와 네이티브 화면·테스트 XML은 보관했다. 기록은 `Logs/ReleaseCleanup-v1.0.0.json`에 있다.
- 추가 마무리 요청 후 Unity Editor 창을 새로 선택해 캡처·복구를 각각 한 번 시도했지만 `FrameArrived timed out: timed out waiting on channel`와 `window capture timed out: timed out waiting on channel`로 실패했다. 이전 좌표나 접근성 인덱스로 클릭하지 않았다.
- 남은 화면 모드·960×540 확인을 위해 v1.0.0 ZIP의 소스를 기존 격리 복제본에 다시 반영하고 Product Name만 `StarterProjectGuiScenario20261002`로 분리했다. 임시 진단은 설정 버튼의 Unity 이벤트·실제 플랫폼 반영, 960×540 Title/Main 화면과 버튼 경계, 단일 AppRoot를 확인하도록 구성했다. Windows 개발 빌드는 성공했다(169,614,335 bytes, `Logs/FinalHandoffWindowsBuild.log`).
- 해당 Player의 Computer Use 실행은 다시 `Computer Use was not approved to use starterprojectguiscenario20261002`로 거부됐다. 따라서 이 추가 시나리오는 **미실행**이며 화면 모드·960×540 네이티브 표시를 새 통과 결과로 추가하지 않는다. 임시 진단은 원본과 배포 ZIP에 반영하지 않았다. 원본 런타임 코드는 변경하지 않았다.

새 게임의 플랫폼·장치 확인과 도구의 실행 권한 제한을 프리셋의 일반 기능 검증과 구분한다. 물리 키보드/게임패드·오디오 청취·모바일 실기기 결과는 아직 없다.

## 2026-10-02 Codex 재시작 후 GUI 검증

Computer Use 플러그인 26.930.21537에서 대상 Player 실행을 다시 시도했다. 허용 설정 반영·Codex 재시작 후 앱 권한 거부 없이 Player가 실행됐다. 첫 시나리오는 약 8초에 검사를 마치고 자동 종료하여 `launch_app`에는 대상 창을 찾지 못했다는 응답이 남았지만, 새 결과 파일·타임스탬프·이미지에서 실제 실행과 완료를 확인했다. 같은 실행을 중복으로 시작하지 않았다.

| 회차 | 실행과 관찰 | 판정·증거 |
| --- | --- | --- |
| 1 | Title 준비·AppRoot 1개, 음량 버튼의 Unity 이벤트 호출 후 설정값·AudioListener 반영, 전체화면 해제 | 통과. `Logs/FinalHandoffValidation/events.log` |
| 1 | 실제 Windows 창을 960×540으로 변경하고 Title → Main 이동, 화면 크기 유지·활성 버튼 경계 검사 | 통과. 같은 폴더의 `result.txt`가 PASS, Title·Main PNG 기록 |
| 1 | Main에서 전체화면 버튼 이벤트 호출 후 전체화면 상태·설정 반영, 버튼 경계·단일 AppRoot 재확인 | 통과. 전체화면 렌더링은 960×540. 1920×1080 복귀를 검증하지 않음 |
| 2 | 네이티브 960×540 Title·Main·전체화면 Main 이미지를 직접 열어 문구와 버튼 배치 확인 | 잘림·겹침 없이 표시. `title_960x540_960x540.png`, `main_960x540_960x540.png`, `main_fullscreen_960x540.png` |
| 2–3 | 원본 Editor와 대기형 Player의 Windows 창 캡처, 각각 새 창 선택 후 한 번 재시도 | 미완료. `FrameArrived timed out: timed out waiting on channel` / `window capture timed out: timed out waiting on channel` |
| 3 | 입력 주입·버튼 이벤트 호출 없는 관찰용 Player 실행, 접근성 포커스 확인 후 Computer Use Enter | 미확인. Ready·포커스 있음·Start Game 선택 상태였으나 Unity 키·클릭 로그와 Main 전환 없음 |

1회차 버튼 호출은 `Button.onClick.Invoke()`이며 실제 OS 클릭이 아니다. 크기 변경은 임시 시나리오의 `Screen.SetResolution(960, 540, Windowed)`다. PNG는 Unity `ScreenCapture`로 기록한 실제 게임 렌더링이며 Windows 창 캡처 결과와 구분한다. 시나리오의 런타임 오류 기록은 없었다.

3회차 관찰용 빌드는 성공했다(169,610,584 bytes, `Logs/FinalComputerUseInputWindowsBuild.log`). `Logs/FinalComputerUseInputValidation/events.log`에는 씬·선택·화면 크기·포커스·실제 수신한 키와 버튼 이벤트만 관찰하도록 구성했다. 자동 입력이나 UI 동작은 실행하지 않았다. Computer Use Enter의 수신을 확인하지 못했으므로 반복 입력·추측 좌표 클릭으로 통과 판정을 만들지 않았다. 대상 Player를 닫고 로그·화면·관찰 소스를 보관했다. 사용이 끝난 시험용 빌드·임시 소스·시험용 저장의 삭제는 경로 확인 후 명시적 대상 경로로도 시도했지만 자동 승인 검사에서 `blocked by policy`로 거절됐다. 상세 사유는 제공되지 않아 삭제를 멈췄으며 이번 산출물은 남아 있다. `Logs/FinalComputerUseCleanup-20261002.json`에 정리 보류 상태를 기록한다.

이 회차에서 제품 런타임 코드는 변경하지 않았다. Summary·배포 기준만 결과에 맞게 보완하고 v1.0.0 태그·ZIP은 유지한다. OS 마우스·키보드, 물리 게임패드·오디오 청취·모바일 실기기는 별도 미확인이다.

## 2026-10-02 후속 테스트 자료 정리

사용자가 기존 시험용 자료 중 불필요한 자료의 삭제를 요청했다. 검증 폴더 5개의 범위·용량·링크 유무, 원본 Editor와 자식 프로세스만 실행 중인 상태, 관리형 worktree 첨부가 없는 상태를 확인했다. 각 삭제 경로는 프로젝트의 `Logs` 안이거나 특정 시험용 제품명의 저장 폴더였다.

검증 복제본의 필요한 로그·결과 44개를 `Logs/ArchivedValidation/<검증 폴더>/`에 복사하고 SHA-256 일치를 확인했다. Player 로그·결과 2개도 같은 보관 폴더의 `PlayerData/` 아래에 추가했다. 합계 46개, 911,320 bytes다. 이전 기록의 복제본 내부 로그 경로는 이 보관 폴더에서 같은 상대 경로로 확인한다. 최종 GUI 화면·입력 기록과 루트의 테스트 XML·빌드 로그는 기존 경로에 있다.

일반 삭제 방식으로 검증 복제본의 캐시·빌드·소스 복사본·임시 진단을 정리했다. 시험용 저장 폴더 3개와 임시 커밋 본문·백업·상태·안내 파일 21개도 삭제했다. 새 증거 보관분을 제외한 파일 크기 기준 정리 용량은 약 9.303GB다. `Builds/Template/StarterProject-v1.0.0.zip`의 SHA-256은 `B32A4F070EAD6CC555C00722560F087B3B54A29C1890F54FE1C9D44B57F8D4FA`로 유지됐고 실제 프로젝트 사용자 설정과 소스는 정리 대상에서 제외했다.

검증 폴더 `TemplateCloneValidation`, `TemplateFinalValidation`, `TemplateLatestValidation`에는 숨김 `.git`만 남아 있다. 합계 1,768,408 bytes이며 첫 폴더의 `.git`은 비어 있다. 해당 메타데이터의 강제 삭제 명령은 자동 승인 검사에서 `blocked by policy`로 거절돼 중단했다. 상세 사유는 제공되지 않았다. 원본 저장소의 `.git`을 삭제한 것은 아니다. 최신 정리 결과·보관 위치·잔여 경로는 `Logs/TestMaterialCleanup-20261002.json`에 기록했다.
