using System.Collections.Generic;
using EFT.InputSystem;
using EFT.UI;
using ImprovedTabUI.UI;
using UnityEngine;

namespace ImprovedTabUI.Eft
{
    internal static class InventoryOpener
    {
        private const int PendingFrames = 60;

        private static bool _injectToggle;
        private static Tab _pendingTab;
        private static int _pendingFrame;

        internal static bool BlockLook;

        internal static InventoryScreen Screen
        {
            get { return MonoBehaviourSingleton<CommonUI>.Instantiated ? MonoBehaviourSingleton<CommonUI>.Instance.InventoryScreen : null; }
        }

        internal static bool InRaid
        {
            get
            {
                if (!Comfort.Common.Singleton<EFT.GameWorld>.Instantiated)
                {
                    return false;
                }

                EFT.GameWorld world = Comfort.Common.Singleton<EFT.GameWorld>.Instance;
                return world != null && !(world is HideoutGameWorld);
            }
        }

        internal static bool InventoryOpen
        {
            get
            {
                InventoryScreen screen = Screen;
                return screen != null && screen.gameObject.activeInHierarchy;
            }
        }

        internal static void Open(InventoryTab tab)
        {
            if (tab == null || tab.Tab == null)
            {
                return;
            }

            InventoryScreen screen = Screen;

            if (screen != null && screen.gameObject.activeInHierarchy)
            {
                TabBar.SelectOnScreen(screen, tab.Tab);
                return;
            }

            _pendingTab = tab.Tab;
            _pendingFrame = Time.frameCount;
            _injectToggle = true;
        }

        internal static Tab TakePendingTab()
        {
            Tab tab = _pendingTab;
            _pendingTab = null;
            return tab != null && Time.frameCount - _pendingFrame <= PendingFrames ? tab : null;
        }

        internal static void BeforeTranslate(List<ECommand> commands, float[] axes)
        {
            if (_injectToggle)
            {
                _injectToggle = false;
                commands?.Add(ECommand.ToggleInventory);
            }

            if (!BlockLook)
            {
                return;
            }

            if (axes != null)
            {
                for (int axis = (int)EAxis.TurnX; axis <= (int)EAxis.LookY && axis < axes.Length; axis++)
                {
                    axes[axis] = 0f;
                }
            }

            if (commands != null && commands.Count > 0)
            {
                commands.RemoveAll(IsBlocked);
            }
        }

        private static HashSet<ECommand> _allowedWhileBlocked;

        private static bool IsBlocked(ECommand command)
        {
            if (_allowedWhileBlocked == null)
            {
                _allowedWhileBlocked = new HashSet<ECommand>();

                foreach (ECommand value in System.Enum.GetValues(typeof(ECommand)))
                {
                    string name = value.ToString();

                    if (name.StartsWith("End") || name.StartsWith("Stop") || name.StartsWith("Release") || name.EndsWith("Release")
                        || name.EndsWith("Up") || name.EndsWith("Off"))
                    {
                        _allowedWhileBlocked.Add(value);
                    }
                }
            }

            return !_allowedWhileBlocked.Contains(command);
        }
    }
}
