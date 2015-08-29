using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using ManagedWin32;
using ManagedWin32.Api;

namespace NIco
{
    public static class PEFormat
    {
        public static Ico Extract(string FileName, int Index)
        {
            using (var Module = new Library(Environment.ExpandEnvironmentVariables(FileName), LoadLibraryFlags.LoadAsDataFile))
            {
                var Resources = Module.EnumerateResources(ResourceType.GroupIcon);

                if (Index >= Resources.Length) throw new IndexOutOfRangeException();

                byte[] ResourceData = Resources[Index].Data;

                //Convert the resouce into an .ico file image.
                using (MemoryStream inputStream = new MemoryStream(ResourceData))
                {
                    //Read the GroupIconDir header.
                    GroupIconDir grpDir = inputStream.Read<GroupIconDir>();

                    int SizeOfIconDir = Marshal.SizeOf(typeof(IconDir)),
                        SizeOfIconDirEntry = Marshal.SizeOf(typeof(IconDirEntry));

                    int numEntries = grpDir.Count;

                    var IconDir = grpDir.ToIconDir();
                    IconDir.Count = 1;

                    int Offset = SizeOfIconDir + SizeOfIconDirEntry;

                    var Icons = new Ico();

                    for (int i = 0; i < numEntries; i++)
                    {
                        using (MemoryStream destStream = new MemoryStream())
                        {
                            //Write the IconDir header.
                            IconDir.Write(destStream);

                            //Read the GroupIconDirEntry.
                            GroupIconDirEntry grpEntry = inputStream.Read<GroupIconDirEntry>();

                            //Write the IconDirEntry.
                            destStream.Seek(SizeOfIconDir, SeekOrigin.Begin);
                            grpEntry.ToIconDirEntry(Offset).Write(destStream);

                            //Get the icon image raw data and write it to the stream.
                            byte[] imgBuf = Module.FindResource((IntPtr)(int)grpEntry.ID, ResourceType.Icon).Data;
                            destStream.Seek(Offset, SeekOrigin.Begin);
                            destStream.Write(imgBuf, 0, imgBuf.Length);

                            destStream.Seek(0, SeekOrigin.Begin);

                            Icons.Add(new Icon(destStream).ToBitmap(), grpEntry.Width);
                        }
                    }

                    return Icons;
                }
            }
        }
    }
}