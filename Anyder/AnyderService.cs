using System;
using Anyder.Interop;
using Anyder.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Anyder;

public class AnyderService
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    public static BgObjectFunctions BgObjectFunctions { get; set; } = null!;
    public static VfxFunctions VfxFunctions { get; set; } = null!;
    public static SharedGroupLayoutFunctions SharedGroupLayoutFunctions { get; private set; } = null!;

    public static ObjectManager ObjectManager { get; private set; } = null!;

    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        PluginInterface.Create<AnyderService>();

        BgObjectFunctions = new BgObjectFunctions();
        VfxFunctions = new VfxFunctions();
        SharedGroupLayoutFunctions = new SharedGroupLayoutFunctions();
        
        ObjectManager = new ObjectManager();
    }

    public static void Dispose()
    {
        ObjectManager.Dispose();
        VfxFunctions.Dispose();
    }
}
