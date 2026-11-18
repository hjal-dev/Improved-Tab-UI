using System.Reflection;
using SPT.Reflection.Patching;
using ImprovedTabUI.UI;

namespace ImprovedTabUI.Patches
{
    internal sealed class CursorVisibilityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return RadialMenu.CursorHandler;
        }

        [PatchPrefix]
        private static bool Prefix(bool isCursorVisible)
        {
            return !RadialMenu.HoldCursor(isCursorVisible);
        }
    }
}
