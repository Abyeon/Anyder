using System;
using System.Linq;
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
            if (HasStains()) SetColor(value);
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
        
        AnyderService.Log.Debug($"Has stains? {HasStains()}");
        AnyderService.Framework.RunOnTick(() => FrameworkQueue.Enqueue(ApplyStainTask), delayTicks: 1);
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

    public bool HasStains()
    {
        string[] housing = ["/hou/", "/ind", "/mji/"];
        return housing.Any(Path.Contains) || (Path.Contains("/pvp_xx/") && Path.Contains("/cst/"));
    }

    public void SetColor(Vector4? color)
    {
        ByteColor byteColor;
        if (!color.HasValue)
        {
            byteColor = *SharedGroupLayoutInstance.GetObjectStainColorByIndex(Data->StainInfo->DefaultStainIndex);
        }
        else
        {
            byteColor = color.Value.ToByteColor();
        }
        
        Data->ApplyStain(&byteColor);
        Data->ReapplyStain();
    }

    private bool TrySetColor(Vector4? color)
    {
        var layout = (LayoutEx*)Data->Layout;
        if (!AnyderService.SharedGroupLayoutFunctions.ReadyToStain(Data)) return false;
        
        SetColor(color);
        
        return layout->ObjectNeedsUpdating != 1;
    }

    private void ApplyStainTask()
    {
        AnyderService.Log.Verbose($"Applying stain {Color} to {Path}");
        if (Data == null || TrySetColor(Color))
        {
            AnyderService.Log.Verbose($"Successfully applied stain!");
            return;
        }
        
        AnyderService.Log.Verbose($"Failed to apply stain, attempting again..");
        
        FrameworkQueue.Enqueue(ApplyStainTask);
    }

    public void SetWallpaper(ushort id)
    {
        if (Data->StainInfo == null) return;

        AnyderService.SharedGroupLayoutFunctions.ApplyProperty(Data,0, id);
        // AnyderService.SharedGroupLayoutFunctions.UpdateStain(Data);
        // var stainEx = (StainInfoEx*)Data->StainInfo;
        // stainEx->Properties[0] = (ushort)(id | 0x8000);
        // Data->StainInfo->Flags &= SharedGroupStainFlags.StainModified;
        ((LayoutEx*)Data->Layout)->ObjectNeedsUpdating = 1;
        // Data->ReapplyStain();
        AnyderService.SharedGroupLayoutFunctions.UpdateRender(Data);
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
        
        if (Data == null) return;
        
        Data->Deinit();
        Data->Dtor(0);
        
        IMemorySpace.Free(Data);
        
        Data = null;
    }
}
