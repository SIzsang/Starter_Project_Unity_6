# 과거 기록 — SETTINGS_AND_SAVE

> 과거 기록입니다. 당시 제안·대기 상태·검증 결과를 보존하며 현재 사양은 [문서 안내](../INDEX.md)를 따릅니다.

## 코드 명명 규칙

2026-09-22: 3·4단계 코드의 이름을 역할이 드러나는 C#/Unity 용어로 정리했다. 타입·메서드·프로퍼티는 `PascalCase`, 지역 변수·매개변수·기존 private 필드는 `camelCase`를 사용한다. 모든 Unity 팀에 하나의 명명 표준이 있는 것은 아니므로, 기존 프로젝트 표기법을 유지하면서 의미가 불분명한 이름을 구체화했다.

| 이전 이름 | 현재 이름 | 의미 |
| --- | --- | --- |
| `Encode` / `Decode` | `Serialize` / `Deserialize` | C# 객체 → JSON 문자열 / JSON 문자열 → C# 객체 |
| `decode` / `encode` | `deserialize` / `serialize` | 저장소에 전달하는 JSON 변환 함수 |
| 파일 저장소의 `Read` / `Write` | `ReadAllText` / `WriteAllText` | 파일 전체 텍스트 읽기 / 쓰기 |
| `JsonData.Parse` / `JsonData.Version` | `JsonData.ParseObject` / `JsonData.GetSchemaVersion` | JSON 객체 해석 / 저장 형식 버전 확인 |
| `JsonRepository.TryRecover` | `JsonRepository.TryRecoverBackup` | 백업 복구 시도 |
| `MaxBytes` | `MaxFileSizeBytes` | 허용 파일 크기, 바이트 단위 |
| `directory` / `temporary` / `backup` | `directoryPath` / `tempFilePath` / `backupFilePath` | 폴더 / 임시 파일 / 백업 파일의 경로 |
| `gate` | `syncRoot` | 같은 객체 안에서 파일 작업 순서를 보호하는 잠금 객체 |
| `defaults` / `current` | `defaultSettings` / `currentSettings` | 기본 설정 / 현재 적용된 설정 |
| `invalid` | `hasInvalidFields` | 잘못된 설정 필드가 있는지 나타내는 bool |
| `volume` (JSON 값) | `volumeToken` | JSON에서 읽은 값; 검사 후 숫자 `volume`으로 변환 |
| `saved` / `candidate` | `savedGameResult` / `sessionToSave` | 저장 게임 로드 결과 / 이번에 저장할 세션 |
| `validatePayload` / `initialPayload` | `payloadValidator` / `initialPayloadJson` | 게임 데이터 검사 함수 / 새 게임의 초기 JSON |
| `lifetime` / `operation` | `lifetimeToken` / `loadOperation` | 앱 수명 취소 토큰 / 진행 중인 씬 로드 작업 |
| `confirmReplacement` | `isAwaitingOverwriteConfirmation` | 저장 덮어쓰기 확인을 기다리는지 나타내는 bool |
| `OnAction` 등 | `OnActionButtonClicked` 등 | 어떤 버튼의 클릭에 반응하는지 나타내는 이벤트 함수 |

`JsonData.ParseObject`는 JSON 문자열을 `JObject`로 읽는 저수준 작업이고, 각 서비스의 `Deserialize`는 필드·버전을 검사하고 `UserSettings` 또는 `GameSession`으로 복원하는 작업이다. 두 단계의 역할을 이름으로 구분한다.

씬과 SO가 참조하는 직렬화 필드, JSON 키(`masterVolume`, `sessionId`, `payload` 등), 저장 파일명·경로·버전은 유지했다. 기존 저장 파일은 그대로 읽을 수 있다. `ITextFileStore`의 별도 구현이나 생성자의 명명된 인자를 사용하는 외부 코드가 있다면 위 변경된 API 이름을 적용해야 한다.

## 검증

2026-10-05 슬롯·Title 메뉴 변경: **구현·필수 자동 검증 완료**. 관련 EditMode 77/77 통과, PlayMode 39개 항목 중 초기 실패 4개 수정 후 영향받는 8/8 재검사 통과. 상세 근거는 [통합 검증 기록](../INTEGRATION_VALIDATION.md)을 따른다. 아래 기존 날짜의 테스트 수치를 이번 3슬롯 변경의 실행 결과로 재사용하지 않는다.

