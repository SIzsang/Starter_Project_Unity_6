# 설정·게임 저장 사용 가이드 — 3·4단계

## 실행

1. `Assets/01_Scenes/Boot/00_StartScene.unity`에서 Play한다.
2. Title의 `Volume`, `Fullscreen`, `Language` 버튼으로 설정 값을 변경한다. 클릭마다 변경을 저장하며 다음 실행에서 복원한다.
3. `New Game`으로 새 세션을 시작하고 Main의 `Save Game`으로 저장한다.
4. `Back to Title` → `Continue`로 저장된 세션을 복원한다. Play를 종료하고 Boot에서 다시 시작해도 같은 세션 ID를 확인할 수 있다.
5. 다른 세션의 저장이 있으면 `Save Game` → `Confirm Replace Save`로 명시적으로 교체한다. `Cancel`은 확인 요청을 취소한다.

새 게임을 시작하거나 Title로 돌아가는 것만으로는 저장하지 않는다. Title로 돌아가면 저장하지 않은 세션 변경은 폐기된다. 예제 진행 데이터는 세션 ID·생성/저장 시각과 빈 JSON payload이며, 실제 게임 규칙은 포함하지 않는다.

설정 버튼은 현재 저장 값의 예제 UI다. 실제 오디오·화면·번역 적용은 로드맵 5단계에서 연결한다. 지원 언어 코드는 `en`, `ko`이며 UI 자체는 현재 영어다.

## 기본값과 소유권

- `AppConfig.defaultSettings`: SO에 보관하는 음량·전체화면·언어 기본값. 필수 기본값이 잘못되면 Boot 실패로 처리한다.
- `SettingsService.Current`: 독립 복사본. 수정한 복사본을 `AppRoot.TrySaveSettings`에 전달해 변경을 확정한다. 쓰기가 실패하면 기존 실행 설정을 유지한다.
- `GameSessionService.Current`: 변경 불가능한 세션 스냅샷. 새 게임·이어하기 이후에만 존재하며 Title 복귀 시 해제한다.
- `AppRoot`: 설정·저장 서비스를 초기화한 뒤 Ready를 공개한다. 파일 오류는 안내하고 복구 가능한 범위에서 Title에 진입한다.

사용자 JSON은 사용자 옵션 세 필드만 덮어쓸 수 있다. 씬 경로나 Unity 에셋 참조는 덮어쓰지 않는다. 누락 필드는 SO 기본값을 유지하고 `0`과 `false`는 유효한 값으로 적용한다. 잘못된 자료형·음량 범위·언어 코드는 해당 필드만 기본값으로 복구하고 안내한다.

필드 부재·자료형·중복 키 검사를 위해 Unity 공식 `com.unity.nuget.newtonsoft-json` 3.2.2를 명시적으로 사용한다. 자동 타입 생성 없이 `JObject`의 허용 필드만 읽는다. [Unity 패키지 문서](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)

## 파일과 버전

실제 폴더는 `Path.Combine(Application.persistentDataPath, "StarterData")`다. 회사명·제품명에 따라 달라지므로 새 게임 복제 시 저장 경로가 겹치지 않는지 확인한다.

| 파일 | 형식 | 용도 |
| --- | --- | --- |
| `settings.json` | schemaVersion 1 | masterVolume, fullscreen, language |
| `save-slot-1.json` | schemaVersion 2 | sessionId, createdUtc, savedUtc, payloadVersion, payload |
| `*.json.bak` | 직전 유효 주 파일 | 한 세대 백업 |
| `*.json.preserved-<GUID>` | 교체 전 원본 그대로 | 파손 파일 복구·설정 재저장 시 원본 보존 |
| `*.json.<GUID>.tmp` | 저장 중 임시 파일 | 검증·교체 후 정리 |

