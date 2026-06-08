# Shark

## Overview

The **Shark** is an underwater predator agent that inherits from `SmartObject`. Sharks move below the rafts, lose HP continuously, and can bite water frogs or flying birds when hungry. Hungry frogs in water can bite sharks, and flying birds can still bite sharks.

## Stats

| Property        | Value                          |
|-----------------|--------------------------------|
| Size            | 50                             |
| Max HP          | 5x base MaxHp (1500 default)   |
| Swim HP Drain   | 0.4 per tick                   |
| Frog Bite Gain  | Up to the configured bite amount, 100 HP by default |
| Bird Bite Gain  | Up to 30 HP per bird bite      |
| Bite Range      | 26 units + target body radius  |
| Hunt Threshold  | Bites only when HP is below the shared hunger threshold |
| Frogs Eaten     | Legacy counter                 |
| GoldenThreshold | `SharkMaxHp / 0.4` (3750 cycles default) |

## Behaviour

### Movement

Sharks use the standard `SmartObject.Act()` method: the first two neural network outputs control rotation and thrust, and the last two outputs write recurrent memory values for the next tick.

### Perception

Sharks use 12-ray raycasting perception. They can perceive:

- Rafts, active and sunk
- Birds, flying and landed
- Frogs in water

Sharks ignore other sharks and frogs that are currently on a raft (`Frog_OnRaft`). This means sharks can chase birds and water frogs visually, but raft frogs do not block or attract shark rays.

Neural network inputs: 1 scalar HP deficit + 2 recurrent memory values + 48 ray signals (12 rays x 2 distinct-category hits x distance + type) = 51 inputs. Outputs = 4: rotation, thrust, memory0, memory1.

### HP and Survival

- Sharks have 5x the HP pool of frogs.
- Rafts give sharks nothing and sharks are never counted as on top of a raft.
- Sharks drain 0.4 HP per tick.
- Sharks die when HP reaches 0.

### Biting

A shark bites the nearest valid target when:

1. The shark is hungry: HP is below the shared hunger threshold.
2. The target is either a frog in water or a flying bird.
3. The target is within the shark bite range.
4. The shark is not in bite cooldown.

On a successful bite, the target loses HP and the shark gains the same amount. Water frogs use the configured bite amount, 100 HP by default. Flying birds lose up to 30 HP. The target is removed only if HP reaches 0. Sharks do not bite frogs on rafts or landed birds on rafts.

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
- The population's optional [Golden Agent](GoldenAgent.md) starts with a `GoldenThreshold` of `SharkMaxHp / 0.4`; qualifying sharks can contribute their neural network to the golden running average.
