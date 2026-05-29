using System.Windows.Media;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Slices the frog sprite sheet (img/frog_sprite_sheet_1024_256px_frames.png) into 16 frozen
    /// frames laid out as a 4×4 grid of 256×256 cells, grouped into named animations.
    ///
    ///   Row 0 (0-3):   swim forward — alternating leg kicks
    ///   Row 1 (4-7):   turn left
    ///   Row 2 (8-11):  turn right
    ///   Row 3 (12-15): fast swim    — burst / strong kick cycle
    ///
    /// Loaded as a pack-URI Resource so it works regardless of working directory.
    /// Falls back to a legacy frog frame only if the sheet cannot be loaded.
    /// See data/Frog.md for the full sprite-sheet specification.
    /// </summary>
    internal static class FrogSheetCache
    {
        private const int Columns = 4;
        private const int Rows = 4;
        private const string SheetResource = "img/frog_sprite_sheet.png";

        /// <summary>All 16 frames, indexed by frame number (row*4 + col).</summary>
        public static readonly ImageSource[] Frames;

        public static readonly int[] SwimForward = { 0, 1, 2, 3 };
        public static readonly int[] TurnLeft = { 4, 5, 6, 7 };
        public static readonly int[] TurnRight = { 8, 9, 10, 11 };
        public static readonly int[] FastSwim = { 12, 13, 14, 15 };

        static FrogSheetCache()
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
