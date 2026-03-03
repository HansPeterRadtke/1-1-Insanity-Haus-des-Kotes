# 1&1 Insanity

Godot 4.6 + C# prototype project with a working main menu, in-game pause menu, configurable controls, save/load slots, player stats, pickups, and dedicated gameplay test scenes.

## Current State

The project now boots into a proper main menu and supports a playable 2D platformer prototype loop:

- main menu with `New Game`, `Load Game`, `Settings`, `Credits`, `Quit`
- in-game pause overlay on `Esc`
- in-game `Continue` and `Save Game`
- save/load slots with delete
- configurable keybinds, audio, and video settings persisted to disk
- player stats with HUD display
- consumable ammo and limited ranged shots
- collectible pickups for health, ammo, speed boost, and coins
- mechanic test scene with:
  - movement
  - sprint
  - crouch
  - jump
  - melee
  - ranged shooting
  - interaction
  - pickups
  - player damage / respawn loop
  - multiple enemy types

## Project Entry Point

The configured main scene is:

- `res://Scenes/Menu/MainMenu.tscn`

Shortest start command from the project directory:

```bash
godot --path .
```

From any directory:

```bash
godot --path /data/src/github/1-1-Insanity-Haus-des-Kotes
```

## Core Structure

### Menu / Global Runtime

- `Scenes/Menu/MainMenu.tscn`
- `Scenes/Menu/SettingsManager.tscn`
- `Scenes/Game/GameManager.tscn`

Autoload singletons:

- `SettingsManager` -> `res://Scenes/Menu/SettingsManager.tscn`
- `GameManager` -> `res://Scenes/Game/GameManager.tscn`

Main responsibilities:

- `Scripts/Menu/MainMenu.cs`
  - builds the menu UI in code
  - startup menu and in-game pause menu use the same scene/script
  - handles load/save slot UI, settings submenus, and runtime button state
- `Scripts/Menu/SettingsManager.cs`
  - stores bindings, audio, and video settings in `user://settings.cfg`
  - applies `InputMap` bindings at runtime
  - supports keyboard and mouse rebinding
- `Scripts/Game/GameManager.cs`
  - starts the game scene
  - controls pause overlay
  - handles save/load/delete for save slots
  - keeps an in-memory runtime snapshot and writes it to slot files

### Gameplay

- `Scenes/Tests/MechanicsTest.tscn`
  - current primary gameplay scene
  - contains the player, HUD, switch, pickups, and enemy variants
- `Scenes/Game/StatsPickupTest.tscn`
  - headless regression scene for player stats, HUD, and pickup behavior
- `Scenes/Player/player.tscn`
  - player scene
- `Scripts/Player/Player.cs`
  - movement, jump, crouch, interact, facing direction, pause input, health, ammo, coins, boosts, respawn
- `Scripts/Player/Attacks.cs`
  - melee and ranged player attacks, ammo consumption
- `Scripts/Player/BlueBalls/BlueBall.cs`
  - player projectile
- `Scenes/hud.tscn`
  - HUD scene shown in the active gameplay test scene
- `Scripts/UI/HudController.cs`
  - updates HP, ammo, coins, movement speed, and boost display

### Enemies

- `Scripts/Enemies/EnemyBody2D.cs`
  - common enemy base
  - health, damage, death, generic save participation
- `Scripts/Enemies/Dummy/DummyEnemy.cs`
  - simple target dummy
- `Scripts/Enemies/MeleeEnemy.cs`
  - follows the player and damages the player in close range
- `Scripts/Enemies/RangedEnemy.cs`
  - keeps distance and spawns damaging projectiles
- `Scripts/Enemies/ExplosiveEnemy.cs`
  - rushes and detonates on close range, damaging the player
- `Scripts/Enemies/EnemyProjectile.cs`
  - enemy projectile with player damage on hit

### Interaction

- `Scripts/Interaction/IInteractable.cs`
- `Scripts/Interaction/SwitchBody2D.cs`
- `Scenes/Props/InteractionSwitch.tscn`

The current interactable object is a switch with ON/OFF visual state.

### Pickups

- `Scripts/Pickups/Pickup.cs`
  - common pickup behavior
- `Scenes/Pickups/HealthPickup.tscn`
- `Scenes/Pickups/AmmoPickup.tscn`
- `Scenes/Pickups/SpeedPickup.tscn`
- `Scenes/Pickups/CoinPickup.tscn`

Pickup effects:

