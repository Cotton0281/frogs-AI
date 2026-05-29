using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Frog is a SmartObject that has is visualized with image of frog instead of shape.
    /// </summary>
    public class Frog : SmartObject, ISmartObject
    {
        private static readonly Random SpriteRandom = new Random();
        private static readonly object SpriteRandomLock = new object();
        private const int MinTicksToNextFrame = 8;
        private const int MaxTicksToNextFrame = 30;

        private int _frameCounter;
        private int _nextFrameAt;
        private int _idleFrameIndex;

        /// <summary>Rotation magnitude (degrees) above which the turn animation is shown.</summary>
        private const double TurnAnimationThreshold = 0.6;
        private const int FramesPerAnimation = 4;

        public Frog()
        {
            Size = 32;
            InitializeSpriteRhythm();
        }

        public Frog(NeuroNetStructure nnStructure, ref RandomWeightInitializer randomInit)
            : base(nnStructure, ref randomInit)
        {
            Size = 32;
            InitializeSpriteRhythm();
        }

        public Frog(INeuralNetwork neuralNetwork)
            : base(neuralNetwork)
        {
            Size = 32;
            InitializeSpriteRhythm();
        }

        public override ImageSource GetSpriteFrame() => GetNextSpriteFrame();

        public ImageSource GetNextSpriteFrame()
        {
            // Fast burst when moving quickly; otherwise swim, turning left/right by last rotation
            int[] animation;
            if (MaxSpeed > 0 && LastSpeed > MaxSpeed * 0.8)
                animation = FrogSheetCache.FastSwim;
            else if (LastRotation < -TurnAnimationThreshold)
                animation = FrogSheetCache.TurnLeft;
            else if (LastRotation > TurnAnimationThreshold)
                animation = FrogSheetCache.TurnRight;
            else
                animation = FrogSheetCache.SwimForward;

            _frameCounter++;
            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _idleFrameIndex = (_idleFrameIndex + 1) % FramesPerAnimation;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return FrogSheetCache.Frame(animation[_idleFrameIndex % FramesPerAnimation]);
        }

        private void InitializeSpriteRhythm()
        {
            _idleFrameIndex = NextRandom(0, FramesPerAnimation);
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