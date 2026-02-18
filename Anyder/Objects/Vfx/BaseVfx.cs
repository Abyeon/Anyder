using System;
using Anyder.Interop;

namespace Anyder.Objects.Vfx;

public abstract unsafe class BaseVfx : IDisposable
{
    public string Path = "";
    public DateTime Expires;
    public bool Loop = false;
    
    public VfxStruct* Vfx;
    public bool IsValid => Vfx != null && (IntPtr)Vfx != IntPtr.Zero;

    public abstract void Refresh();
    protected abstract void Remove();

    public void CheckForRefresh()
    {
        if (DateTime.Now >= Expires && Loop)
        {
            AnyderService.Log.Verbose($"Refreshing Vfx {Path}");
            if (IsValid) Remove();
            Refresh();
        }
    }
    
    public void Dispose()
    {
        AnyderService.Log.Verbose($"Disposing Vfx {Path}");
        
        try
        {
            if (AnyderService.VfxFunctions == null) throw new NullReferenceException("Vfx functions are not initialized");
            if (IsValid) Remove();
            Vfx = null;
        }
        catch (Exception e)
        {
            AnyderService.Log.Error(e, $"Error while trying to dispose {Path}");
        }
        
        GC.SuppressFinalize(this);
    }
}
