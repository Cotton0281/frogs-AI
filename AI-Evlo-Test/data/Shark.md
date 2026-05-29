# Shark

## Overview

The **Shark** is an underwater predator agent that inherits from `SmartObject`. Sharks move only
under water (they are rendered *beneath* the rafts) and survive by hunting frogs swimming in open
water. Crucially, a shark **cannot eat a frog that is sitting on a raft** — rafts are a safe haven
from sharks (but not from birds). Sharks therefore push frogs *onto* rafts, while birds push them
*off*, squeezing the frog population from both sides.

## Stats

| Property        | Value                          |
|-----------------|--------------------------------|
| Size            | 50                             |
| Max HP          | 5× base MaxHp (1500 default)   |
| Speed           | 1.5× base agent speed          |
| Max Stamina     | 200 (shared with all agents)   |
| Swim HP Drain   | 0.4 per tick                   |
| Hunt HP Gain    | 200 per frog eaten             |
| Hunt Range      | 26 units                       |
| Hunt threshold  | Hunts only when HP < 90% of max|
| Frogs Eaten     | Per-shark counter, shown in UI |

## Behaviour

### Movement

Sharks use the standard `SmartObject.Act()` (two NN outputs → rotation + thrust), then apply a
**1.5× speed multiplier** so they are fast underwater hunters. Stamina mechanics are shared with
all agents.

### Perception

Sharks use the same 12-ray raycasting perception with **no ignored categories** — they see
everything, including frogs (their prey), rafts, birds, and other sharks. Sharks broadcast as the
`Shark` category to other agents' rays, so frogs can evolve to avoid them.

Neural network inputs: 2 scalars (HP deficit, stamina deficit) + 24 ray signals = 26 inputs.

### HP and Survival

- Sharks have **5× the HP pool** of frogs (1500 vs 300 at defaults).
- They gain **nothing** from rafts and are never counted as "on top" of a raft (they pass beneath).
- They drain **0.4 HP/tick** continuously, so they must keep hunting to survive.
- A shark dies when HP reaches 0, like all agents.

### Hunting

A shark eats a frog when:

1. The shark is **hungry** (HP below 90% of its max).
2. A frog is **in open water** (touching no raft) within **hunt range** (26 units).

On a successful hunt the shark gains 200 HP, increments `FrogsEaten`, plays its bite animation, and
the frog is removed. Frogs resting on a raft are **never** valid shark prey.

### Rendering

Sharks are drawn with `Canvas` Z-index **-1**, placing them under the rafts and birds to read as
"under water," at 85% opacity.

## Sprite sheet (`img/Shark.png`)

The shark is animated from a single **sprite sheet**: one image containing all frames in a grid,
sliced at load time into individual frozen frames (`SharkSpriteCache` → `SpriteSheet.Slice`,
using WPF `CroppedBitmap`). The sheet is loaded as an embedded **Resource via a pack URI**, so it
resolves regardless of the working directory and is immune to Visual Studio flipping the image's
build action between Content and Resource.

| Property      | Value                                          |
|---------------|------------------------------------------------|
| File          | `img/shark_sprite_sheet_1024_256px_frames.png` |
| Build action  | Resource                                       |
| Sheet size    | 1024 × 1024 px                                 |
| Grid          | 4 columns × 4 rows                             |
| Total frames  | 16                                             |
| Frame size    | 256 × 256 px                                   |
| Frame index   | `row * 4 + column` (left-to-right, top-to-bottom) |

### Frame layout

| Row | Frames | Animation     | Per-frame meaning                                                   |
|-----|--------|---------------|---------------------------------------------------------------------|
| 0   | 0–3    | `swimForward` | 0 neutral · 1 tail bends left · 2 tail bends right · 3 glide/recover |
| 1   | 4–7    | `turnLeft`    | 4 start · 5 stronger curve · 6 mid turn · 7 finish/recover          |
| 2   | 8–11   | `turnRight`   | 8 start · 9 stronger curve · 10 mid turn · 11 finish/recover        |
| 3   | 12–15  | `bite`        | 12 mouth closed · 13 opening · 14 fully open/bite · 15 closed/recover|

### Animation selection (runtime)

- **Bite** (frames 12–15) plays once, taking priority, for a few ticks after the shark eats a frog
  (`Shark.TriggerBite()`), stepping through all four frames.
- Otherwise the animation is chosen from the shark's last applied rotation
  (`SmartObject.LastRotation`): **turn left** (4–7) when rotating left past a small threshold,
  **turn right** (8–11) when rotating right, else **swim forward** (0–3).
- Within the chosen animation, frames advance on a randomized rhythm (8–24 ticks per frame) so
  sharks don't animate in lockstep.

### Machine-readable layout

```json
{
  "image": "img/shark_sprite_sheet_1024_256px_frames.png",
  "grid": { "columns": 4, "rows": 4 },
  "frameSize": { "width": 256, "height": 256 },
  "animations": {
    "swimForward": [0, 1, 2, 3],
    "turnLeft":    [4, 5, 6, 7],
    "turnRight":   [8, 9, 10, 11],
    "bite":        [12, 13, 14, 15]
  }
}
```

> The frog uses the same slice-at-load approach via `FrogSheetCache` (see Frog.md). Birds still
> use individual image files.

## Evolution

Sharks evolve through the same genetic algorithm as all agents:

- **Fitness** = Cycles survived − Offspring count.
- When the population drops below its size limit, the fittest survivors reproduce via neural-network
  mutation; best genes are archived for restarts.
- Offspring inherit parent HP and spawn near the parent.
