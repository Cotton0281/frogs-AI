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
        /// <summary>
        /// Pixels whose R, G and B channels are all at or above this value are treated as the
        /// (near-white) sheet background and made fully transparent. The provided sheets ship with
        /// an opaque ~230 grey background rather than a real alpha channel.
        /// </summary>
        private const byte DefaultBackgroundThreshold = 200;

        public static ImageSource[] Slice(string resourceRelativePath, int columns, int rows,
            byte backgroundThreshold = DefaultBackgroundThreshold)
        {
            try
            {
                var sheet = new BitmapImage();
                sheet.BeginInit();
                sheet.UriSource = new Uri("pack://application:,,,/" + resourceRelativePath, UriKind.Absolute);
                sheet.CacheOption = BitmapCacheOption.OnLoad;
                sheet.EndInit();
                sheet.Freeze();

                BitmapSource keyed = KeyOutBackground(sheet, backgroundThreshold);

                int frameW = keyed.PixelWidth / columns;
                int frameH = keyed.PixelHeight / rows;

                var frames = new ImageSource[columns * rows];
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        var cropped = new CroppedBitmap(keyed, new Int32Rect(c * frameW, r * frameH, frameW, frameH));
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

        /// <summary>
        /// Returns a copy of the source with near-white background pixels made transparent.
        /// Colored sprite pixels (where any channel is below the threshold) are left untouched.
        /// </summary>
        private static BitmapSource KeyOutBackground(BitmapSource source, byte threshold)
        {
            var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = bgra.PixelWidth;
            int height = bgra.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            bgra.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                if (b >= threshold && g >= threshold && r >= threshold)
                    pixels[i + 3] = 0; // fully transparent
            }

            var result = BitmapSource.Create(width, height, bgra.DpiX, bgra.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }
}
