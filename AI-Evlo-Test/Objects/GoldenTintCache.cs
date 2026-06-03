using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test.Objects
{
    public static class GoldenTintCache
    {
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<ImageSource, ImageSource> Cache = new Dictionary<ImageSource, ImageSource>();

        public static ImageSource GetTinted(ImageSource source)
        {
            if (!(source is BitmapSource bitmapSource))
                return source;

            lock (CacheLock)
            {
                if (Cache.TryGetValue(source, out ImageSource cached))
                    return cached;

                ImageSource tinted = CreateTinted(bitmapSource);
                Cache[source] = tinted;
                return tinted;
            }
        }

        private static ImageSource CreateTinted(BitmapSource source)
        {
            BitmapSource formatted = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int stride = formatted.PixelWidth * 4;
            byte[] pixels = new byte[stride * formatted.PixelHeight];
            formatted.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte alpha = pixels[i + 3];
                if (alpha == 0)
                    continue;

                pixels[i] = (byte)Math.Min(255, pixels[i] * 0.35 + 28);          // B
                pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * 0.55 + 150); // G
                pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * 0.55 + 165); // R
            }

            BitmapSource result = BitmapSource.Create(
                formatted.PixelWidth,
                formatted.PixelHeight,
                formatted.DpiX,
                formatted.DpiY,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);
            result.Freeze();
            return result;
        }
    }
}
