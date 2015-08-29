using System.IO;

namespace NIco
{
    class IconFrame
    {
        public byte ColorCount = 0;
        public short Planes = 0, BitsPerPixel = 32;

        public byte Width, Height;
        public int Bytes;
        public byte[] Image;

        public IconFrame(byte[] Image, int Width, int Height, int Bytes)
        {
            this.Image = Image;
            this.Width = (byte)Width;
            this.Height = (byte)Height;
            this.Bytes = Bytes;
        }

        public IconFrame(BinaryReader Reader)
        {
            Width = Reader.ReadByte();
            Height = Reader.ReadByte();
            ColorCount = Reader.ReadByte(); // ColorCount
            Reader.ReadByte(); //Reserved
            Planes = Reader.ReadInt16(); // Planes
            BitsPerPixel = Reader.ReadInt16(); //BitsPerPixel
            Bytes = Reader.ReadInt32();
            Reader.ReadInt32(); //Offset
        }

        public void ReadImage(BinaryReader Reader) { Image = Reader.ReadBytes(Bytes); }

        public void Write(BinaryWriter Writer)
        {
            Writer.Write(Width);
            Writer.Write(Height);
            Writer.Write(ColorCount);

            Writer.Write((byte)0); //Reserved

            Writer.Write(Planes);
            Writer.Write(BitsPerPixel);
            Writer.Write(Bytes);

            //Writer.Write(Offset);
        }
    }
}