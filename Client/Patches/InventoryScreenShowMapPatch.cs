using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class InventoryScreenShowMapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.ShowMap));
        }

        [PatchPrefix]
        private static void Prefix(InventoryScreen __instance)
        {
            TabBar.SetShowingMap(__instance, true);
        }

        [PatchPostfix]
        private static void Postfix(InventoryScreen __instance)
        {
            TabBar.SetShowingMap(__instance, false);
        }
    }
}
