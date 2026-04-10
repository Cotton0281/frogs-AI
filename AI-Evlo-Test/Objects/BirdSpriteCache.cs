using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    internal static class BirdSpriteCache
    {
        private static readonly string ImgDir = Directory.GetCurrentDirectory() + "\\img\\";

        public static readonly BitmapImage[] FlightFrames;
        public static readonly BitmapImage LandedFrame;

        static BirdSpriteCache()
        {
            string[] flightFiles = new[]
            {
                "bird1.png",
                "bird2.png",
                "bird3.png",
                "bird4.png",
                "bird5.png"
            };

            FlightFrames = new BitmapImage[flightFiles.Length];
            for (int i = 0; i < flightFiles.Length; i++)
            {
                FlightFrames[i] = LoadFrozenOrFallback(flightFiles[i], FrogSpriteCache.IdleFrames[i % FrogSpriteCache.IdleFrames.Length]);
            }

            LandedFrame = LoadFrozenOrFallback("bird_landed.png", FlightFrames[0]);
        }

        private static BitmapImage LoadFrozenOrFallback(string fileName, BitmapImage fallback)
        {
            string imagePath = Path.Combine(ImgDir, fileName);
            if (!File.Exists(imagePath))
                return fallback;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(imagePath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}