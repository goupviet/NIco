using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace NIco
{
    public class IconFrameItem : INotifyPropertyChanged
    {
        public string FilePath;

        public string FileName { get { return Path.GetFileNameWithoutExtension(FilePath); } }

        bool DisposeFile;

        public bool KeepAspectRatio = true;

        int rotateBy = 0;

        int RotateBy
        {
            get { return rotateBy; }
            set
            {
                rotateBy = value;
                while (rotateBy >= 360) rotateBy -= 360;
                while (rotateBy < 0) rotateBy += 360;

                if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Image"));
            }
        }

        ~IconFrameItem()
        {
            if (DisposeFile)
                try { File.Delete(FilePath); }
                catch { }
        }

        public void RotateRight() { RotateBy -= 90; }
        public void RotateLeft() { RotateBy += 90; }

        public IconFrameItem(string FilePath, int Size = 256, bool dispose = false)
        {
            this.FilePath = FilePath;
            this.Size = Size;
            this.DisposeFile = dispose;
        }

        RotateFlipType RFFlags { get { return (RotateFlipType)Enum.Parse(typeof(RotateFlipType), "Rotate" + RotateBy + "FlipNone"); } }

        public Bitmap Bitmap
        {
            get
            {
                Bitmap bmp = new Bitmap(FilePath);

                if (RotateBy != 0) bmp.RotateFlip(RFFlags);

                return bmp;
            }
        }

        public System.Windows.Controls.Image Image
        {
            get
            {
                if (RotateBy != 0)
                {
                    var ms = new MemoryStream();
                    Bitmap.Save(ms, ImageFormat.Png);

                    var Decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                    if (Decoder.Frames.Count > 0) return new System.Windows.Controls.Image() { Source = (BitmapSource)Decoder.Frames[0] };
                    else return null;
                }
                else
                {
                    var Decoder = BitmapDecoder.Create(new Uri(FilePath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                    if (Decoder.Frames.Count > 0) return new System.Windows.Controls.Image() { Source = (BitmapSource)Decoder.Frames[0] };
                    else return null;
                }
            }
        }

        public int Size { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}