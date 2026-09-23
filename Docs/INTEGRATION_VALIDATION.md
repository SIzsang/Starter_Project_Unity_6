# 7단계 통합·Windows 빌드 검증

상태: 2026-09-23 진행 중. 아래 자동 검증은 통과했으며 GUI 직접 조작·기기 확인은 남아 있다. 로드맵의 7단계 완료 판정과 95% 반영은 보류한다.

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

## 남은 확인

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
