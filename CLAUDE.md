# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PressureSalvage** (working title: **ABYSSAL**) is a first-person underwater survival horror game built with Unity 6 (6000.3.13f1) using URP and the new Unity Input System.

### Game World
- **Setting**: Talon-9 — an ocean planet. Players are trapped workers on a deteriorating orbital *giàn khoan* (space station/drilling platform) owned by **Deep-Six Salvage Corp**.
- **Premise**: Players are bound by a 99-year labor contract called the **Breathing Debt**. The only way out is the **Escape Route** — a hidden path discovered through 10 in-game days of dives. The contract includes a **biological reinstatement clause**: an indentured worker who drowns is re-grown by the station so they can keep working off the debt — death does not release you.
- **Loop**: Each successful dive to the ocean bed = 1 in-game day. Players salvage items to meet daily quotas (1000₡ → 1500₡ → 2000₡ scaling across 10 days) while discovering the escape path.
- **Endings**: Escape Ending (complete Escape Quest before Day 10) or Trapped Ending (fail → loop restarts, attempt counter increments).

### Core Mechanics
Player movement, oxygen management, object carrying, enemy AI, and a 10-day progression system with a defined escape-route endgame.

## Development Commands

This is a Unity project — there is no CLI build command. Open the project in Unity Hub (version 6000.3.13f1+). The Visual Studio solution `PressureSalvage.sln` is used for editing C# scripts.

- **Primary scene**: `Assets/Scenes/MainGame.unity`
- **Play/test**: Press Play in the Unity Editor
- **Build**: File → Build Settings in the Unity Editor

There are no automated tests in this project.

## Architecture

### Input Pipeline

`Assets/Input/PlayerInput.inputactions` defines all bindings. `Assets/Input/PlayerInput.cs` is auto-generated — do not edit directly. `Assets/Scripts/InputManager.cs` is the hub that subscribes to input events.

**Note:** Jump and Sprint are wired in `InputManager.cs`, but Interact (E) and Drop (Q) are subscribed directly in `PlayerInteract.Start()` — this is an intentional inconsistency.

Input actions (all in the **OnFoot** map):
- Movement (WASD) → `PlayerMotor.ProcessMove(Vector2)`
- Look (mouse delta) → `PlayerLook.ProcessLook(Vector2)`
- Jump (Space) → `PlayerMotor.Jump()`
- Sprint (Left Shift) → `PlayerMotor.Sprint(bool)`
- Interact (E) / Drop (Q) → wired inside `PlayerInteract`

### Player Systems (`Assets/Scripts/Player/`)

| Script | Responsibility |
|---|---|
| `PlayerController/PlayerMotor.cs` | Movement & physics via `CharacterController` (walk 3 m/s, sprint 5 m/s, gravity −9.8). Exposes `IsSprinting` for `OxygenSystem`. |
| `PlayerController/PlayerLook.cs` | Camera pitch (±80°) and body yaw via mouse delta (sensitivity: 30) |
| `PlayerController/PlayerInteract.cs` | Raycast (3-unit range), outline highlight, single-item carry tracking, exposes `CarriedWeightKg` from held item's `ItemData` |
| `PlayerController/OxygenSystem.cs` | Oxygen drain with sprint/weight/panic modifiers; fires `OnOxygenChanged` and `OnOxygenDepleted` UnityEvents |
| `PlayerView/PlayerUI.cs` | TextMeshPro prompt text + oxygen bar fill with color coding (cyan >60%, orange >25%, red ≤25%) |

### Oxygen System (`OxygenSystem.cs`)

Drain rate = `baseDrainRate` (1/s) + `sprintDrainBonus` when sprinting (1.5/s) + `weightKg × weightDrainPerKg` from carried item + active temporary debuffs via `AddDrain(drainPerSec, duration)`.

Panic triggers at <20% oxygen (`panicThreshhold`), multiplying all drain by `panicMultiplier` (1.5×). Property `IsPanic` is computed but not yet wired to post-processing (Phase 3). On depletion (`currentOxygen ≤ 0`): fires `OnOxygenDepleted` and stops updating.

### Interactable System (`Assets/Scripts/`)

`Interactable` is the abstract base. `PlayerInteract` calls `BaseInteract()` which:
1. Optionally invokes a `UnityEvent` via `InteractionEvent` component (if `useEvents = true`)
2. Calls virtual `Interact()` — overridden by subclasses

