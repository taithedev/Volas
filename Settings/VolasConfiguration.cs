using BepInEx.Configuration;
using UnityEngine;

namespace Volas
{
    internal sealed class VolasConfiguration
    {
        #region General controls & appearance

        internal readonly ConfigEntry<KeyboardShortcut> DesktopShortcut;
        internal readonly ConfigEntry<KeyboardShortcut> LegacyDesktopShortcut;
        internal readonly ConfigEntry<float> LegacyWindowX;
        internal readonly ConfigEntry<float> LegacyWindowY;
        internal readonly ConfigEntry<float> LegacyWindowWidth;
        internal readonly ConfigEntry<KeyboardShortcut> VrFallbackShortcut;
        internal readonly ConfigEntry<bool> VrLegacyYFallback;
        internal readonly ConfigEntry<KeyboardShortcut> ResetShortcut;
        internal readonly ConfigEntry<bool> OpenDesktopOnLaunch;
        internal readonly ConfigEntry<bool> CompactMenu;
        internal readonly ConfigEntry<bool> RainbowAccent;
        internal readonly ConfigEntry<float> AccentHue;
        internal readonly ConfigEntry<float> VrScale;
        internal readonly ConfigEntry<float> VrTextScale;
        internal readonly ConfigEntry<bool> VrPointerRay;
        internal readonly ConfigEntry<bool> VrNotifications;
        internal readonly ConfigEntry<float> DesktopOpacity;
        #endregion

        #region Desktop visual effects & audio

        internal readonly ConfigEntry<bool> DesktopGlass;
        internal readonly ConfigEntry<float> DesktopBlurStrength;
        internal readonly ConfigEntry<bool> DesktopBloom;
        internal readonly ConfigEntry<float> DesktopBloomStrength;
        internal readonly ConfigEntry<bool> DesktopMotion;
        internal readonly ConfigEntry<bool> DesktopSounds;
        internal readonly ConfigEntry<float> DesktopSoundVolume;
        #endregion

        #region Marker & wrist settings

        internal readonly ConfigEntry<float> MarkerRange;
        internal readonly ConfigEntry<float> MarkerRefresh;
        internal readonly ConfigEntry<int> MarkerLimit;
        internal readonly ConfigEntry<float> MarkerWidth;
        internal readonly ConfigEntry<float> VrOffsetX;
        internal readonly ConfigEntry<float> VrOffsetY;
        internal readonly ConfigEntry<float> VrOffsetZ;
        #endregion

        #region Pointer configuration

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
        #endregion

        #region Saved preferences

        internal readonly ConfigEntry<string> FavoriteIds;
        internal readonly ConfigEntry<float> WindowX;
        internal readonly ConfigEntry<float> WindowY;
        internal readonly ConfigEntry<float> WindowWidth;
        internal readonly ConfigEntry<float> WindowHeight;

        #endregion

        #region Configuration bindings

        internal VolasConfiguration(ConfigFile config)
        {
            DesktopShortcut = config.Bind("Controls", "Toggle desktop",
                new KeyboardShortcut(KeyCode.F6), "Open or close the desktop menu.");
            LegacyDesktopShortcut = config.Bind("Controls", "Toggle legacy desktop",
                new KeyboardShortcut(KeyCode.F5), "Open or close the legacy Unity IMGUI menu.");
            LegacyWindowX = config.Bind("Legacy Desktop", "Window X", 80f);
            LegacyWindowY = config.Bind("Legacy Desktop", "Window Y", 80f);
            LegacyWindowWidth = config.Bind("Legacy Desktop", "Window width", 390f,
                new ConfigDescription("Legacy window width in pixels.", new AcceptableValueRange<float>(320f, 640f)));
            VrFallbackShortcut = config.Bind("Controls", "VR menu keyboard fallback",
                new KeyboardShortcut(KeyCode.F7),
                "Open/close the physical VR menu from the PC keyboard to test tracking and rendering.");
            VrLegacyYFallback = config.Bind("Controls", "Legacy controller Y fallback", true,
                "Also read the common Unity legacy joystick button 3 mapping for left-controller Y.");
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
            DesktopGlass = config.Bind("Desktop Effects", "Acrylic blur", true, "Blur a local scene snapshot when the desktop menu opens.");
            DesktopBlurStrength = config.Bind("Desktop Effects", "Blur intensity", .72f, new ConfigDescription("Acrylic backdrop blend.", new AcceptableValueRange<float>(0f, 1f)));
            DesktopBloom = config.Bind("Desktop Effects", "Bloom", true, "Local soft accent glows, without changing game post-processing.");
            DesktopBloomStrength = config.Bind("Desktop Effects", "Bloom intensity", .55f, new ConfigDescription("Glow opacity.", new AcceptableValueRange<float>(0f, 1f)));
            DesktopMotion = config.Bind("Desktop Effects", "Animations", true, "Fade transitions and button feedback.");
            DesktopSounds = config.Bind("Desktop Effects", "UI sounds", true, "Local procedural menu sounds.");
            DesktopSoundVolume = config.Bind("Desktop Effects", "Sound volume", .25f, new ConfigDescription("UI audio volume.", new AcceptableValueRange<float>(0f, 1f)));
            MarkerRange = config.Bind("Player ESP", "Range", 25f, new ConfigDescription("Maximum marker distance in meters.", new AcceptableValueRange<float>(5f, 60f)));
            MarkerRefresh = config.Bind("Player ESP", "Refresh seconds", .20f, new ConfigDescription("Distance label and range refresh interval.", new AcceptableValueRange<float>(.10f, .75f)));
            MarkerLimit = config.Bind("Player ESP", "Marker limit", 8, new ConfigDescription("Maximum remote markers.", new AcceptableValueRange<int>(1, 12)));
            MarkerWidth = config.Bind("Player ESP", "Line width", .012f, new ConfigDescription("Box line width in meters.", new AcceptableValueRange<float>(.005f, .04f)));
            VrScale = config.Bind("Appearance", "VR menu scale", 1f,
                new ConfigDescription("Physical VR menu size multiplier.",
                    new AcceptableValueRange<float>(0.65f, 1.30f)));
            VrTextScale = config.Bind("VR Menu", "Text scale", 1f, new ConfigDescription("Wrist text multiplier.", new AcceptableValueRange<float>(.85f, 1.25f)));
            VrPointerRay = config.Bind("VR Menu", "Pointer ray", true, "Draw a controller selection ray.");
            VrNotifications = config.Bind("VR Menu", "Notifications", true, "Show temporary wrist feedback.");
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
            WindowWidth = config.Bind("Desktop", "Window width", 760f, "Desktop window width.");
            WindowHeight = config.Bind("Desktop", "Window height", 500f, "Desktop window height.");
        }

        #endregion
    }
}
