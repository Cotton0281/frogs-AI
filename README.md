# 🧠🐸 AI-Evlo — Evolutionary Neural-Net Ecosystem

**An AI research sandbox for studying unsupervised learning and evolutionary computation.**

This project explores how intelligent behavior can *emerge* without labels, reward-engineering, or a
teacher — using **neuroevolution**: a genetic algorithm that breeds and mutates the neural networks
("brains") of digital creatures, keeping whatever survives. There is no training set and no gradient
descent. The only signal is **survival**, and the only feedback loop is **natural selection**.

<img width="2145" height="1189" alt="image" src="https://github.com/user-attachments/assets/b5a9ddcb-22e5-47b5-b12d-b5cdd3294a47" />

## What it actually is

A living **ecosystem** runs in real time on a canvas, with multiple co-evolving species:

- **[Frogs](AI-Evlo-Test/data/Frog.md)** swim in open water and must rest on **rafts** to recover HP.
- **[Birds](AI-Evlo-Test/data/Bird.md)** are aerial predators that land on rafts and eat nearby sharks.
- **[Sharks](AI-Evlo-Test/data/Shark.md)** are underwater predators that eat frogs in open water — but never frogs safe on a raft.

Each population can maintain one **[Golden Agent](AI-Evlo-Test/data/GoldenAgent.md)**: a golden-tinted representative whose brain is the running average of long-lived survivor brains.

Each creature senses its world through **ego-centric raycasting** (whisker-like "vision"), feeds those
signals into its own neural network, and acts — rotating and thrusting — entirely on its own. Sharks
pressure frogs in open water while birds pressure sharks from rafts, so there is no single safe strategy; the population has to
*discover* one.

## The research angles it lets you poke at

- **Unsupervised / self-supervised emergence** — behavior arises only from interaction with the
  environment, not from labeled examples.
- **Neuroevolution** — networks are encoded as genomes, mutated weight-and-bias, and selected by fitness;
  topology is preserved while behavior is explored.
- **Multi-agent & predator–prey co-evolution** — three populations evolve against each other, producing
  arms-race dynamics.
- **Open-ended experimentation** — you control population sizes, brain sizes (Small / Medium / Large), and
  species, then watch generations rise and fall.

## How the loop works

- Manage one or more **populations** of neural-net-driven agents.
- Score every agent by a **fitness** value (lifespan vs. reproduction cost).
- Archive each population's **best genomes** so depleted populations can regrow gradually from a
  rotating mix of archived best, live best, mutated, and random brains.
- Maintain an optional **golden brain** per population by incrementally averaging the neural networks
  of agents that exceed that population's dynamic `GoldenThreshold`.
- **Mutate, compete, rank, repeat** — across generations, fitter brains dominate the gene pool.
- Inspect any individual live: HP, generation, and a peek at its neural network.

## The desktop viewer

A WPF application visualizes the whole thing in real time — every creature, every raft, the rays an agent
is "seeing," and a live leaderboard of populations and their best genes — so the human operator can watch
evolution happen and intervene (add a species, change a brain, cull a population). On first launch it
restores your last session, or seeds a default scenario (2 rafts, 50 frogs, 10 birds) and starts running.

## Build & run

- **Stack:** C# / WPF on .NET Framework 4.7.2 (Windows). Neural network engine:
  [`NeuralNetwork` 7.4.0](https://www.nuget.org/packages/NeuralNetwork) (`ArtificialNeuralNetwork`).
- Restore packages, then build the solution:

```bash
nuget restore AI-Evlo-WPF.sln
msbuild AI-Evlo-WPF.sln /p:Configuration=Release /p:Platform="Any CPU"
# Output: bin\Release\ML-Evolutions.exe
```

See [CLAUDE.md](AI-Evlo-Test/CLAUDE.md) for the architecture overview and the per-species docs in
[`AI-Evlo-Test/data/`](AI-Evlo-Test/data).

---

*So, the story version:* a colony of robot frogs enters an open-ended survival tournament against birds
and sharks. Nobody tells them how to swim, hide, or hunt — they simply live, die, and pass on whatever
brains kept them alive. The clever lineages get promoted; the bad thinkers get quietly judged by a
DataGridView. It's part laboratory, part leaderboard, part amphibian talent show — and underneath the
whimsy, a working testbed for **emergent, evolved intelligence**.
