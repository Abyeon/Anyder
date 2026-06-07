using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Anyder.Interop;

[StructLayout(LayoutKind.Explicit, Size = 0xE0)]
public struct StainInfoEx
{
    [FieldOffset(0xC0)] public ByteColor Color;
}
