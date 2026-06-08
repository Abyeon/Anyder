using System.Runtime.InteropServices;

namespace Anyder.Interop;

[StructLayout(LayoutKind.Explicit, Size = 0xC60)]
public struct LayoutEx
{
    [FieldOffset(0x106)] public short StainNeedsUpdating;
}