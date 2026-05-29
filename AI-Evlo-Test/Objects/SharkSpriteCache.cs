using System.Windows.Media;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Slices the shark sprite sheet (img/shark_sprite_sheet_1024_256px_frames.png) into 16 frozen
    /// frames laid out as a 4×4 grid of 256×256 cells, grouped into named animations.
    ///
    ///   Row 0 (0-3):   swim forward — 0 neutral, 1 tail-left, 2 tail-right, 3 glide
    ///   Row 1 (4-7):   turn left    — 4 start, 5 stronger, 6 mid, 7 finish
    ///   Row 2 (8-11):  turn right   — 8 start, 9 stronger, 10 mid, 11 finish
    ///   Row 3 (12-15): bite/attack  — 12 closed, 13 opening, 14 full bite, 15 recover
    ///
    /// Loaded as a pack-URI Resource so it works regardless of working directory.
    /// Falls back to a single frog frame only if the sheet cannot be loaded.
    /// See data/Shark.md for the full sprite-sheet specification.
    /// </summary>
    internal static class SharkSpriteCache
    {
        private const int Columns = 4;
        private const int Rows = 4;
        private const string SheetResource = "img/shark_sprite_sheet_1024_256px_frames.png";

        /// <summary>All 16 frames, indexed by frame number (row*4 + col).</summary>
        public static readonly ImageSource[] Frames;

        public static readonly int[] SwimForward = { 0, 1, 2, 3 };
        public static readonly int[] TurnLeft = { 4, 5, 6, 7 };
        public static readonly int[] TurnRight = { 8, 9, 10, 11 };
        public static readonly int[] Bite = { 12, 13, 14, 15 };

        static SharkSpriteCache()
        {
            ImageSource[] sliced = SpriteSheet.Slice(SheetResource, Columns, Rows);
            Frames = sliced.Length > 0 ? sliced : new ImageSource[] { SpriteSheet.Placeholder };
        }

        /// <summary>Returns the frame for the given index, guarding against the fallback case.</summary>
        public static ImageSource Frame(int index)
        {
            if (index < 0 || index >= Frames.Length)
                return Frames[0];
            return Frames[index];
        }
    }
}
