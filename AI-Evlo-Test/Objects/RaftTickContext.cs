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

        /// <summary>Flying birds hungry enough to bite sharks this tick.</summary>
        public readonly List<Bird> HungryBirds = new List<Bird>();

        /// <summary>Flying birds in open air, valid prey for sharks.</summary>
        public readonly List<Bird> FlyingBirds = new List<Bird>();

        /// <summary>Landed birds resting on a raft, valid prey for raft frogs.</summary>
        public readonly List<Bird> LandedBirds = new List<Bird>();

        /// <summary>Landed birds hungry enough to bite raft frogs this tick.</summary>
        public readonly List<Bird> HungryLandedBirds = new List<Bird>();

        /// <summary>Frogs currently on a raft, valid prey for landed birds.</summary>
        public readonly List<Frog> FrogsOnRafts = new List<Frog>();

        /// <summary>Frogs on rafts hungry enough to bite landed birds this tick.</summary>
        public readonly List<Frog> HungryFrogsOnRafts = new List<Frog>();

        /// <summary>Frogs in open water, touching no raft.</summary>
        public readonly List<Frog> FrogsInWater = new List<Frog>();

        /// <summary>Frogs in water hungry enough to bite sharks this tick.</summary>
        public readonly List<Frog> HungryFrogsInWater = new List<Frog>();

        /// <summary>All sharks in open water, valid prey for flying birds and water frogs.</summary>
        public readonly List<Shark> Sharks = new List<Shark>();

        /// <summary>Sharks hungry enough to bite water frogs or flying birds this tick.</summary>
        public readonly List<Shark> HungrySharks = new List<Shark>();
    }
}
