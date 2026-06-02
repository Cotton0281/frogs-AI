# Bird

## Overview

The **Bird** is a predator agent that inherits from `SmartObject`. Birds evolve with the same neural-network and genetic-selection pipeline as the other agents. They can hunt sharks while flying or landed, and landing on an active raft lets them spend HP more slowly.

## Stats

| Property         | Value                            |
|------------------|----------------------------------|
| Size             | 40                               |
| Max HP           | 5x base MaxHp (1500 default)     |
| Flight HP Drain  | 0.45 per tick                    |
| Landed HP Drain  | 0.09 per tick                    |
| Hunt HP Gain     | Eaten shark's remaining HP       |
| Hunt Range       | 34 units + shark body radius     |
| Sharks Eaten     | Per-bird counter, displayed in UI |

## Behaviour

### Movement

Birds use the standard `SmartObject.Act()` method: two neural network outputs control rotation and thrust.

### Perception

Birds use the same 12-ray raycasting perception as other smart agents. Their rays ignore other birds, including landed birds, so bird clusters do not block bird vision. Birds can perceive:

- Rafts, active and sunk
- Frogs, whether in water or on rafts
- Sharks

Birds themselves appear as `Bird` while flying and `Bird_Landed` while landed.

Neural network inputs: 1 scalar HP deficit + 24 ray signals (12 rays x distance + type) = 25 inputs.

### HP and Survival

- Birds have 5x the HP pool of frogs.
- While flying, birds drain 0.45 HP per tick.
- While landed on an active raft, birds drain 0.09 HP per tick, which is 5x less than flying.
- Birds do not gain HP from the raft itself. Their only HP recovery is eating sharks.
- Birds die when HP reaches 0.

### Landing

A bird is considered landed when it is positioned within the radius of an active raft (`HpCharge > 0`). Landed birds:

- Use the landed sprite state.
- Drain HP at the reduced landed rate.
- Stay eligible to hunt nearby sharks when hungry.

### Hunting

Birds can hunt sharks when all of the following are true:

1. The bird is hungry: HP is below 70% of `BirdMaxHp`.
2. A shark is within strike range.

When a hunt succeeds:

- The nearest qualifying shark is removed from the simulation.
- The bird gains the shark's remaining HP, capped at `BirdMaxHp`.
- The bird's `SharksEaten` counter increments.

Birds no longer eat frogs.

### Sprites

Birds are animated from `img/bird_sprite_sheet_1024_256px_frames.png`, sliced at load time into 16 frozen frames via `BirdSheetCache`.

| Property      | Value                                             |
|---------------|---------------------------------------------------|
| Build action  | Resource                                          |
| Grid          | 4 columns x 4 rows                                |
| Total frames  | 16                                                |
| Frame size    | 256 x 256 px                                      |

| Row | Frames | Animation      | Meaning                 |
|-----|--------|----------------|-------------------------|
| 0   | 0-3    | `flyStraight`  | Wing-flap cycle         |
| 1   | 4-7    | `circleLeft`   | Banked left turn        |
| 2   | 8-11   | `circleRight`  | Banked right turn       |
| 3   | 12-15  | landed         | Landing, walk, idle     |

## Evolution

Birds evolve through the same genetic algorithm as all agents:

- Fitness = cycles survived - offspring count.
- When population drops, the fittest surviving birds reproduce through neural-network mutation.
- Best genes are archived for population restarts.
- Offspring inherit parent HP and spawn near the parent.
