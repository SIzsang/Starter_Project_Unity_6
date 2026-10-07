# 프리셋 개요와 현재 사용 기준

기준일: 2026-10-07. 현재 main의 구현과 실제 Gate 기록을 기준으로 합니다.

**Windows·URP 2D 게임 제작에 착수할 수 있습니다.** 공통 기반과 선정 범위의 Gate를 마쳤습니다. 다음 권장 작업은 실제 게임의 작은 Game Layer 흐름을 연결하는 것입니다.

## 현재 제작 준비 단계 — 2026-10-07

| 범위 | 상태 | 제공 기능 |
| --- | --- | --- |
| Phase 1 실행 골격 | 완료 | Boot·Title·Main, 3슬롯 저장·삭제·복구, 설정·로딩·예제 UI |
| Phase 2 실행 기반 | 10개 작업·Gate 완료 | GameState, SceneRoot, 서비스 조립, 비동기 수명, Definition/Runtime, 입력·Pause·Audio |
| Phase 3 개발 지원 | 5개 작업·Gate 완료 | Logging, Debug Menu, 빌드 환경, Validator, Smoke |
| Phase 4 선택형 모듈 | 선정 Pool 범위·Gate 완료 | PrefabPool·PoolLease, 준비 후 활성화, 안전한 반납·종료 |
| Game Layer | 새 게임에서 연결 | 게임 규칙·콘텐츠·payload·조작·HUD |

Localization·Addressables·UI Stack·Rebinding·Time·Platform은 현재 필수 요구가 없어 보류합니다. 선택 모듈 없이도 Core가 동작해야 합니다.

## 사용 환경과 소스 기준

| 항목 | 기준 |
| --- | --- |
| Unity / 렌더링 | 6000.3.16f1 / URP 2D 17.3.0 |
| 입력 / UI | Input System 1.19.0 / uGUI |
| 저장 | Newtonsoft JSON 3.2.2, 기본 독립 슬롯 3개 |
| 첫 환경 | Windows PC, 오프라인 2D 시작점 |

현재 기능은 main의 `52688c0`(실행·진단), `91f413a`(개발 도구), `a24ff7d`(Pool)에 포함됩니다. 문서 정리 커밋은 이 코드 위에 이어집니다. 새 게임에는 실제 사용한 커밋을 기록합니다.

기존 [v1.0.0](RELEASE.md)은 배포가 완료된 이전 단일 슬롯 버전입니다. 태그·ZIP은 유지하며 현재 main의 기능과 구분합니다. 개인 Cloud 설정은 커밋 소스에서 제외합니다.

## 새 게임에서 연결할 것

1. 게임 고유 Company/Product/App ID와 대상 플랫폼을 정합니다.
2. Boot·Title·Main과 AppConfig 연결을 확인합니다.
3. Gameplay SceneRoot의 초기화·진입·종료 훅에 게임을 조립합니다.
4. 고정 Definition, 변경 가능한 Runtime, 저장 payload를 연결합니다.
5. Gameplay 입력·HUD·Pause UI·Audio 콘텐츠를 연결합니다.
6. 반복 프리팹이 필요하면 Pool을 선택합니다.
7. 해당 게임의 Player에서 시작·저장·재실행·이어하기와 필요한 장치를 확인합니다.

자동 저장 시점, 전투·인벤토리·성장·AI·스테이지·Animator는 게임의 책임입니다. 예제 Main은 공통 흐름을 사용하는 시작 화면입니다. [새 게임 시작 절차](NEW_GAME_SETUP.md)

## 확인된 범위와 남은 확인

| 범위 | 근거 |
| --- | --- |
| Phase 2 | EditMode 20/20, PlayMode 고유 34개를 실행·재검사로 확인 |
| Phase 3 | EditMode 20/20, PlayMode 4/4, 빌드 심볼 3종의 환경·로그 기본값 |
| Phase 4 | Pool PlayMode 11/11, 어셈블리 분리 후 대표 1/1 재검사 |
| 소스 인계 | Gate 당시 C#/asmdef 60개 원본/검증 해시 일치. 이번 문서 정리에서 스크립트/메타 139개의 Git 내용 일치 |
| 기존 Windows Player | 이전 소스의 프로세스 재실행·저장, 합성 입력, 네이티브 화면·창/전체화면 확인 |

최신 소스의 새 Windows Player 빌드, 물리 입력·오디오 청취·모바일 실기기·게임 부하 측정은 아직 수행하지 않았습니다. 단일 실행의 테스트 수와 재검사 수를 합산하지 않습니다. 자세한 근거는 [검증 현황](INTEGRATION_VALIDATION.md)에 둡니다.

## 다음 작업과 참고

- 다음 Work: [작은 Game Layer 적용 사례](NEXT_STEPS.md#2-작은-game-layer-적용-사례)
- 사용법 찾기: [문서 안내](INDEX.md)
- 책임·데이터 경계: [설계 기준](INITIAL_SETTING.md)
- 이전 인계·검증의 상세 서술: [과거 Summary 기록](Archive/PRESET_SUMMARY_HISTORY.md)
