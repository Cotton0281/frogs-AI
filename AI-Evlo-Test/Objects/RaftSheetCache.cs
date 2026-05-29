using System.Windows.Media;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Slices the raft sprite sheet (img/raft_sprite_sheet.png) into 4 frozen frames laid out as a
    /// 4×1 grid (627×627 each) — a subtle floating/bobbing cycle. The sheet already has a real
    /// alpha channel, so no color-keying is applied. Loaded as a pack-URI Resource.
    ///
    /// The raft is animated much more slowly than agents (see MainWindow raft animation), since the
    /// motion is only a gentle bob.
    /// </summary>
    internal static class RaftSheetCache
    {
        private const int Columns = 4;
        private const int Rows = 1;
        private const string SheetResource = "img/raft_sprite_sheet.png";

        public static readonly ImageSource[] Frames;
        public static int FrameCount => Frames.Length;

        static RaftSheetCache()
        {
            Frames = SpriteSheet.SliceWithAlpha(SheetResource, Columns, Rows);
        }

        public static ImageSource Frame(int index)
        {
            if (Frames.Length == 0)
                return null;
            return Frames[((index % Frames.Length) + Frames.Length) % Frames.Length];
        }
    }
}
