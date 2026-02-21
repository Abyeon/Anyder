using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Anyder.Objects.Vfx;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace Anyder.Objects;

/// <summary>
/// Manager for handling spawning and deleting multiple types of objects.
/// </summary>
public class ObjectManager : IDisposable
{
    public List<SpawnedObject> Objects = [];
    
    private readonly IClientState clientState;
    private readonly IFramework framework;

    public ObjectManager()
    {
        clientState = AnyderService.ClientState;
        clientState.ZoneInit += ClientStateOnZoneInit;
        clientState.Logout += ClientStateOnLogout;
        
        framework = AnyderService.Framework;
        framework.Update += FrameworkOnUpdate;
    }

    private void FrameworkOnUpdate(IFramework arg)
    {
        for (var i = Objects.Count - 1; i >= 0; i--)
        {
            var obj = Objects[i];

            switch (obj)
            {
                case { Type: ObjectType.Model, Model.Dirty: true }:
                    obj.Model.TryFixCulling();
                    break;
                case { Type: ObjectType.StaticVfx or ObjectType.ActorVfx, Vfx: not null }:
                {
                    var item = obj.Vfx;
                    if (!item.Loop && DateTime.UtcNow >= item.Expires)
                    {
                        obj.Dispose(); 
                        Objects.RemoveAt(i);
                    }
                    else
                    {
                        item.CheckForRefresh();
                    }

                    break;
                }
            }
        }
    }

    private void ClientStateOnLogout(int type, int code) => Clear();
    private void ClientStateOnZoneInit(ZoneInitEventArgs obj) => Clear();

    /// <summary>
    /// Takes a path and determines what type of object to spawn, returning the wrapped object.
    /// </summary>
    public SpawnedObject Add(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = false)
    {
        var newObj = new SpawnedObject(path, position, rotation, scale, collide);
        if (!newObj.IsValid) throw new ArgumentException("Object is not valid!");
        
        Objects.Add(newObj);
        return newObj;
    }

    /// <summary>
    /// Takes a path and determines what type of object to spawn, returning the wrapped object.
    /// </summary>
    public SpawnedObject Add(string path, IGameObject target, bool collide = false, int seconds = 5, bool loop = false)
    {
        var newObj = new SpawnedObject(path, target, collide, seconds, loop);
        if (!newObj.IsValid) throw new ArgumentException("Object is not valid!");
        
        Objects.Add(newObj);
        return newObj;
    }
    
    public void Add(Model model) => Objects.Add(new SpawnedObject(model));
    public void Add(Group group) => Objects.Add(new SpawnedObject(group));
    public void Add(BaseVfx vfx) => Objects.Add(new SpawnedObject(vfx));

    /// <summary>
    /// Clears all currently tracked objects.
    /// </summary>
    public void Clear()
    {
        foreach (var obj in Objects)
        {
            if (obj.Type is ObjectType.StaticVfx or ObjectType.ActorVfx && obj.Vfx != null)
            {
                obj.Vfx.Loop = false; // Disable loop so it doesn't get re-enabled after disposing
            }
            obj.Dispose();
        }
        Objects.Clear();
    }

    internal unsafe void InteropRemoved(IntPtr pointer)
    {
        for (var i = 0; i < Objects.Count; i++)
        {
            var obj = Objects[i];
            if (obj is not { Type: ObjectType.StaticVfx or ObjectType.ActorVfx, Vfx: not null }) continue;
            if ((IntPtr)obj.Vfx.Vfx != pointer) continue;
            
            if (obj.Vfx.Loop)
            {
                obj.Vfx.Refresh();
            }
            else
            {
                Objects.RemoveAt(i);
            }
            break;
        }
    }

    public void Dispose() 
    {
        framework.Update -= FrameworkOnUpdate;
        clientState.ZoneInit -= ClientStateOnZoneInit;
        clientState.Logout -= ClientStateOnLogout;
        
        Clear();
    }
}
