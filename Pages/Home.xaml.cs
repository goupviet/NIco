using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ManagedWin32;
using Microsoft.Win32;

namespace NIco
{
    partial class Home : UserControl
    {
        OpenFileDialog OFD;
        SaveFileDialog SFD;

        public Home()
        {
            InitializeComponent();

            #region File Dialogs
            OFD = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                ValidateNames = true,
                Title = "Select Image Files",
                Filter = "Image Files|*.bmp;*.emf;*.exif;*.jpeg;*.jpg;*.png;*.tiff;*.wmf|All Files|*.*"
            };

            SFD = new SaveFileDialog()
            {
                Title = "Destination",
                ValidateNames = true,
                AddExtension = true,

                DefaultExt = ".ico",
                Filter = "Icon|*.ico"
            };
            #endregion

            #region Command Bindings
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (s, e) => Save(),
                (s, e) => e.CanExecute = Gallery.Items.Count != 0));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => AddFromClipboard(),
                (s, e) => e.CanExecute = Clipboard.ContainsImage()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (s, e) =>
            {
                var SelectedItems = new object[Gallery.Items.Count];
                Gallery.SelectedItems.CopyTo(SelectedItems, 0);

                foreach (var SelectedItem in SelectedItems) Gallery.Items.Remove(SelectedItem);

                if (Gallery.Items.Count == 0) Preview.Content = null;
            }, (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => new MainWindow().Show()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (s, e) => Open()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, (s, e) =>
            {
                var exportfd = new SaveFileDialog()
                {
                    Title = "Destination",
                    ValidateNames = true,
                };

                if (exportfd.ShowDialog().Value) SelectedItem.Bitmap.Save(exportfd.FileName);
            }, (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(NavigationCommands.IncreaseZoom, (s, e) => zoomSlider.Value += 0.1,
                (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(NavigationCommands.DecreaseZoom, (s, e) => zoomSlider.Value -= 0.1,
                (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            NavigationCommands.IncreaseZoom.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.Control));
            NavigationCommands.DecreaseZoom.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.Control));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (s, e) =>
            {
                SelectedItem.RotateRight();
                Refresh();
            }, (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (s, e) =>
            {
                SelectedItem.RotateLeft();
                Refresh();
            }, (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (s, e) => Gallery.SelectAll(),
                (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) =>
            {
                var ms = new MemoryStream();
                SelectedItem.Bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                Clipboard.SetImage(new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0]);
            },
            (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (s, e) =>
            {
                var ms = new MemoryStream();
                SelectedItem.Bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                Clipboard.SetImage(new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0]);
                Gallery.Items.Remove(SelectedItem);

                if (Gallery.Items.Count == 0) Preview.Content = null;
            },
            (s, e) => e.CanExecute = Gallery.SelectedIndex != -1));
            #endregion
        }

        void Open()
        {
            var ifd = new IconPickerDialog();

            if (ifd.ShowDialog().Value)
            {
                new Thread(new ThreadStart(() =>
                    {
                        try
                        {
                            foreach (var frame in !ifd.FileName.EndsWith(".ico")
                                ? PEFormat.Extract(ifd.FileName, ifd.IconIndex)
                                    : Ico.Load(ifd.FileName))
                            {
                                var path = Path.GetTempFileName();

                                frame.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                                Add(path, frame.Size.Width, true);
                            }
                        }
                        catch (Exception e) { Dispatcher.Invoke(new Action(() => Status.Text = e.Message)); }
                    })).Start();
            }
        }

        void BrowseForImages<T>(object sender, T e)
        {
            if (OFD.ShowDialog().Value)
            {
                new Thread(new ThreadStart(() =>
                            {
                                foreach (string FileName in OFD.FileNames)
                                    Add(FileName, 256);
                            })).Start();
            }
        }

        void Gallery_SelectionChanged(object sender, SelectionChangedEventArgs e) { Refresh(); }

        void Refresh()
        {
            if (Gallery.SelectedIndex == -1) return;

            try
            {
                var img = SelectedItem.Image;
                if (img != null) Preview.Content = img;
            }
            catch { }

            try
            {
                int Size = SelectedItem.Size;

                KeepAR.IsChecked = SelectedItem.KeepAspectRatio;

                for (int i = 0; i < Sizes.Items.Count; ++i)
                {
                    if (int.Parse(((ComboBoxItem)Sizes.Items[i]).Tag.ToString()) == Size)
                    {
                        Sizes.SelectedIndex = i;
                        break;
                    }
                }

                Preview.Height = Preview.Width = Size;
            }
            catch { }
        }

        void AddFromClipboard()
        {
            var Encoder = new BmpBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));

            string path = Path.GetTempFileName();

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);

            Encoder.Save(fs);

            fs.Close();

            Add(path, 256, true);
        }

        void Gallery_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var Paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                new Thread(new ThreadStart(() =>
                    {
                        foreach (var Path in Paths)
                            Add(Path, 256);
                    })).Start();
            }
        }

        void Save()
        {
            if (SFD.ShowDialog().Value)
            {
                try
                {
                    using (var nico = new Ico())
                    {
                        foreach (var item in Gallery.Items.Cast<IconFrameItem>())
                            nico.Add(item.Bitmap, item.Size, item.KeepAspectRatio);

                        nico.Save(SFD.FileName);
                    }
                }
                finally { }
            }
        }

        void Sizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (Gallery.SelectedIndex != -1)
                {
                    int Size = int.Parse(((ComboBoxItem)e.AddedItems[0]).Tag.ToString());
                    SelectedItem.Size = Size;
                    Preview.Height = Preview.Width = Size;
                }
            }
            catch { }
        }

        void KeepAR_Checked(object sender, RoutedEventArgs e) { SelectedItem.KeepAspectRatio = true; }

        void KeepAR_Unchecked(object sender, RoutedEventArgs e) { SelectedItem.KeepAspectRatio = false; }

        void Add(string FilePath, int Size, bool Dispose = false)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Gallery.Items.Add(new IconFrameItem(FilePath, Size, Dispose));
                if (Gallery.Items.Count == 1) Gallery.SelectedIndex = 0;
            }));
        }

        public IconFrameItem SelectedItem { get { return (IconFrameItem)Gallery.SelectedItem; } }

        void MakeCursor(object sender, RoutedEventArgs e)
        {
            var CursorSFD = new SaveFileDialog()
            {
                Title = "Output Path",
                ValidateNames = true,
                AddExtension = true,

                DefaultExt = ".ico",
                Filter = "Cursor|*.cur"
            };

            if (CursorSFD.ShowDialog().Value)
            {
                try
                {
                    var item = SelectedItem;

                    item.Bitmap.SaveCursor(CursorSFD.FileName, item.Size);
                }
                finally { }
            }
        }
    }
}
