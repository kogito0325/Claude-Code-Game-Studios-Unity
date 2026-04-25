# BlueArch Proto — 할일 리스트

> 사용자가 요청한 다음 작업들. 위에서 아래로 우선순위 순.
> 진행 시 체크박스 갱신 + 커밋 메시지에 항목 번호 명시.

---

## 진행 예정 작업

### [x] #1 몬스터 스폰 방식 변경 — 스폰 포인트 기반 (구현 완료)
- **현재**: `BlueEnemySpawner`가 플레이어 주변 원형 ring(반경 15)에서 무작위 스폰
- **변경**:
  - 사용자가 맵에 `BlueEnemySpawnPoint` 게임오브젝트를 자유롭게 여러 개 배치
  - Spawner는 등록된 SpawnPoint들 중 하나를 (랜덤 또는 라운드로빈) 골라서 그 위치에서 스폰
  - 시각화: SpawnPoint에 Gizmo 표시 (Editor 전용)
  - 거리 옵션: 플레이어로부터 너무 가깝거나 너무 먼 SpawnPoint는 제외
- **수정 대상**: `BlueEnemySpawner.cs`, 신규 `BlueEnemySpawnPoint.cs`
- **검증**: Play 시 SpawnPoint 위치에서만 적이 등장하는지
- **구현 메모**:
  - 신규 `BlueEnemySpawnPoint` (Awake 시 정적 리스트 자기등록 / OnDisable 해제, Editor Gizmo 구체+jitter 원, 선택 시 라벨)
  - `BlueEnemySpawner` 옵션: `_pickMode` (Random/RoundRobin), `_minDistanceFromPlayer`, `_maxDistanceFromPlayer` (0=비활성)
  - SpawnPoint 0개 시 1회 경고 후 스폰 중단
  - 씬 초기 배치: `Spawner/SpawnPoints/SpawnPoint_{N,NE,E,SE,S,SW,W,NW}` (distance 18, jitter 0)

### [x] #2 몬스터 지형 인식 추적 — NavMesh 적용 (구현 완료)
- **현재**: 적이 플레이어 위치로 직선 이동 (지형 무시)
- **변경**:
  - 몬스터가 일정 경사 이상 / 벽은 오르지 않음
  - 가능하면 도로·평지 우선 이동
- **구현 후보**:
  - (a) Unity NavMesh — 맵에 NavMesh Bake 후 NavMeshAgent 사용 (정석, 가장 깔끔) ← 채택
  - (b) 간이 경사 검사 — 다음 스텝 위치의 ground 노멀 검사해서 너무 가파르면 거부
  - (c) Layer 분리 — 도로/평지에 별도 콜라이더 레이어 부여, 그 위로만 이동
- **수정 대상**: `BlueEnemy.cs` (Update의 추적 로직), 또는 새 컴포넌트 `BlueEnemyNavigator`
- **검증**: 적이 산·언덕·벽을 우회하거나 멈추는지
- **구현 메모**:
  - 패키지: `com.unity.ai.navigation 2.0.12` (이미 설치)
  - `BlueEnemy.cs`: NavMeshAgent 우선, `isOnNavMesh` false 시 MovementAgent 직선 fallback
  - 적 프리팹 6개 (BlueEnemy + Enemies/MC01-05) 에 NavMeshAgent 컴포넌트 추가 (radius 0.4, height 1.8, stoppingDistance 1)
  - 씬에 `NavMesh` GameObject + `NavMeshSurface` (collectObjects=All, useGeometry=RenderMeshes) 추가, 베이크 1.36초 통과
  - SpawnPoint 8개 모두 NavMesh 위 (`SamplePosition` within 2m 검증 PASS)
- **알려진 부수 이슈**: Player 시작 위치가 (0,0,0)인데 RPGTinyHero 맵 표면은 Y≈-4.92 → Player가 공중에 떠 있음. 적은 path 자동 보정으로 도달 가능하지만 사용자 확인 필요.

### [x] #3 플레이어 조작 방식 변경 — 마우스 조준 + 좌클릭 발사 (구현 완료)
- **현재**: 자동으로 가장 가까운 적 향해 회전·발사 (`BlueAutoAttacker.HasTarget`)
- **변경**:
  - **상체(회전)**: 항상 마우스 커서 위치를 향해 회전 — 플레이어 발 위치에서 카메라 ray로 ground 평면(y=0) 교점 계산
  - **다리(이동)**: WASD로 자유 이동 (현재 그대로 유지) — 8방향 에임-워크 매핑은 그대로
  - **사격**: 좌클릭 시점에만 발사 (자동 사격 → 수동 사격), 누르고 있으면 연사
