using UnityEngine;

namespace Volas
{
    /// <summary>Independent Unity IMGUI interface for local telemetry and menu preferences.</summary>
    internal sealed class LegacyDesktopMenu
    {
        #region State

        private readonly VolasPlugin plugin;
        private Rect window;
        private bool visible;
        private bool savedCursor;
        private CursorLockMode savedLock;
        private string search = "";
        private Vector2 scroll;

        #endregion

        #region Initialization & visibility

        internal LegacyDesktopMenu(VolasPlugin owner)
        {
            plugin = owner;
            window = new Rect(owner.Settings.LegacyWindowX.Value,
                owner.Settings.LegacyWindowY.Value, owner.Settings.LegacyWindowWidth.Value, 400f);
        }

        internal void Tick()
        {
            if (!plugin.LegacyDesktopOpen || plugin.DesktopOpen)
            {
                Close();
                return;
            }
            if (!visible)
            {
                savedCursor = Cursor.visible;
                savedLock = Cursor.lockState;
                visible = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal void Close()
        {
            if (!visible) return;
            visible = false;
            Cursor.lockState = savedLock;
            Cursor.visible = savedCursor;
            plugin.Settings.LegacyWindowX.Value = window.x;
            plugin.Settings.LegacyWindowY.Value = window.y;
        }

        #endregion

        #region IMGUI rendering

        internal void Draw()
        {
            if (!visible || plugin.DesktopOpen) return;
            Color old = GUI.color;
            int oldDepth = GUI.depth;
            try
            {
                GUI.depth = -100;
                GUI.color = new Color(1f, 1f, 1f, plugin.Settings.DesktopOpacity.Value);
                window.width = Mathf.Min(plugin.Settings.LegacyWindowWidth.Value, Mathf.Max(180f, Screen.width));
                window.height = Mathf.Min(420f, Mathf.Max(120f, Screen.height));
                window.x = Mathf.Clamp(window.x, 0f, Mathf.Max(0f, Screen.width - window.width));
                window.y = Mathf.Clamp(window.y, 0f, Mathf.Max(0f, Screen.height - window.height));
                window = GUI.Window(plugin.GetInstanceID(), window, DrawWindow, "VOLAS  /  LEGACY");
            }
            finally { GUI.color = old; GUI.depth = oldDepth; }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("LOCAL MENU   |   FPS " + plugin.FramesPerSecond.ToString("0"));
            if (GUILayout.Button("Close", GUILayout.Width(64))) plugin.LegacyDesktopOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("Filter local display tools");
            search = GUILayout.TextField(search ?? "");
            scroll = GUILayout.BeginScrollView(scroll);
            DrawFeature("fps_hud");
            DrawFeature("clock_hud");
            DrawFeature("pos_hud");
            DrawFeature("heading_hud");
            DrawFeature("frame_time");
            DrawFeature("session_clock");
            DrawFeature("tracking_status");
            DrawFeature("controller_battery");
            GUILayout.Space(8f);
            GUILayout.Label("APPEARANCE");
            GUILayout.Label("Opacity: " + plugin.Settings.DesktopOpacity.Value.ToString("0.00"));
            float opacity = GUILayout.HorizontalSlider(plugin.Settings.DesktopOpacity.Value, .72f, 1f);
            if (Mathf.Abs(opacity - plugin.Settings.DesktopOpacity.Value) > .001f)
                plugin.Settings.DesktopOpacity.Value = opacity;
            GUILayout.Label("Window width: " + plugin.Settings.LegacyWindowWidth.Value.ToString("0"));
            float width = GUILayout.HorizontalSlider(plugin.Settings.LegacyWindowWidth.Value, 320f, 640f);
            if (Mathf.Abs(width - plugin.Settings.LegacyWindowWidth.Value) > .1f)
                plugin.Settings.LegacyWindowWidth.Value = width;
            plugin.Compact = GUILayout.Toggle(plugin.Compact, "Compact physical VR menu");
            plugin.RainbowTheme = GUILayout.Toggle(plugin.RainbowTheme, "Rainbow VR accent");
            GUILayout.Label("VR scale: " + plugin.Settings.VrScale.Value.ToString("0.00"));
            float scale = GUILayout.HorizontalSlider(plugin.Settings.VrScale.Value, .65f, 1.30f);
            if (Mathf.Abs(scale - plugin.Settings.VrScale.Value) > .001f)
                plugin.Settings.VrScale.Value = scale;
            if (GUILayout.Button("Reset window position"))
                window.position = new Vector2(20f, 20f);
            if (GUILayout.Button("Disable all active features")) plugin.Features.DisableAll();
            GUILayout.EndScrollView();
            GUILayout.Label("F5: legacy  |  F6: modern  |  F7: VR test");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private void DrawFeature(string id)
        {
            Feature feature = plugin.Features.Find(id);
            if (feature == null || feature.Label.IndexOf(search ?? "", System.StringComparison.OrdinalIgnoreCase) < 0) return;
            bool enabled = GUILayout.Toggle(feature.Enabled, feature.Label);
            if (enabled != feature.Enabled) plugin.Features.Activate(feature);
        }

        #endregion
    }
}
