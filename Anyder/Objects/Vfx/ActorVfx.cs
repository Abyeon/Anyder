using System;
using Dalamud.Game.ClientState.Objects.Types;

namespace Anyder.Objects.Vfx;

/// <summary>
/// Rewrite of Picto's GameObjectVFX class
/// </summary>
public unsafe class ActorVfx : BaseVfx
{
    public IGameObject Target;
    public IGameObject Source;
    
    public ActorVfx(string path, IGameObject target, IGameObject source, TimeSpan? expiration = null, bool loop = false)
    {
        AnyderService.Log.Verbose($"Creating ActorVfx {path}");
        if (AnyderService.VfxFunctions == null) throw new NullReferenceException("Vfx functions are not initialized");
        
        Path = path;
        Target = target;
        Source = source;
        Loop = loop;
        Expires = expiration.HasValue ? DateTime.UtcNow + expiration.Value : DateTime.UtcNow + TimeSpan.FromSeconds(5);

        try
        {
            Vfx = AnyderService.VfxFunctions.ActorVfxCreate(Path, Source.Address, Target.Address);
        }
        catch (Exception e)
        {
            AnyderService.Log.Error(e, "Failed to create Vfx");
        }
    }

    public override void Refresh()
    {
        // if (IsValid) Plugin.VfxFunctions.ActorVfxRemove(Vfx);
        Vfx = AnyderService.VfxFunctions.ActorVfxCreate(Path, Source.Address, Target.Address);
    }

    protected override void Remove()
    {
        AnyderService.VfxFunctions.ActorVfxRemove(Vfx);
    }
}
