using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using ImprovedTabUI.Eft;
using ImprovedTabUI.Logging;

namespace ImprovedTabUI.Patches
{
    internal sealed class InputTreeTranslatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InputNodeAbstract), nameof(InputNodeAbstract.TranslateInput));
        }

        [PatchPrefix]
        private static void Prefix(InputNodeAbstract __instance, List<ECommand> commands, ref float[] axes)
        {
            if (!(__instance is InputTree))
            {
                return;
            }

            try
            {
                InventoryOpener.BeforeTranslate(commands, axes);
            }
            catch (Exception ex)
            {
                TabLog.Error("InputTree.TranslateInput prefix failed", ex);
            }
        }
    }
}
