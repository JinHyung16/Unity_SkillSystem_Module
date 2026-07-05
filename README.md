# Unity Skill System Module

> **방향키로 뛰어다니며 Q/W/E/R로 스킬을 마구 난사해, 몰려드는 몬스터를 쓸어담는 2.5D 액션 게임.**
> 스킬 하나하나는 코드가 아니라 **구글 시트에 정의한 모듈 조합(행동 트리)** 으로 만들어진다 —
> 시트만 고쳐 새 스킬을 얼마든지 찍어낼 수 있는 **데이터 주도형 스킬 시스템**과 **BT 기반 적 AI**가 핵심.

엔진: **Unity 2022 LTS / URP**
플레이 한 줄 요약: 방향키 이동 · 몬스터가 붙으면 서로 자동 평타 · **Q/W/E/R 스킬 난사** · 처치하면 몬스터 리스폰.

---

## 1. 프로젝트 한눈에

- **플레이 루프**: 방향키로 이동 → 몬스터가 접근하면 서로 **자동 평타** → **Q/W/E/R로 스킬 난사**해 처치 → 몬스터는 풀에서 다시 스폰. "스킬 마구 쓰기"가 메인 재미이고, 스킬은 전부 DB에서 조립된다.
- **장르 / 카메라**: 3D 월드 + 측면 시점(2.5D), DFO식 좌우 플립 캐릭터, 8방향 이동.
- **무엇을 보여주는가**:
  1. 구글 시트 → JSON → 컴파일된 BT 노드 → 런타임 발동까지 이어지는 **데이터 파이프라인**.
  2. 역할(Trigger / Targeting / Launch / Hit / Despawn / Buff)을 **직교(orthogonal)** 로 분해한 스킬 구조.
  3. 이벤트 허브(`GameEvents`) 기반 **Player ↔ Camera ↔ Spawner 디커플링**.
  4. 외부 패키지 없이 직접 짠 **경량 Behavior Tree** + Selector/Sequence/Condition/Action.
  5. `Damageable.OnDied` 이벤트 → Spawner 풀 반환으로 닫는 **GC-free 풀링 루프**.

---

## 2. 기술 스택 & 환경

| 분류 | 사용 기술 |
|---|---|
| Engine | Unity 2022.3 LTS |
| Render | Universal Render Pipeline (URP) + 자작 Outline Shader |
| Data | Google Sheets → JSON sync (Editor 툴 직접 구현, OAuth2) |
| Code | C# / .NET Standard 2.1 |
| Build | Windows Mono |
| Source | Git |

---

## 3. 설계 원칙 — 어떤 기준으로 짰는가

이 프로젝트는 "동작하는 것"이 아니라 **"바뀔 때 덜 깨지는 것"** 을 우선했습니다.
실무에서 가장 자주 다치는 지점들을 의식해서 다음 원칙을 잡았습니다.

### 3.1 직교성 (Orthogonality)
스킬을 6개 역할 노드로 쪼개 **한 축이 바뀌어도 다른 축은 무관**하게 했습니다.
새 Hit 패턴(`RingHit`)을 추가해도 Launch / Targeting / Despawn 코드는 안 건드립니다.

### 3.2 데이터 분리
스킬 수치는 코드가 아니라 **시트**. 평타는 인스펙터 수치(빠른 튜닝).
"운영 중 수정 빈도"가 다른 두 영역을 의도적으로 분리했습니다.

### 3.3 이벤트 허브 패턴
Player / CameraFollow / EnemySpawner는 서로를 직접 참조하지 않고
`GameEvents.OnPlayerSpawned`로만 만납니다.

- 라이프타임 사이드 이펙트 차단 (한쪽이 죽어도 다른 쪽 NRE 없음).
- `CurrentPlayer` 캐시 + 구독자가 OnEnable에서 즉시 1회 호출 → **Unity Enable 순서 무관**.

### 3.4 Find\* 런타임 사용 금지
모든 의존성은 **인스펙터 와이어링 + 이벤트 구독** 으로만 해결.
씬 스캔/`FindObjectOfType` 호출은 코드 전체에서 0회. 빌드 후 씬 트리가 커져도 페널티 없음.

### 3.5 풀링은 이벤트로 닫는다
`Damageable.OnDied` 이벤트가 `DestroyOnDeath` 분기 **이전**에 발행됩니다.
Spawner는 이 이벤트만 잡아 풀로 반환 → Enemy 클래스는 Spawner 존재를 모릅니다.

