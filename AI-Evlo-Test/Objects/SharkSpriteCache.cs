using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Preloads shark swim frames once, frozen for cross-thread use.
    /// Prefers dedicated shark{1..5}.png frames if present; otherwise falls back to
    /// sharks.jpg, and finally to a frog frame so the cache always yields an image.
    /// </summary>
    internal static class SharkSpriteCache
    {
        private static readonly string ImgDir = Directory.GetCurrentDirectory() + "\\img\\";

        public static readonly BitmapImage[] SwimFrames;

        static SharkSpriteCache()
        {
            BitmapImage baseFrame = LoadFrozenOrFallback("sharks.jpg", FrogSpriteCache.FastFrame);

            var frames = new List<BitmapImage>();
            for (int i = 1; i <= 5; i++)
            {
                string file = "shark" + i + ".png";
                if (File.Exists(Path.Combine(ImgDir, file)))
                    frames.Add(LoadFrozenOrFallback(file, baseFrame));
            }

            if (frames.Count == 0)
                frames.Add(baseFrame);

            SwimFrames = frames.ToArray();
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
