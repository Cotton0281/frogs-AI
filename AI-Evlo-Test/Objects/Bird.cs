using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    public class Bird : SmartObject, ISmartObject
    {
        private static readonly Random SpriteRandom = new Random();
        private static readonly object SpriteRandomLock = new object();
        private const int MinTicksToNextFrame = 6;
        private const int MaxTicksToNextFrame = 18;

        private int _frameCounter;
        private int _nextFrameAt;
        private int _flightFrameIndex;

        /// <summary>Rotation magnitude above which turn animations are shown.</summary>
        private const double TurnAnimationThreshold = 0.6;
        private const int FlightFramesPerAnimation = 4;

        public const double FlightHpDrain = 0.45;
        public const double LandedHpDrain = 0.08;
        public const double HuntHpGain = 200;
        public const double HuntRange = 10; // decreased from 34

        /// <summary>Birds move 1.3× the base agent speed — a slight edge over frogs.</summary>
        public const double SpeedMultiplier = 1.3;
        public static int BirdMaxHp => MaxHp * 5;

        /// <summary>Birds can only hunt when HP is below this fraction of BirdMaxHp.</summary>
        public const double HuntHpThreshold = 0.9;

        /// <summary>Returns true when the bird is hungry enough to hunt.</summary>
        public bool IsHungry => HP < BirdMaxHp * HuntHpThreshold;

        /// <summary>Birds have 5× the base HP cap.</summary>
        public override int EffectiveMaxHp => BirdMaxHp;

        /// <summary>Birds broadcast as landed or flying so frogs can react accordingly.</summary>
        public override ObjectCategory SenseCategory => IsLanded ? ObjectCategory.Bird_Landed : ObjectCategory.Bird;

        /// <summary>Birds see everything — they hunt by sight.</summary>
        public override ObjectCategory[] IgnoredCategories => null;

        public override ImageSource GetSpriteFrame() => GetNextSpriteFrame();

        /// <summary>
        /// Birds fly over the water and land on rafts. They ignore raft HP charge, drain HP
        /// (less when resting), and register as predators when landed, hungry, and on a raft.
        /// </summary>
        public override void InteractWithRafts(RaftTickContext ctx)
        {
            TargetObj landedRaft = null;
            foreach (TargetObj raft in ctx.Rafts)
            {
                double raftRadius = raft.Size / 2D;
                Vector toRaft = Point.Subtract(raft.Location, Location);
                if (toRaft.LengthSquared <= raftRadius * raftRadius)
                {
                    raft.ObjectsOnTop++;
                    if (raft.HpCharge > 0 && landedRaft == null)
                        landedRaft = raft;
                }
            }

            IsLanded = landedRaft != null;
            IsGettingHP = false;
            HP -= IsLanded ? LandedHpDrain : FlightHpDrain;

            if (IsLanded && IsHungry)
                ctx.LandedHungryBirds.Add(Tuple.Create(this, landedRaft));
        }

        /// <summary>Number of frogs this bird has eaten.</summary>
        public int FrogsEaten { get; set; }

        public bool IsLanded { get; set; }

        public Bird()
        {
            Size = 40;
            HP = MaxHp * 5;
            InitializeSpriteRhythm();
        }

        public Bird(NeuroNetStructure nnStructure, ref RandomWeightInitializer randomInit)
            : base(nnStructure, ref randomInit)
        {
            Size = 40;
            HP = MaxHp * 5;
            InitializeSpriteRhythm();
        }

        public Bird(INeuralNetwork neuralNetwork)
            : base(neuralNetwork)
        {
            Size = 40;
            HP = MaxHp * 5;
            InitializeSpriteRhythm();
        }

        public ImageSource GetNextSpriteFrame()
        {
            if (IsLanded)
            {
                // Cycle walk frames while landed; when not moving use idle
                _frameCounter++;
                if (_frameCounter >= _nextFrameAt)
                {
                    _frameCounter = 0;
                    _flightFrameIndex = (_flightFrameIndex + 1) % BirdSheetCache.Walk.Length;
                    _nextFrameAt = NextRandom(MinTicksToNextFrame * 2, MaxTicksToNextFrame * 2 + 1);
                }
                int walkIdx = _flightFrameIndex % BirdSheetCache.Walk.Length;
                return LastSpeed > 0.01
                    ? BirdSheetCache.Frame(BirdSheetCache.Walk[walkIdx])
                    : BirdSheetCache.Frame(BirdSheetCache.IdleGround);
            }

            // In-flight: pick bank direction from last rotation
            int[] animation;
            if (LastRotation < -TurnAnimationThreshold)
                animation = BirdSheetCache.CircleLeft;
            else if (LastRotation > TurnAnimationThreshold)
                animation = BirdSheetCache.CircleRight;
            else
                animation = BirdSheetCache.FlyStraight;

            _frameCounter++;
            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _flightFrameIndex = (_flightFrameIndex + 1) % FlightFramesPerAnimation;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return BirdSheetCache.Frame(animation[_flightFrameIndex % FlightFramesPerAnimation]);
        }

        private void InitializeSpriteRhythm()
        {
            _flightFrameIndex = NextRandom(0, FlightFramesPerAnimation);
            _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            _frameCounter = NextRandom(0, _nextFrameAt);
        }

        /// <summary>
        /// Birds move faster than the base agent. Overrides base Act to apply the speed multiplier.
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

        private static int NextRandom(int minValue, int maxValue)
        {
            lock (SpriteRandomLock)
            {
                return SpriteRandom.Next(minValue, maxValue);
            }
        }
    }
}
