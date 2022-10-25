using System.Runtime.InteropServices;

namespace win32;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct LARGE_INTEGER
{
    [FieldOffset(0)] public long QuadPart;
    [FieldOffset(0)] public uint LowPart;
    [FieldOffset(4)] public int HighPart;
}