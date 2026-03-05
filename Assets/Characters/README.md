# Character Animation + SFX Asset Convention

Per actor:

- `res://Assets/Characters/<Actor>/Animations/<State>/`
- `res://Assets/Characters/<Actor>/Sounds/<Event>/`

Supported animation sources:

- PNG/JPG/WebP frame sequences in a variant folder, commonly `frame_000.png`, `frame_001.png`, ...
- root-level single-file variants such as `run_01.png`, `run_02.png`
- root-level named frame sequences using `variant__000.png`, `variant__001.png`
- sprite sheets tagged with `@<columns>x<rows>`, for example `run_03@4x1.png`

Supported sound sources:

- `.wav`
- `.ogg`

Do not use GIF.

Canonical states:

- `idle`, `run`, `jump`, `fall`, `land`, `crouch`, `dash`, `wall_slide`, `wall_jump`
- `attack_melee`, `attack_ranged`, `attack_charge`, `hit`, `die`
- `interact`, `pickup`, `spawn`, `despawn`

Canonical events:

- `footstep`, `jump`, `land`, `swing`, `shoot`, `hit`, `explode`, `pickup`

Fallbacks:

- state lookup trims suffixes (`jump_super` -> `jump` -> `idle`)
- missing animation assets generate a runtime placeholder clip
- missing sounds do nothing unless the test scene enables debug tones

The runtime picks a random variant every time a state is entered.
