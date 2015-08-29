using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace NIco
{
    public static class Cur
    {
        const short CursorType = 2;

        public static Image Load(Stream InputStream)
        {
            InputStream.Position = 0;

            using (var Reader = new BinaryReader(InputStream))
            {
                // Load the cursor Header
                if (Reader.ReadInt16() != 0) throw new FormatException("Invalid Cur Format"); //Reserved
                if (Reader.ReadInt16() != CursorType) throw new FormatException("Not a Cursor"); //Type
                int count = Reader.ReadInt16();

                var Frame = new IconFrame(Reader);
                Frame.ReadImage(Reader);

                using (var ms = new MemoryStream(Frame.Image))
                    return Bitmap.FromStream(ms);
            }
        }

        public static Image Load(string FileName)
        {
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }

        public static void SaveCursor(this Image Image, Stream OutputStream, int Size, int PointerXOffset = 0, int PointerYOffset = 0, bool KeepAspectRatio = true)
        {
            if (Image == null) throw new InvalidDataException();

            int Width = Size, Height = KeepAspectRatio ? (int)((double)Image.Height / Image.Width * Size) : Size;

            Bitmap ResizedBitmap = new Bitmap(Image, new System.Drawing.Size(Width, Height));

            IconFrame Frame = null;

            if (ResizedBitmap != null)
            {
                using (var MemoryData = new MemoryStream())
                {
                    ResizedBitmap.Save(MemoryData, ImageFormat.Png);

                    Frame = new IconFrame(MemoryData.ToArray(), Width, Height, (int)MemoryData.Length);

                    Frame.Planes = (short)PointerXOffset;
                    Frame.BitsPerPixel = (short)PointerYOffset;

                    ResizedBitmap.Dispose();
                }
            }

            using (var Writer = new BinaryWriter(OutputStream))
            {
                // Write Header
                Writer.Write((short)0); //Reserved
                Writer.Write(CursorType);
                Writer.Write((short)1);

                int Offset = 16 + 6;

                Frame.Write(Writer);

                Writer.Write(Offset);

                Writer.Write(Frame.Image);
            }
        }

        public static void SaveCursor(this Image Image, string FileName, int Size, int PointerXOffset = 0, int PointerYOffset = 0, bool KeepAspectRatio = true)
        {
            using (var fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                SaveCursor(Image, fs, Size, PointerXOffset, PointerYOffset, KeepAspectRatio);
        }

        public static bool IsRecognizedFormat(Stream InputStream)
        {
            InputStream.Position = 0;

            using (var Reader = new BinaryReader(InputStream))
            {
                // Load the icon Header
                if (Reader.ReadInt16() != 0) return false; //Reserved

                if (Reader.ReadInt16() != CursorType) return false;

                if (Reader.ReadInt16() <= 0) return false; //Count
            }

            return true;
        }
    }
}