```
TakeDamage → HP ≤ 0 → OnDied 발행 → (Spawner가 받아 SetActive(false) + enqueue)
                              ↘ (DestroyOnDeath=true면 그제서야 Destroy)
```

### 3.6 GC 최소화 (실전 흔한 함정)
- `SkillObject._scratchCtx` 1개를 매 발사마다 `Reset()` 재사용 — 발사 1회당 alloc 0.
- `Damageable.All`은 OnEnable/OnDisable에서 정적 리스트 자가 등록 → `Damageable.GetAllOfTeam(team, buffer)`은 외부 버퍼 채우기로 알로케이션 없음.
- BT 노드는 람다 캡처 없이 메서드 그룹 참조로 등록.

---

## 4. 시스템 아키텍처

```
                          [Google Sheet]
                              │ sync (Editor 툴)
                              ▼
                       [Resources/*.json]
                              │ DataManager.LoadAsync
                              ▼
                  ┌───────────────────────────────┐
                  │   SkillRegistry (정적)         │
                  │   ─ SkillDefinition[id]       │
                  │   ─ SkillBuff / Debuff         │
                  └──────────────┬────────────────┘
                                 │ Compile(def, level)
                                 ▼
                          [CompiledSkill]
                                 │
   ┌─────────────────────────────┼─────────────────────────────┐
   ▼                             ▼                             ▼
[SkillObject]              [TargetingResolver]          [LaunchExecutor]
 - Loadout(SO)                                          - 풀에서 VFX 스폰
 - TryFireSlot(KeyCode)    → ctx.Targets             → [SkillEffect]
 - Stunned/Multiplier                                   - 이동/Hit/Despawn

   ▲                                                          │
   │                                  데미지 + Debuff           ▼
   │                                                    [Damageable]
   │                                                    - Hp/Shield/Stunned
   │                                                    - OnDied 이벤트
   │                                                          │
   └──── ActiveStatusEffect (Buff/Debuff 인스턴스) ◀───────────┘
                                                              │
                                                              ▼
                                                       [EnemySpawner]
                                                       풀 반환

                            ━━━━━━━━━━━━━━━

[Player] ──RaisePlayerSpawned──▶ [GameEvents] ◀──── OnPlayerSpawned ──── [CameraFollow]
                                       │                                       │
                                       └──── OnPlayerSpawned ──── [EnemySpawner]
```

---

## 5. 주요 모듈

### 5.1 Skill System (`Assets/Scripts/SkillSystem/`)

스킬 한 개 = **행동 트리(BT)** 하나. 노드를 역할별로 조합해 정의한다 (`ESkillNodeType`):

| 역할 | 노드 타입 |
|---|---|
| Composite | `Sequence`, `Selector`, `Parallel`, `Inverter` |
| Decorator | `Cooldown`, `Chance` |
| Trigger | `TriggerOnAttack`, `TriggerOnTick`, `TriggerOnOreBreak` |
| Targeting | `TargetSelf`, `TargetNearest`, `TargetFarthest`, `TargetRandom`, `TargetAll`, `TargetNearestForward`, `TargetCone` |
| Hit | `HitSingle`, `HitArea`, `HitBeam`, `HitChain`, `HitDeathBurst` |
| Despawn | `DespawnAfterTime`, `DespawnAfterHits`, `DespawnAfterBounces`, `DespawnOnWall` |
| Launch | `LaunchInstant`, `LaunchStraight`, `LaunchArc`, `LaunchCurve` |
| Side Effect | `BuffSelf`, `DebuffOnHit` |

