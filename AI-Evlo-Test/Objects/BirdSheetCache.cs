using System.Windows.Media;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Slices the bird sprite sheet (img/bird_sprite_sheet_1024_256px_frames.png) into 16 frozen
    /// frames laid out as a 4×4 grid, grouped into named animations.
    ///
    ///   Row 0 (0-3):   flyStraight  — wing-flap cycle
    ///   Row 1 (4-7):   circleLeft   — banked left-turn cycle
    ///   Row 2 (8-11):  circleRight  — banked right-turn cycle
    ///   Row 3 (12-15): landed       — 12 landing, 13-14 walk, 15 idle
    ///
    /// The sheet already has a proper alpha channel so no color-keying is applied.
    /// Loaded as a pack-URI Resource so it works regardless of working directory.
    /// See data/Bird.md for the full sprite-sheet specification.
    /// </summary>
    internal static class BirdSheetCache
    {
        private const int Columns = 4;
        private const int Rows = 4;
        private const string SheetResource = "img/bird_sprite_sheet_1024_256px_frames.png";

        public static readonly ImageSource[] Frames;

        public static readonly int[] FlyStraight = { 0, 1, 2, 3 };
        public static readonly int[] CircleLeft = { 4, 5, 6, 7 };
        public static readonly int[] CircleRight = { 8, 9, 10, 11 };
        /// <summary>Single landing frame shown briefly when the bird first touches a raft.</summary>
        public static readonly int LandFrame = 12;
        public static readonly int[] Walk = { 13, 14 };
        public static readonly int IdleGround = 15;

        static BirdSheetCache()
        {
            ImageSource[] sliced = SpriteSheet.SliceWithAlpha(SheetResource, Columns, Rows);
            Frames = sliced.Length > 0 ? sliced : new ImageSource[] { BirdSpriteCache.FlightFrames[0] };
        }

        public static ImageSource Frame(int index)
        {
            if (index < 0 || index >= Frames.Length)
                return Frames[0];
            return Frames[index];
        }
    }
}
