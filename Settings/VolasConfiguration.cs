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
        internal readonly ConfigEntry<float> DesktopOpacity;
        internal readonly ConfigEntry<float> VrOffsetX;
        internal readonly ConfigEntry<float> VrOffsetY;
        internal readonly ConfigEntry<float> VrOffsetZ;
        internal readonly ConfigEntry<float> GunDistance;
        internal readonly ConfigEntry<float> GunBeamWidth;
        internal readonly ConfigEntry<float> GunReticleSize;
        internal readonly ConfigEntry<bool> GunUseForward;
        internal readonly ConfigEntry<bool> GunLeftHand;
        internal readonly ConfigEntry<int> GunBeamStyle;
        internal readonly ConfigEntry<int> GunReticleStyle;
        internal readonly ConfigEntry<bool> GunRainbow;
        internal readonly ConfigEntry<bool> GunGlow;
        internal readonly ConfigEntry<bool> GunPulse;
        internal readonly ConfigEntry<float> GunAnimationSpeed;
        internal readonly ConfigEntry<float> GunMarkScale;
        internal readonly ConfigEntry<float> GunMarkLifetime;
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
            DesktopOpacity = config.Bind("Appearance", "Desktop opacity", 0.91f,
                new ConfigDescription("Black glass background opacity.",
                    new AcceptableValueRange<float>(0.72f, 1f)));
            VrScale = config.Bind("Appearance", "VR menu scale", 1f,
                new ConfigDescription("Physical VR menu size multiplier.",
                    new AcceptableValueRange<float>(0.65f, 1.30f)));
            VrOffsetX = config.Bind("VR Menu", "Offset X", 0f,
                new ConfigDescription("Wrist tablet horizontal position in meters.",
                    new AcceptableValueRange<float>(-0.24f, 0.24f)));
            VrOffsetY = config.Bind("VR Menu", "Offset Y", 0.06f,
                new ConfigDescription("Wrist tablet vertical position in meters.",
                    new AcceptableValueRange<float>(-0.24f, 0.24f)));
            VrOffsetZ = config.Bind("VR Menu", "Offset Z", 0.16f,
                new ConfigDescription("Wrist tablet forward position in meters.",
                    new AcceptableValueRange<float>(0.02f, 0.42f)));
            GunDistance = config.Bind("GunLib", "Maximum distance", 80f,
                new ConfigDescription("Raycast distance, in meters.",
                    new AcceptableValueRange<float>(10f, 250f)));
            GunBeamWidth = config.Bind("GunLib", "Beam width", 0.012f,
                new ConfigDescription("Pointer beam width in meters.",
                    new AcceptableValueRange<float>(0.002f, 0.06f)));
            GunReticleSize = config.Bind("GunLib", "Reticle diameter", 0.07f,
                new ConfigDescription("Reticle diameter in meters.",
                    new AcceptableValueRange<float>(0.025f, 0.20f)));
            GunUseForward = config.Bind("GunLib", "Aim along forward", false,
                "If true, aim from right hand +forward. Otherwise aim along -up.");
            GunLeftHand = config.Bind("GunLib", "Left-hand pointer", false, "Use the left controller for aiming instead of right.");
            GunBeamStyle = config.Bind("GunLib", "Beam style", 0,
                new ConfigDescription("0 Straight / 1 Wave / 2 Spiral",
                    new AcceptableValueRange<int>(0, 2)));
            GunReticleStyle = config.Bind("GunLib", "Reticle style", 0,
                new ConfigDescription("0 Orb / 1 Block / 2 Ring / 3 Diamond",
                    new AcceptableValueRange<int>(0, 3)));
            GunRainbow = config.Bind("GunLib", "Rainbow pointer", false, "Cycle pointer color over time.");
            GunGlow = config.Bind("GunLib", "Soft glow", true, "Draw a local translucent outline along the beam.");
            GunPulse = config.Bind("GunLib", "Pulse animation", true, "Animate the pointer width and reticle size.");
            GunAnimationSpeed = config.Bind("GunLib", "Animation speed", 1f,
                new ConfigDescription("Multiplier for pointer animation speed.",
                    new AcceptableValueRange<float>(0.15f, 4f)));
            GunMarkScale = config.Bind("GunLib", "Decorative stamp scale", 1f,
                new ConfigDescription("Size multiplier for geometric pointer stamps.",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));
            GunMarkLifetime = config.Bind("GunLib", "Decorative stamp lifetime", 30f,
                new ConfigDescription("How long decorative stamps remain, in seconds.",
                    new AcceptableValueRange<float>(3f, 120f)));
            FavoriteIds = config.Bind("Favorites", "Feature IDs", "",
                "Comma-separated favorite feature IDs. Manage using the desktop star controls.");
            WindowX = config.Bind("Desktop", "Window X", 55f, "Horizontal desktop window position.");
            WindowY = config.Bind("Desktop", "Window Y", 55f, "Vertical desktop window position.");
            WindowWidth = config.Bind("Desktop", "Window width", 900f, "Desktop window width.");
            WindowHeight = config.Bind("Desktop", "Window height", 580f, "Desktop window height.");
        }
    }
}