노드 param이 비어 있으면 **(노드 정적값 → 레벨 modifier → fallback)** 순서로 값을 찾는다 —
"같은 노드를 레벨업으로 강화"를 표준화한 룩업. 자세한 작성법은 [8. 스킬 제작 가이드](#8-스킬-제작-가이드) 참고.

**핵심 파일**
- `SkillRegistry.cs` — 멱등 LoadAsync, 동시 호출 시 같은 Task 공유
- `SkillCompiler.cs` — Nodes 리스트를 역할 슬롯으로 평탄화
- `Runtime/SkillObject.cs` — 시전자 컨테이너, `SkillLoadout` SO 참조 기반 장착
- `Runtime/SkillEffect.cs` — 발사된 VFX 1개의 라이프 (모션 + Hit + Despawn)
- `Runtime/ActiveStatusEffect.cs` — 버프/디버프 런타임 인스턴스, enter/tick/exit 라이프사이클

**확장**: 새 Hit 패턴 추가 절차는 *시트 `_Enum` 탭에 enum 추가 → `BTBuilder.Create` switch 한 줄 → `SkillEffect.ProcessHit` switch 한 줄* 의 3 step.

### 5.2 Character / Input (`Assets/Scripts/Character/`, `Assets/Scripts/Input/`)

DFO식 2.5D 컨트롤:

- **Motor**: `CharacterController` 기반 8방향 이동 + 중력 + `SpeedMultiplier`.
- **Facing**: 마우스 추격 회전 ❌. **좌우 플립 only** (`FacingSign ∈ {-1, +1}`). 이동 가로 입력 부호로 갱신.
- **Attack**: 부채꼴 평타. 데이터는 인스펙터(빠른 튜닝), 발동은 `SkillObject` 외부.
- **Input**: `IInputProvider` 추상화 — PC/모바일 구현 교체 가능. PC는 `KeyboardInputProvider`.
- **Bindings**: `ScriptableObject` 매핑. 이동(방향키)과 스킬 슬롯(Q W E R) 키 충돌 없게 배치.
- **자동 평타**: 사거리 안에 적이 있으면 `PlayerController`가 매 프레임 가장 가까운 적을 향해 평타를 시도(쿨다운은 `CharacterAttack`이 게이트). 좌클릭 수동 평타도 유지.

**Player.cs** 는 형제 컴포넌트를 `[SerializeField]`로 보관하고 `Init()`에서 일괄 상태 리셋(리스폰 멱등).
외부에는 `Damageable` / `Skills` 만 노출 — **표면적 최소화**.

### 5.3 Camera (`CameraFollow.cs`)

처음엔 `OBJ_Camera`를 Player 자식으로 두는 안을 잡았다가
**캐릭터 jitter / 충돌 반동이 그대로 화면에 전달**되는 문제로 폐기했습니다.

현재: 씬 최상위 OBJ_Camera + `CameraFollow` 컴포넌트.
- `GameEvents.OnPlayerSpawned` 구독으로 타겟 자동 바인딩.
- `LateUpdate` + `Vector3.SmoothDamp`로 부드럽게 추격.
- 추후 흔들기/줌 같은 연출이 이 위에 깔끔하게 얹힘.

### 5.4 AI / Behavior Tree (`Assets/Scripts/AI/`)

외부 패키지 없이 **경량 BT 직접 구현**:

```
Root: Selector
 ├─ Sequence "Flee"   (HP ≤ FleeThreshold && 적 존재 → 도주)
 ├─ Sequence "Attack" (Aware && InRange → 정지 + AttackInterval마다 발사)
 ├─ Sequence "Chase"  (Aware → 추격)
 └─ Action "Patrol"   (waypoint 무작위 선택 + 막힘 감지)
```

- 모든 액션이 1프레임 단위 `Success` 반환 → Selector가 매 프레임 위에서 재평가 → HP가 떨어지면 추격 중에도 즉시 Flee로 전환.
- **Aware 히스테리시스**: `DetectionRange`(8m) < `LoseSightRange`(12m) → 끈끈한 어그로.
- **Patrol 막힘 감지**: `CharacterController.Move`의 `CollisionFlags` 를 `CharacterMotor.LastCollisionFlags`로 노출, AI가 `& Sides`로 보고 즉시 새 waypoint 재선택.

### 5.5 Pooling & Lifecycle

- `EnemySpawner`: Queue 풀 + 활성 List. Spawn/Despawn은 `SetActive` 토글만.
- `Enemy` 는 Spawner를 **모름**. `Damageable.OnDied → Enemy.OnDespawnRequested` 이벤트로만 통지.
- Spawn 시 `-= / +=` 패턴으로 핸들러 idempotent 등록.

### 5.6 Editor Tools (`Assets/Scripts/GoogleSheetDataLoader/Editor/`)

- OAuth2 인증 + Google Sheets API v4 호출.
- 시트의 `_Enum` 탭을 읽어 `Assets/Scripts/GeneratedEnums/GameEnum.cs` 자동 생성 (수동 편집 금지).
- 시트 한 탭 = 한 JSON. 런타임은 `Resources.Load<TextAsset>` 으로만 접근.

---

## 6. 폴더 구조

```
Assets/
├─ Resources/
│  ├─ GoogleSheetData/         JSON sync 결과물 (DB 영역, 코드가 손대지 않음)
│  └─ Prefabs/                 OBJ_Player, OBJ_Enemy, OBJ_Camera, VFX...
├─ Scripts/
│  ├─ AI/                      Jinhyeong_AI       (Enemy + EnemyAI + Spawner)
│  │  └─ BehaviorTree/         Jinhyeong_AI.BehaviorTree
│  ├─ Character/               Jinhyeong_Character (Player/Motor/Facing/Camera)
│  ├─ Common/                  Jinhyeong_Common    (CommonConfig 전역 상수)
│  ├─ GeneratedEnums/          시트 자동 생성, 손대지 않음
│  ├─ GoogleSheetDataLoader/   Editor 전용 데이터 sync 툴
│  ├─ Input/                   Jinhyeong_Input     (IInputProvider 추상화)
│  ├─ JsonParsing/             공용 데이터 파싱 인프라
│  ├─ Managers/                Jinhyeong_Managers  (GameEvents/Pool/Skill/Character)
│  ├─ Shaders/                 Outline_Lit 셰이더 + smooth normal baker
│  └─ SkillSystem/             Jinhyeong_SkillSystem (스킬 데이터 + 런타임)
│     └─ Runtime/              발사체/타겟팅/컴파일러
├─ Settings/                   InputBindings.asset, URP Asset
└─ Scenes/Main.unity
```

**컨벤션**: 폴더 = 네임스페이스 1:1. 모든 namespace에 `Jinhyeong_` 접두어.

---

## 7. 빌드 / 실행

```bash
# 요구사항
Unity 2022.3 LTS
```

1. 프로젝트 클론 후 Unity Hub에서 열기.
2. (선택) Google Sheets sync가 필요하면 `Tools > Jinhyeong > Google Sheet Loader` 에서 OAuth2 인증.
   (이미 동기화된 JSON이 `Resources/GoogleSheetData/`에 있어 sync 없이도 실행 가능)
3. `Assets/Scenes/Main.unity` 오픈 → `Play` → 시작 화면에서 Q/W/E/R 슬롯 스킬을 골라 **GAME START**.
4. 조작:
   - **방향키(↑↓←→)**: 이동 (8방향)
   - **Q / W / E / R**: 스킬 시전 (시작 화면에서 슬롯별로 스킬 선택)
   - **평타**: 사거리 안 적이 있으면 자동. (원하면 마우스 좌클릭으로 수동)
   - **마우스 우클릭 드래그 / 휠**: 카메라 회전 · 줌

> VFX 프리팹을 새로 만들거나 갱신하려면 `Tools > Skills > Rebuild VFX Prefabs (URP Particles)` 실행.
> 스킬이 참조하는 `Visual` 키(`vfx_bolt` 등 9종)에 대응하는 파티클 프리팹을 생성하고 Addressables에 등록한다.

---

## 8. 스킬 제작 가이드

스킬은 **코드가 아니라 구글 시트(SSOT)** 에서 만든다. 클라이언트에는 스킬별 하드코딩이 없고,
시트의 노드 조합을 런타임에 행동 트리로 컴파일해 발동한다. 새 스킬 = **시트에 행 추가**.

### 8.1 3개 테이블

| 시트 탭 | 역할 | 핵심 컬럼 |
|---|---|---|
| `Skill` | 스킬 메타 1행 | `id`, `name`, `desc`, `max_level`, `visual_path` |
| `SkillLevel` | 스킬별 레벨 모디파이어 | `id`, `skill_id`, `level`, `modifier0..5`, `value0..5` |
| `SkillBTNode` | 행동 트리 노드들 | `skill_id`, `node_id`, `parent_id`, `order`, `node_type`, `param0..4`, `value0..4` |

### 8.2 트리 구조 규칙

- 스킬 하나는 `SkillBTNode`에서 `skill_id`가 같은 행들의 집합. `parent_id`로 부모-자식을 엮고, 같은 부모의 자식은 `order`대로 실행된다.
- `parent_id = 0` 인 노드가 루트. 보통 루트는 `Sequence`.
- **표준형** (평타/틱 발동 스킬):

```
Sequence (node 1, parent 0)
├─ order 0: Trigger      (TriggerOnAttack / TriggerOnTick)   ← 발동 게이트
├─ order 1: Targeting    (TargetNearest 등)                  → ctx.Targets/Direction 채움
├─ order 2: Hit          (HitArea 등)                        → 히트 규칙 등록
├─ order 3: Despawn      (DespawnAfterHits 등)               → 소멸 규칙 등록
├─ order 4: (선택) DebuffOnHit / BuffSelf
└─ order 5: Launch       (LaunchInstant 등)                  → VFX 스폰 + 위 규칙 적용
```

Sequence는 자식이 하나라도 `Failure`면 멈춘다 → **Trigger가 실패하면 그 프레임은 발동 안 함**.

### 8.3 파라미터 (`ESkillParamKey`)

노드별로 자주 쓰는 param: `Cooldown`(틱 주기/쿨), `Chance`(발동 확률 %), `Range`(탐색 반경),
`Damage`, `Radius`(피해 범위), `Speed`·`MaxDistance`·`ArcHeight`(발사 모션), `MaxBounces`(체인 연쇄 **총 대상 수** — 3이면 3명),
`MaxPerTarget`(다중 타겟 수), `Value`(디스폰 임계), `Visual`(VFX 키), `BuffId`/`DebuffId`.

**레벨 스케일 규칙**: `SkillBTNode`의 `value`를 **비워두면** 그 param은
해당 레벨의 `SkillLevel` modifier 값을 따라간다(레벨업으로 강화). 값을 넣으면 고정값.
예) `HitArea` 노드에 `Damage`(빈 값) + `Radius=2.5` → 데미지는 레벨별, 반경은 항상 2.5.

