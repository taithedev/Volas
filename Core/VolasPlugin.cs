using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    [BepInPlugin("com.volas.gtag", "Volas", "1.2.0")]
    public sealed class VolasPlugin : BaseUnityPlugin
    {
        internal static VolasPlugin Instance { get; private set; }
        internal ManualLogSource Log { get { return Logger; } }
        internal GameBridge Game { get; private set; }
        internal FeatureRegistry Features { get; private set; }
        internal VisualEffects Effects { get; private set; }
        internal GunLib Gun { get; private set; }
        internal GunEffects Guns { get; private set; }
        internal MovementEffects Movement { get; private set; }
        internal VrMenu Vr { get; private set; }
        internal VolasConfiguration Settings { get; private set; }
        private DesktopMenu desktop;
        private bool lastVrButton;

        internal bool VrOpen { get; set; }
        internal bool DesktopOpen { get; set; }
        private bool compact;
        private bool rainbowTheme;
        internal bool Compact
        {
            get { return compact; }
            set { compact = value; if (Settings != null) Settings.CompactMenu.Value = value; }
        }
        internal bool RainbowTheme
        {
            get { return rainbowTheme; }
            set { rainbowTheme = value; if (Settings != null) Settings.RainbowAccent.Value = value; }
        }
        internal Color Accent
        {
            get
            {
                return RainbowTheme
                    ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.13f, 1f), 0.72f, 1f)
                    : Color.HSVToRGB(Settings != null ? Settings.AccentHue.Value : 0.52f, 0.72f, 1f);
            }
        }

        internal float FramesPerSecond { get; private set; }
        private float smoothFrameDelta = 1f / 90f;

        private void Awake()
        {
            Instance = this;
            Settings = new VolasConfiguration(Config);
            Compact = Settings.CompactMenu.Value;
            RainbowTheme = Settings.RainbowAccent.Value;
            DesktopOpen = Settings.OpenDesktopOnLaunch.Value;
            Game = new GameBridge();
            Effects = new VisualEffects(this);
            Gun = new GunLib(this);
            Guns = new GunEffects(this);
            Movement = new MovementEffects(this);
            Features = new FeatureRegistry(this);
            FeatureCatalog.Register(this);
            Features.LoadFavorites(Settings.FavoriteIds.Value);
            Vr = new VrMenu(this);
            desktop = new DesktopMenu(this);
            Logger.LogInfo("Volas loaded. F6 = desktop menu, left Y = VR menu, End = reset mods.");
        }

        private void Update()
        {
            smoothFrameDelta = Mathf.Lerp(smoothFrameDelta, Mathf.Max(Time.unscaledDeltaTime, 0.00001f), 0.06f);
            FramesPerSecond = 1f / smoothFrameDelta;
            Game.Refresh();

            if (Settings.DesktopShortcut.Value.IsDown())
                DesktopOpen = !DesktopOpen;

            if (Settings.ResetShortcut.Value.IsDown())
            {
                Features.DisableAll();
                Logger.LogInfo("Volas: all active features disabled.");
            }

            bool vrPressed = false;
            var controller = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (controller.isValid)
                controller.TryGetFeatureValue(CommonUsages.secondaryButton, out vrPressed);
            if (vrPressed && !lastVrButton)
                VrOpen = !VrOpen;
            lastVrButton = vrPressed;

            Features.Tick();
            Gun.Tick(Guns.AnyEnabled);
            Guns.Tick();
            Movement.Tick();
            Effects.Tick();
            Vr.Tick();
        }

        private void FixedUpdate()
        {
            if (Features != null)
                Features.FixedTick();
            if (Movement != null)
                Movement.FixedTick();
            if (Guns != null)
                Guns.FixedTick();
        }

        private void OnGUI()
        {
            if (desktop != null)
                desktop.Draw();
        }

        private void OnDestroy()
        {
            if (Features != null) Features.DisableAll();
            if (Movement != null) Movement.Reset();
            if (Guns != null) Guns.Dispose();
            if (Gun != null) Gun.Dispose();
            if (Effects != null) Effects.Dispose();
            if (Vr != null) Vr.Dispose();
            if (desktop != null) desktop.Dispose();
            Instance = null;
        }
    }
}
