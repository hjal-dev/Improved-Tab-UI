using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.Logging;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class InventoryScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Show),
                new Type[] { typeof(InventoryScreen.InventoryScreenController) });
        }

        [PatchPostfix]
        private static void Postfix(InventoryScreen __instance)
        {
            try
            {
                TabBar.Attach(__instance);
            }
            catch (Exception ex)
            {
                TabLog.Error("InventoryScreen.Show postfix failed", ex);
            }
        }
    }
}