### 8.4 슬롯(Q/W/E/R) vs 자동 발동

- 스킬을 **슬롯 키에 배치**하면(로드아웃 `SlotKey`) 그 키를 눌렀을 때만 발동하는 **수동 스킬**이 된다.
- 수동 발동은 트리의 **Trigger / Chance / Cooldown 게이트를 통과**시킨다 — 키 입력 자체가 발동 조건(`SkillContext.ManualCast`). 그래서 "마구 난사"가 된다.
- `SlotKey`가 없으면(`None`) 매 프레임 자동으로 틱하는 **자동 스킬** — 이때는 Trigger/Cooldown/Chance 게이트가 그대로 살아있다.

### 8.5 작성 예 — `Split Bolt` (id 1003)

`Skill`: `1003 | Split Bolt | OnAttack 시 전방 직선 발사 | 5 |`

`SkillBTNode` (skill_id=1003):

| node_id | parent_id | order | node_type | param0/value0 | param1/value1 | param2/value2 |
|---|---|---|---|---|---|---|
| 1 | 0 | 0 | `Sequence` | | | |
| 2 | 1 | 0 | `TriggerOnAttack` | `Chance` / (빈값→레벨) | | |
| 3 | 1 | 1 | `TargetNearestForward` | `Range` / (빈값→레벨) | | |
| 4 | 1 | 2 | `HitArea` | `Damage` / (빈값→레벨) | `Radius` / `0.5` | |
| 5 | 1 | 3 | `DespawnAfterHits` | `Value` / `1` | | |
| 6 | 1 | 4 | `LaunchStraight` | `Speed` / `15` | `MaxDistance` / `10` | `Visual` / `vfx_bolt` |