2026-09-22 명칭 정리 후: Core·UI·Editor·EditModeTests·PlayModeTests 어셈블리 컴파일 성공. 현재 소스를 `Logs/Stage34Validation`에 반영한 복제 프로젝트에서 Unity 6000.3.16f1로 **EditMode 30개 + PlayMode 14개 통과, 실패·건너뜀 0개**를 다시 확인했다. 결과는 `Logs/NamingEditMode.xml`, `Logs/NamingPlayMode.xml`이며, 기존 테스트 시나리오를 유지하고 변경된 메서드·매개변수 이름과 테스트 헬퍼만 갱신했다. 원본 Editor의 Play 실행이나 사용자 저장 파일을 통한 검증은 수행하지 않았다.

2026-09-10, Unity 6000.3.16f1: **EditMode 30개 + PlayMode 14개 통과, 실패·건너뜀 0개**.

- 기본값 복사와 SO 보호, 누락 필드·0·false·잘못된 자료형 처리, 사용자 설정 재생성 후 복원
- JSON 파손·중복 키·지원하지 않는 큰 버전 값, 설정 원본 보존과 백업 복구
- 새 세션의 기존 저장 보호, 교체 확인·취소 UI, 재시작 후 Continue 및 payload 복원
- 게임 v1→v2 메모리 변환과 원본 백업, 미래 파일·다른 게임 데이터 버전 보호
- 디스크 실패 주입, 실제 파일 잠금, 임시 파일 검증 실패, 크기 제한·잘못된 경로 처리
- 기존 Bootstrap 실패·중복·취소·씬 왕복·입력 도구 회귀 테스트

열려 있는 Unity 작업을 유지하기 위해 `Logs/Stage34Validation`에 Assets·Packages·ProjectSettings를 복사하고 새 Library에서 패키지를 복원해 검증했다. 생성한 예제 씬·새 meta·패키지 잠금 파일을 원본 프로젝트에 반영했다. 테스트 결과는 `Logs/Stage34EditMode.xml`, `Logs/Stage34PlayMode.xml`이다. 실제 사용자 저장은 건드리지 않고 임시 디렉터리를 사용한다.

이 테스트의 재시작 검증은 서비스·AppRoot 재생성이다. 전체 배포 앱 종료·재실행, 다양한 기기, 정전·강제 종료 내구성 검증은 7단계에 남긴다.

Windows x64 Development 빌드 생성 성공: `Builds/Stage34/Windows/StarterProject.exe` (2026-09-10). 빌드 로그는 `Logs/Stage34WindowsBuild.log`다. 이 결과는 빌드 생성 검증이며 배포 실행 검증은 아니다.

## 원본 에디터 패키지 복원 오류 확인

2026-09-10: 열려 있던 원본 에디터에서 `Newtonsoft`/`JObject`를 찾을 수 없다는 CS0246 오류가 발생했다. manifest와 lock에는 의존성이 있었지만 원본 Library의 패키지 해석 결과와 PackageCache에는 JSON 패키지가 없었다. 복제본의 테스트 통과만으로 원본 에디터의 패키지 반영까지 확인할 수 없었던 사례다.

manifest를 다시 기록하고 새 JSON 패키지의 lock 항목만 재생성하도록 해 원본 에디터에서 의존성을 다시 해석했다. 이후 PackageCache에 Newtonsoft JSON 3.2.2가 생성되고 원본 Core 어셈블리 컴파일 성공을 확인했다. 이후에는 패키지 변경 시 원본 에디터의 패키지 복원·컴파일 결과도 확인한다.

원본 에디터에서도 **EditMode 30개·PlayMode 14개 전부 통과, 실패·건너뜀 0개**를 확인했다. 결과는 `Logs/OriginalEditMode.xml`, `Logs/OriginalPlayMode.xml`이다. 열려 있던 Boot 씬은 기존 디스크 파일을 `Logs/BootBeforeLocalTests.unity`에 백업한 뒤 미저장 변경을 저장하고 테스트했다. 임시 TestRunner API 실행 도구는 검증 후 제거했다.

임시 도구 제거 후에도 컴파일 성공을 확인했으며, 일반 Play 단축키로 Boot → Title 진입을 직접 확인한 뒤 Play를 종료했다.
