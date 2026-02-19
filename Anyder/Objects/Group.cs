using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Anyder.Interop;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using InteropGenerator.Runtime;

namespace Anyder.Objects;

public unsafe class Group : IDisposable
{
    public SharedGroupLayoutInstance* Data;
    public string Path;
    
    public Transform Transform;
    public bool Collide;
    public byte Alpha = 0;
    public Vector3 Stain = Vector3.Zero;
    
    public bool IsHovered = false;
    public byte HighlightColor = 70;

    public Group(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = true)
    {
        Data = IMemorySpace.GetDefaultSpace()->Malloc<SharedGroupLayoutInstance>();
        Path = path;
        
        AnyderService.SharedGroupLayoutFunctions.Ctor(Data);
        AnyderService.Log.Verbose($"Attempting to create group {path} @ {((IntPtr)Data):x8}");
        Path = path;

        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };

        Transform.OnUpdate += UpdateTransform;
        
        Collide = collide;
        
        AnyderService.Framework.RunOnTick(SetModel);
    }

    private void SetModel()
    {
        // var layers = LayoutWorld.Instance()->ActiveLayout->Layers;
        // if (layers.Count == 0) return;
        // var manager = layers.First().Value.Value;
        //
        // var first = (IntPtr)Data->Instances.Instances.First;
        // var last = (IntPtr)Data->Instances.Instances.Last;
        //
        // if (first != last)
        // {
        //     AnyderService.SharedGroupLayoutFunctions.FixGroupChildren(Data);
        // }
        //
        // var bytes = Encoding.UTF8.GetBytes(Path + "\0");
        // fixed (byte* ptr = bytes)
        // {
        //     Data->Init(manager, ptr);
        // }
        //
        // AnyderService.SharedGroupLayoutFunctions.AssignResource(Data, Path);
        AnyderService.SharedGroupLayoutFunctions.LoadSgb(Data, Path);
        
        UpdateTransform();

        var first = (IntPtr)Data->Instances.Instances.First;
        var last = (IntPtr)Data->Instances.Instances.Last;
        
        if (first != last)
        {
            AnyderService.SharedGroupLayoutFunctions.FixGroupChildren(Data);
        }
    }

    private void UpdateTransform()
    {
        var t = Data->GetTransformImpl();
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

    public void SetAlpha(byte alpha)
    {
        Alpha = alpha;
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            var graphics = (BgObjectEx*)instance->GetGraphics();
            if (graphics == null) continue;
            graphics->Alpha = alpha;
        }
    }

    public void ApplyStain(Vector3 color)
    {
        Stain = color;
        
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);

        var byteColor = new ByteColor
        {
            A = 255, R = r, G = g, B = b
        };
        
        Data->ApplyStain(&byteColor);
    }

    public byte TryGetStain()
    {
        var stain = Data->StainInfo;
        return stain == null ? (byte)0 : stain->DefaultStainIndex;
    }

    public SharedGroupStainInfo* GetStainInfo()
    {
        return Data->StainInfo;
    }

    public void SetHighlight(byte color)
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
