# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PressureSalvage** is a first-person game built with Unity 6 (6000.3.13f1) using the Universal Render Pipeline (URP) and the new Unity Input System. The gameplay centers on player movement, camera control, and object interaction/carrying mechanics.

## Development Commands

This is a Unity project — there is no CLI build command. Open the project in the Unity Editor (version 6000.3.13f1+) via the Unity Hub. The Visual Studio solution `PressureSalvage.sln` is used for editing C# scripts.

- **Open project**: Unity Hub → Add → select the repo root
- **Primary scene**: `Assets/Scenes/MainGame.unity`
- **Play/test**: Press Play in the Unity Editor
- **Build**: File → Build Settings in the Unity Editor

There are no automated tests in this project.

## Architecture

### Input Pipeline

`Assets/Input/PlayerInput.inputactions` defines all bindings. `Assets/Input/PlayerInput.cs` is auto-generated from that asset — do not edit it directly. `Assets/Scripts/InputManager.cs` is the central hub that subscribes to input events and forwards them to player subsystems.

Input actions (all in the **OnFoot** map):
- Movement (WASD) → `PlayerMotor.ProcessMove(Vector2)`
- Look (mouse delta) → `PlayerLook.ProcessLook(Vector2)`
- Jump (Space) → `PlayerMotor.Jump()`
- Sprint (Left Shift) → `PlayerMotor.Sprint(bool)`
- Interact (E) → `PlayerInteract`
- Drop (Q) → `PlayerInteract`

### Player Systems (`Assets/Scripts/Player/`)

| Script | Responsibility |
|---|---|
| `PlayerMotor.cs` | Movement & physics via `CharacterController` (walk 3 m/s, sprint 5 m/s, gravity −9.8) |
| `PlayerLook.cs` | Camera pitch (±80°) and body yaw via mouse delta |
| `PlayerInteract.cs` | Raycast (3-unit range) interaction, outline highlight, single-item carry tracking |
| `PlayerUI.cs` | Updates TextMeshPro prompt text shown to the player |

### Interactable System (`Assets/Scripts/Interactable.cs` + `Assets/Scripts/Interactable/`)

`Interactable` is the abstract base class. `PlayerInteract` calls `BaseInteract()` on hit objects, which:
1. Optionally invokes a `UnityEvent` via `InteractionEvent` (if `useEvents = true`) — allows designer wiring in the Inspector
2. Calls the virtual `Interact()` method — overridden by derived classes

Concrete subclasses:
- `EventOnlyInteractable` — no code logic, fully driven by UnityEvents
- `PickupItem` — logs pickup and destroys the object
- `OneHandCarryItem` — parents object to the player's carry point, toggles `Rigidbody.isKinematic`
- `TwoHandCarryItem` — stub placeholder

### Outline / Highlight Effect

`PlayerInteract` manipulates a `_Scale` property on the hovered object's material to drive an outline shader effect (scale 1.05 when hovered, 1.0 when not). The shaders live in `Assets/Shaders/`.

### Key Packages

- `com.unity.inputsystem` — new Input System
- `com.unity.render-pipelines.universal` — URP
- `com.unity.ai.navigation` — NavMesh (not yet wired to scripts)
- TextMesh Pro — UI text
