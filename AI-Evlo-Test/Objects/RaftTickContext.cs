using System;
using System.Collections.Generic;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Per-tick scratch space passed to each agent's <see cref="SmartObject.InteractWithRafts"/>.
    /// Agents register themselves as predators or prey here; hunts are resolved afterwards.
    /// </summary>
    public sealed class RaftTickContext
    {
        /// <summary>All rafts (targets) in the world this tick.</summary>
        public List<TargetObj> Rafts;

        /// <summary>Birds that are landed on a charged raft and hungry enough to hunt.</summary>
        public readonly List<Tuple<Bird, TargetObj>> LandedHungryBirds = new List<Tuple<Bird, TargetObj>>();

        /// <summary>Frogs currently resting on a charged raft (prey for landed birds).</summary>
        public readonly List<Tuple<Frog, TargetObj>> FrogsOnRafts = new List<Tuple<Frog, TargetObj>>();

        /// <summary>Frogs in open water, touching no raft (prey for sharks).</summary>
        public readonly List<Frog> FrogsInWater = new List<Frog>();

        /// <summary>Sharks hungry enough to hunt in-water frogs this tick.</summary>
        public readonly List<Shark> HungrySharks = new List<Shark>();
    }
}
