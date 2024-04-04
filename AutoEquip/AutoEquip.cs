using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace AetharNet.Mods.ZumbiBlocks2.AutoEquip;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class AutoEquip : BaseUnityPlugin
{
    public const string PluginGUID = "AetharNet.Mods.ZumbiBlocks2.AutoEquip";
    public const string PluginAuthor = "awoi";
    public const string PluginName = "AutoEquip";
    public const string PluginVersion = "0.1.0";

    private void Awake()
    {
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }
}