Concrete subclasses:
- `EventOnlyInteractable` — fully designer-driven via UnityEvents
- `PickupItem` — logs pickup and destroys the object
- `CarryItem` (abstract) — implements `ICarryable`; disables collider, sets `Rigidbody.isKinematic`, lerps toward carry `Transform` each frame
- `OneHandCarryItem` / `TwoHandCarryItem` — set `IsTwoHandRequired`; two-hand blocks ALL further carry but not non-carry interactions

`Assets/Editor/InteractableEditor.cs` — custom Inspector that auto-adds/removes the `InteractionEvent` component when `useEvents` is toggled.

### Item System (`Assets/Scripts/Item/`)

`ItemData` is a ScriptableObject with: `itemName`, `ItemRank` enum (F/D/C/B/A/S), `minValue`/`maxValue` (economy), `weightKg`, `canBreak`.

`CarryItem` holds a reference to `ItemData`; `PlayerInteract.CarriedWeightKg` reads `currentCarried.ItemData.weightKg` to feed `OxygenSystem`.

### Outline / Highlight Effect

`PlayerInteract` uses a `MaterialPropertyBlock` on material slot index 1 of the hovered `MeshRenderer`. Sets `_Scale` shader property: 1.05 when hovering, 0 otherwise. Interactable objects must have a second material that reads `_Scale`. Shaders live in `Assets/Shaders/`.

### Enemy System (`Assets/Scripts/Enemy/`)

`EnemyBase` (abstract) requires `NavMeshAgent` and `EnemyAnimator`. Each enemy is configured by an `EnemyStats` ScriptableObject (speeds, view radius/angle, attack range/cooldown, damage, wander params).

Vision check in `Update()`: distance → view cone angle → line-of-sight raycast at 1.5m height against obstacle layer. If visible → `ChaseBehavior()`, else → `PatrolBehavior()` (both abstract).

`Zombie.cs` is the first concrete enemy: patrols idle, chases by navigating to player, attacks via animation trigger at `attackRange` with cooldown. Damage dealing is stubbed.

### Key Packages

- `com.unity.inputsystem` 1.19.0 — new Input System
- `com.unity.render-pipelines.universal` 17.3.0 — URP
- `com.unity.ai.navigation` 2.0.12 — NavMesh (used by enemy system)
- `com.unity.behavior` 1.0.15 — Behavior trees (imported, not yet used — current enemy AI is imperative)
- `com.unity.cinemachine` 3.1.6 — imported, not yet used in scripts
- `com.unity.multiplayer.center` 1.0.1 — prep for Phase 7 co-op

## Roadmap

- [x] **Phase 1** — Oxygen System (`OxygenSystem.cs` complete; `PanicEffect.cs`/`PlayerDeath.cs` stubs remain)
- [x] **Phase 2** — Item Data System (`ItemData` ScriptableObject + weight integrated into `OxygenSystem` via `CarriedWeightKg`)
- [ ] **Phase 3** — HUD & Visual Feedback (oxygen gauge, panic Vignette+ChromaticAberration, heartbeat SFX placeholder, `PlayerDeath.cs`, Day Counter "DAY X / 10" UI)
- [ ] **Phase 4** — Monsters: San Hô Ký Sinh → Hermit-Bot → Siren-Diver → Cá Mập Xương → Leviathan / Đèn Lồng / Rùa / Con Hàu Đỏ (`Zombie.cs` is first stub; spawn density scales per Escape Quest phase)
- [ ] **Phase 5** — Economy & Quota System (`QuotaManager`, `SellPoint`; quota scales 1000₡/1500₡/2000₡ across the 10 days — see below)
- [ ] **Phase 6** — Zone & Level Design (Tier 1/2/3 depth zones, `DepthManager`; Deep Zone contains Escape Pod coordinates at 48.7°N 112.3°W depth 400m)
- [ ] **Phase 7** — Escape Route Progression System (see below)
- [ ] **Phase 8** — Co-op Multiplayer (Unity Netcode for GameObjects)

### Phase 5 Detail — Economy, Upgrades & Quota Penalty

