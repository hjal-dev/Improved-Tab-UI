using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace ImprovedTabUI.Patches
{
    internal sealed class ForeignTabPatch : ModulePatch
    {
        private readonly string _method;

        internal ForeignTabPatch(string method)
        {
            _method = method;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_method);
        }

        [PatchPrefix]
        private static bool Prefix()
        {
            return false;
        }
    }
}
