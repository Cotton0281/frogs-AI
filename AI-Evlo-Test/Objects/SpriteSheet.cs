using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Loads a sprite-sheet image (as an embedded WPF Resource, via a pack URI) and slices it
    /// into a grid of frozen frames. Using a pack URI means the frames resolve regardless of the
    /// working directory and survive Visual Studio flipping the image's build action between
    /// Content and Resource — the recurring gotcha that left sharks showing the fallback frame.
    ///
    /// The image's Build Action must be "Resource". Frame index = row * columns + column
    /// (left-to-right, top-to-bottom).
    /// </summary>
    internal static class SpriteSheet
    {
        public static ImageSource[] Slice(string resourceRelativePath, int columns, int rows)
        {
            try
            {
                var sheet = new BitmapImage();
                sheet.BeginInit();
                sheet.UriSource = new Uri("pack://application:,,,/" + resourceRelativePath, UriKind.Absolute);
                sheet.CacheOption = BitmapCacheOption.OnLoad;
                sheet.EndInit();
                sheet.Freeze();

                int frameW = sheet.PixelWidth / columns;
                int frameH = sheet.PixelHeight / rows;

                var frames = new ImageSource[columns * rows];
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        var cropped = new CroppedBitmap(sheet, new Int32Rect(c * frameW, r * frameH, frameW, frameH));
                        cropped.Freeze();
                        frames[r * columns + c] = cropped;
                    }
                }
                return frames;
            }
            catch
            {
                return new ImageSource[0];
            }
        }
    }
}