게임 저장 v1은 동일한 메타데이터와 `data` 객체를 사용한다. 읽을 때 v2의 payload로 메모리 변환하며, 다음 명시적 저장 시 v2로 쓰고 v1 원본을 백업한다. v1은 마이그레이션 검증용 지원 형식이며 이전 출시 버전이 있다는 뜻은 아니다. 설정 v1 이전 형식은 지원하지 않는다.

주 파일의 알 수 없는 과거/미래 버전 및 게임 payloadVersion 불일치는 보호 상태다. 이 경우 과거 백업을 자동 선택하지 않으며 새 게임을 시작하더라도 저장을 덮어쓰지 않는다.

## 오류·복구

| 상황 | 동작 |
| --- | --- |
| 주 파일·백업 없음 | 설정 기본값, Continue 비활성. 디스크에 자동 생성하지 않음 |
| 설정 일부 값 오류 | 잘못된 필드만 기본값, 원본은 명시적 저장 전까지 유지 |
| 설정 JSON 파손, 유효 백업 없음 | 기본값으로 시작. 사용자가 설정을 저장하면 원본을 preserved 파일로 보존 |
| 게임 JSON 파손, 유효 백업 없음 | Continue와 저장 실패. 새 게임은 메모리에서만 진행 가능 |
| 주 파일 파손/누락, 유효 백업 있음 | 백업을 메모리로 읽고 안내. `Recover Game Backup` 또는 `Recover Settings Backup`으로 복구 확정 |
| 지원하지 않는 버전 | 원본 보호, 해당 저장 거절. 설정은 기본값으로 시작 |
| 권한·잠금·용량·교체 실패 | 실패 결과를 반환. 저장 성공으로 표시하지 않음 |

백업 복구 시 손상 원본과 기존 `.bak`을 보존한다. 임시 파일은 자동 복구 후보로 사용하지 않는다. 파손된 게임 파일에 유효 백업이 없을 때 파일 제거·초기화는 이번 UI에 포함하지 않는다. 사용자가 원본을 별도로 보관하고 문제 파일을 이동한 뒤 앱을 다시 실행할 수 있다. 초기화 도구는 6단계다.

저장 순서: JSON 생성·검증 → 같은 폴더에 임시 파일 쓰기·Flush → 임시 파일 재검증 → 기존 파일을 백업하며 교체. [File.Replace 문서](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace)

첫 파일은 이동으로 확정하고 기존 파일은 `File.Replace`를 사용한다. 교체가 실패했다고 원본을 먼저 삭제하는 우회는 하지 않는다. 정전 시 내구성 및 모든 플랫폼에서의 원자성을 보장하는 구현은 아니다. Windows 로컬 파일을 첫 검증 대상으로 한다.

## 게임별 확장

`GameSessionService`는 게임 규칙을 모르는 저장 기반이다. `Gameplay`에서 자신의 모델을 JSON 객체로 만들고 `StartNew(initialPayloadJson)` / `TrySave(payloadJson)`에 전달한다. 생성자의 `payloadVersion`과 `payloadValidator`를 게임에 맞게 지정한다. 검증기는 잘못된 데이터에 `JsonException` 또는 `InvalidDataException`을 던지도록 작성한다. `AppRoot.Initialize`에서 해당 구성을 연결하고 `Current.PayloadJson`을 게임 상태로 복원하는 단계도 추가한다.

현재는 단일 슬롯·단일 앱 소유권·메인 스레드 호출을 전제로 한다. 파일 읽기/쓰기는 동기이며 한 파일 최대 1 MiB, JSON 깊이 최대 32다. 동일 저장소/리포지토리 내부 작업은 잠금으로 직렬화하지만 여러 앱 프로세스가 같은 경로를 공유하는 동시 편집은 지원하지 않는다. 대규모 데이터·자동 저장·다중 슬롯·클라우드 저장은 요구가 생기면 추가한다.

`ITextFileStore` 경계로 실패를 주입할 수 있고, `AppRoot.ConfigureStorage`는 Begin 전에 테스트 저장소를 전달할 때 사용한다. 테스트는 실제 사용자 저장 폴더를 사용하지 않는다.

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
