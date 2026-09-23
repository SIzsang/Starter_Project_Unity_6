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

`CREATE_PASS`와 `RESUME_PASS`는 **서로 다른 Windows Player 프로세스**에서 생성했다. 저장 위치는 검증 전용 제품명 아래의 `Application.persistentDataPath/StarterData`다. `ERROR_PASS`는 파손 파일 준비와 검증을 또 다른 두 프로세스로 실행했고, Direct3D 11 경로에서도 한 번 더 확인했다. 이어 별도 두 프로세스로 정상 백업을 준비·복구해 `BACKUP_PASS`를 확인했다. 완료 직후 강제 종료와 재실행도 다른 프로세스에서 확인했다. 저장 도중 종료 실험에서는 복제본의 `JsonFileStore`에만 임시 파일 flush 직후 일시 정지 지점을 넣고 외부에서 해당 Player를 종료했다. 남은 `.tmp` 파일은 교체되지 않은 새 payload를 담고 있으며 정상 주 파일·백업은 이전 payload를 유지했다. 원본 템플릿 코드에는 이 지연 지점이나 일회성 런타임 검사 코드를 넣지 않았다. 이 검증용 빌드는 배포물로 사용하지 않는다.

## 남은 확인

1. 원본 Editor GUI에서 Main 직접 Play 두 번, 미저장 씬·기존 시작 씬 설정과 메뉴 확인·취소를 눈으로 확인한다. 복제본 자동 검증 결과와 구분한다.
2. 잘못된 AppConfig·입력 참조의 Console 안내, Reset Test Data의 백업 경로·파일과 취소 동작을 GUI에서 확인한다.
3. Windows 창/전체화면 전환, 실제 오디오 출력, 키보드·게임패드 선택·확인·취소를 화면과 장치에서 확인한다.
4. 일반 GUI 실행에서 저장 도중 강제 종료와 재시작을 확인한다. 현재 자동 검증은 계측 복제본의 **임시 파일 flush 후·주 파일 교체 전** 한 지점을 다루며, 전원 손실·파일 시스템별 동작이나 교체 도중의 모든 중단 시점을 증명하지 않는다. 이 실험에서 중단된 `.tmp` 파일은 남으므로 장기 사용 시 정리 정책은 후속 검토가 필요하다.

첫 지원 환경의 일반 실행과 화면 품질까지 확인한 뒤 7단계를 완료로 기록한다. 현재 자동 검증은 Windows 빌드의 시작·저장·재실행 복원 및 주요 오류 보호가 동작한다는 범위로 해석한다.

설정 저장의 플랫폼 적용 실패가 새 JSON을 남기지 않도록 보완한 근거와 SOLID 책임 경계는 [설계 점검](ARCHITECTURE_REVIEW.md)에 기록한다. 회귀 빌드에서는 검증 복제본의 `productName`을 `StarterProjectSolidValidation`으로 바꿔 이전 Stage7 저장과 경로를 분리했다. 검증 스크립트와 제품명 변경은 원본 프로젝트에 포함하지 않는다.
