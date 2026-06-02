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

        /// <summary>Birds hungry enough to hunt sharks this tick, whether flying or landed.</summary>
        public readonly List<Bird> HungryBirds = new List<Bird>();

        /// <summary>Frogs in open water, touching no raft (prey for sharks).</summary>
        public readonly List<Frog> FrogsInWater = new List<Frog>();

        /// <summary>All sharks in open water (prey for landed hungry birds).</summary>
        public readonly List<Shark> Sharks = new List<Shark>();

        /// <summary>Sharks hungry enough to hunt in-water frogs this tick.</summary>
        public readonly List<Shark> HungrySharks = new List<Shark>();
    }
}
