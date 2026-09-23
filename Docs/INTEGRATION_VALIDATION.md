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
| 그래픽 장치 경로 | Direct3D 11, NVIDIA GeForce GTX 1660 SUPER에서 오류 흐름 검증 프로세스 정상 종료 | 복제본 `Builds/Windows/Stage7PlayerGraphics.log` |

`CREATE_PASS`와 `RESUME_PASS`는 **서로 다른 Windows Player 프로세스**에서 생성했다. 저장 위치는 검증 전용 제품명 아래의 `Application.persistentDataPath/StarterData`다. `ERROR_PASS`는 파손 파일 준비와 검증을 또 다른 두 프로세스로 실행했고, Direct3D 11 경로에서도 한 번 더 확인했다. 빌드에만 넣은 일회성 스크립트가 AppRoot API와 씬 상태를 확인했다. 자동 검증용 빌드이므로 템플릿 배포물로 사용하지 않는다.

## 남은 확인

1. 원본 Editor GUI에서 Main 직접 Play 두 번, 미저장 씬·기존 시작 씬 설정과 메뉴 확인·취소를 눈으로 확인한다. 복제본 자동 검증 결과와 구분한다.
2. 잘못된 AppConfig·입력 참조의 Console 안내, Reset Test Data의 백업 경로·파일과 취소 동작을 GUI에서 확인한다.
3. Windows 창/전체화면 전환, 실제 오디오 출력, 키보드·게임패드 선택·확인·취소를 화면과 장치에서 확인한다.
4. 정상 빌드에서 강제 종료 중 저장 파일·백업 보존과 복구 시나리오를 확인한다. 기존 자동 테스트는 파일 쓰기 실패를 서비스 경계에서 다루지만 프로세스 강제 종료 내구성을 대체하지 않는다.

첫 지원 환경의 일반 실행과 화면 품질까지 확인한 뒤 7단계를 완료로 기록한다. 현재 자동 검증은 Windows 빌드의 시작·저장·재실행 복원 및 주요 오류 보호가 동작한다는 범위로 해석한다.
