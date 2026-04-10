using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
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

        public const double FlightHpDrain = 0.45;
        public const double LandedHpDrain = 0.08;
        public const double HuntHpGain = 200;
        public const double HuntRange = 10; // decreased from 34
        public const double SpeedMultiplier = 0.5;
        public static int BirdMaxHp => MaxHp * 5;

        /// <summary>Birds can only hunt when HP is below this fraction of BirdMaxHp.</summary>
        public const double HuntHpThreshold = 0.9;

        /// <summary>Returns true when the bird is hungry enough to hunt.</summary>
        public bool IsHungry => HP < BirdMaxHp * HuntHpThreshold;

        /// <summary>Birds have 5× the base HP cap.</summary>
        protected override int EffectiveMaxHp => BirdMaxHp;

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

        public BitmapImage GetCurrentSpriteFrame()
        {
            if (IsLanded)
                return BirdSpriteCache.LandedFrame;

            _frameCounter++;
            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _flightFrameIndex = (_flightFrameIndex + 1) % BirdSpriteCache.FlightFrames.Length;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return BirdSpriteCache.FlightFrames[_flightFrameIndex];
        }

        private void InitializeSpriteRhythm()
        {
            _flightFrameIndex = NextRandom(0, BirdSpriteCache.FlightFrames.Length);
            _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            _frameCounter = NextRandom(0, _nextFrameAt);
        }

        /// <summary>
        /// Birds move at 2x the base max speed. Overrides base Act to apply the speed multiplier.
        /// </summary>
        public new double[] Act(double[] arrayInputs)
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
