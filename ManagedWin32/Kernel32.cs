using System;
using System.Runtime.InteropServices;

namespace ManagedWin32.Api
{
    public delegate bool EnumResNameProc(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, IntPtr lParam);

    public enum ResourceType { Icon = 3, GroupIcon = 14 }

    public enum LoadLibraryFlags { LoadAsDataFile = 0x00000002 }

    public static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibraryEx(string path, IntPtr hFile, LoadLibraryFlags flags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr FindResource(IntPtr hModule, IntPtr resourceID, ResourceType type);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceNames(IntPtr hModule, ResourceType pType, EnumResNameProc callback, IntPtr param);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
    }
}
