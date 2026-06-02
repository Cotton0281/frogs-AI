using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Windows.Media;

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

        /// <summary>Ticks the bite animation plays after a kill (one frame per ~quarter).</summary>
        private const int BiteAnimationTicks = 8;
        private int _biteTicksLeft;

        /// <summary>Rotation magnitude (degrees) above which the turn animation is shown.</summary>
        private const double TurnAnimationThreshold = 0.6;
        private const int FramesPerAnimation = 4;

        /// <summary>HP lost each tick — sharks must eat to survive.</summary>
        public const double SwimHpDrain = 0.4;

        /// <summary>Sharks can strike a frog within this distance in open water.</summary>
        public const double HuntRange = 26;

        /// <summary>Sharks have 5× the base HP cap, like birds.</summary>
        public static int SharkMaxHp => MaxHp * 5;

        /// <summary>Sharks only hunt when HP is below this fraction of SharkMaxHp (≤70%).</summary>
        public const double HuntHpThreshold = 0.7;

        /// <summary>Returns true when the shark is hungry enough to hunt.</summary>
        public bool IsHungry => HP < SharkMaxHp * HuntHpThreshold;

        public override int EffectiveMaxHp => SharkMaxHp;

        /// <summary>Number of frogs this shark has eaten.</summary>
        public int FrogsEaten { get; set; }

        /// <summary>Sharks broadcast as sharks so frogs can learn to avoid them.</summary>
        public override ObjectCategory SenseCategory => ObjectCategory.Shark;

        /// <summary>
        /// Sharks can see rafts, birds, and frogs in water. Sharks ignore other sharks and
        /// frogs resting on rafts.
        /// </summary>
        private static readonly ObjectCategory[] SharkIgnored =
            { ObjectCategory.Shark, ObjectCategory.Frog_OnRaft };
        public override ObjectCategory[] IgnoredCategories => SharkIgnored;

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

        /// <summary>Plays the bite animation for the next few ticks (called when the shark eats).</summary>
        public void TriggerBite()
        {
            _biteTicksLeft = BiteAnimationTicks;
        }

        public override ImageSource GetSpriteFrame()
        {
            // Bite animation takes priority and plays through its 4 frames once
            if (_biteTicksLeft > 0)
            {
                int elapsed = BiteAnimationTicks - _biteTicksLeft;
                int biteFrame = elapsed * FramesPerAnimation / BiteAnimationTicks; // 0..3
                if (biteFrame >= FramesPerAnimation) biteFrame = FramesPerAnimation - 1;
                _biteTicksLeft--;
                return SharkSpriteCache.Frame(SharkSpriteCache.Bite[biteFrame]);
            }

            // Otherwise pick swim / turn-left / turn-right based on the last rotation
            int[] animation;
            if (LastRotation < -TurnAnimationThreshold)
                animation = SharkSpriteCache.TurnLeft;
            else if (LastRotation > TurnAnimationThreshold)
                animation = SharkSpriteCache.TurnRight;
            else
                animation = SharkSpriteCache.SwimForward;

            _frameCounter++;
            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _swimFrameIndex = (_swimFrameIndex + 1) % FramesPerAnimation;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return SharkSpriteCache.Frame(animation[_swimFrameIndex % FramesPerAnimation]);
        }

        /// <summary>
        /// Sharks move under water: rafts give them nothing and they are never counted on top.
        /// They drain HP every tick and register as predators when hungry.
        /// </summary>
        public override void InteractWithRafts(RaftTickContext ctx)
        {
            IsGettingHP = false;
            HP -= SwimHpDrain;
            ctx.Sharks.Add(this);

            if (IsHungry)
                ctx.HungrySharks.Add(this);
        }



        private void InitializeSpriteRhythm()
        {
            _swimFrameIndex = NextRandom(0, FramesPerAnimation);
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
