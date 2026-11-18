using System.Runtime.InteropServices;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal static class UiInput
    {
        internal static bool IsTyping()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;

            if (selected == null)
            {
                return false;
            }

            TMP_InputField textMeshField = selected.GetComponent<TMP_InputField>();

            if (textMeshField != null && textMeshField.isFocused)
            {
                return true;
            }

            InputField field = selected.GetComponent<InputField>();
            return field != null && field.isFocused;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out CursorPoint point);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CursorPoint
        {
            public int X;
            public int Y;
        }

        private static bool _nativeFailed;

        internal static bool TryGetCursor(out CursorPoint point)
        {
            point = default(CursorPoint);

            if (_nativeFailed)
            {
                return false;
            }

            try
            {
                return GetCursorPos(out point);
            }
            catch (System.Exception)
            {
                _nativeFailed = true;
                return false;
            }
        }

        internal static void PlaceCursor(CursorPoint point)
        {
            if (_nativeFailed)
            {
                return;
            }

            try
            {
                SetCursorPos(point.X, point.Y);
            }
            catch (System.Exception)
            {
                _nativeFailed = true;
            }
        }

        internal static bool IsHeld(KeyboardShortcut shortcut, bool native)
        {
            KeyCode main = shortcut.MainKey;

            if (main == KeyCode.None || !KeyHeld(main, native))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (modifier != main && !KeyHeld(modifier, native))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool KeyHeld(KeyCode key, bool native)
        {
            if (Input.GetKey(key))
            {
                return true;
            }

            if (!native || _nativeFailed || !Application.isFocused)
            {
                return false;
            }

            int virtualKey = VirtualKey(key);

            if (virtualKey == 0)
            {
                return false;
            }

            try
            {
                return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
            }
            catch (System.Exception)
            {
                _nativeFailed = true;
                return false;
            }
        }

        private static int VirtualKey(KeyCode key)
        {
            int code = (int)key;

            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return 0x41 + code - (int)KeyCode.A;
            }

            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                return 0x30 + code - (int)KeyCode.Alpha0;
            }

            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            {
                return 0x60 + code - (int)KeyCode.Keypad0;
            }

            if (key >= KeyCode.F1 && key <= KeyCode.F15)
            {
                return 0x70 + code - (int)KeyCode.F1;
            }

            switch (key)
            {
                case KeyCode.Mouse0: return 0x01;
                case KeyCode.Mouse1: return 0x02;
                case KeyCode.Mouse2: return 0x04;
                case KeyCode.Mouse3: return 0x05;
                case KeyCode.Mouse4: return 0x06;
                case KeyCode.Tab: return 0x09;
                case KeyCode.Space: return 0x20;
                case KeyCode.CapsLock: return 0x14;
                case KeyCode.BackQuote: return 0xC0;
                case KeyCode.LeftShift: return 0xA0;
                case KeyCode.RightShift: return 0xA1;
                case KeyCode.LeftControl: return 0xA2;
                case KeyCode.RightControl: return 0xA3;
                case KeyCode.LeftAlt: return 0xA4;
                case KeyCode.RightAlt: return 0xA5;
                default: return 0;
            }
        }

        internal static int PressedNow(KeyboardShortcut shortcut)
        {
            KeyCode main = shortcut.MainKey;

            if (main == KeyCode.None || !Input.GetKeyDown(main))
            {
                return -1;
            }

            int count = 0;

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (modifier == main)
                {
                    continue;
                }

                if (!Input.GetKey(modifier))
                {
                    return -1;
                }

                count++;
            }

            return count;
        }

        internal static string KeyText(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
            {
                return string.Empty;
            }

            string text = string.Empty;

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                text += Name(modifier) + "+";
            }

            return text + Name(shortcut.MainKey);
        }

        private static string Name(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "Alt";
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.Mouse2:
                    return "MMB";
                case KeyCode.Mouse3:
                    return "M4";
                case KeyCode.Mouse4:
                    return "M5";
                default:
                    return key.ToString().Replace("Alpha", string.Empty).Replace("Keypad", "Num");
            }
        }
    }
}
