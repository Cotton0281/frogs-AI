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
        private static readonly Dictionary<ImageSource, ImageSource> RedCache = new Dictionary<ImageSource, ImageSource>();

        public static ImageSource GetTinted(ImageSource source)
        {
            return GetTinted(source, Cache, TintGold);
        }

        public static ImageSource GetRedTinted(ImageSource source)
        {
            return GetTinted(source, RedCache, TintRed);
        }

        private static ImageSource GetTinted(
            ImageSource source,
            Dictionary<ImageSource, ImageSource> cache,
            Action<byte[], int> tintPixel)
        {
            if (!(source is BitmapSource bitmapSource))
                return source;

            lock (CacheLock)
            {
                if (cache.TryGetValue(source, out ImageSource cached))
                    return cached;

                ImageSource tinted = CreateTinted(bitmapSource, tintPixel);
                cache[source] = tinted;
                return tinted;
            }
        }

        private static ImageSource CreateTinted(BitmapSource source, Action<byte[], int> tintPixel)
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

                tintPixel(pixels, i);
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

        private static void TintGold(byte[] pixels, int offset)
        {
            pixels[offset] = (byte)Math.Min(255, pixels[offset] * 0.35 + 28);              // B
            pixels[offset + 1] = (byte)Math.Min(255, pixels[offset + 1] * 0.55 + 150);     // G
            pixels[offset + 2] = (byte)Math.Min(255, pixels[offset + 2] * 0.55 + 165);     // R
        }

        private static void TintRed(byte[] pixels, int offset)
        {
            pixels[offset] = (byte)Math.Min(255, pixels[offset] * 0.25 + 24);              // B
            pixels[offset + 1] = (byte)Math.Min(255, pixels[offset + 1] * 0.25 + 24);      // G
            pixels[offset + 2] = (byte)Math.Min(255, pixels[offset + 2] * 0.65 + 190);     // R
        }
    }
}