**Upgrade loop (the core engagement loop):** loot → sell for ₡ → buy/lease gear at the station company store → dive deeper/longer → reach higher-value loot. Gear is leased from Deep-Six and **adds to the Breathing Debt** (thematic — nothing is free).

- **Oxygen Tank tiers** (T1 → T2 → T3): each tier raises `OxygenSystem.maxOxygen`, letting the player stay down longer.
- **Carry capacity upgrade by mechanism** (not just a bigger number): bare hands (1 item) → salvage satchel (several light items) → cargo sled/drone (large weight budget). This reuses the existing `weightKg` → oxygen-drain link (`PlayerInteract.CarriedWeightKg` → `OxygenSystem`): carrying more drains oxygen faster, so a bigger tank and more carry capacity naturally balance each other.

**Quota Penalty — Strike + Carry-over:** each day's quota due = `baseQuota` (1000/1500/2000) + carry-over from the previous day.

When the player **fails to meet the day's quota** (including dying = earning 0₡):
1. **+1 strike** (`strikes`, max 3).
2. **80% of the shortfall** carries into the next day's quota (`carryOverRate = 0.8f`), **compounding**:
   - `quotaToday = baseQuota + carriedFromYesterday`
   - `shortfall = max(0, quotaToday - earnedToday)`
   - `carriedToTomorrow = shortfall * 0.8` (includes already-carried debt → debt grows, but slower than 100%)
3. If the failure was caused by death: all carried loot is lost (see Reinstatement Clause below).

**Loss condition → Trapped Ending** (loop restarts, attempt counter +1):
- `strikes == 3` → contract default → Trapped Ending **immediately** (does not wait for Day 10), OR
- Reaching **Day 10** without the Escape Quest complete.

UI: a "STRIKE x/3 — Deep-Six delinquency notice" warning fires on each strike.

### Phase 7 Detail — Escape Route Progression

**`GameProgressionManager`** — singleton tracking `day` (1–10), `questPhase`, economy state (`strikes`, `carriedQuota`, `reinstatementCount`), flags (`logsCollected`, `keyCardObtained`, `engineerMet`, `podLocated`). Fires events: `OnDiveComplete`, `OnDay10`, `OnEscapePodEnter`.

**Reinstatement Clause (death model):** when `OxygenSystem.OnOxygenDepleted` → `PlayerDeath.OnPlayerDrown` → `GameProgressionManager`:
1. The dive fails → no quota income that day (counts as a missed quota → triggers the Strike + Carry-over penalty above).
2. All loot the player was carrying is lost.
3. The day still advances toward Day 10 → the player respawns at the station.
4. `reinstatementCount` increments (feeds the Trapped Ending's attempt counter flavor).

Death therefore **does not end the run** — it costs the player a dive, their loot, a strike, and carried debt, but they keep working off the Breathing Debt.

**3-phase Escape Quest:**
| Quest Phase | Days | Objective | Key Items |
|---|---|---|---|
| Discovery | 1–3 | Collect 3 Encrypted Data Chips (random Tier 1–2 spawn) | Encrypted Data Chip (Rank B) |
| Exploration | 4–7 | Find Log #2 + Log #3 + Ancient Key Card in Deep Zone | Ancient Key Card (Rank A, 1kg) |
| Preparation | 8–9 | Meet Engineer NPC on station OR locate pod manually | — |

**On Day 10:** Station UI shows "BOARD ESCAPE POD" → final gauntlet dive → Siren-Diver boss → reach pod → launch.

**Endings:**
- **Escape Ending**: All 3 logs + Key Card + Engineer/pod found + Day 10 dive success → cinematic + credits
- **Trapped Ending**: Day 10 without quest complete (or 3 strikes earlier) → "Day 11. The quota reset." → loop restarts, attempt counter +1

**New scripts needed:**
- `GameProgressionManager.cs` — day tracking, quest state, economy (strikes/carried quota/reinstatements), difficulty scaling
- `QuestLog.cs` (UI) — persistent quest panel
- `LootSpawner.cs` — places logs + key card by zone/day range
- `EscapePod.cs` — entrance trigger + launch sequence
- `FinalGauntlet.cs` — heightened spawn rates on the Day 10 dive
- `EndingManager.cs` — plays Escape or Trapped ending cinematics
- `EngineerNPC.cs` — station NPC with dialogue trigger
