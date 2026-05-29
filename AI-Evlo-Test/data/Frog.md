# Frog

## Overview

The **Frog** is the primary prey agent that inherits from `SmartObject`. Frogs are smaller and slower than birds but sustain themselves by reaching and staying on active rafts. They evolve via neural networks and natural selection, learning to navigate toward rafts while avoiding overcrowding that causes rafts to sink.

## Stats

| Property         | Value                        |
|------------------|------------------------------|
| Size             | 32                           |
| Max HP           | 300 (base MaxHp)            |
| Max Speed        | 1.5 (base MaxSpeed)         |
| Max Stamina      | 200 (shared with all agents) |
| HP Drain (water) | 0.35 per tick                |
| HP Gain (raft)   | +1.0 per tick (from active raft HpCharge) |
| Net HP on raft   | +0.65 per tick               |

## Behaviour

### Movement

Frogs use the standard `SmartObject.Act()` method: two neural network outputs control rotation and thrust. Movement magnitude is scaled by remaining stamina — exhausted frogs slow down proportionally.

- **Stamina cost**: 0.15 per unit of combined rotation + thrust output magnitude.
- **Stamina regen**: 0.3 per tick, capped at 200.
- Unlike birds, frogs have **no speed multiplier** — they move at base `MaxSpeed` (1.5).

### Perception

Frogs use 12-ray raycasting perception but with a **filtered view** — frogs **cannot** see other frogs. Their rays pass through all frog agents. Frogs can only perceive:

- **Rafts** (active and sunk)
- **Birds** (flying and landed — shown as distinct categories)

This means frogs navigate by sensing rafts and birds, but are completely unaware of other frogs around them. This design choice prevents frog populations from developing "follow the crowd" strategies and forces each frog to independently learn to find rafts.

Frogs can **distinguish landed birds from flying birds** because landed birds emit a different signal value (`Bird_Landed = 1.000` vs `Bird = 0.833`). This allows frog NNs to potentially learn to avoid rafts occupied by landed (hunting-capable) birds.

Neural network inputs: 2 scalars (HP deficit, stamina deficit) + 24 ray signals (12 rays × distance + type) = 26 inputs.

### HP and Survival

- Frogs start with 300 HP (base `MaxHp`).
- Every tick, frogs lose **0.35 HP** regardless of position.
- When on an **active raft** (`HpCharge > 0`), frogs gain the raft's HP charge (+1.0/tick), netting **+0.65 HP/tick**.
- When on a **sunk raft** (`HpCharge = 0`), frogs get no charge, netting **−0.35 HP/tick** (same as water).
- Frogs can be on **multiple overlapping rafts** simultaneously, gaining HP from each active one.
- Frogs die when HP reaches 0 and are removed from the simulation.

### Raft Interaction

Frogs contribute to a raft's `ObjectsOnTop` counter when positioned within its radius. This counter drives the raft sinking mechanic:

- When `ObjectsOnTop ≤ half total population size`: the raft's `Underwater` counter increases (raft stays afloat or recovers).
- When `ObjectsOnTop > half total population size`: `Underwater` decreases (raft sinks).
- A sunk raft (`Underwater < 0`) stops providing HP charge.

This creates an evolutionary pressure for frogs to **spread across rafts** rather than all crowding onto one. Populations that learn to balance across rafts survive longer.

### Vulnerability to Birds

Frogs on active rafts can be hunted by hungry, landed birds within hunt range (34 units). A hunted frog is immediately removed from the simulation. Frogs have no active defense — their survival depends on the bird's hunger state (birds only hunt below 90% max HP) and positioning.

### Sprites

Frogs are animated from a **sprite sheet** (`img/frog_sprite_sheet.png`), sliced at load time into
16 frozen frames (`FrogSheetCache` → `SpriteSheet.Slice`, WPF `CroppedBitmap`). The sheet is loaded
as an embedded **Resource via a pack URI**, so it resolves regardless of the working directory and
is immune to Visual Studio flipping the image's build action.

| Property      | Value                         |
|---------------|-------------------------------|
| File          | `img/frog_sprite_sheet.png`   |
| Build action  | Resource                      |
| Sheet size    | 1024 × 1024 px                |
| Grid          | 4 columns × 4 rows            |
| Total frames  | 16                            |
| Frame size    | 256 × 256 px                  |
| Frame index   | `row * 4 + column` (left-to-right, top-to-bottom) |

| Row | Frames | Animation     | Meaning                              |
|-----|--------|---------------|--------------------------------------|
| 0   | 0–3    | `swimForward` | forward swim, alternating leg kicks  |
| 1   | 4–7    | `turnLeft`    | left turn cycle                      |
| 2   | 8–11   | `turnRight`   | right turn cycle                     |
| 3   | 12–15  | `fastSwim`    | fast burst / strong kick cycle       |

At runtime the frog picks **fastSwim** when moving above 80% of max speed, otherwise **turnLeft** /
**turnRight** when its last rotation exceeds a small threshold, otherwise **swimForward**. Frames
advance on a randomized rhythm (8–30 ticks/frame), each frog starting at a random phase.

## Evolution

Frogs evolve through the same genetic algorithm as all agents:

- **Fitness** = Cycles survived − Offspring count.
- When population drops below its size limit, the fittest surviving frogs (with HP above 10% of MaxHp) reproduce via neural network mutation.
- Best genes are archived for population restarts when all members die.
- Offspring inherit parent HP and spawn near the parent's location.
- Population is regrown to 120% of the size limit each generation cycle.
