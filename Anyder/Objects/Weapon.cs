using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSWeapon = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Weapon;

namespace Anyder.Objects;

public unsafe class Weapon : IDisposable
{
    public CSWeapon* Data;
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}
