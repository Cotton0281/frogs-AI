using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Shark is a predator that moves only under water. It gains no rest from rafts and is
    /// never counted as "on top" of one. It eats frogs swimming in open water, but cannot
    /// eat frogs that are sitting on a raft. It must keep hunting or it starves.
    /// </summary>
    public class Shark : SmartObject, ISmartObject
    {
        private static readonly Random SpriteRandom = new Random();
        private static readonly object SpriteRandomLock = new object();
        private const int MinTicksToNextFrame = 8;
        private const int MaxTicksToNextFrame = 24;

        private int _frameCounter;
        private int _nextFrameAt;
        private int _swimFrameIndex;

        /// <summary>HP lost each tick — sharks must eat to survive.</summary>
        public const double SwimHpDrain = 0.4;

        /// <summary>HP gained per frog eaten.</summary>
        public const double HuntHpGain = 200;

        /// <summary>Sharks can strike a frog within this distance in open water.</summary>
        public const double HuntRange = 26;

        /// <summary>Sharks move 1.5× the base agent speed — fast underwater hunters.</summary>
        public const double SpeedMultiplier = 1.5;

        /// <summary>Sharks have 5× the base HP cap, like birds.</summary>
        public static int SharkMaxHp => MaxHp * 5;

        /// <summary>Sharks only hunt when HP is below this fraction of SharkMaxHp.</summary>
        public const double HuntHpThreshold = 0.9;

        /// <summary>Returns true when the shark is hungry enough to hunt.</summary>
        public bool IsHungry => HP < SharkMaxHp * HuntHpThreshold;

        public override int EffectiveMaxHp => SharkMaxHp;

        /// <summary>Number of frogs this shark has eaten.</summary>
        public int FrogsEaten { get; set; }

        /// <summary>Sharks broadcast as sharks so frogs can learn to avoid them.</summary>
        public override ObjectCategory SenseCategory => ObjectCategory.Shark;

        /// <summary>Sharks see everything — they hunt by sight.</summary>
        public override ObjectCategory[] IgnoredCategories => null;

        public Shark()
        {
            Size = 50;
            HP = SharkMaxHp;
            InitializeSpriteRhythm();
        }

        public Shark(NeuroNetStructure nnStructure, ref RandomWeightInitializer randomInit)
            : base(nnStructure, ref randomInit)
        {
            Size = 50;
            HP = SharkMaxHp;
            InitializeSpriteRhythm();
        }

        public Shark(INeuralNetwork neuralNetwork)
            : base(neuralNetwork)
        {
            Size = 50;
            HP = SharkMaxHp;
            InitializeSpriteRhythm();
        }

        public override BitmapImage GetSpriteFrame()
        {
            _frameCounter++;
            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _swimFrameIndex = (_swimFrameIndex + 1) % SharkSpriteCache.SwimFrames.Length;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return SharkSpriteCache.SwimFrames[_swimFrameIndex];
        }

        /// <summary>
        /// Sharks move under water: rafts give them nothing and they are never counted on top.
        /// They drain HP every tick and register as predators when hungry.
        /// </summary>
        public override void InteractWithRafts(RaftTickContext ctx)
        {
            IsGettingHP = false;
            HP -= SwimHpDrain;

            if (IsHungry)
                ctx.HungrySharks.Add(this);
        }

        /// <summary>
        /// Sharks swim faster than the base agent. Overrides base Act to apply the speed multiplier.
        /// </summary>
        public override double[] Act(double[] arrayInputs)
        {
            double[] outputs = base.Act(arrayInputs);
            if (outputs.Length > 0)
            {
                double boost = SpeedMultiplier - 1.0;
                this.PushForward(LastSpeed * boost);
                LastSpeed *= SpeedMultiplier;
            }
            return outputs;
        }

        private void InitializeSpriteRhythm()
        {
            _swimFrameIndex = NextRandom(0, SharkSpriteCache.SwimFrames.Length);
            _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            _frameCounter = NextRandom(0, _nextFrameAt);
        }

        private static int NextRandom(int minValue, int maxValue)
        {
            lock (SpriteRandomLock)
            {
                return SpriteRandom.Next(minValue, maxValue);
            }
        }
    }
}
