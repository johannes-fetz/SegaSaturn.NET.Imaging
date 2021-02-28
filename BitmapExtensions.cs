using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SegaSaturn.NET.Imaging
{
    public static class BitmapExtensions
    {
        public static Bitmap AdjustBrightness(this Bitmap image, float brightness)
        {
            float b = brightness;
            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] {b, 0, 0, 0, 0},
                new float[] {0, b, 0, 0, 0},
                new float[] {0, 0, b, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1},
            });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(cm);
            Point[] points =
            {
                new Point(0, 0),
                new Point(image.Width, 0),
                new Point(0, image.Height),
            };
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            Bitmap bm = new Bitmap(image.Width, image.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(image, points, rect, GraphicsUnit.Pixel, attributes);
            }
            return bm;
        }

        public static Bitmap Resize(this Bitmap originalBitmap, int? maxWidth, int? maxHeight, bool saturn, Bitmap lastBitmap = null)
        {
            if (originalBitmap == null)
                return null;
            int currentHeight = originalBitmap.Height;
            int currentWidth = originalBitmap.Width;

            double? widthScale = null;
            double? heightScale = null;
            if (maxWidth.HasValue)
                widthScale = maxWidth / (double)currentWidth;
            if (maxHeight.HasValue)
                heightScale = maxHeight / (double)currentHeight;

            double scale;
            if (widthScale.HasValue && heightScale.HasValue)
                scale = heightScale < widthScale ? heightScale.Value : widthScale.Value;
            else if (widthScale.HasValue)
                scale = widthScale.Value;
            else
                scale = heightScale ?? 1.0;
            if (scale <= 0.0f || Math.Abs(scale - 1.0) < double.Epsilon)
                return originalBitmap;

            int newWidth = (int)(currentWidth * scale);
            int newHeight = (int)(currentHeight * scale);
            Bitmap resizedImage = lastBitmap != null && lastBitmap.Width == newWidth && lastBitmap.Height == newHeight ? lastBitmap : new Bitmap(newWidth, newHeight);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                if (saturn)
                {
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                }
                graphics.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
            }
            return resizedImage;
        }

        public static void ReplaceColor(this Bitmap img, SegaSaturnColor src, SegaSaturnColor dest)
        {
            using (BmpPixelSnoop tmp = new BmpPixelSnoop(img))
            {
                int srcColor = src.ToArgb();
                for (int y = 0; y < tmp.Height; ++y)
                {
                    for (int x = 0; x < tmp.Width; ++x)
                    {
                        if (tmp.GetPixel(x, y).ToArgb() == srcColor)
                            tmp.SetPixel(x, y, dest);
                    }
                }
            }
        }

        public static bool IsTransparent(this Bitmap img, SegaSaturnColor transparentColor)
        {
            using (BmpPixelSnoop tmp = new BmpPixelSnoop(img))
            {
                int tc = transparentColor.ToArgb();
                for (int y = 0; y < tmp.Height; ++y)
                {
                    for (int x = 0; x < tmp.Width; ++x)
                    {
                        Color c = tmp.GetPixel(x, y);
                        if (c.A > 0 || c.ToArgb() == tc)
                            return false;
                    }
                }
                return true;
            }
        }

        public static bool ContainsColor(this Bitmap img, SegaSaturnColor toFind)
        {
            using (BmpPixelSnoop tmp = new BmpPixelSnoop(img))
            {
                for (int y = 0; y < tmp.Height; ++y)
                {
                    for (int x = 0; x < tmp.Width; ++x)
                    {
                        if (tmp.GetPixel(x, y) == toFind)
                            return true;
                    }
                }
                return true;
            }
        }

        public static Bitmap Crop(this Bitmap img, Rectangle cropArea)
        {
            return img.Clone(cropArea, img.PixelFormat);
        }
    }
}
