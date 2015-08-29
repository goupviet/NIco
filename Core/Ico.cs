using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace NIco
{
    public class Ico : IDisposable, IEnumerable<Image>
    {
        const short IconType = 1;
        
        List<IconFrame> Frames = new List<IconFrame>();

        #region Factory
        public Ico() { }

        public static Ico Load(Stream InputStream)
        {
            InputStream.Position = 0;

            using (var Reader = new BinaryReader(InputStream))
            {
                // Load the icon Header
                if (Reader.ReadInt16() != 0) throw new FormatException("Invalid Ico Format"); //Reserved
                if (Reader.ReadInt16() != IconType) throw new FormatException("Not an Icon"); //Type
                int count = Reader.ReadInt16();

                var Return = new Ico();

                // Read the Icon Headers
                for (int i = 0; i < count; ++i) Return.Frames.Add(new IconFrame(Reader));

                foreach (var Frame in Return.Frames) Frame.ReadImage(Reader);

                return Return;
            }
        }

        public static Ico Load(string FileName)
        {
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }

        public void Dispose()
        {
            Clear();

            Frames = null;
        }
        #endregion

        public static bool IsRecognizedFormat(Stream InputStream)
        {
            InputStream.Position = 0;

            using (var Reader = new BinaryReader(InputStream))
            {
                // Load the icon Header
                if (Reader.ReadInt16() != 0) return false; //Reserved

                if (Reader.ReadInt16() != IconType) return false;

                if (Reader.ReadInt16() <= 0) return false; //Count
            }

            return true;
        }

        public int Count { get { return Frames.Count; } }

        public Image this[int Index]
        {
            get
            {
                var MStream = new MemoryStream();

                using (BinaryWriter Writer = new BinaryWriter(MStream))
                {
                    Writer.Write(Frames[Index].Image);

                    return new Bitmap(MStream);
                }
            }
            set
            {
                if (Index < Count)
                {
                    Bitmap ResizedBitmap = new Bitmap(value, new System.Drawing.Size(Frames[Index].Width, Frames[Index].Height));
                    if (ResizedBitmap != null)
                    {
                        using (var MemoryData = new MemoryStream())
                        {
                            ResizedBitmap.Save(MemoryData, ImageFormat.Png);

                            Frames[Index].Bytes = (int)MemoryData.Length;

                            Frames[Index].Image = MemoryData.ToArray();

                            ResizedBitmap.Dispose();
                        }
                    }
                }
            }
        }

        public void RemoveAt(int Index) { Frames.RemoveAt(Index); }

        public void Clear() { Frames.Clear(); }

        public void Add(Image bitmap, int Size, bool KeepAspectRatio = true)
        {
            if (bitmap == null) throw new InvalidDataException();

            int Width = Size, Height = KeepAspectRatio ? (int)((double)bitmap.Height / bitmap.Width * Size) : Size;

            Bitmap ResizedBitmap = new Bitmap(bitmap, new System.Drawing.Size(Width, Height));

            if (ResizedBitmap != null)
            {
                using (var MemoryData = new MemoryStream())
                {
                    ResizedBitmap.Save(MemoryData, ImageFormat.Png);

                    Frames.Add(new IconFrame(MemoryData.ToArray(), Width, Height, (int)MemoryData.Length));

                    ResizedBitmap.Dispose();
                }
            }
        }

        public void Save(Stream OutputStream)
        {
            using (var Writer = new BinaryWriter(OutputStream))
            {
                // Write Header
                Writer.Write((short)0); //Reserved
                Writer.Write(IconType);
                Writer.Write((short)Count);

                int Offset = (Count * 16) + 6;

                foreach (var Frame in Frames)
                {
                    Frame.Write(Writer);

                    Writer.Write(Offset);

                    Offset += Frame.Bytes;
                }

                foreach (var Frame in Frames) Writer.Write(Frame.Image);
            }
        }

        public void Save(string FileName)
        {
            using (var fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                Save(fs);
        }

        public int SizeOf(int Index) { return Frames[Index].Width; }
        
        #region IEnumerable
        public IEnumerator<Image> GetEnumerator() { for (int i = 0; i < Count; ++i) yield return this[i]; }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion
    }
}