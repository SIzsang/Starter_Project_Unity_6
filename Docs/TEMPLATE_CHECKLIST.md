# 복제·인계 체크리스트

현재 main으로 새 게임을 시작할 때의 확인 항목입니다. 기존 v1.0.0 배포는 완료된 버전 기록이며 최신 소스의 사용 범위와 구분합니다. [문서 안내](INDEX.md) · [개요](PRESET_SUMMARY.md)

## 현재 기준

- Phase 1 실행 골격, Phase 2·3, 선정 Phase 4 Pool 범위와 Gate 완료
- 코드·메타·가이드를 실행/진단·개발 도구·Pool·인계로 나눠 커밋
- 개인 Cloud 연결과 Library/Logs/Builds 등 생성 파일은 템플릿 소스에서 제외
- 새 버전 태그·Release ZIP은 실제 배포 범위를 정한 뒤 별도 생성

v1.0.0의 정리·복제·Player·태그·Template 완료 항목은 [기존 배포 체크리스트](Archive/TEMPLATE_CHECKLIST_V1.md)에 보관합니다. 당시 Clean Clone·빌드 결과를 현재 소스의 새 실행으로 표시하지 않습니다.

## 새 게임의 첫 실행 전

- [ ] 현재 main 또는 선택한 소스 커밋에서 새 프로젝트 생성·기준 커밋 기록
- [ ] Unity 6000.3.16f1과 Packages 복원 확인
- [ ] Company/Product/Application Identifier·아이콘을 게임 고유 값으로 변경
- [ ] 필요할 때 새 Unity Cloud 프로젝트 연결
- [ ] 다른 프로젝트와 사용자 저장 경로가 겹치지 않는지 확인
- [ ] 대상 플랫폼·렌더러·화면 방향·입력 장치 결정
- [ ] Boot·Title·Main/AppConfig·빌드 씬 연결 확인
- [ ] Validate Setup 실행. 기본 화면을 유지하면 Validate Example UI도 선택 실행

## 게임 코드 연결

- [ ] Gameplay asmdef에 사용하는 Core/JSON/Input/UI/Pool 참조 지정
- [ ] 활성 Gameplay SceneRoot 하나와 초기화·진입·종료 훅 연결
- [ ] Definition·Runtime·payload의 필드·버전·검증 규칙 연결
- [ ] Gameplay 입력·HUD·Pause UI·Audio 콘텐츠 연결
- [ ] 필요 시 Pool reset·스폰·반납·용량과 비동기 결과 유효성 연결
- [ ] 자동 저장·체크포인트·사망/성장 등 게임 규칙은 Game Layer에서 결정

## 적용·인계 시 필요한 확인

- [ ] 게임의 첫 실행 → 조작/상태 변경 → 저장 → 앱 재실행 → Continue 확인
- [ ] 씬 종료 시 작업·입력 범위·대여 객체 정리 확인
- [ ] 사용할 Player 빌드 환경과 실제 입력·오디오·UI 품질 확인
- [ ] 추가 플랫폼이면 해당 기기의 파일·화면·입력 확인
- [ ] 사용 소스·포함 기능·검증 범위·알려진 한계를 함께 기록

모든 변경마다 위 절차와 전체 회귀·빌드·Clean Clone을 반복하지 않습니다. 변경 위험과 실제 인계 목적에 필요한 범위만 확인합니다.

## 배포와 유지보수

main 업데이트와 버전 Release는 구분합니다. 원격 반영은 일반 fast-forward Push를 사용하고 기존 태그를 덮어쓰지 않습니다. 이미 복제한 게임에 템플릿 변경이 자동 적용되지는 않습니다.

공통 수정은 Starter에서 관리하고 적용한 게임의 기준을 기록합니다. 여러 게임에서 같은 변경을 반복할 때 Common Module/패키지 분리를 검토합니다. [새 게임 시작](NEW_GAME_SETUP.md) · [실제 검증](INTEGRATION_VALIDATION.md)
