using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Loads the shark sprite sheet (img/Shark.png) once and slices it into 16 frozen frames
    /// laid out as a 4×4 grid. Frames are grouped into named animations.
    ///
    /// Layout (column-major within each row, frame index = row*4 + col):
    ///   Row 0 (0-3):   swim forward   — 0 neutral, 1 tail-left, 2 tail-right, 3 glide
    ///   Row 1 (4-7):   turn left      — 4 start, 5 stronger, 6 mid, 7 finish
    ///   Row 2 (8-11):  turn right     — 8 start, 9 stronger, 10 mid, 11 finish
    ///   Row 3 (12-15): bite/attack    — 12 closed, 13 opening, 14 full bite, 15 recover
    ///
    /// If the sheet is missing it falls back to a single frog frame so the cache always
    /// yields a usable image. See data/Shark.md for the full sprite-sheet specification.
    /// </summary>
    internal static class SharkSpriteCache
    {
        private const int Columns = 4;
        private const int Rows = 4;

        private static readonly string SheetPath = Directory.GetCurrentDirectory() + "\\img\\Shark.png";

        /// <summary>All 16 frames, indexed by frame number (row*4 + col).</summary>
        public static readonly ImageSource[] Frames;

        public static readonly int[] SwimForward = { 0, 1, 2, 3 };
        public static readonly int[] TurnLeft = { 4, 5, 6, 7 };
        public static readonly int[] TurnRight = { 8, 9, 10, 11 };
        public static readonly int[] Bite = { 12, 13, 14, 15 };

        static SharkSpriteCache()
        {
            Frames = SliceSheet(SheetPath, Columns, Rows);
        }

        /// <summary>Returns the frame for the given index, guarding against the fallback case.</summary>
        public static ImageSource Frame(int index)
        {
            if (index < 0 || index >= Frames.Length)
                return Frames[0];
            return Frames[index];
        }

        private static ImageSource[] SliceSheet(string path, int cols, int rows)
        {
            if (!File.Exists(path))
                return new ImageSource[] { FrogSpriteCache.FastFrame };

            BitmapImage sheet = new BitmapImage();
            sheet.BeginInit();
            sheet.UriSource = new Uri(path);
            sheet.CacheOption = BitmapCacheOption.OnLoad;
            sheet.EndInit();
            sheet.Freeze();

            int frameW = sheet.PixelWidth / cols;
            int frameH = sheet.PixelHeight / rows;

            var frames = new ImageSource[cols * rows];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var rect = new Int32Rect(c * frameW, r * frameH, frameW, frameH);
                    var cropped = new CroppedBitmap(sheet, rect);
                    cropped.Freeze();
                    frames[r * cols + c] = cropped;
                }
            }
            return frames;
        }
    }
}
