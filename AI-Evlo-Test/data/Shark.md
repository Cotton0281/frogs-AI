# Shark

## Overview

The **Shark** is an underwater predator agent that inherits from `SmartObject`. Sharks move below the rafts, lose HP continuously, and survive by hunting frogs in open water. Frogs on rafts are safe from sharks. Birds now pressure sharks directly by hunting them from rafts.

## Stats

| Property        | Value                          |
|-----------------|--------------------------------|
| Size            | 50                             |
| Max HP          | 5x base MaxHp (1500 default)   |
| Swim HP Drain   | 0.4 per tick                   |
| Hunt HP Gain    | HP from the eaten frog         |
| Hunt Range      | 26 units                       |
| Hunt Threshold  | Hunts only when HP < 70% max   |
| Frogs Eaten     | Per-shark counter, shown in UI |

## Behaviour

### Movement

Sharks use the standard `SmartObject.Act()` method: two neural network outputs control rotation and thrust.

### Perception

Sharks use 12-ray raycasting perception. They can perceive:

- Rafts, active and sunk
- Birds, flying and landed
- Frogs in water

Sharks ignore other sharks and frogs that are currently on a raft (`Frog_OnRaft`). This means sharks can chase birds and water frogs visually, but raft frogs do not block or attract shark rays.

Neural network inputs: 1 scalar HP deficit + 24 ray signals = 25 inputs.

### HP and Survival

- Sharks have 5x the HP pool of frogs.
- Rafts give sharks nothing and sharks are never counted as on top of a raft.
- Sharks drain 0.4 HP per tick.
- Sharks die when HP reaches 0.

### Hunting

A shark eats a frog when:

1. The shark is hungry: HP is below 70% of `SharkMaxHp`.
2. A frog is in open water, touching no raft.
3. The frog is within the shark hunt range.

On a successful hunt the shark gains the eaten frog's remaining HP, increments `FrogsEaten`, plays the bite animation, and the frog is removed. Frogs resting on any raft are never valid shark prey.

### Rendering

Sharks are drawn with a low `Canvas` Z-index so they appear beneath rafts and birds.

### Sprites

Sharks are animated from `img/shark_sprite_sheet_1024_256px_frames.png`, sliced at load time into 16 frozen frames via `SharkSpriteCache`.

| Row | Frames | Animation     | Meaning             |
|-----|--------|---------------|---------------------|
| 0   | 0-3    | `swimForward` | Forward swim        |
| 1   | 4-7    | `turnLeft`    | Left turn cycle     |
| 2   | 8-11   | `turnRight`   | Right turn cycle    |
| 3   | 12-15  | `bite`        | Bite animation      |

## Evolution

Sharks evolve through the same genetic algorithm as all agents:

- Fitness = cycles survived - offspring count.
- When population drops below its size limit, the fittest surviving sharks reproduce through neural-network mutation.
- Best genes are archived for population restarts.
- Offspring inherit parent HP and spawn near the parent.
