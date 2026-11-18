using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.Logging;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class TabGroupShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TabGroup), nameof(TabGroup.Show));
        }

        [PatchPrefix]
        private static void Prefix(TabGroup __instance, ref Tab tab)
        {
            try
            {
                TabBar.BeforeGroupShow(__instance, ref tab);
            }
            catch (Exception ex)
            {
                TabLog.Error("TabGroup.Show prefix failed", ex);
            }
        }
    }
}
