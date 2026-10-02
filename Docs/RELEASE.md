# v1.0.0 새 게임 인계·배포 기준

기준일: 2026-10-02. 이 버전은 새 게임 제작을 시작하는 범용 공통 기반이다. 기본 프로젝트는 Unity 6000.3.16f1, URP 2D이며 첫 검증 플랫폼은 Windows다.

## 새 프로젝트 만들기

1. GitHub 저장소의 `Use this template` 또는 `v1.0.0` 태그 소스 ZIP으로 새 프로젝트를 만든다. GitHub Template은 새 게임 저장소를 만들고, ZIP은 Git 이력 없이 Unity 프로젝트 파일을 제공한다.
2. Unity 6000.3.16f1로 연다. `Assets`, `Packages`, `ProjectSettings`가 있는 폴더를 프로젝트 루트로 선택한다.
3. 첫 Play 전에 Company Name·Product Name·Application Identifier를 게임 고유 값으로 바꾼다. 필요한 게임만 별도 Unity Cloud 프로젝트를 연결한다.
4. `Tools > Starter Project > Validate Setup`을 실행하고 `00_StartScene`에서 시작한다.
5. [새 게임 시작 가이드](NEW_GAME_SETUP.md)에 따라 `GamePayloadPolicy`와 실제 게임 데이터를 연결한다.

원본 작업 폴더에서 Editor가 다시 채운 Cloud ID·Library·검증 복제본·사용자 저장 파일은 배포 소스에 포함하지 않는다. 배포 파일은 Git의 커밋된 파일로 만든다.

## 포함한 공통 기능

- Boot → Title → Main 초기화·씬 전환과 싱글 앱 루트 수명.
- 기본 SO 설정, 사용자 JSON 설정, 시스템 음량·화면 모드 적용.
- 단일 슬롯 JSON 저장·이어하기, 버전 정책·백업·복구·쓰기 실패 보호.
- 게임별 초기 데이터·payload 버전·검증 규칙을 연결하는 `GamePayloadPolicy`.
- Main 직접 Play의 Boot 경유·개발 저장소 격리와 설정 검사 도구.
- 입력 선택 복원·전환 입력 차단·로딩 표시, 예제 배경·버튼 스프라이트와 로딩 프리팹 교체.

전투·캐릭터 성장·스테이지·인벤토리·자동 저장 시점은 새 게임의 Gameplay에서 구현한다. 프레임 스프라이트의 Animator 작업도 새 게임에서 추가한다. 3D 게임은 렌더러·카메라·조명·입력 요구사항을 별도로 구성한다.

## 필수 검증 결과

- 깨끗한 Git 복제본의 패키지 복원, EditMode 50/50, 기존 PlayMode 30/30과 Windows 개발 빌드 통과.
- 로딩 프리팹 확장 후 PlayMode 31/31, 입력 차단 보완 후 해당 테스트 1/1, 최종 Windows 개발 빌드 통과.
- Windows 별도 프로세스의 첫 저장·재실행 복원·이어하기와 파손/미래 버전 보호·백업 복구 확인.
- Main 직접 Play·도메인 재로드·저장소 격리, 가로형 안전 영역·해상도 변경을 자동 확인.
- 실제 Windows Player에서 Unity 입력 시스템에 키 상태를 주입해 New Game → Save → Return → Continue와 같은 세션 복원을 확인. 이것은 물리 키보드 입력 검증과 구분한다.
- Player의 네이티브 1920×1080 Title·Main 화면에서 문구·버튼 배치와 겹침 없는 표시를 확인.

이번 임시 Player 설정 테스트의 중단은 테스트가 아래 방향키의 선택 대상을 Volume으로 잘못 가정한 것이었다. 실제 선택은 Fullscreen이었다. 보정한 추가 화면 모드·960×540 시나리오 실행은 Computer Use 앱 권한 승인 대기 상태다. 원본 기능 코드 오류로 기록하지 않는다.

## 지원 범위와 새 게임의 확인 항목

예제 UI는 **Screen Space Overlay + Scale With Screen Size, 1920×1080, Match 0.5**다. 1920×1080은 디자인 기준이며 최대 출력 해상도가 아니다. 가로 화면 콘텐츠와 안전 영역 축소는 자동 검증했다. 실제 에셋을 넣는 방법은 [UI 기준](RESPONSIVE_UI.md)을 따른다.

Android·iOS 패키지·실기기 실행은 이 버전에서 검증하지 않았다. 모바일 게임을 출시할 때 해당 Unity 빌드 모듈·터치·노치·화면 비율·실제 글꼴과 버튼 크기를 확인한다. 게임패드의 물리 장치, 오디오 청취와 OS 입력 전달도 게임의 대상 장치에서 확인한다.

Computer Use의 Windows 창 캡처는 시간 초과됐고, 도구로 보낸 Enter·F8이 Unity 입력 로그에 도착하지 않았다. 이 환경의 UI 자동화 제한을 게임 입력 결함으로 판정하지 않았다. 네이티브 화면 이미지와 Unity 입력 시스템 기반 검증을 별도 증거로 사용했다.

임의 전원 손실·모든 저장 중단 시점, 온라인·클라우드 저장·다중 슬롯·플랫폼 인증까지 보장하는 프리셋은 아니다. 필요한 기능은 게임 요구사항에 맞춰 확장한다.
