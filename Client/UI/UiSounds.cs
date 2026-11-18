using Comfort.Common;
using EFT.UI;

namespace ImprovedTabUI.UI
{
    internal static class UiSounds
    {
        internal static void Play(EUISoundType sound)
        {
            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlayUISound(sound);
            }
        }
    }
}
