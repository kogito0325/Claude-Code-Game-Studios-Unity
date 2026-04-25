# BlueArch Proto — 블루아카이브 스타일 탑뷰 3D 자동전투

- **장르**: 탑뷰 3D 실시간 자동전투 / 웨이브 서바이벌
- **한 문장 요약**: 탑뷰 쿼터뷰에서 WASD로 움직이며 가까운 적을 자동 사격해 3분간 버티는 블아 풍 프로토타입
- **3분 플레이 시나리오**:
  - 0:00~0:30 — 느린 속도로 근거리 적 1~2마리 스폰, 기본 자동사격 감각 익히기
  - 0:30~2:00 — 스폰 밀도 상승, Space 스킬(원형 AoE)로 뭉친 적 정리
  - 2:00~3:00 — 최대 압박 구간, HP 관리 + 스킬 쿨타임 타이밍이 생존 핵심
- **조작**:
  - WASD — 플레이어 이동
  - Space — 스킬 (원형 범위 공격, 쿨다운 있음)
  - Mouse — (선택) 미사용 / 자동 조준이 대신 수행
- **성공 조건**: 3분(180초) 생존 시 승리, 플레이어 HP 0 시 패배
- **참고 게임**: Blue Archive (실시간 자동전투 소대 슈터), Vampire Survivors (웨이브 서바이벌)

---

## 스코프 결정 (사용자 확정: A)

- 플레이어 **1명** (소대 아님 — 향후 확장 여지만 남김)
- 자동 사격: 사거리 내 **가장 가까운 적** 주기적 공격
- 스킬: **1개** (Space, 원형 AoE)
- 적: 단일 타입 (플레이어를 쫓아옴, HP/데미지 보유)
- 씬: **단일 씬** (`BlueArch.unity`)
- 카메라: 기존 `Proto.Camera.CameraRig`의 **QuarterView** 모드 재사용

## 재사용할 공용 인프라

| 모듈 | 경로 | 사용처 |
|---|---|---|
| MovementAgent (HR-1 준수, no Rigidbody) | `Assets/Proto/Runtime/Movement/` | 플레이어/적 이동 |
| CameraRig (QuarterView) | `Assets/Proto/Runtime/Camera/` | 플레이어 추적 카메라 |
| Rng (결정론 난수) | `Assets/Proto/Runtime/Shared/` | 스폰 위치 난수 |

## 새로 추가할 스크립트 (`Assets/Proto/Sample/BlueArch/Runtime/`)

1. `BluePlayerController.cs` — WASD 입력 → `MovementAgent.MoveBy`
2. `BlueAutoAttacker.cs` — 주기적으로 가장 가까운 `BlueEnemy` 찾아 투사체 발사
3. `BlueProjectile.cs` — 직선 이동 + 충돌 시 데미지
4. `BlueEnemy.cs` — 플레이어 추적 + HP + 접촉 데미지
5. `BlueEnemySpawner.cs` — 화면 밖 링에서 주기적 스폰 (밀도 시간에 따라 증가)
6. `BlueSkill.cs` — Space 입력 시 플레이어 주변 원형 AoE, 쿨다운
7. `BlueGameManager.cs` — 3분 타이머 + 플레이어 HP + 승/패 판정 + 점수

## Phase 플랜 (20시간 내)

| 단계 | 내용 | 예상 시간 |
|---|---|---|
| P1 | 스크립트 7개 작성 + 컴파일 통과 | 2h |
| P2 | Unity 씬 구성 (플레이어·카메라·스폰 포인트·지면) | 2h |
| P3 | 플레이 테스트 후 파라미터 튜닝 (속도·데미지·스폰율) | 4h |
| P4 | 간단한 HUD (HP·타이머·스킬 쿨) | 3h |
| P5 | 기본 비주얼 (플레이어/적 머티리얼·파티클) | 3h |
| P6 | 빌드 + 버그픽스 | 6h |

## VKL 검증 원칙

- 경량 VKL 모드 (Proto 기본). 판단 응답에 oracle ID + FT ID + 신뢰도 명시.
- spec gap 발견 시 FT-01 분류, 추측 금지 → 사용자 확인.
- 복잡 판단 필요 시 `/verify` 호출.

## 나머지 기존 장르 (turn3d / td / rail-shooter)

- 이전 proto-concept (3장르 demo)는 **archived** 상태. 본 BlueArch 프로토가 현재 유일 활성 proto.
- 필요 시 `git log` 의 이전 proto-concept 내용 참조 가능.
