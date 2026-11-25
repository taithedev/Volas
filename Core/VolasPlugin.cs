using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    [BepInPlugin("com.volas.gtag", "Volas", "1.9.1")]
    public sealed class VolasPlugin : BaseUnityPlugin
    {
        #region Services & runtime state

        internal static VolasPlugin Instance { get; private set; }
        internal ManualLogSource Log { get { return Logger; } }
        internal GameBridge Game { get; private set; }
        internal FeatureRegistry Features { get; private set; }
        internal VisualEffects Effects { get; private set; }
        internal PlayerMarkers Markers { get; private set; }
        internal ExtraVisualEffects ExtraVisuals { get; private set; }
        internal GunLib Gun { get; private set; }
        internal GunEffects Guns { get; private set; }
        internal CreativeGunEffects CreativeGun { get; private set; }
        internal VisualDecorations Decorations { get; private set; }
        internal LocalCharms Charms { get; private set; }
        internal LicenseClient Licensing { get; private set; }
        internal CommunityEffects Community { get; private set; }
        internal MovementEffects Movement { get; private set; }
        internal VrMenu Vr { get; private set; }
        internal VolasConfiguration Settings { get; private set; }
        private DesktopMenu desktop;
        private LegacyDesktopMenu legacyDesktop;
        private bool legacyFaultLogged;
        private bool lastVrButton;
        private float nextInputWarning;
        private bool desktopFaultLogged;
        private bool desktopFailed;
        private readonly Dictionary<string, float> nextSystemWarning = new Dictionary<string, float>();
        private bool vrFaultLogged;
        private readonly List<InputDevice> leftControllerCandidates = new List<InputDevice>(4);

        #endregion

        #region Menu & theme properties

        internal bool VrOpen { get; set; }
        internal bool DesktopOpen { get; set; }
        internal bool LegacyDesktopOpen { get; set; }
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
                    : new Color(0.97f, 0.97f, 1f);
            }
        }

        #endregion

        #region Frame telemetry

        internal float FramesPerSecond { get; private set; }
        internal float FrameMilliseconds { get { return smoothFrameDelta * 1000f; } }
        internal float SessionStarted { get; private set; }
        private float smoothFrameDelta = 1f / 90f;

        #endregion

        #region Unity lifecycle & frame updates

        private void Awake()
        {
            Instance = this;
            SessionStarted = Time.realtimeSinceStartup;
            Settings = new VolasConfiguration(Config);
            Licensing = new LicenseClient(this);
            Community = new CommunityEffects(this);
            Compact = Settings.CompactMenu.Value;
            RainbowTheme = Settings.RainbowAccent.Value;
            DesktopOpen = false;
            Game = new GameBridge();
            Effects = new VisualEffects(this);
            Markers = new PlayerMarkers(this);
            ExtraVisuals = new ExtraVisualEffects(this);
            Gun = new GunLib(this);
            Guns = new GunEffects(this);
            CreativeGun = new CreativeGunEffects(this);
            Decorations = new VisualDecorations(this);
            Charms = new LocalCharms(this);
            Movement = new MovementEffects(this);
            Features = new FeatureRegistry(this);
            FeatureCatalog.Register(this);
            Features.ValidateCatalog();
            Features.LoadFavorites(Settings.FavoriteIds.Value);
            Vr = new VrMenu(this);
            // A desktop uGUI initialization failure must never disable the physical VR menu.
            try { desktop = new DesktopMenu(this); }
            catch (Exception ex) { Logger.LogError("Volas PC menu initialization failed (VR remains usable): " + ex); }
            legacyDesktop = new LegacyDesktopMenu(this);
            Logger.LogInfo("Volas loaded. F6 = modern PC, F5 = legacy IMGUI, left Y = VR menu, F7 = VR test, End = reset.");
            Logger.LogInfo(AssemblyDiagnostics.AssemblyInfo());
        }

        private void Update()
        {
            smoothFrameDelta = Mathf.Lerp(smoothFrameDelta, Mathf.Max(Time.unscaledDeltaTime, 0.00001f), 0.06f);
            FramesPerSecond = 1f / smoothFrameDelta;
            try { Licensing.Tick(); } catch (Exception ex) { ReportSystemFault("License tick", ex); }
            if (!Licensing.Authenticated)
            {
                try { if (desktop != null) desktop.Tick(); } catch (Exception ex) { if (!desktopFaultLogged) { desktopFaultLogged = true; Logger.LogError("Volas desktop: " + ex.Message); } }
                return;
            }
            RunSystem("Game bridge", Game.Refresh);
            RunSystem("Community", Community.Tick);

            if (!Licensing.PanelOpen && MenuInput.Shortcut(Settings.DesktopShortcut.Value, KeyCode.F6))
            {
                if (DesktopOpen || ((desktopFailed || desktop == null) && LegacyDesktopOpen)) { DesktopOpen = false; LegacyDesktopOpen = false; }
                else OpenVerifiedDesktop();
            }
            else if (!Licensing.PanelOpen && MenuInput.Shortcut(Settings.LegacyDesktopShortcut.Value, KeyCode.F5))
            {
                if (LegacyDesktopOpen) LegacyDesktopOpen = false;
                else OpenVerifiedLegacy();
            }

            if (MenuInput.Shortcut(Settings.ResetShortcut.Value, KeyCode.End))
            {
                RunSystem("Emergency reset", Features.DisableAll);
                Licensing.SetCommunityEffects(false);
                Community.Hide();
                Logger.LogInfo("Volas: all active features disabled.");
            }

            bool vrPressed = false;
            try { vrPressed = GetLeftYPressed(); } catch (Exception ex) { ReportSystemFault("VR input", ex); }
            bool testPressed = MenuInput.Shortcut(Settings.VrFallbackShortcut.Value, KeyCode.F7);
            if (!Licensing.PanelOpen && ((vrPressed && !lastVrButton) || testPressed))
            {
                VrOpen = !VrOpen;
                Logger.LogInfo("Volas VR menu " + (VrOpen ? "opened" : "closed") +
                    (testPressed ? " (F7 keyboard)" : " (left Y controller)"));
                if (VrOpen && !InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid)
                    Logger.LogWarning("Volas: XRNode.LeftHand not valid; checking alternate tracked input and head-anchored preview.");
            }
            lastVrButton = vrPressed;

            RunSystem("Feature registry", Features.Tick);
            try { Gun.Tick(!VrOpen && !DesktopOpen && !LegacyDesktopOpen && !Licensing.PanelOpen && (Gun.Preview || Guns.AnyEnabled || CreativeGun.AnyEnabled)); }
            catch (Exception ex) { Gun.Reset(); ReportSystemFault("GunLib", ex); }
            RunSystem("Pointer effects", Guns.Tick);
            RunSystem("Creative effects", CreativeGun.Tick);
            RunSystem("Movement", Movement.Tick);
            RunSystem("Visual effects", Effects.Tick);
            RunSystem("Markers", Markers.Tick);
            RunSystem("Extra visuals", ExtraVisuals.Tick);
            RunSystem("Decorations", Decorations.Tick);
            RunSystem("Charms", Charms.Tick);
            // Keep VR independently operational if the optional desktop UI crashes.
            try { Vr.Tick(); }
            catch (Exception ex)
            {
                if (!vrFaultLogged || Time.unscaledTime >= nextInputWarning)
                {
                    Logger.LogError("Volas physical menu update failed: " + ex);
                    vrFaultLogged = true;
                    nextInputWarning = Time.unscaledTime + 10f;
                }
            }
            if (desktop != null)
            {
                try { desktop.Tick(); }
                catch (Exception ex)
                {
                    if (!desktopFaultLogged)
                    {
                        desktopFaultLogged = true;
                        Logger.LogError("Volas desktop menu update failed; switching to legacy: " + ex);
                        desktopFailed = true;
                        DesktopOpen = false;
                        try { desktop.Dispose(); } catch { }
                        desktop = null;
                        OpenVerifiedLegacy();
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (Licensing == null || !Licensing.Authenticated) return;
            if (legacyDesktop != null) RunSystem("Legacy menu", legacyDesktop.Tick);
        }

        internal void OnLicenseVerified()
        {
            Logger.LogInfo("Volas license verified; opening desktop workspace.");
            OpenVerifiedDesktop();
        }

        internal void OpenVerifiedDesktop()
        {
            if (!Licensing.Authenticated) return;
            if (desktop == null || desktopFailed) { OpenVerifiedLegacy(); return; }
            LegacyDesktopOpen = false; VrOpen = false;
            legacyDesktop?.Close(); DesktopOpen = true;
        }

        internal void OpenVerifiedLegacy()
        {
            if (!Licensing.Authenticated) return;
            DesktopOpen = false; VrOpen = false; LegacyDesktopOpen = true;
        }

        private void RunSystem(string name, Action update)
        {
            try { update(); } catch (Exception ex) { ReportSystemFault(name, ex); }
        }
        private void ReportSystemFault(string name, Exception ex)
        {
            float next;
            if (nextSystemWarning.TryGetValue(name, out next) && Time.unscaledTime < next) return;
            nextSystemWarning[name] = Time.unscaledTime + 10f;
            Logger.LogError("Volas " + name + ": " + ex);
        }

        private void OnGUI()
        {
            if (Licensing != null) Licensing.Draw();
            if (Licensing == null || !Licensing.Authenticated || Licensing.PanelOpen) return;
            if (legacyDesktop == null || !LegacyDesktopOpen) return;
            try { legacyDesktop.Draw(); }
            catch (Exception ex)
            {
                LegacyDesktopOpen = false;
                legacyDesktop.Close();
                if (!legacyFaultLogged)
                {
                    legacyFaultLogged = true;
                    Logger.LogError("Volas legacy desktop menu failed: " + ex);
                }
            }
        }

        #endregion

        #region Controller input

        /// <summary>
        /// OpenXR/XR SDKs do not all expose Y the same way. Read the standard
        /// XR secondaryButton on the left device, then tracked left-handed
        /// controllers, and finally Unity's common legacy Y index.
        /// </summary>
        private bool GetLeftYPressed()
        {
            bool pressed;
            // GT's ControllerInputPoller normalizes SteamVR + Meta controller
            // actions. Unity XR secondaryButton alone can remain false.
            if (Game != null &&
                Game.TryControllerButton("leftControllerSecondaryButton", out pressed) &&
                pressed)
                return true;
            InputDevice primary = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (primary.isValid &&
                primary.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed)
                return true;

            leftControllerCandidates.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftControllerCandidates);
            for (int i = 0; i < leftControllerCandidates.Count; i++)
            {
                InputDevice device = leftControllerCandidates[i];
                if (device.isValid &&
                    device.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed)
                    return true;
            }

            if (!Settings.VrLegacyYFallback.Value) return false;
            try
            {
                return Input.GetKey(KeyCode.JoystickButton3) ||
                    Input.GetKey(KeyCode.Joystick1Button3) ||
                    Input.GetKey(KeyCode.Joystick2Button3);
            }
            catch (Exception)
            {
                // The game's active Input System may disable legacy joystick APIs.
                return false;
            }
        }

        #endregion

        #region Physics & shutdown

        private void FixedUpdate()
        {
            if (Licensing == null || !Licensing.Authenticated) return;
            if (Features != null)
                Features.FixedTick();
            if (Movement != null)
                Movement.FixedTick();
            if (Guns != null)
                Guns.FixedTick();
        }

        internal void CloseLicensedMenus()
        {
            DesktopOpen = false; LegacyDesktopOpen = false; VrOpen = false;
            if (Community != null) Community.Hide();
            if (Features != null) Features.DisableAll();
            if (Movement != null) Movement.Reset();
            try { if (desktop != null) desktop.Tick(); } catch { }
            try { if (legacyDesktop != null) legacyDesktop.Close(); } catch { }
            try { if (Vr != null) Vr.Tick(); } catch { }
            try { if (Effects != null) Effects.Tick(); } catch { }
            try { if (Markers != null) Markers.Tick(); } catch { }
            try { if (ExtraVisuals != null) ExtraVisuals.Tick(); } catch { }
            try { if (Decorations != null) Decorations.Tick(); } catch { }
            try { if (Charms != null) Charms.Tick(); } catch { }
        }

        private void OnDestroy()
        {
            if (Community != null) Community.Dispose();
            if (Licensing != null) Licensing.Dispose();
            if (Features != null) Features.DisableAll();
            if (Movement != null) Movement.Reset();
            if (Guns != null) Guns.Dispose();
            if (CreativeGun != null) CreativeGun.Dispose();
            if (Decorations != null) Decorations.Dispose();
            if (Charms != null) Charms.Dispose();
            if (Gun != null) Gun.Dispose();
            if (Markers != null) Markers.Dispose();
            if (Effects != null) Effects.Dispose();
            if (ExtraVisuals != null) ExtraVisuals.Dispose();
            if (Vr != null) Vr.Dispose();
            if (legacyDesktop != null) legacyDesktop.Close();
            if (desktop != null) desktop.Dispose();
            if (Game != null) Game.Dispose();
            Instance = null;
        }

        #endregion
    }
}
