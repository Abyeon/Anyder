using System;
using System.Numerics;
using Anyder.Interop;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace Anyder.Objects;

public unsafe class Group : IDisposable
{
    public SharedGroupLayoutInstance* Data;
    public string Path;
    
    public Transform Transform;
    public bool Collide;

    public Vector4? Color
    {
        get;
        set
        {
            field = value;
            SetColor();
        }
    }

    public Group(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = true, Vector4? color = null)
    {
        Data = IMemorySpace.GetDefaultSpace()->Malloc<SharedGroupLayoutInstance>();
        
        AnyderService.Log.Verbose($"Attempting to create group {path} @ {((IntPtr)Data):x8}");

        try
        {
            AnyderService.SharedGroupLayoutFunctions.Ctor(Data);
            AnyderService.SharedGroupLayoutFunctions.Init(Data, path);
        }
        catch (Exception e)
        {
            AnyderService.Log.Error($"Failed to create group {path}: {e}");
        }
        
        
        Path = path;

        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };

        Transform.OnUpdate += UpdateTransform;
        
        Collide = collide;
        Color = color;
        
        UpdateTransform();
        
        AnyderService.Framework.RunOnTick(SetColor);
    }

    private void UpdateTransform()
    {
        var t = Data->GetTransformImpl();
        if (t == null) return;
        
        t->Translation = Transform.Position;
        t->Rotation = Transform.Rotation;
        t->Scale = Transform.Scale;

        Data->SetTransformImpl(t);
        Data->SetColliderActive(Collide);
        
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            var graphics = (BgObjectEx*)instance->GetGraphics();
            if (graphics == null) continue;
            graphics->UpdateCulling();
        }
    }
    
    public void SetColor()
    {
        if (Data->StainInfo == null) return;
        if (!TrySetColorInternal())
        {
            AnyderService.Framework.Update += ApplyStainTask;
        }
    }

    private bool TrySetColorInternal()
    {
        if (!AnyderService.SharedGroupLayoutFunctions.ReadyToStain(Data)) return false;
        
        ByteColor color;
        if (!Color.HasValue)
        {
            color = *SharedGroupLayoutInstance.GetObjectStainColorByIndex(Data->StainInfo->DefaultStainIndex);
        }
        else
        {
            color = Color.Value.ToByteColor();
        }
        
        Data->ApplyStain(&color);
        Data->ReapplyStain();
        return true;
    }

    private void ApplyStainTask(IFramework framework)
    {
        if (Data == null)
        {
            AnyderService.Framework.Update -= ApplyStainTask;
        }
        
        AnyderService.Log.Verbose($"Applying stain {Path}");
        
        if (TrySetColorInternal())
        {
            AnyderService.Framework.Update -= ApplyStainTask;
        }
    }

    public void SetWallpaper(ushort id)
    {
        if (Data->StainInfo == null) return;

        AnyderService.SharedGroupLayoutFunctions.SetProperty(Data, id);
    }

    public void SetCollision(bool enabled)
    {
        Collide = enabled;
        Data->SetColliderActive(Collide);
    }

    public void SetAlpha(byte alpha)
    {
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            var graphics = (BgObjectEx*)instance->GetGraphics();
            if (graphics == null) continue;
            graphics->Alpha = alpha;
        }
    }

    public void SetHighlightColor(byte color)
    {
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            if (instance->Id.Type != InstanceType.BgPart) continue;
            
            var graphics = (BgObjectEx*)instance->GetGraphics();
            var transform = instance->GetTransformImpl();
            if (graphics != null)
            {
                graphics->HighlightFlags = color;
            }
            instance->SetGraphics((FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object*)graphics, transform);
        }
    }
    
    public void Dispose()
    {
        AnyderService.Log.Verbose($"Disposing group {Path}");
        AnyderService.Framework.Update -= ApplyStainTask;
        
        if (Data == null) return;
        
        Data->Deinit();
        Data->Dtor(0);
        
        IMemorySpace.Free(Data);
        
        Data = null;
    }
}
