# Unity Skill System Module

> 던전앤파이터 스타일 2.5D 액션 RPG의 **데이터 주도형 스킬 시스템**과 **BT 기반 적 AI**를
> 한 프로젝트에 묶어낸 Unity 클라이언트 개인 프로젝트.

엔진: **Unity 2022 LTS / URP**
스코프: 클라이언트 단일 씬, 60fps 기준 동작 확인.

---

## 1. 프로젝트 한눈에

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

스킬 한 개를 **6개 역할 노드** 로 정의:

| 역할 | 단/복수 | 예시 노드 |
|---|---|---|
| Trigger | 1 | `OnAttackTrigger`, `OnTickTrigger`, `OnOreBreakTrigger` |
| Targeting | 1 | `Self`, `AreaNear`, `ScreenAll`, `Ray`, `NearestDirection` |
| Launch | 1 | `Instant`, `Straight`, `Parabolic`, `Curve` |
| Hit | 1 | `Single`, `AoE`, `Beam`, `ChainLightning`, `DeathChain` |
| Despawn | 1 | `Duration`, `OnHit`, `OnBounceLimit`, `OnWallHit` |
| Side Effect | N | `ApplyBuffSelf`, `ApplyDebuffOnHit` |

레벨 modifier가 노드 param을 덮어쓰는 **(node default → level modifier → fallback)** 룩업 순서로
"한 노드를 레벨업으로 강화" 패턴을 표준화했습니다.

**핵심 파일**
- `SkillRegistry.cs` — 멱등 LoadAsync, 동시 호출 시 같은 Task 공유
- `SkillCompiler.cs` — Nodes 리스트를 역할 슬롯으로 평탄화
- `Runtime/SkillObject.cs` — 시전자 컨테이너, `SkillLoadout` SO 참조 기반 장착
- `Runtime/SkillEffect.cs` — 발사된 VFX 1개의 라이프 (모션 + Hit + Despawn)
- `Runtime/ActiveStatusEffect.cs` — 버프/디버프 런타임 인스턴스, enter/tick/exit 라이프사이클

**확장**: 새 Hit 패턴 추가 절차는 *시트 enum 추가 → SkillCompiler switch 한 줄 → SkillEffect.ProcessHit switch 한 줄* 의 3 step.

### 5.2 Character / Input (`Assets/Scripts/Character/`, `Assets/Scripts/Input/`)

DFO식 2.5D 컨트롤:

- **Motor**: `CharacterController` 기반 8방향 이동 + 중력 + `SpeedMultiplier`.
- **Facing**: 마우스 추격 회전 ❌. **좌우 플립 only** (`FacingSign ∈ {-1, +1}`). 이동 가로 입력 부호로 갱신.
- **Attack**: 부채꼴 평타. 데이터는 인스펙터(빠른 튜닝), 발동은 `SkillObject` 외부.
- **Input**: `IInputProvider` 추상화 — PC/모바일 구현 교체 가능. PC는 `KeyboardInputProvider`.
- **Bindings**: `ScriptableObject` 매핑. 이동(WASD)과 스킬 슬롯(Q E R F Z X C V) 키 충돌 없게 배치.

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
3. `Assets/Scenes/Main.unity` 오픈 → `Play`.
4. 입력:
   - **WASD**: 이동 (8방향)
   - **마우스 좌클릭**: 평타
   - **Q / E / R / F / Z / X / C / V**: 스킬 슬롯 (OBJ_Player의 `SkillObject.Loadout`에 등록한 스킬)

### 새 스킬 장착하기

1. Project 창 우클릭 → `Create > Jinhyeong > Skill > Skill Loadout` → `MyLoadout.asset`
2. Entries에 `SkillId` / `Level` / `SlotKey` 추가
3. `OBJ_Player.SkillObject.Loadout` 필드에 드래그

---

## 8. 회고 — 시니어가 보면 잡힐 만한 부분

스스로 개선 여지가 보이는 지점들:

- **Despawn**: `OnWallHitDespawn`이 아직 미구현. SkillEffect에 Trigger 콜라이더 콜백 한 줄이 필요.
- **버프 스택 정책**: 같은 BuffId가 여러 번 들어오면 `ActiveStatusEffect`가 중복 부착되어 enter/exit가 각각 돕니다. 스택/리프레시 정책을 외부에서 선택할 수 있게 빼는 게 다음 작업.
- **카메라 데드존**: 현재 SmoothDamp만 — DFO처럼 화면 중앙 박스 안에선 카메라 정지하는 데드존을 추가하면 시야 안정성 ↑.
- **Input System 전환**: 현재 레거시 `UnityEngine.Input`. 같은 `IInputProvider` 인터페이스를 새 InputSystem 구현으로 교체하면 동작 변경 없이 마이그레이션 가능 (이걸 의식해서 추상화를 깐 것).
- **Damageable.All 정적 리스트**: 도메인 리로드 비활성 환경에서 잔존 가능. `RuntimeInitializeOnLoadMethod` 로 클리어 처리 검토 필요.

---

## 9. 라이선스

개인 학습용 프로젝트.

