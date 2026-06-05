using System;
using System.Linq;
using System.Text;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;

namespace Anyder.Interop;

public unsafe class SharedGroupLayoutFunctions
{
    internal delegate void FixGroupChildrenDelegate(SharedGroupLayoutInstance* self);
    internal delegate void AssignResourceHandlerDelegate(SharedGroupLayoutInstance* self, byte* pathBytes);
    internal delegate sbyte InitSgbDelegate(SharedGroupLayoutInstance* self, nint* initArgs, byte* pathBytes, byte a4);
    internal delegate LayerManager* TryGetLayerDelegate(LayoutManager* layout, ushort key);
    internal delegate IntPtr UnknownLayerFunctionDelegate(LayerManager* creator, SharedGroupLayoutInstance* self);
    internal delegate SharedGroupLayoutInstance* CtorDelegate(SharedGroupLayoutInstance* self);
    internal delegate byte LoadSgbDelegate(SharedGroupLayoutInstance* self, byte* pathBytes);
    internal delegate LayerManager* GetPreferredLayerManagerDelegate(LayoutManager* layoutManager);
    internal delegate IntPtr LayerIDGenDelegate(Pointer<ResourceHandle>* layerGroupResourceHandle);  // see ffxiv_dx11.exe+7110E0
    internal delegate LayerManager* LayerCtorDelegate(LayerManager* self, LayoutManager* owner);
    
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 33 F6 C7 41 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 71 ?? ?? ?? ?? 48 8B F9 48 89 71 ?? 89 71 ?? 48 89 71 ?? C7 41 ?? ?? ?? ?? ?? 48 83 C1 ?? E8 ?? ?? ?? ?? 48 89 77")]
    internal CtorDelegate? CtorInternal = null;
    
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 81 ?? ?? ?? ?? 48 8D 79")]
    internal LoadSgbDelegate? LoadSgbInternal = null;
    
    [Signature("BA ?? ?? ?? ?? E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 4C 8B 81")]
    internal GetPreferredLayerManagerDelegate? GetPreferredLayerManagerInternal = null;

    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 48 83 B9 ?? ?? ?? ?? 00 48 8B DA 48 8B F9 0F 85")]
    internal AssignResourceHandlerDelegate? AssignResourceInternal = null;
    
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 48 8B B1 ?? ?? ?? ?? 45 0F B6 F9")]
    internal InitSgbDelegate? InitSgbInternal = null;
    
    [Signature("4C 8B 81 ?? ?? ?? ?? 4C 8B D1 44 0F B7 CA")]
    internal TryGetLayerDelegate? TryGetLayerInternal = null;
    
    [Signature("40 55 57 41 55 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B F9")]
    internal FixGroupChildrenDelegate? FixGroupChildrenInternal = null;
    
    [Signature("40 53 48 83 EC ?? 48 8B D9 48 89 51 ?? C7 41 ?? ?? ?? ?? ?? 48 8D 05")]
    internal LayerCtorDelegate? LayerCtorInternal = null;
    
    [Signature("48 83 EC ?? ?? ?? ?? FF 90 ?? ?? ?? ?? 45 33 C0 48 89 5C 24")]
    internal LayerIDGenDelegate? LayerIdGenInternal = null;

    [Signature("48 89 5C 24 ?? 57 48 83 EC ?? 4C 8B 41 ?? 48 8B F9 44 8B 4A")]
    internal UnknownLayerFunctionDelegate? UnknownLayerFunctionInternal = null;
    
    public SharedGroupLayoutFunctions()
    {
        AnyderService.GameInteropProvider.InitializeFromAttributes(this);
    }

    public SharedGroupLayoutInstance* Ctor(SharedGroupLayoutInstance* self)
    {
        if (CtorInternal == null)
            throw new InvalidOperationException("Ctor sig was not found!");
        
        return CtorInternal(self);
    }

    public byte LoadSgb(SharedGroupLayoutInstance* self, string path)
    {
        if (LoadSgbInternal == null)
            throw new InvalidOperationException("LoadSgb sig was not found!");

        var bytes = Encoding.UTF8.GetBytes(path + "\0");
        fixed (byte* pathPtr = bytes)
        { 
            return LoadSgbInternal(self, pathPtr);
        }
    }

    public void FixGroupChildren(SharedGroupLayoutInstance* self)
    {
        if (FixGroupChildrenInternal == null)
            throw new InvalidOperationException("FixGroup sig was not found!");
        
        FixGroupChildrenInternal(self);
    }

    public void AssignResource(SharedGroupLayoutInstance* self, string path)
    {
        if (AssignResourceInternal == null)
            throw new InvalidOperationException("AssignResource sig was not found!");
        
        var bytes = Encoding.UTF8.GetBytes(path + "\0");
        fixed (byte* pathPtr = bytes)
        { 
            AssignResourceInternal(self, pathPtr);
        }
    }

    public sbyte Init(SharedGroupLayoutInstance* self, string path)
    {
        if (InitSgbInternal == null)
            throw new InvalidOperationException("CreateSgb sig was not found!");

        var bytes = Encoding.UTF8.GetBytes(path + "\0");

        LayerManager* layer;
        
        if (!LayoutWorld.Instance()->GlobalLayout->Layers.ContainsKey(6969))
        {
            var manager = IMemorySpace.GetDefaultSpace()->Malloc<LayerManager>();
            layer = LayerCtor(manager, LayoutWorld.Instance()->GlobalLayout);
            layer->Id = 6969;
            layer->Initialize();
            layer = manager;
        }
        else
        {
            layer = LayoutWorld.Instance()->GlobalLayout->Layers[6969];
        }
        
        fixed (byte* pathPtr = bytes)
        {
            var initArgs = stackalloc nint[2];
            initArgs[0] = (nint)layer;
            initArgs[1] = 0;
            
            var result = InitSgbInternal(self, initArgs, pathPtr, 1);
            return result;
        }
    }

    public LayerManager* TryGetLayer(LayoutManager* layout, ushort key)
    {
        if (TryGetLayerInternal == null)
            throw new InvalidOperationException("TryGetLayer sig was not found!");
        
        return TryGetLayerInternal(layout, key);
    }

    public IntPtr UnknownLayerFunction(LayerManager* creator, SharedGroupLayoutInstance* self)
    {
        if (UnknownLayerFunctionInternal == null)
            throw new InvalidOperationException("FixSgb sig was not found!");
        
        return UnknownLayerFunctionInternal(creator, self);
    }

    public LayerManager* GetPreferredLayerManager(LayoutManager* self)
    {
        if (GetPreferredLayerManagerInternal == null)
            throw new InvalidOperationException("GetPreferredLayerManager sig was not found!");
        
        return GetPreferredLayerManagerInternal(self);
    }
    
    public LayerManager* LayerCtor(LayerManager* self, LayoutManager* owner)
    {
        if (LayerCtorInternal == null)
            throw new InvalidOperationException("LayerCtorInternal sig was not found!");
        
        return LayerCtorInternal(self , owner);
    }
}