`SkillLevel` (skill_id=1003, level 1): `Chance=12`, `Cooldown=0.2`, `Range=6`.

### 8.6 시트 → 게임 반영 절차

1. 구글 시트(SSOT)의 `Skill` / `SkillLevel` / `SkillBTNode` 탭에 행 추가.
2. Unity에서 `Tools > Jinhyeong > Google Sheet Loader`로 sync → `Resources/GoogleSheetData/*.json` 갱신. *(생성된 JSON은 직접 편집하지 않는다.)*
3. 새 `Visual` 키를 썼다면 `Tools > Skills > Rebuild VFX Prefabs` 실행(프리팹 + Addressables 등록).
4. Play → 시작 화면에서 슬롯에 새 스킬을 골라 시전.

---

## 9. 회고 — 시니어가 보면 잡힐 만한 부분

스스로 개선 여지가 보이는 지점들:

- **Despawn**: `OnWallHitDespawn`이 아직 미구현. SkillEffect에 Trigger 콜라이더 콜백 한 줄이 필요.
- **버프 스택 정책**: 같은 BuffId가 여러 번 들어오면 `ActiveStatusEffect`가 중복 부착되어 enter/exit가 각각 돕니다. 스택/리프레시 정책을 외부에서 선택할 수 있게 빼는 게 다음 작업.
- **카메라 데드존**: 현재 SmoothDamp만 — DFO처럼 화면 중앙 박스 안에선 카메라 정지하는 데드존을 추가하면 시야 안정성 ↑.
- **Input System 전환**: 현재 레거시 `UnityEngine.Input`. 같은 `IInputProvider` 인터페이스를 새 InputSystem 구현으로 교체하면 동작 변경 없이 마이그레이션 가능 (이걸 의식해서 추상화를 깐 것).
- **Damageable.All 정적 리스트**: 도메인 리로드 비활성 환경에서 잔존 가능. `RuntimeInitializeOnLoadMethod` 로 클리어 처리 검토 필요.

---

## 10. 라이선스

개인 학습용 프로젝트.

