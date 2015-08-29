using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ManagedWin32.Api;

namespace ManagedWin32
{
    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct IconDir
    {
        public short Reserved;   // Reserved (must be 0)
        public short Type;       // Resource Type (1 for icons)
        public short Count;      // How many images?
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct IconDirEntry
    {
        public byte Width;          // Width, in pixels, of the image
        public byte Height;         // Height, in pixels, of the image
        public byte ColorCount;     // Number of colors in image (0 if >=8bpp)
        public byte Reserved;       // Reserved ( must be 0)
        public short Planes;         // Color Planes
        public short BitCount;       // Bits per pixel
        public int BytesInRes;     // How many bytes in this resource?
        public int ImageOffset;    // Where in the file is this image?
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
    struct GroupIconDir
    {
        public short Reserved;   // Reserved (must be 0)
        public short Type;       // Resource Type (1 for icons)
        public short Count;      // How many images?

        /// <summary>
        /// Converts the current NIco.GroupIconDir into NIco.IconDir.
        /// </summary>
        /// <returns>NIco.IconDir</returns>
        public IconDir ToIconDir()
        {
            return new IconDir()
            {
                Reserved = Reserved,
                Type = Type,
                Count = Count
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 14)]
    struct GroupIconDirEntry
    {
        public byte Width;          // Width, in pixels, of the image
        public byte Height;         // Height, in pixels, of the image
        public byte ColorCount;     // Number of colors in image (0 if >=8bpp)
        public byte Reserved;       // Reserved ( must be 0)
        public short Planes;         // Color Planes
        public short BitCount;       // Bits per pixel
        public int BytesInRes;     // How many bytes in this resource?
        public short ID;             // the ID

        /// <summary>
        /// Converts the current NIco.GroupIconDirEntry into NIco.IconDirEntry.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <returns>NIco.IconDirEntry</returns>
        public IconDirEntry ToIconDirEntry(int imageOffiset)
        {
            return new IconDirEntry()
            {
                Width = Width,
                Height = Height,
                ColorCount = ColorCount,
                Reserved = Reserved,
                Planes = Planes,
                BitCount = BitCount,
                BytesInRes = BytesInRes,
                ImageOffset = imageOffiset
            };
        }
    }

    enum Win32Error { FileNotFound = 0x2, BadFormat = 0xb }

    class Library : IDisposable
    {
        public IntPtr Handle { get; private set; }

        #region Factory
        Library(IntPtr Handle)
        {
            if (Handle == IntPtr.Zero)
                switch ((Win32Error)Marshal.GetLastWin32Error())
                {
                    case Win32Error.FileNotFound:
                        throw new FileNotFoundException();
                    case Win32Error.BadFormat:
                        throw new ArgumentException("The file is not a valid win32 executable or dll.");
                    default:
                        throw new Exception("Failed to Load the Dll");
                }

            this.Handle = Handle;
        }

        public Library(string Path, LoadLibraryFlags Flags)
            : this(Kernel32.LoadLibraryEx(Path, IntPtr.Zero, Flags)) { }

        public void Dispose()
        {
            Kernel32.FreeLibrary(Handle);
            Handle = IntPtr.Zero;
        }
        #endregion

        #region Resources
        public LibraryResource FindResource(IntPtr ResourceID, ResourceType RType)
        {
            return new LibraryResource(Kernel32.FindResource(Handle, ResourceID, RType), Handle, RType, ResourceID);
        }

        public LibraryResource[] EnumerateResources(ResourceType RType)
        {
            List<LibraryResource> FoundResources = new List<LibraryResource>();

            EnumResNameProc Callback = (h, t, name, l) =>
            {
                FoundResources.Add(FindResource(name, RType));

                return true;
            };

            Kernel32.EnumResourceNames(Handle, RType, Callback, IntPtr.Zero);

            return FoundResources.ToArray();
        }
        #endregion
    }

    public class LibraryResource
    {
        public IntPtr Handle { get; private set; }

        IntPtr LibraryHandle;

        public IntPtr ResourceId { get; private set; }

        public ResourceType ResourceType { get; private set; }

        public LibraryResource(IntPtr Handle, IntPtr LibraryHandle, ResourceType RType, IntPtr ResourceId)
        {
            this.Handle = Handle;
            this.LibraryHandle = LibraryHandle;
            ResourceType = RType;
            this.ResourceId = ResourceId;

            if (Handle == IntPtr.Zero || LibraryHandle == IntPtr.Zero)
                throw new Exception();
        }

        public int Size { get { return (int)Kernel32.SizeofResource(LibraryHandle, Handle); } }

        public byte[] Data
        {
            get
            {
                IntPtr hRes = Kernel32.LoadResource(LibraryHandle, Handle);
                IntPtr LockedRes = Kernel32.LockResource(hRes);

                byte[] buffer = new byte[Size];
                Marshal.Copy(LockedRes, buffer, 0, Size);

                return buffer;
            }
        }
    }
}