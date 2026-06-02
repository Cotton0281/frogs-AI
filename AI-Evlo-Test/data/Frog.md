# Frog

## Overview

The **Frog** is the primary prey and raft-survival agent. Frogs sustain themselves by reaching active rafts, where they recover HP, and they evolve through the same neural-network and genetic-selection pipeline as the other agents.

## Stats

| Property         | Value                                      |
|------------------|--------------------------------------------|
| Size             | 32                                         |
| Max HP           | 300 base MaxHp                             |
| HP Drain         | 0.35 per tick                              |
| HP Gain on Raft  | +1.0 per tick from active raft `HpCharge`  |
| Net HP on Raft   | +0.65 per tick                             |

## Behaviour

### Movement

Frogs use the standard `SmartObject.Act()` method: two neural network outputs control rotation and thrust.

### Perception

Frogs use 12-ray raycasting perception with a filtered view. Frogs ignore both water frogs and raft frogs, so frog rays pass through all frogs. Frogs can perceive:

- Rafts, active and sunk
- Birds, flying and landed
- Sharks

This keeps frogs from evolving by simply following each other and forces them to learn from environmental and predator signals.

Neural network inputs: 1 scalar HP deficit + 24 ray signals (12 rays x distance + type) = 25 inputs.

### HP and Survival

- Frogs start with 300 HP.
- Every tick, frogs lose 0.35 HP.
- When on an active raft, frogs gain the raft's HP charge, netting +0.65 HP per tick at default charge.
- When on a sunk raft, frogs get no charge and still lose 0.35 HP per tick.
- Frogs can be on multiple overlapping rafts and receive charge from each active raft.
- Frogs die when HP reaches 0.

### Raft Interaction

Frogs contribute to a raft's `ObjectsOnTop` counter when positioned within its radius. This counter drives raft sinking:

- When `ObjectsOnTop` is at or below half the total population size, the raft recovers or stays afloat.
- When `ObjectsOnTop` is above half the total population size, the raft sinks.
- A sunk raft stops providing HP charge.

This creates pressure for frogs to spread across rafts rather than crowding onto one.

### Predators

Frogs are hunted by sharks only when they are in open water. A frog on any raft is marked as `Frog_OnRaft`, which sharks ignore in perception and do not consider valid prey. Birds no longer eat frogs.

### Sprites

Frogs are animated from `img/frog_sprite_sheet.png`, sliced at load time into 16 frozen frames via `FrogSheetCache`.

| Row | Frames | Animation     | Meaning              |
|-----|--------|---------------|----------------------|
| 0   | 0-3    | `swimForward` | Forward swim         |
| 1   | 4-7    | `turnLeft`    | Left turn cycle      |
| 2   | 8-11   | `turnRight`   | Right turn cycle     |
| 3   | 12-15  | `fastSwim`    | Fast burst cycle     |

## Evolution

Frogs evolve through the same genetic algorithm as all agents:

- Fitness = cycles survived - offspring count.
- When population drops below its size limit, the fittest surviving frogs reproduce through neural-network mutation.
- Best genes are archived for population restarts.
- Offspring inherit parent HP and spawn near the parent.
