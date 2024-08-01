using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AetharNet.Mods.ZumbiBlocks2.AutoEquip;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class AutoEquip : BaseUnityPlugin
{
    public const string PluginGUID = "AetharNet.Mods.ZumbiBlocks2.AutoEquip";
    public const string PluginAuthor = "wowi";
    public const string PluginName = "AutoEquip";
    public const string PluginVersion = "0.2.0";

    internal new static ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);
    }
}
