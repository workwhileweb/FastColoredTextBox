using System;
using System.Runtime.InteropServices;

namespace FastColoredTextBoxNS
{
    public static class PlatformType
    {
        private const ushort ProcessorArchitectureIntel = 0;
        private const ushort ProcessorArchitectureIa64 = 6;
        private const ushort ProcessorArchitectureAmd64 = 9;
        private const ushort ProcessorArchitectureUnknown = 0xFFFF;

        [DllImport("kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref SystemInfo lpSystemInfo);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(ref SystemInfo lpSystemInfo);

        public static Platform GetOperationSystemPlatform()
        {
            var sysInfo = new SystemInfo();

            // WinXP and older - use GetNativeSystemInfo
            if (Environment.OSVersion.Version.Major > 5 ||
                (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
            {
                GetNativeSystemInfo(ref sysInfo);
            }
            // else use GetSystemInfo
            else
            {
                GetSystemInfo(ref sysInfo);
            }

            switch (sysInfo.wProcessorArchitecture)
            {
                case ProcessorArchitectureIa64:
                case ProcessorArchitectureAmd64:
                    return Platform.X64;

                case ProcessorArchitectureIntel:
                    return Platform.X86;

                default:
                    return Platform.Unknown;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemInfo
        {
            public readonly ushort wProcessorArchitecture;
            public readonly ushort wReserved;
            public readonly uint dwPageSize;
            public readonly IntPtr lpMinimumApplicationAddress;
            public readonly IntPtr lpMaximumApplicationAddress;
            public readonly UIntPtr dwActiveProcessorMask;
            public readonly uint dwNumberOfProcessors;
            public readonly uint dwProcessorType;
            public readonly uint dwAllocationGranularity;
            public readonly ushort wProcessorLevel;
            public readonly ushort wProcessorRevision;
        };
    }

    public enum Platform
    {
        X86,
        X64,
        Unknown
    }
}