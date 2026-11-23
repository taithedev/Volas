using BepInEx.Configuration;
using UnityEngine;

namespace Volas
{
    internal sealed class VolasConfiguration
    {
        internal readonly ConfigEntry<KeyboardShortcut> DesktopShortcut;
        internal readonly ConfigEntry<KeyboardShortcut> ResetShortcut;
        internal readonly ConfigEntry<bool> OpenDesktopOnLaunch;
        internal readonly ConfigEntry<bool> CompactMenu;
        internal readonly ConfigEntry<bool> RainbowAccent;
        internal readonly ConfigEntry<float> AccentHue;
        internal readonly ConfigEntry<float> VrScale;
        internal readonly ConfigEntry<string> FavoriteIds;
        internal readonly ConfigEntry<float> WindowX;
        internal readonly ConfigEntry<float> WindowY;
        internal readonly ConfigEntry<float> WindowWidth;
        internal readonly ConfigEntry<float> WindowHeight;

        internal VolasConfiguration(ConfigFile config)
        {
            DesktopShortcut = config.Bind("Controls", "Toggle desktop",
                new KeyboardShortcut(KeyCode.F6), "Open or close the desktop menu.");
            ResetShortcut = config.Bind("Controls", "Disable all",
                new KeyboardShortcut(KeyCode.End), "Safely disable all active features.");
            OpenDesktopOnLaunch = config.Bind("Appearance", "Desktop open on launch", false,
                "Show the desktop menu immediately when the plugin loads.");
            CompactMenu = config.Bind("Appearance", "Compact VR menu", false,
                "Use a smaller physical VR menu.");
            RainbowAccent = config.Bind("Appearance", "Rainbow accent", false,
                "Animate the accent hue through the rainbow.");
            AccentHue = config.Bind("Appearance", "Accent hue", 0.52f,
                new ConfigDescription("Static accent hue (0 to 1).",
                    new AcceptableValueRange<float>(0f, 1f)));
            VrScale = config.Bind("Appearance", "VR menu scale", 1f,
                new ConfigDescription("Physical VR menu size multiplier.",
                    new AcceptableValueRange<float>(0.65f, 1.30f)));
            FavoriteIds = config.Bind("Favorites", "Feature IDs", "",
                "Comma-separated favorite feature IDs. Manage using the desktop star controls.");
            WindowX = config.Bind("Desktop", "Window X", 55f, "Horizontal desktop window position.");
            WindowY = config.Bind("Desktop", "Window Y", 55f, "Vertical desktop window position.");
            WindowWidth = config.Bind("Desktop", "Window width", 505f, "Desktop window width.");
            WindowHeight = config.Bind("Desktop", "Window height", 560f, "Desktop window height.");
        }
    }
}
