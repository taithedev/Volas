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
        private bool availableOnly = true;
        private System.Collections.Generic.List<Feature> results;
        private string lastSearch;
        private int lastRevision = -1;
        private bool lastAvailable, lastRoom, lastMarkerRoom;

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
            GUILayout.Label("Search the complete feature catalog");
            GUILayout.BeginHorizontal();
            search=GUILayout.TextField(search??"",100);
            if(GUILayout.Button("Clear",GUILayout.Width(60)))search="";
            GUILayout.EndHorizontal();
            availableOnly=GUILayout.Toggle(availableOnly,"Available in current room only");
            bool room=plugin.Game.IsSoloOrModdedRoom(), markerRoom=plugin.Game.IsInModdedRoom();
            if(results==null||lastSearch!=search||lastRevision!=plugin.Features.Revision||lastAvailable!=availableOnly||lastRoom!=room||lastMarkerRoom!=markerRoom)
            {
                if(lastSearch!=search)scroll=Vector2.zero;
                lastSearch=search;lastRevision=plugin.Features.Revision;lastAvailable=availableOnly;lastRoom=room;lastMarkerRoom=markerRoom;
                results=plugin.Features.Search(plugin.Features.All,search,f=>!availableOnly||f.Enabled||!f.NeedsModdedRoom||
                    ((f.Id=="marker_boxes"||f.Id=="marker_labels")?markerRoom:room));
            }
            GUILayout.Label(results.Count+" results");
            scroll=GUILayout.BeginScrollView(scroll);
            foreach(var feature in results) DrawFeature(feature);
            if(results.Count==0)GUILayout.Label("No matching available features.");
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
            GUILayout.Label(plugin.Features.LastMessage);
            GUILayout.Label("F5: legacy  |  F6: modern  |  F7: VR test");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private void DrawFeature(Feature feature)
        {
            if(feature==null)return;
            GUILayout.BeginHorizontal();
            bool old=GUI.enabled;
            bool allowed=!feature.NeedsModdedRoom||((feature.Id=="marker_boxes"||feature.Id=="marker_labels")?lastMarkerRoom:lastRoom);
            GUI.enabled=old&&(allowed||feature.Enabled);
            if(feature.ActionOnly){if(GUILayout.Button(feature.Label+" / Run"))plugin.Features.Activate(feature);}
            else { bool enabled=GUILayout.Toggle(feature.Enabled,feature.Label);if(enabled!=feature.Enabled)plugin.Features.Activate(feature); }
            GUI.enabled=old;
            if(GUILayout.Button(plugin.Features.IsFavorite(feature)?"Saved":"+ Save",GUILayout.Width(60)))plugin.Features.ToggleFavorite(feature);
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
