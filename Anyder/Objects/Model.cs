using System;
using System.Numerics;
using System.Text;
using Anyder.Interop;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Anyder.Objects;

public unsafe class Model : IDisposable
{
    public readonly BgObject* Data;
    public string Path;
    public Transform Transform;
    public bool Dirty = true;
    
    public Vector4 Color
    {
        get;
        set
        {
            field = value;
            var color = value.ToByteColor();
            Data->TrySetStainColor(color);
        }
    }

    public Model(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        AnyderService.Log.Verbose($"Creating BgObject {path}");
        Path = path;
        Data = BgObject.Create(path, "Anyder.BgObject");
        
        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };
        
        Transform.OnUpdate += UpdateTransform; 
        UpdateTransform();

        if (Data != null && Data->ModelResourceHandle->LoadState == 7)
        {
            Data->UpdateCulling();
            Dirty = false;
        }
    }

    public void SetAlpha(byte alpha)
    {
        var ex = (BgObjectEx*)Data;
        ex->Alpha = alpha;
        UpdateRender();
    }

    public void SetHighlightColor(byte color)
    {
        var ex = (BgObjectEx*)Data;
        ex->HighlightFlags = color;
        UpdateRender();
    }

    private void UpdateTransform()
    { 
        Data->Position = Transform.Position;
        Data->Rotation = Transform.Rotation;
        Data->Scale = Transform.Scale;
        TryFixCulling();
    }

    public void UpdateRender()
    {
        if (Data == null) return;
        AnyderService.Log.Verbose($"Updating BgObject {Path}");
        Data->UpdateRender();
    }

    public void TryFixCulling()
    {
        AnyderService.Log.Verbose($"Trying to fix BgObject culling {Path}");
        if (Data == null) return;
        
        if (Data->ModelResourceHandle->LoadState == 7)
        {
            Data->UpdateRender();
            Dirty = false;
        }
    }

    public void Dispose()
    {
        Dirty = false;
        
        AnyderService.Log.Verbose($"Disposing BgObject {Path}");
        AnyderService.Framework.RunOnFrameworkThread(() =>
        {
            if (Data == null) return;
        
            var ex = (BgObjectEx*) Data;
            ex->CleanupRender();
            ex->Dtor();
        });
        
        Transform.OnUpdate -= UpdateTransform;
    }
}
