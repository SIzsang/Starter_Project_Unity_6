# 이 프리셋으로 시작할 수 있는 게임

**Windows·URP 2D 게임의 제작 기반으로 사용할 수 있습니다.** 현재는 Phase 1 기본 골격, Phase 2 실행 기반, Phase 3 개발 지원, 선정 Phase 4 Pool 범위와 Gate를 완료했습니다. [현재 개요](PRESET_SUMMARY.md)

## 제공 기반과 게임의 책임

| 영역 | 프리셋 제공 | 게임에서 연결 |
| --- | --- | --- |
| 실행·씬 | Boot·Title·Main, SceneRoot 수명·취소 | 실제 씬·시작 조건·스테이지 |
| 저장·설정 | 기본 3슬롯·삭제·복구·버전 보호·공통 옵션 | payload·복원·저장 시점 |
| 플레이 기반 | Runtime/Definition, 입력 상황·Pause·Audio | 조작·HUD·클립·게임 규칙 |
| 반복 객체 | 선택형 PrefabPool·PoolLease | reset·스폰·반납 시점·용량 |
| 개발 | 로그·Debug·Validator·빌드 환경·Smoke | 게임 기준 표식·대상 장치 확인 |

작은 오프라인 2D 게임은 필요한 기능만 사용합니다. 퍼즐·액션·턴제 등 장르 규칙은 Gameplay에 구현하며 Core에 고정하지 않습니다.

## 추가 환경을 선택할 때

3D는 Core 계약을 재사용하되 현재 2D Renderer·Camera·조명·화면 구성을 조정합니다. 모바일은 해당 빌드 모듈·실기기 입력·Safe Area·가독성을 확인합니다. 온라인·스토어 SDK·대규모 콘텐츠는 실제 요구에 따라 별도 설계합니다.

최신 소스의 새 Player·물리 입력·Audio 청취·실제 게임 부하까지 확인된 상태는 아닙니다. [검증의 범위](INTEGRATION_VALIDATION.md)

## 화면과 캐릭터 에셋

1920×1080은 UI 디자인 기준입니다. 출력 해상도 상한이 아니며 작은 화면의 가독성과 터치 크기는 게임의 최종 에셋으로 확인합니다. [화면 교체 방법](RESPONSIVE_UI.md)

Walk·Attack·Hurt가 포함된 스프라이트/FBX는 새 게임에서 동작별 Clip과 Animator로 연결합니다. 공격 판정·전환 우선순위·반복 여부는 게임이 정합니다. 특정 캐릭터 Animator를 공통 프리셋에 미리 넣지 않습니다. 기존 상세 설명은 [캐릭터 에셋 인계 기록](Archive/GAME_CAPABILITY_20260923.md#walkattackhurt가-한-에셋에-들어-있는-경우)에 보관합니다.

## 다음 시작점

[새 게임 설정](NEW_GAME_SETUP.md) → [작은 Game Layer 적용 흐름](NEXT_STEPS.md#2-작은-game-layer-적용-사례) 순서로 진행합니다.
