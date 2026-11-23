using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    [BepInPlugin("com.volas.gtag", "Volas", "1.0.0")]
    public sealed class VolasPlugin : BaseUnityPlugin
    {
        internal static VolasPlugin Instance { get; private set; }
        internal ManualLogSource Log { get { return Logger; } }
        internal GameBridge Game { get; private set; }
        internal FeatureRegistry Features { get; private set; }
        internal VisualEffects Effects { get; private set; }
        internal MovementEffects Movement { get; private set; }
        internal VrMenu Vr { get; private set; }
        private DesktopMenu desktop;
        private bool lastVrButton;

        internal bool VrOpen { get; set; }
        internal bool DesktopOpen { get; set; }
        internal bool Compact { get; set; }
        internal bool RainbowTheme { get; set; }
        internal Color Accent
        {
            get
            {
                return RainbowTheme
                    ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.13f, 1f), 0.72f, 1f)
                    : new Color(0.23f, 0.85f, 1f);
            }
        }

        internal float FramesPerSecond { get; private set; }
        private float smoothFrameDelta = 1f / 90f;

        private void Awake()
        {
            Instance = this;
            Game = new GameBridge();
            Effects = new VisualEffects(this);
            Movement = new MovementEffects(this);
            Features = new FeatureRegistry(this);
            FeatureCatalog.Register(this);
            Vr = new VrMenu(this);
            desktop = new DesktopMenu(this);
            Logger.LogInfo("Volas loaded. F6 = desktop menu, left Y = VR menu, End = reset mods.");
        }

        private void Update()
        {
            smoothFrameDelta = Mathf.Lerp(smoothFrameDelta, Mathf.Max(Time.unscaledDeltaTime, 0.00001f), 0.06f);
            FramesPerSecond = 1f / smoothFrameDelta;
            Game.Refresh();

            if (Input.GetKeyDown(KeyCode.F6))
                DesktopOpen = !DesktopOpen;

            if (Input.GetKeyDown(KeyCode.End))
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
            if (Effects != null) Effects.Dispose();
            if (Vr != null) Vr.Dispose();
            if (desktop != null) desktop.Dispose();
            Instance = null;
        }
    }
}
