using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Preloads all frog _64.png sprites once. Thread-safe via lazy init.
    /// Use IdleFrames for normal animation, FastFrame for high-speed movement.
    /// </summary>
    internal static class FrogSpriteCache
    {
        private static readonly string ImgDir = System.IO.Directory.GetCurrentDirectory() + "\\img\\";

        /// <summary>Frames cycled during normal movement.</summary>
        public static readonly BitmapImage[] IdleFrames;

        /// <summary>Single frame shown when frog moves above 80% max speed.</summary>
        public static readonly BitmapImage FastFrame;

        /// <summary>Animation interval in ticks (roughly 1/3 second at default speed).</summary>
        public const int FrameInterval = 20;

        static FrogSpriteCache()
        {
            string[] idleFiles = new[]
            {
                "frog1_64.png",
                "frog2_64.png",
                "frog3_64.png",
                "frog4_64.png",
                "frog5_64.png",
                "frog9_64.png"
            };

            IdleFrames = new BitmapImage[idleFiles.Length];
            for (int i = 0; i < idleFiles.Length; i++)
            {
                IdleFrames[i] = LoadFrozen(idleFiles[i]);
            }

            FastFrame = LoadFrozen("frog8_64.png");
        }

        private static BitmapImage LoadFrozen(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(ImgDir + fileName);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Returns the correct sprite frame for a frog based on speed and tick count.
        /// </summary>
        /// <param name="lastSpeed">Frog's last movement magnitude</param>
        /// <param name="maxSpeed">Maximum possible speed</param>
        /// <param name="cycleCount">Global tick counter used for frame cycling</param>
        /// <param name="phaseOffset">Per-frog phase offset to desynchronize idle animation</param>
        public static BitmapImage GetFrame(double lastSpeed, double maxSpeed, int cycleCount, int phaseOffset)
        {
            if (maxSpeed > 0 && lastSpeed > maxSpeed * 0.8)
                return FastFrame;

            int frameIndex = ((cycleCount + phaseOffset) / FrameInterval) % IdleFrames.Length;
            return IdleFrames[frameIndex];
        }
    }
}
