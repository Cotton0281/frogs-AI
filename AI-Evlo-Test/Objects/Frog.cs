using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Frog is a SmartObject that has is visualized with image of frog instead of shape.
    /// </summary>
    internal class Frog : SmartObject, ISmartObject
    {
        private static readonly Random SpriteRandom = new Random();
        private static readonly object SpriteRandomLock = new object();
        private const int MinTicksToNextFrame = 8;
        private const int MaxTicksToNextFrame = 30;

        private int _frameCounter;
        private int _nextFrameAt;
        private int _idleFrameIndex;

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

        public BitmapImage GetNextSpriteFrame()
        {
            if (MaxSpeed > 0 && LastSpeed > MaxSpeed * 0.8)
                return FrogSpriteCache.FastFrame;

            _frameCounter++;

            if (_frameCounter >= _nextFrameAt)
            {
                _frameCounter = 0;
                _idleFrameIndex = (_idleFrameIndex + 1) % FrogSpriteCache.IdleFrames.Length;
                _nextFrameAt = NextRandom(MinTicksToNextFrame, MaxTicksToNextFrame + 1);
            }

            return FrogSpriteCache.IdleFrames[_idleFrameIndex];
        }

        private void InitializeSpriteRhythm()
        {
            _idleFrameIndex = NextRandom(0, FrogSpriteCache.IdleFrames.Length);
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