## Animation System

This project uses a reusable 2D animation and SFX pipeline built around:

- `AnimationTree` as the runtime state machine
- `AnimationPlayer` clips plus method-call tracks for animation events
- `AnimatedSprite2D` for playback through `SpriteFrames`
- `SfxPlayer` (`AudioStreamPlayer2D`) for event-driven sound playback

### Folder Layout

Per actor:

- `res://Assets/Characters/<Actor>/Animations/<State>/`
- `res://Assets/Characters/<Actor>/Sounds/<Event>/`

Examples:

- `res://Assets/Characters/Player/Animations/run/run_01.png`
- `res://Assets/Characters/Player/Animations/run/heavy/frame_000.png`
- `res://Assets/Characters/Player/Animations/run/run_03@4x1.png`
- `res://Assets/Characters/Player/Sounds/footstep/step_01.wav`

### State Taxonomy

Core:

- `idle`
- `run`
- `jump`
- `fall`
- `land`
- `crouch`
- `dash`
- `wall_slide`
- `wall_jump`

Combat:

- `attack_melee`
- `attack_ranged`
- `attack_charge`
- `hit`
- `die`

Interaction:

- `interact`
- `pickup`
- `spawn`
- `despawn`

Custom states are allowed. The controller builds a runtime state node for any requested state, then falls back by suffix trimming.

### Adding Animations

Use one of these patterns inside a state folder:

- Variant subfolders: `run/heavy/frame_000.png`, `run/heavy/frame_001.png`
- Root single-file variants: `run/run_01.png`, `run/run_02.png`
- Root named frame sequences: `run/run_03__000.png`, `run/run_03__001.png`
- Sprite sheets: add `@<columns>x<rows>` to the filename, for example `run/run_04@4x1.png`

Optional FPS override:

- append `@12fps` after the grid token, for example `run_04@4x1@12fps.png`

Notes:

- The runtime chooses a random variant on state entry.
- GIF is not supported.
- If a state has no assets, the controller generates a runtime placeholder clip so the state machine still runs.

### Adding Sounds

Place `.wav` or `.ogg` files in the event folder:

- `Sounds/footstep/step_01.wav`
- `Sounds/footstep/step_02.wav`
- `Sounds/hit/hit_light.ogg`

The `SfxPlayer` chooses a random variant when an event fires. If no sound exists, playback is skipped safely.

### Animation Events

Method-call tracks in generated `AnimationPlayer` clips call:

- `AnimationController.OnAnimEvent(string eventName)`

Canonical events:

- `footstep`
- `jump`
- `land`
- `swing`
- `shoot`
- `hit`
- `explode`
- `pickup`

`OnAnimEvent` forwards the event to `SfxPlayer` and also exposes it to gameplay code through the controller's `AnimationEventRaised` event.

### Fallback Rules

State lookup trims underscore suffixes until it finds a match:

- `jump_super_fast` -> `jump_super` -> `jump` -> `idle`

Sound lookup also trims suffixes, but it does not force an `idle` fallback:

- `footstep_heavy` -> `footstep`

If no animation match exists after fallback, a placeholder clip is built at runtime.
