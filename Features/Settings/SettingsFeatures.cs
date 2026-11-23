using System;
using UnityEngine;

namespace Volas
{
    internal static class SettingsFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            registry.Add(new Feature(
                            "compact", "Settings", "Compact VR menu", "Scale the in-game VR menu down slightly.",
                            p => p.Compact = true, p => p.Compact = false));

            registry.Add(new Feature(
                            "rainbow_theme", "Settings", "Rainbow accent", "Cycle the OLED menu's accent color.",
                            p => p.RainbowTheme = true, p => p.RainbowTheme = false));

            registry.Add(new Feature(
                            "desktop", "Settings", "Toggle PC panel", "Open or close the F6 desktop menu.",
                            p => p.DesktopOpen = !p.DesktopOpen, actionOnly: true));

            registry.Add(new Feature(
                            "reset", "Settings", "Disable all mods", "Turn every active feature off, restoring changed settings.",
                            p => p.Features.DisableAll(), actionOnly: true));
        }
    }
}
