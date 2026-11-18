using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class TabSizeControllerUpdateLayoutPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TabSizeController), nameof(TabSizeController.UpdateLayout));
        }

        [PatchPostfix]
        private static void Postfix(TabSizeController __instance)
        {
            TabBar bar = __instance.GetComponent<TabBar>();

            if (bar != null)
            {
                bar.MarkDirty();
            }
        }
    }
}
