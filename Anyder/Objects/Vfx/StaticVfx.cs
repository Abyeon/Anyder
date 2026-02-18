using System;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Anyder.Objects.Vfx;

/// <summary>
/// VFX that is not attached to an actor.
/// </summary>
public unsafe class StaticVfx : BaseVfx
{
    public Transform Transform;

    public StaticVfx(string path, Vector3 position, Quaternion rotation, Vector3 scale, TimeSpan? expiration = null, bool loop = false)
    {
        AnyderService.Log.Verbose($"Creating StaticVfx {path}");
        if (AnyderService.VfxFunctions == null) throw new NullReferenceException("Vfx functions are not initialized");

        Path = path;
        Transform = new Transform()
        {
            Position = position,
            Rotation = rotation,
            Scale = scale
        };
        
        Transform.OnUpdate += UpdateTransform;
        
        Loop = loop;
        Expires = expiration.HasValue ? DateTime.UtcNow + expiration.Value : DateTime.UtcNow + TimeSpan.FromSeconds(5);
        
        try
        {
            Vfx = AnyderService.VfxFunctions.StaticVfxCreate(Path);
            AnyderService.VfxFunctions.StaticVfxRun(Vfx);
                
            if (!IsValid)
                throw new Exception("Vfx pointer is null");
                
            UpdateTransform();
        }
        catch (Exception e)
        {
            AnyderService.Log.Error(e, "Failed to create Vfx");
        }
    }

    private void UpdateTransform()
    {
        Vfx->Position = Transform.Position;
        Vfx->Scale = Transform.Scale;
        Vfx->Rotation = Transform.Rotation;
        Vfx->Flags |= 0x2;
    }

    public StaticVfx(string path, Vector3 position, Vector3 scale, float rotation, TimeSpan? expiration = null, bool loop = false)
        : this(path, position, Quaternion.CreateFromYawPitchRoll(rotation, 0f, 0f), scale, expiration, loop)
    { }

    public override void Refresh()
    {
        try
        {
            // if (IsValid) Plugin.VfxFunctions.StaticVfxRemove(Vfx);
            Vfx = AnyderService.VfxFunctions.StaticVfxCreate(Path);
            AnyderService.VfxFunctions.StaticVfxRun(Vfx);
                
            if (!IsValid)
                throw new Exception("Vfx pointer is null");
                
            UpdateTransform();
        }
        catch (Exception e)
        {
            AnyderService.Log.Error(e, "Failed to create Vfx");
        }
    }

    protected override void Remove()
    {
        AnyderService.VfxFunctions.StaticVfxRemove(Vfx);
    }
}
