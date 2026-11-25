using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace Volas
{
    internal static class MenuInput
    {
        #region Keyboard compatibility
        private static Type keyboardType;
        private static PropertyInfo current;
        private static float nextLookup;
        private static bool legacyUnavailable;
        private static readonly Dictionary<string, PropertyInfo> controls = new Dictionary<string, PropertyInfo>();
        internal static bool Pressed(KeyCode key) { return Read(key, true); }
        internal static bool Shortcut(KeyboardShortcut shortcut, KeyCode fallback)
        {
            try
            {
                if (!legacyUnavailable && shortcut.IsDown()) return true;
            }
            catch { legacyUnavailable = true; }
            try
            {
                object boxed = shortcut;
                object main = GameBridge.GetMember(boxed.GetType(), boxed, "MainKey", "mainKey");
                KeyCode key = main is KeyCode ? (KeyCode)main : fallback;
                if (!Read(key, true)) return false;
                var modifiers = GameBridge.GetMember(boxed.GetType(), boxed, "Modifiers", "modifiers") as IEnumerable;
                if (modifiers != null) foreach (object modifier in modifiers)
                    if (modifier is KeyCode && !Read((KeyCode)modifier, false)) return false;
                return true;
            }
            catch { return Pressed(fallback); }
        }

        private static bool Read(KeyCode key, bool down)
        {
            if (!legacyUnavailable)
            {
                try { if (down ? Input.GetKeyDown(key) : Input.GetKey(key)) return true; }
                catch { legacyUnavailable = true; }
            }
            try
            {
                if (keyboardType == null)
                {
                    if (Time.realtimeSinceStartup < nextLookup) return false;
                    nextLookup = Time.realtimeSinceStartup + 2f;
                    keyboardType = GameBridge.FindType("UnityEngine.InputSystem.Keyboard");
                    if (keyboardType == null) return false;
                    current = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                }
                object keyboard = current?.GetValue(null, null);
                if (keyboard == null) return false;
                string name = key.ToString();
                if (name.StartsWith("Alpha", StringComparison.Ordinal)) name = "digit" + name.Substring(5);
                else if (name == "Return") name = "enter";
                else name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                name += "Key";
                PropertyInfo property;
                if (!controls.TryGetValue(name, out property)) controls[name] = property = keyboardType.GetProperty(name);
                object control = property?.GetValue(keyboard, null);
                return control != null && object.Equals(GameBridge.GetMember(control.GetType(), control, down ? "wasPressedThisFrame" : "isPressed"), true);
            }
            catch { return false; }
        }
        #endregion
    }
}
