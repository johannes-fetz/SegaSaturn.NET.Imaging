/*
** SegaSaturn.NET
** Copyright (c) 2020-2021, Johannes Fetz (johannesfetz@gmail.com)
** All rights reserved.
**
** Redistribution and use in source and binary forms, with or without
** modification, are permitted provided that the following conditions are met:
**     * Redistributions of source code must retain the above copyright
**       notice, this list of conditions and the following disclaimer.
**     * Redistributions in binary form must reproduce the above copyright
**       notice, this list of conditions and the following disclaimer in the
**       documentation and/or other materials provided with the distribution.
**     * Neither the name of the Johannes Fetz nor the
**       names of its contributors may be used to endorse or promote products
**       derived from this software without specific prior written permission.
**
** THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
** ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
** WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
** DISCLAIMED. IN NO EVENT SHALL Johannes Fetz BE LIABLE FOR ANY
** DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
** (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
** LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
** ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
** (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
** SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Paloma;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SegaSaturn.NET.Imaging
{
    public static class SegaSaturnImageConverter
    {
        public static readonly string[] ValidFileExtensions = { ".jpg", ".png", ".tga", ".bmp", ".gif", ".bin" };

        public static void ToPng(Bitmap bitmap, Stream stream)
        {
            bitmap.Save(stream, ImageFormat.Png);
        }

        public static void ToPng(SegaSaturnTexture texture, Stream stream, SegaSaturnColor transparentColor = null)
        {
            SegaSaturnImageConverter.ToBitmap(texture, transparentColor).Save(stream, ImageFormat.Png);
        }

        public static void ToPng(Bitmap bitmap, string path)
        {
            bitmap.Save(path, ImageFormat.Png);
        }

        public static void ToPng(SegaSaturnTexture texture, string path, SegaSaturnColor transparentColor = null)
        {
            SegaSaturnImageConverter.ToBitmap(texture, transparentColor).Save(path, ImageFormat.Png);
        }

        public static Bitmap LoadBitmapFromFile(string path, SegaSaturnColor transparentColor = null)
        {
            if (Path.GetExtension(path).ToLowerInvariant() == ".bin")
            {
                using (Stream stream = File.Open(path, FileMode.Open))
                {
                    using (BinaryReader streamReader = new BinaryReader(stream))
                    {
                        Bitmap bin = new Bitmap(streamReader.ReadUInt16(), streamReader.ReadUInt16(), PixelFormat.Format32bppArgb);
                        using (BmpPixelSnoop tmp = new BmpPixelSnoop(bin))
                        {
                            for (int y = 0; y < tmp.Height; ++y)
                            {
                                for (int x = 0; x < tmp.Width; ++x)
                                {
                                    tmp.SetPixel(x, y, new SegaSaturnColor(streamReader.ReadUInt16()));
                                }
                            }
                        }
                        return bin;
                    }
                }
            }
            if (Path.GetExtension(path).ToLowerInvariant() == ".tga")
            {
                Bitmap tga = TargaImage.LoadTargaImage(path);
                if (transparentColor != null)
                    tga.ReplaceColor(transparentColor, Color.Transparent);
                return tga;
            }
            Bitmap bitmap = (Bitmap)Bitmap.FromFile(path);
            if (transparentColor != null)
                bitmap.ReplaceColor(transparentColor, Color.Transparent);
            return bitmap;
        }

        public static SegaSaturnTexture LoadTextureFromFile(string path, SegaSaturnColor transparentColor = null)
        {
            return SegaSaturnImageConverter.LoadFromBitmap(SegaSaturnImageConverter.LoadBitmapFromFile(path), transparentColor);
        }

        public static SegaSaturnTexture LoadFromBitmap(Bitmap bitmap, SegaSaturnColor transparentColor = null)
        {
            if (bitmap == null)
                return null;
            SegaSaturnTexture toReturn = new SegaSaturnTexture(bitmap.Width, bitmap.Height);
            using (BmpPixelSnoop tmp = new BmpPixelSnoop(bitmap))
            {
                for (int y = 0; y < tmp.Height; y++)
                {
                    for (int x = 0; x < tmp.Width; x++)
                    {
                        Color c = tmp.GetPixel(x, y);
                        if (c.A != 255 && transparentColor != null && transparentColor.A == 255)
                            c = transparentColor;
                        toReturn.SetPixel(x, y, c);
                    }
                }
            }
            return toReturn;
        }

        public static Bitmap ToBitmap(SegaSaturnTexture texture, SegaSaturnColor transparentColor = null)
        {
            if (texture == null)
                return null;
            Bitmap toReturn = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
            using (BmpPixelSnoop tmp = new BmpPixelSnoop(toReturn))
            {
                for (int y = 0; y < tmp.Height; y++)
                {
                    for (int x = 0; x < tmp.Width; x++)
                    {
                        SegaSaturnColor color = texture.GetPixel(x, y);
                        toReturn.SetPixel(x, y, transparentColor != null && color == transparentColor ? Color.Transparent : (Color)color);
                    }
                }
            }
            return toReturn;
        }

        public static void To24BitsTga(SegaSaturnTexture texture, string path, SegaSaturnColor transparentColor = null)
        {
            using (Bitmap bmp = SegaSaturnImageConverter.ToBitmap(texture))
            {
                SegaSaturnImageConverter.To24BitsTga(bmp, path, transparentColor);
            }
        }

        public static void To24BitsTga(SegaSaturnTexture texture, Stream output, SegaSaturnColor transparentColor = null)
        {
            using (Bitmap bmp = SegaSaturnImageConverter.ToBitmap(texture))
            {
                SegaSaturnImageConverter.To24BitsTga(bmp, output, transparentColor);
            }
        }

        public static void To24BitsTga(Bitmap bitmap, string path, SegaSaturnColor transparentColor = null)
        {
            using (Stream file = File.Create(path))
            {
                SegaSaturnImageConverter.To24BitsTga(bitmap, file, transparentColor);
            }
        }

        public static void To24BitsTga(Bitmap bitmap, Stream output, SegaSaturnColor transparentColor = null)
        {
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                using (BmpPixelSnoop tmp = new BmpPixelSnoop(bitmap))
                {
                    writer.Write(new byte[]
                    {
                        0, // ID length
                        0, // no color map
                        2, // uncompressed, true color
                        0, 0, 0, 0,
                        0,
                        0, 0, 0, 0, // x and y origin
                        (byte)(tmp.Width & 0x00FF),
                        (byte)((tmp.Width & 0xFF00) >> 8),
                        (byte)(tmp.Height & 0x00FF),
                        (byte)((tmp.Height & 0xFF00) >> 8),
                        24, // 24 bit bitmap
                        0
                    });
                    for (int y = 0; y < tmp.Height; y++)
                    {
                        for (int x = 0; x < tmp.Width; x++)
                        {
                            Color c = tmp.GetPixel(x, tmp.Height - y - 1);
                            if (c.A != 255 && transparentColor != null && transparentColor.A == 255)
                                c = transparentColor;
                            writer.Write(new[]
                            {
                                c.B,
                                c.G,
                                c.R
                            });
                        }
                    }
                }
            }
        }

        public static void ToBin(Bitmap bmp, string path, bool header)
        {
            using (Stream stream = File.Create(path))
            {
                SegaSaturnImageConverter.ToBin(bmp, stream, header);
            }
        }

        public static void ToBin(Bitmap bmp, Stream outputStream, bool header)
        {
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                if (header)
                {
                    writer.Write((ushort)bmp.Width);
                    writer.Write((ushort)bmp.Height);
                }
                using (BmpPixelSnoop tmp = new BmpPixelSnoop(bmp))
                {
                    for (int y = 0; y < tmp.Height; ++y)
                    {
                        for (int x = 0; x < tmp.Width; ++x)
                        {
                            writer.Write(new SegaSaturnColor(tmp.GetPixel(x, y)).SaturnColor);
                        }
                    }
                }
            }
        }

        public static void ToBin(SegaSaturnTexture texture, string path, bool header)
        {
            using (Stream stream = File.Create(path))
            {
                SegaSaturnImageConverter.ToBin(texture, stream, header);
            }
        }

        public static void ToBin(SegaSaturnTexture texture, Stream outputStream, bool header)
        {
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                if (header)
                {
                    writer.Write((ushort)texture.Width);
                    writer.Write((ushort)texture.Height);
                }
                for (int y = 0; y < texture.Height; y += texture.Width)
                {
                    for (int x = 0; x < texture.Width; ++x)
                    {
                        writer.Write(texture.GetPixel(x, y).SaturnColor);
                    }
                }
            }
        }
    }
}
