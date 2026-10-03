# 게임 제작 시작 전 점검 — 2026-10-04

범위: 범용 템플릿의 복제·컴파일·초기화·씬 수명·저장 경계·입력 호환성·UI 교체·설정 검사. 사용자 요청에 따라 캐릭터 수·능력치·스테이지 진행·전투·자동 저장 규칙 등 게임별 구현은 검사 대상에서 제외한다.

v1.0.0 이후 발견한 공통 기반의 문제는 이번 소스에서 보완했다. 기존 v1.0.0 ZIP은 이전 소스로 유지한다. 보완을 사용하는 새 프로젝트는 최신 main의 GitHub Template에서 만든다.

## 발견해 수정한 사항

| 항목 | 제작 중 발생할 문제 | 보완 |
| --- | --- | --- |
| Main에서만 허용한 저장 | Main을 나가 스테이지 씬을 활성화하면 `TrySaveGame(payloadJson)`이 false를 반환 | 활성 세션이 있는 추가 Gameplay 씬에서도 허용. Boot·Title·전환 중 저장은 차단 |
| 꺼져 있던 Resizable Window | 반응형 레이아웃을 구현했어도 PC Player 창을 사용자가 늘리거나 줄이지 못함 | Player 설정의 `resizableWindow`를 활성화. 1920×1080 디자인 기준 유지 |
| Title·Main 입력 검사 누락 | 두 씬의 EventSystem·포인터·클릭 연결을 지워도 Validate Setup이 통과할 수 있음 | 세 씬 모두 단일 활성 모듈과 UI Point·Navigate·Submit·Cancel·Click의 올바른 연결 검사 |
| 비활성 Bootstrap 검사 누락 | Boot의 초기화 컴포넌트나 오브젝트를 끄면 시작되지 않는데 설정 검사는 통과 | 단일 활성 AppBootstrap과 AppConfig 참조 검사. 비활성·중복 진입점 거부 |

저장 파일 형식·payload 버전·백업·복구·기존 슬롯 교체 확인은 유지한다. 특정 게임 데이터 구조는 추가하지 않는다. 추가 씬의 저장 허용 기준은 앱 Ready, 유효한 활성 세션, Boot·Title 이외의 활성 씬이며 AppRoot가 시작한 전환 중에는 저장하지 않는다.

## 템플릿 사용 시 알아야 할 제약

| 항목 | 문제가 발생할 조건 | 템플릿 사용 기준 |
| --- | --- | --- |
| 저장 경로 공유 | Company/Product 이름을 템플릿 값으로 유지한 프로젝트 여러 개가 같은 사용자 저장 폴더에 접근 | 새 프로젝트의 첫 실행 전에 게임별 Company/Product/Application Identifier 지정 |
| 설정·씬 경로 이동 | SO_AppConfig를 이동하면 Editor 도구의 고정 ConfigPath가 유효하지 않음. 씬 경로는 문자열이어서 이름·폴더 변경 후 오래된 값이 남을 수 있음 | Config 경로를 유지하거나 Editor 도구도 갱신. 씬 변경 시 AppConfig·빌드 씬 목록을 갱신하고 Validate Setup 실행 |
| JSON 어셈블리 | 새 asmdef가 Core나 JSON DLL을 참조하지 않으면 확장 코드 컴파일 실패 | 시작 가이드의 명시적 참조 예제 사용. 새 DLL 의존성도 해당 asmdef에 추가 |
| 외부 입력 에셋 | 현재 새 Input System만 활성화돼 있어 구 Input Manager 전용 코드와 호환되지 않을 수 있음 | 에셋의 입력 시스템 요구사항 확인. 템플릿의 UI 입력 모듈을 중복 설치하지 않음 |
| UI 컴포넌트 교체 | uGUI Text 필드를 TMP 컴포넌트로 바로 대체할 수 없음. StarterScreen 제거 후에도 예제 기반 검사 도구가 해당 컴포넌트를 요구 | 단순 Sprite 교체는 기존 참조 유지. 표시 컴포넌트 전면 교체 시 표시 계층과 검사 계약도 함께 변경 |
| 추가 씬의 단독 Editor Play | Boot 경유 개발용 진입은 설정된 Main만 처리 | 일반 시작은 Boot, 개발용 초기화 진입은 Main을 사용. 추가 씬도 자동 초기화된다고 가정하지 않음 |
| 저장·플랫폼 지원 범위 | 저장은 단일 슬롯·메인 스레드 동기식·한 프로세스 소유 전제. 새 플랫폼의 파일 교체·입력·화면은 검증되지 않음 | 문서화된 Windows 기반을 기준으로 사용하고 새 플랫폼의 기본 동작은 해당 기기에서 확인 |

첫 실행 전에 Company Name·Product Name·Application Identifier를 게임 고유 값으로 정한다. Windows 저장 경로가 회사명·제품명에 따라 정해지므로 템플릿 이름을 그대로 사용하는 여러 게임의 저장은 충돌할 수 있다. [Unity 저장 경로](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html)

입력 시스템을 변경하거나 외부 컨트롤러를 이관할 때는 현재의 새 Input System 설정을 기준으로 판단한다. [Unity 입력 백엔드 설정](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Installation.html). PC 창 크기 조절 속성은 [Unity Player 설정](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-resizableWindow.html)을 따른다.

## 이번 검증과 실제 한계

- 원본 프로젝트의 Unity 6000.3.16f1 PlayMode `RuntimeFlowTests` **15/15 통과**, 실패·건너뜀 0개. Main을 언로드하고 추가 씬을 활성화해 범용 JSON 표식 하나를 저장·이어가는 동작과 Title·전환 저장 차단을 확인했다. 캐릭터·진행 샘플은 검사 대상에서 제외했다. 결과: `Logs/GameStartAuditPlayMode.xml`, 로그: `Logs/GameStartAuditPlayMode.log`.
- EditMode `StarterProjectSetupTests` **16/16 통과**, 실패·건너뜀 0개. EventSystem 누락, Point·Navigate·Click 연결 누락, AppBootstrap 비활성·중복을 거부하고 정상 설정을 열린 씬 변경 없이 검사했다. 최종 결과: `Logs/GameStartAuditEditModeFinal.xml`, 로그: `Logs/GameStartAuditEditModeFinal.log`.
- PlayMode 15개에는 기존 설정 적용·실패·복구, 로딩 프리팹, 합성 키보드·게임패드 선택과 슬롯 교체 확인·취소도 포함됐다. 새로운 복제본·시험용 Player 빌드 없이 격리된 테스트 저장소를 사용했다.
- PC 창 조절 옵션은 소스 설정으로 확인했다. 이번 회차에서 새 Windows Player 빌드·창 드래그·Computer Use·물리 입력·모바일 실기기는 재검사하지 않았다. 기존 배포 검증 결과의 범위를 늘려 주장하지 않는다.

게임별 기능은 이번 판정에 포함하지 않는다. [템플릿 복제·설정 절차](NEW_GAME_SETUP.md)
