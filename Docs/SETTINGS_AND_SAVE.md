# 설정·게임 저장 사용 가이드

## 이 문서의 순서

- [기본 사용자 흐름](#기본-사용자-흐름)
- [슬롯 API와 호환성](#슬롯-api와-호환성)
- [기본값과 소유권](#기본값과-소유권)
- [파일과 버전](#파일과-버전)
- [오류·복구](#오류복구)
- [게임별 확장](#게임별-확장)
- [슬롯 삭제와 대체 저장소](#슬롯-삭제와-대체-저장소)
- [검증과 이전 기록](#검증과-이전-기록)

## 기본 사용자 흐름

상태: **3개 독립 슬롯·메인 메뉴 구현·필수 자동 검증 완료**. 아래 사용법은 이번 후속 소스 기준이며 기존 v1.0.0 ZIP은 단일 슬롯 버전이다. 과거 테스트 수치는 아래 날짜별 기록으로 보존한다.

1. `Assets/01_Scenes/Boot/00_StartScene.unity`에서 Play한다.
2. 좌측 메인 메뉴의 `New Game`(게임 시작)은 빈 슬롯 선택 화면을 연다. 슬롯을 선택한 뒤 Main으로 진입한다. 사용 중인 슬롯을 누르는 것만으로 새 진행을 덮어쓰지 않는다.
3. Main의 `Save Game`은 현재 활성 슬롯에 저장한다. `Back to Title`로 돌아와 `Continue` → 저장 슬롯을 선택하면 해당 세션을 복원한다.
4. 세 슬롯이 모두 비어 있으면 `Continue`는 비활성화된다. 모두 사용 중이면 새 게임 화면에서 `Slot Management`로 들어가 정리할 슬롯을 선택한다.
5. 슬롯 관리의 `Delete`는 확인창을 연다. 대상 번호·마지막 저장 시각을 표시하고 기본 선택은 `Cancel`이다. 확인해야 해당 슬롯과 복구용 파일을 삭제하며 취소·성공 뒤 같은 슬롯 카드에 선택을 복원한다. `Recover`는 해당 슬롯의 복구 가능한 백업을 명시적으로 복원한다.
6. `Options`에서 `Volume`, `Fullscreen`, `Language`를 바꾸고 `Credits`에서 제작진 정보를 확인한다. `Quit`는 PC 메뉴다. 설정은 모든 슬롯이 함께 사용하는 `settings.json`에 저장한다.

새 게임을 시작하거나 Title로 돌아가는 것만으로는 저장하지 않는다. 첫 명시적 저장 전에 종료하면 선택했던 빈 슬롯에는 저장 파일이 생기지 않는다. Title 복귀 시 저장하지 않은 세션 변경은 폐기된다. 예제 진행 데이터는 세션 ID·생성/저장 시각과 빈 JSON payload이며 실제 게임 규칙은 포함하지 않는다.

기본 UI는 `en`/`ko`를 전환하지만 Core는 1~64자이며 공백·제어 문자가 없는 언어 식별자를 저장한다. 지원 언어 목록·번역 콘텐츠는 게임이 정하며 예제 UI는 영어다. 음량·전체화면의 적용과 실패 복원 계약은 [공통 기능 가이드](COMMON_SERVICES.md)를 따른다.

## 슬롯 API와 호환성

기본은 **서로 독립된 슬롯 3개와 동시에 하나의 활성 세션**이다. 세 슬롯에 동일 진행의 시점별 복사본을 임의로 저장하는 Save As 기능은 제공하지 않는다.

| 공개 상태/API | 계약 |
| --- | --- |
| `Game.Current`, `CurrentSlotId` | 현재 진행 스냅샷과 슬롯 번호. 활성 진행이 없으면 둘 다 null이며 Title 복귀 시 해제한다. |
| `Game.Slots`, `GetSlot(id)` | 읽기 전용 목록과 불변 `SaveSlotInfo`. 번호·상태·비어 있음·이어하기 가능·마지막 저장 시각을 제공한다. 갱신 시 새 목록을 만들므로 상태 알림 뒤 다시 조회한다. |
| `AnyCanContinue`, `CanDelete` | 전체 슬롯 중 이어갈 데이터가 있는지, 저장소가 삭제 capability를 지원하는지 나타낸다. |
| `AppRoot.TryStartNewGame(id)` | Title에서 빈 슬롯을 다시 확인한 뒤 시작한다. 사용 중·손상·호환 불가 슬롯을 빈 슬롯으로 취급하지 않는다. |
| `AppRoot.TryContinueGame(id)` | Title에서 지정 슬롯을 읽어 Main으로 이동한다. 실패하면 다른 진행을 활성화하지 않는다. |
| `AppRoot.TrySaveGame(payloadJson)` | Main·추가 Gameplay 씬의 현재 활성 슬롯만 저장한다. Boot·Title·전환 중에는 차단한다. |
| `AppRoot.TryRefreshGameSlots()` | Title에서 슬롯 목록과 상태 메시지를 다시 읽고 알린다. |
| `TryDeleteGameSlot(id)`, `TryRecoverGameBackup(id)` | Title이며 활성 진행이 없을 때만 삭제·명시적 복구를 요청한다. 삭제 확인 UI는 호출 전에 대상 슬롯을 고정한다. |

`GameSessionService` 생성자의 `slotCount`는 1~10이며 기본값은 3이다. AppRoot의 기본 조립과 예제 Title UI는 3개에 맞춰져 있다. 개수를 바꿀 때는 서비스 구성·UI 참조 배열·예제 검사도 함께 맞춰야 하며 숫자 하나로 화면이 자동 확장되는 계약은 아니다.

기존 슬롯 없는 `StartNew()`/`TryContinue()`와 AppRoot의 `TryStartNewGame()`/`TryContinueGame()`은 슬롯 1 호환 경로다. 슬롯을 지정하지 않는 `TryRecoverBackup()`은 활성 슬롯, 활성 진행이 없으면 슬롯 1을 사용한다. 기존 `Status`·`CanContinue`도 활성 슬롯 또는 슬롯 1의 상태다. 새 UI는 전체 목록·명시적 슬롯 API를 사용한다. 낮은 수준의 `Game.StartNew(id)`와 기존 호환 경로는 새 메모리 세션을 만들 수 있지만, 기존 파일은 실제 저장과 교체 확인 전까지 유지한다. 새 메뉴의 빈 슬롯 제한은 AppRoot의 명시적 슬롯 시작 경로가 적용한다.

## 기본값과 소유권

- `AppConfig.defaultSettings`: SO에 보관하는 음량·전체화면·언어 기본값. 필수 기본값이 잘못되면 Boot 실패로 처리한다.
- `SettingsService.Current`: 독립 복사본. 수정한 복사본을 `AppRoot.TrySaveSettings`에 전달해 변경을 확정한다. 쓰기가 실패하면 기존 실행 설정을 유지한다.
- `GameSessionService.Current`와 `CurrentSlotId`: 변경 불가능한 세션 스냅샷과 활성 슬롯. 새 게임·이어하기 이후에만 존재하며 Title 복귀 시 함께 해제한다.
- `AppRoot`: 설정·저장 서비스를 초기화한 뒤 Ready를 공개한다. 파일 오류는 안내하고 복구 가능한 범위에서 Title에 진입한다.

사용자 JSON은 사용자 옵션 세 필드만 덮어쓸 수 있다. 씬 경로나 Unity 에셋 참조는 덮어쓰지 않는다. 누락 필드는 SO 기본값을 유지하고 `0`과 `false`는 유효한 값으로 적용한다. 잘못된 자료형·음량 범위·언어 코드는 해당 필드만 기본값으로 복구하고 안내한다.

필드 부재·자료형·중복 키 검사를 위해 Unity 공식 `com.unity.nuget.newtonsoft-json` 3.2.2를 명시적으로 사용한다. 자동 타입 생성 없이 `JObject`의 허용 필드만 읽는다. [Unity 패키지 문서](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)

## 파일과 버전

실제 폴더는 `Path.Combine(Application.persistentDataPath, "StarterData")`다. 회사명·제품명에 따라 달라지므로 새 게임 복제 시 저장 경로가 겹치지 않는지 확인한다.

| 파일 | 형식 | 용도 |
| --- | --- | --- |
| `settings.json` | schemaVersion 1 | masterVolume, fullscreen, language |
| `save-slot-1.json` ~ `save-slot-3.json` | schemaVersion 2 | 슬롯별 sessionId, createdUtc, savedUtc, payloadVersion, payload. 기존 슬롯 1 파일은 그대로 사용 |
| `*.json.bak` | 직전 유효 주 파일 | 한 세대 백업 |
| `*.json.preserved-<GUID>` | 교체 전 원본 그대로 | 파손 파일 복구·설정 재저장 시 원본 보존 |
| `*.json.<GUID>.tmp` | 저장 중 임시 파일 | 검증·교체 후 정리 |

게임 저장 v1은 동일한 메타데이터와 `data` 객체를 사용한다. 읽을 때 v2의 payload로 메모리 변환하며, 다음 명시적 저장 시 v2로 쓰고 v1 원본을 백업한다. v1은 마이그레이션 검증용 지원 형식이며 이전 출시 버전이 있다는 뜻은 아니다. 설정 v1 이전 형식은 지원하지 않는다.

슬롯 추가로 기존 슬롯 1의 파일명·JSON 형식·백업을 옮기거나 버전을 올리지 않는다. 슬롯 2·3이 없으면 빈 슬롯으로 시작한다. 회사명·제품명·저장 경로가 같아야 기존 파일을 이어 읽는다.

주 파일의 알 수 없는 과거/미래 버전 및 게임 payloadVersion 불일치는 보호 상태다. 이 경우 과거 백업을 자동 선택하지 않으며 새 게임을 시작하더라도 저장을 덮어쓰지 않는다.

## 오류·복구

| 상황 | 동작 |
| --- | --- |
| 주 파일·백업 없음 | 설정은 기본값, 해당 슬롯은 비어 있음. 모든 슬롯에 이어갈 데이터가 없으면 Continue 비활성. 디스크에 자동 생성하지 않음 |
| 설정 일부 값 오류 | 잘못된 필드만 기본값, 원본은 명시적 저장 전까지 유지 |
| 설정 JSON 파손, 유효 백업 없음 | 기본값으로 시작. 사용자가 설정을 저장하면 원본을 preserved 파일로 보존 |
| 게임 JSON 파손, 유효 백업 없음 | 해당 슬롯의 Continue와 저장 차단. 다른 빈 슬롯은 새 게임 가능. 문제 슬롯은 관리 화면에서 명시적으로 삭제 가능 |
| 주 파일 파손/누락, 유효 백업 있음 | 백업을 메모리로 읽고 안내. 게임은 Title의 슬롯 관리, 설정은 Options의 복구 버튼으로 복구 확정 |
| 지원하지 않는 버전 | 원본 보호, 해당 저장 거절. 설정은 기본값으로 시작 |
| 권한·잠금·용량·교체 실패 | 실패 결과를 반환. 저장 성공으로 표시하지 않음 |

백업 복구 시 손상 원본과 기존 `.bak`을 보존한다. 임시 파일은 자동 복구 후보로 사용하지 않는다. Recovered 슬롯은 백업을 메모리로 읽어 이어갈 수 있지만, 명시적으로 백업을 복구하기 전에는 저장하지 않는다. Title의 슬롯 관리에서 해당 슬롯의 Recover를 사용한다. 슬롯 하나의 오류는 다른 정상 슬롯의 사용을 막지 않는다.

저장 순서: JSON 생성·검증 → 같은 폴더에 임시 파일 쓰기·Flush → 임시 파일 재검증 → 기존 파일을 백업하며 교체. [File.Replace 문서](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace)

첫 파일은 이동으로 확정하고 기존 파일은 `File.Replace`를 사용한다. 교체가 실패했다고 원본을 먼저 삭제하는 우회는 하지 않는다. 정전 시 내구성 및 모든 플랫폼에서의 원자성을 보장하는 구현은 아니다. Windows 로컬 파일을 첫 검증 대상으로 한다.

## 게임별 확장

`GameSessionService`는 게임 규칙을 모르는 저장 기반이다. `Gameplay`에서 `GamePayloadPolicy` 파생 ScriptableObject를 만들고 Boot의 `AppBootstrap`에 연결해 초기 JSON·`PayloadVersion`·검증 규칙을 지정한다. 검증기는 읽기·저장 과정에서 반복 호출되어도 상태를 바꾸지 않아야 한다. 잘못된 데이터에 `JsonException`·`InvalidDataException`·`FormatException`·`OverflowException`을 던지면 실패 결과로 처리하고 기존 저장을 보호한다. 다른 코드 결함 예외까지 일반 실패로 숨기지는 않는다. 실제 게임 상태는 JSON 객체로 만들어 `AppRoot.TrySaveGame(payloadJson)`에 전달하고, `Continue` 뒤 `AppRoot.Game.Current.PayloadJson`을 게임 모델로 복원한다. Core 코드를 수정할 필요는 없다. Runtime 작업 JSON과 저장 스냅샷의 연결은 [Phase 2 데이터 계약](PHASE_2_RUNTIME.md#definition--runtime--저장-경계)을 따른다. [새 게임 시작 가이드](NEW_GAME_SETUP.md)

2026-10-04 최신 main부터 저장은 활성 세션이 있는 Main·추가 Gameplay 씬에서 가능하다. Boot·Title·AppRoot 전환 중에는 차단한다. v1.0.0의 AppRoot 저장 진입점은 Main에서만 허용했다. Continue는 계속 Main으로 진입하며 실제 스테이지 재진입은 게임 코드가 payload를 읽어 결정한다. 저장 파일 형식·버전·백업·교체 확인 정책은 유지한다.

현재는 기본 3개 독립 슬롯·하나의 활성 세션·단일 앱 소유권·메인 스레드 호출을 전제로 한다. 파일 읽기/쓰기는 동기이며 한 파일 최대 1 MiB, JSON 깊이 최대 32다. 동일 저장소/리포지토리 내부 작업은 잠금으로 직렬화하지만 여러 앱 프로세스가 같은 경로를 공유하는 동시 편집은 지원하지 않는다. 대규모 데이터·자동 저장 시점·슬롯 간 복사·클라우드 저장은 해당 게임 요구가 생기면 추가한다.

`ITextFileStore` 구현을 `AppRoot.ConfigureStorage`로 Begin 전에 주입할 수 있다. 파일 부재만 null로 반환하고, 검증·쓰기 실패에는 기존 주 파일을 보호하며 백업·명시적 복구 보존 계약을 지킨다. AppRoot는 저장소를 Dispose하지 않으므로 외부 자원의 수명은 주입자가 관리한다. Editor의 Main 직접 Play는 저장소를 격리된 개발 경로로 교체한다. 테스트도 실제 사용자 저장 폴더를 사용하지 않는다. 연결 시점과 대체 구현의 상세 계약은 [설계 점검](ARCHITECTURE_REVIEW.md)을 따른다.

## 슬롯 삭제와 대체 저장소

`IDeleteSaveFileStore.DeleteSaveFiles(fileName)`은 `ITextFileStore`와 별개인 선택 기능이다. `JsonFileStore`는 둘 다 구현한다. GameSessionService는 명시적으로 전달한 삭제 구현을 사용하거나 같은 fileStore가 이 인터페이스를 구현했는지 확인한다. 둘 다 없으면 `CanDelete`는 false이고 삭제를 거절하지만 읽기·저장은 계속 사용할 수 있다. 별도 삭제 구현을 전달할 경우 읽기·쓰기와 같은 논리 저장 위치를 가리켜야 한다. AppRoot에는 ConfigureStorage로 두 기능을 구현한 저장소를 주입하면 된다.

삭제 대상은 지정 주 파일, 정확한 `.bak`, `.preserved-<32자리 N형 GUID>`, `.<32자리 N형 GUID>.tmp`다. 파일명 경로 검증 후 같은 저장소 lock 안에서 `.bak`과 생성된 보조 파일을 먼저, 주 파일을 마지막에 삭제한다. 다른 슬롯·settings·유사한 이름의 일반 파일·하위 디렉터리는 삭제하지 않는다. 이미 파일이 없으면 성공이며 접근·잠금·권한 오류는 숨기지 않는다.

보조 파일 정리가 실패하면 주 파일 삭제를 진행하지 않는다. 주 파일 삭제가 실패할 때 일부 보조 파일이 이미 제거되었을 수 있으므로 모든 파일을 원상복구하는 트랜잭션은 아니다. 서비스는 결과를 새로 읽고, 삭제 뒤 슬롯이 비어 있음을 확인해야 성공을 반환한다. 정전 시 여러 파일 삭제의 원자성을 보장하지 않는다.

## 검증과 이전 기록

현재 Gate 결과는 [검증 현황](INTEGRATION_VALIDATION.md)에 둡니다. 명명 변경·초기 저장 테스트·원본 Editor 패키지 복원은 [당시 기록](Archive/SETTINGS_SAVE_VALIDATION.md)에 보관합니다.
