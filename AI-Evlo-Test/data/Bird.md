# Bird

## Overview

The **Bird** is a predator agent that inherits from `SmartObject`. Birds are larger, slower, and tougher than frogs. They evolve via neural networks and natural selection, just like frogs, but their survival strategy revolves around landing on rafts and hunting frogs for HP recovery.

## Stats

| Property         | Value                        |
|------------------|------------------------------|
| Size             | 40                           |
| Max HP           | 5× base MaxHp (1500 default)|
| Max Speed        | 0.5× frog max speed (0.75)  |
| Max Stamina      | 200 (shared with all agents) |
| Flight HP Drain  | 0.45 per tick                |
| Landed HP Drain  | 0.08 per tick                |
| Hunt HP Gain     | 200 per frog eaten           |
| Hunt Range       | 34 units                     |
| Frogs Eaten      | Per-bird counter, displayed in UI |

## Behaviour

### Movement

Birds use the same neural-network-driven movement as all `SmartObject` agents: two NN outputs control rotation and thrust. However, after the base `Act()` computes thrust, the bird applies a **0.5× speed multiplier** via an additional `PushForward` call. This makes birds significantly **slower** than frogs, moving at half their max speed.

Stamina mechanics are shared with frogs: movement drains stamina proportional to effort, and stamina regenerates at 0.3 per tick. Exhausted birds slow down proportionally.

### Perception

Birds use the same 12-ray raycasting perception as frogs, but with **no ignored categories** — birds can see everything:

- Rafts (active and sunk)
- Frogs
- Other birds (flying and landed)

Birds themselves appear as `Bird` (flying) or `Bird_Landed` to other agents' perception rays.

Neural network inputs: 2 scalars (HP deficit, stamina deficit) + 24 ray signals (12 rays × distance + type) = 26 inputs.

The type signal encodes each `ObjectCategory` as a distinct float (enum value + 1, divided by 6):

| Category    | Signal value |
|-------------|-------------|
| Food        | 0.167       |
| Raft        | 0.333       |
| Raft_Sunk   | 0.500       |
| Frog        | 0.667       |
| Bird        | 0.833       |
| Bird_Landed | 1.000       |

### HP and Survival

- Birds have **5× the HP pool** of frogs (1500 vs 300 at default settings).
- While **flying** (not on an active raft), birds drain 0.45 HP/tick.
- While **landed** on an active raft, birds drain only 0.08 HP/tick.
- Birds do **not** gain HP from rafts like frogs do. Their only HP recovery is through hunting frogs.
- Birds die when HP reaches 0, same as all agents.

### Landing

A bird is considered "landed" when it is positioned within the radius of an **active** raft (one whose `HpCharge > 0`). Landed birds:

- Switch to the `bird_landed.png` sprite.
- Drain HP at the reduced landed rate (0.08/tick).
- Become eligible to hunt frogs on the same raft (if hungry).

### Hunting

Birds can hunt frogs **only** when all of the following conditions are met:

1. The bird is **landed** on an active raft.
2. The bird is **hungry**: its HP is below 90% of `BirdMaxHp` (below 1350 at default settings).
3. A frog is on the **same raft** within **hunt range** (34 units).

When a hunt succeeds:

- The bird gains 200 HP (capped at BirdMaxHp).
- The `FrogsEaten` counter on the bird increments.
- The nearest qualifying frog is removed from the simulation.

After eating one frog, the bird's HP typically rises above the 90% threshold, preventing further hunting until HP drains enough again.

### Sprites

Birds are animated from a **sprite sheet** (`img/bird_sprite_sheet_1024_256px_frames.png`), sliced
at load time into 16 frozen frames via `BirdSheetCache` → `SpriteSheet.SliceWithAlpha`. The sheet
already has a real alpha channel so no color-keying is applied.

| Property      | Value                                             |
|---------------|---------------------------------------------------|
| File          | `img/bird_sprite_sheet_1024_256px_frames.png`     |
| Build action  | Resource (embedded, loaded via pack URI)          |
| Sheet size    | 1024 × 1024 px                                    |
| Grid          | 4 columns × 4 rows                                |
| Total frames  | 16                                                |
| Frame size    | 256 × 256 px                                      |
| Frame index   | `row * 4 + column` (left-to-right, top-to-bottom) |

| Row | Frames | Animation      | Meaning                                     |
|-----|--------|----------------|---------------------------------------------|
| 0   | 0–3    | `flyStraight`  | wing-flap cycle                             |
| 1   | 4–7    | `circleLeft`   | banked left-turn cycle                      |
| 2   | 8–11   | `circleRight`  | banked right-turn cycle                     |
| 3   | 12–15  | landed         | 12 landing, 13–14 walk, 15 idle             |

At runtime the bird picks **circleLeft** / **circleRight** / **flyStraight** based on `LastRotation`.
When landed it cycles the two walk frames (or shows the idle frame when stationary). Frame timing
is randomized (6–18 ticks/frame flying, 12–36 ticks/frame walking) with per-bird phase offsets.

## Evolution

Birds evolve through the same genetic algorithm as frogs:

- **Fitness** = Cycles survived − Offspring count.
- When population drops, the fittest surviving birds reproduce via neural network mutation.
- Best genes are archived for population restarts.
- Offspring inherit parent HP and spawn near the parent's location.