- health pickup -> restores HP
- ammo pickup -> restores ranged ammo
- speed pickup -> temporary movement speed boost
- coin pickup -> increments a standard collectible counter

## Controls

Current default controls:

- `A` -> move left
- `D` -> move right
- `Space` -> jump
- `Shift` -> sprint
- `C` -> crouch toggle
- `W` -> stand up from crouch
- `Left Mouse` -> shoot toward mouse
- `Q` -> shoot forward in facing direction
- `F` -> melee
- `E` -> interact
- `Esc` -> open/close in-game pause menu

### Crouch Behavior

- `C` toggles crouch on/off
- `W` only disables crouch
- `Space` while crouched first tries to stand up, then jumps immediately in the same keypress if there is headroom
- while crouched:
  - movement is slower
  - jump is not performed unless the player can stand back up
  - the collision height is reduced so the player can pass under low ceilings

## Combat

### Melee

- Melee uses a close overlap area first, then a short forward raycast fallback
- This avoids the old dead zone where enemies were too close to hit

### Shooting

- Mouse shooting uses the current cursor direction
- Forward shooting (`Q`) uses the current facing direction
- Ranged shots consume ammo
- If ammo is empty, ranged shooting is blocked
- Projectiles are added to the active scene so they participate in save/load state

## Player Stats / HUD

The player now has a conventional baseline stat set:

- health / max health
- ammo / max ammo
- coin counter
- temporary movement speed boost
- short post-hit invulnerability window
- respawn on death

HUD location:

- top-left corner of the gameplay screen

HUD contents:

- HP text and bar
- ammo text and bar
- coin count
- current move speed
- active boost state and remaining time

## Interaction

Interaction now checks:

1. nearby interactables in a circular radius around the player
2. forward raycast fallback

This allows using the switch while standing directly on it, not only from in front.

## Save / Load System

Save slots are stored as:

- `user://save_slot_1.cfg`
- `user://save_slot_2.cfg`
- `user://save_slot_3.cfg`

### Current Save Model

The save system is not hardcoded to only player/switch data anymore.

`GameManager` maintains one in-memory runtime snapshot for the active game scene:

- scene path
- game active state
- runtime node list

For each runtime node in the active scene tree, it stores:

- node path
- parent path
- node name
- Godot class name
- originating scene file if available
- script type for custom C# nodes
- selected built-in runtime state
- serializable reflected C# instance fields
- optional custom `ISaveStateNode` payload

On load, it:

- reloads the saved scene
- recreates missing runtime-created nodes when possible
- restores saved state onto nodes
- removes nodes not present in the saved snapshot

This is currently intended to restore the active gameplay scene runtime state, not arbitrary engine-global state outside the scene tree.

The runtime snapshot now also covers the player stat layer and runtime-created pickup/test objects inside the active gameplay scene.

### Slot UI

Each slot supports:

- `Load`
- `Save`
- `Delete`

Slot labels show:

- timestamp
- scene name
- number of saved nodes

## Settings

Settings are stored in:

- `user://settings.cfg`

Supported settings:

- keybindings
- master volume
- fullscreen
- window scale

The controls UI supports rebinding for both keyboard keys and mouse buttons.

## Test / Verification Commands

### Build

```bash
dotnet build
```

### Load the default project entry point headlessly

```bash
godot --headless --path . --quit-after 1
```

### Load the mechanic test scene headlessly

```bash
godot --headless --path . --main-scene res://Scenes/Tests/MechanicsTest.tscn --quit-after 1
```

### Save/load regression test

```bash
godot --headless --path . --main-scene res://Scenes/Game/SaveStateTest.tscn
```

### Settings/input regression test

```bash
godot --headless --path . --main-scene res://Scenes/Menu/SettingsInputMapTest.tscn
```

### Player stats / pickup regression test

```bash
godot --headless --path . --main-scene res://Scenes/Game/StatsPickupTest.tscn
```

## Notes

- `.godot/` is ignored and not tracked.
- `Tools/SimulationRunner/bin/` and `Tools/SimulationRunner/obj/` are ignored and not tracked.
- The repository should now contain source/config/assets only, not generated build artifacts.

## Next Technical Steps

Logical next extensions:

- save additional global/autoload runtime state if needed
- add more interactables (doors, pickups, triggers)
- add stronger gameplay regression tests for enemy behavior and player state transitions under combat pressure
- add UI polish (icons, better bars, pickup feedback, damage flashes, sounds)
