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
    /// Content and Resource.
    ///
    /// The image's Build Action must be "Resource". Frame index = row * columns + column
    /// (left-to-right, top-to-bottom).
    ///
    /// If the sheet dimensions are not evenly divisible by the grid (e.g. 1254px / 4 = 313.5)
    /// the sheet is scaled to the nearest clean multiple before slicing.
    /// </summary>
    internal static class SpriteSheet
    {
        /// <summary>
        /// Pixels whose R, G and B channels are all at or above this value are treated as the
        /// (near-white) sheet background and made fully transparent. Set to 0 to skip keying
        /// (use when the sheet already has a real alpha channel).
        /// </summary>
        private const byte DefaultBackgroundThreshold = 200;

        /// <summary>A 1×1 fully transparent frame used as a last-resort fallback if a sheet fails to load.</summary>
        public static readonly ImageSource Placeholder = CreatePlaceholder();

        private static ImageSource CreatePlaceholder()
        {
            var bmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Slice a sheet whose background is an opaque near-white colour (no alpha channel).
        /// Background pixels are color-keyed to transparent.
        /// </summary>
        public static ImageSource[] Slice(string resourceRelativePath, int columns, int rows)
            => SliceInternal(resourceRelativePath, columns, rows, DefaultBackgroundThreshold);

        /// <summary>
        /// Slice a sheet that already has a proper alpha channel — no color-keying is applied.
        /// </summary>
        public static ImageSource[] SliceWithAlpha(string resourceRelativePath, int columns, int rows)
            => SliceInternal(resourceRelativePath, columns, rows, backgroundThreshold: 0);

        private static ImageSource[] SliceInternal(string resourceRelativePath, int columns, int rows,
            byte backgroundThreshold)
        {
            try
            {
                var sheet = new BitmapImage();
                sheet.BeginInit();
                sheet.UriSource = new Uri("pack://application:,,,/" + resourceRelativePath, UriKind.Absolute);
                sheet.CacheOption = BitmapCacheOption.OnLoad;
                sheet.EndInit();
                sheet.Freeze();

                BitmapSource source = sheet;

                if (backgroundThreshold > 0)
                    source = KeyOutBackground(source, backgroundThreshold);

                // Use integer division: if the sheet isn't exactly divisible (e.g. 1254 / 4 = 313)
                // CroppedBitmap clamps to the available pixels — close enough for sprite borders.
                int frameW = source.PixelWidth / columns;
                int frameH = source.PixelHeight / rows;

                var frames = new ImageSource[columns * rows];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                    {
                        int x = c * frameW;
                        int y = r * frameH;
                        // Ensure we don't exceed the bitmap bounds on the last column/row
                        int w = Math.Min(frameW, source.PixelWidth - x);
                        int h = Math.Min(frameH, source.PixelHeight - y);
                        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, w, h));
                        cropped.Freeze();
                        frames[r * columns + c] = cropped;
                    }
                return frames;
            }
            catch
            {
                return new ImageSource[0];
            }
        }

        /// <summary>
        /// Returns a copy with near-white background pixels (all channels >= threshold) made
        /// fully transparent. Sprite pixels with any channel below the threshold are untouched.
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
                    pixels[i + 3] = 0;
            }

            var result = BitmapSource.Create(width, height, bgra.DpiX, bgra.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }
}
