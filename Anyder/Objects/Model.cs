using System;
using Anyder.Interop;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Anyder.Objects;

public unsafe class Model : IDisposable
{
    public readonly BgObject* BgObject;
    public string Path;
    public Transform Transform;
    public bool Dirty = true;

    public Model(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        AnyderService.Log.Verbose($"Creating BgObject {path}");
        if (AnyderService.BgObjectFunctions == null) throw new NullReferenceException("BgObject functions are not initialized");
        
        Path = path;
        BgObject = AnyderService.BgObjectFunctions.BgObjectCreate(path);
        
        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };
        
        Transform.OnUpdate += UpdateTransform; 
        UpdateTransform();

        if (BgObject->ModelResourceHandle->LoadState == 7)
        {
            var ex = (BgObjectEx*)BgObject;
            ex->UpdateCulling();
            Dirty = false;
        }
    }

    public void SetAlpha(byte alpha)
    {
        var ex = (BgObjectEx*)BgObject;
        ex->Alpha = alpha;
        UpdateRender();
    }

    public void SetHighlightColor(byte color)
    {
        var ex = (BgObjectEx*)BgObject;
        ex->HighlightFlags = color;
        UpdateRender();
    }

    private void UpdateTransform()
    { 
        var ex = (BgObjectEx*)BgObject;
        BgObject->Position = Transform.Position;
        BgObject->Rotation = Transform.Rotation;
        BgObject->Scale = Transform.Scale;
        TryFixCulling();
    }

    public void UpdateRender()
    {
        AnyderService.Log.Verbose($"Updating BgObject {Path}");
        var ex = (BgObjectEx*)BgObject;
        ex->UpdateRender();
    }

    public void TryFixCulling()
    {
        AnyderService.Log.Verbose($"Trying to fix BgObject culling {Path}");
        if (BgObject == null) return;
        
        if (BgObject->ModelResourceHandle->LoadState == 7)
        {
            var ex = (BgObjectEx*)BgObject;
            ex->UpdateCulling();
        }
    }

    public void Dispose()
    {
        Dirty = false;
        
        AnyderService.Log.Verbose($"Disposing BgObject {Path}");
        AnyderService.Framework.RunOnFrameworkThread(() =>
        {
            if (BgObject == null) return;
        
            var ex = (BgObjectEx*) BgObject;
            ex->CleanupRender();
            ex->Dtor();
        });
        
        Transform.OnUpdate -= UpdateTransform;
    }
}
