using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.Logging;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class TabGroupSelectTabPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TabGroup), nameof(TabGroup.SelectTab));
        }

        [PatchPostfix]
        private static void Postfix(TabGroup __instance, Tab tab)
        {
            try
            {
                TabBar.AfterGroupSelect(__instance, tab);
            }
            catch (Exception ex)
            {
                TabLog.Error("TabGroup.SelectTab postfix failed", ex);
            }
        }
    }
}