- **수정 대상**: `BluePlayerController.cs` (회전 소스 변경: 적 → 마우스 ray), `BlueAutoAttacker.cs` (자동 → 입력 트리거)
- **검증**: 마우스 움직이면 상체 즉시 따라옴, 좌클릭 시에만 총알 발사
- **구현 메모**:
  - `BluePlayerController`: `Camera.ScreenPointToRay(Mouse.position) ∩ Plane(up, player.position)`. ray가 평면 못 만나면 `_lastAimDir` 유지. AutoAttacker 의존 제거.
  - `_aimCamera` 인스펙터 미할당 시 `Camera.main` fallback (Proto.Camera 네임스페이스 충돌 회피로 `UnityEngine.Camera` 명시)
  - `BlueAutoAttacker`: Update에서 `IsFireHeld()` && interval 만족 시 발사. 사격 방향=`transform.forward` (회전이 마우스 따라가므로 자동 정렬). 자동 타겟탐지/사거리(`_range`) 제거.
  - Input: `_fireAction` (InputActionReference) 우선, fallback `Mouse.current.leftButton.isPressed`
  - 사양 spec gap (FT-01) "ground y=0" → player 현재 Y 평면으로 해석 (새 맵 표면 -4.92 호환)

### [ ] #4 카메라 무브먼트 변경 — 마우스-플레이어 보간
- **현재**: `CameraRig` QuarterView 모드 (플레이어 항상 중앙)
- **변경**:
  - 카메라 타겟 위치 = `Lerp(player.position, mouseWorldPosition, factor)` (factor 약 0.3~0.5 튜닝)
  - 마우스를 화면 가장자리로 옮기면 카메라가 그 방향으로 살짝 미끄러짐
  - 최대 오프셋 거리 클램프 (예: 5 ~ 8 unit)
- **구현 후보**:
  - (a) 새 `IFramingTarget` 가상 타겟 게임오브젝트 — 매 프레임 위치를 보간 결과로 갱신, CameraRig가 추적
  - (b) `CameraRig`에 직접 LookOffset 기능 추가 — 더 침습적
- **수정 대상**: 신규 `BlueCameraAimOffset.cs` 또는 `CameraRig.cs` 확장
- **검증**: 마우스 위치 변경에 따라 화면이 부드럽게 이동, 플레이어가 화면 가장자리로 안 빠지는지

---

## 완료된 작업 (참고용)

- [x] BlueArch 프로토 — 스크립트 8종 + 컨셉 문서 (`b526990`)
- [x] 씬 + 프리팹(Player/Enemy/Projectile) + HUD + 공장 프롭 (`52e5022`)
- [x] 캐릭터 머티리얼 — Body.mat 셰이더 + BlueEnemy 슬롯 (`c772cc1`)
- [x] RifleGirl 머티리얼 일괄 URP/Lit + Spawner._enemyPrefab 재연결 (`59227b6`)
- [x] BlueEnemy AnimationEvent SwitchSocket 스텁 (`2d5b8fe`)
- [x] Magica Cloth 자식 13개 제거 (`1254187`)
- [x] Rifle_Full_Body 프리팹 missing script 13개 제거 (`74788c5`)
- [x] BlueEnemy 메시 누락 복구 (Humanoid_Female mesh 재연결) (`e6348fc`)
- [x] 적 외형 다양화 (MC01-05) + BlueAnimDriver 모션 시스템 (`df9c22b`)
- [x] Player Animator state name 정정 (R_Idle 등) (`75a7922`)
- [x] Player 8방향 에임-워크 + AutoAttacker 회전 권한 이전 (`fa0c4e9`)
- [x] 발 미끄러짐 보정 — Animator.speed 동기화 (`1deb041`)

---

## 보류 중 (사용자 결정 대기)

- 새 맵(RPG Tiny Fantasy World) 적용 후 발견된 부수 이슈 — 사용자 결정 후 #2 작업과 같이 처리될 가능성
  - 적 스폰이 나무 위에 떨어질 수 있음 → #1 SpawnPoint 도입으로 해결됨
  - 총알이 나무·바위에 막힘 → 별도 LayerMask 정리 필요 (또는 BlueProjectile 코드에서 BlueEnemy 컴포넌트 없으면 통과 처리)
  - MovementAgent 블로킹 레이어 좁히기 → #2 작업 시 같이 처리

---

**파일 위치**: `design/proto-todo.md` (이 파일)
**원본 컨셉 문서**: `design/proto-concept.md`
