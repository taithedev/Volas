using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    internal sealed class DesktopMenu
    {
        private readonly VolasPlugin plugin;
        private Rect window = new Rect(55f, 55f, 505f, 560f);
        private Vector2 scroll;
        private int selectedCategory;
        private bool resizing;
        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle smallStyle;
        private GUIStyle lineStyle;
        private GUIStyle enabledStyle;
        private GUIStyle disabledStyle;
        private GUIStyle textStyle;
        private Texture2D background;
        private Texture2D row;
        private Texture2D lit;
        private Texture2D hover;
        private Texture2D white;

        internal DesktopMenu(VolasPlugin owner) { plugin = owner; }

        internal void Draw()
        {
            InitializeStyles();
            if (plugin.Effects.DesktopCrosshair)
            {
                Color previous = GUI.color;
                GUI.color = plugin.Accent;
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 1f, Screen.height * 0.5f - 9f, 2f, 18f), white);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 9f, Screen.height * 0.5f - 1f, 18f, 2f), white);
                GUI.color = previous;
            }

            DrawTelemetry();
            if (!plugin.DesktopOpen) return;

            window.x = Mathf.Clamp(window.x, 0f, Mathf.Max(0f, Screen.width - 75f));
            window.y = Mathf.Clamp(window.y, 0f, Mathf.Max(0f, Screen.height - 38f));
            window = GUI.Window(289109, window, DrawWindow, "", windowStyle);
        }

        private void DrawTelemetry()
        {
            var effects = plugin.Effects;
            var lines = new List<string>();
            if (effects.FpsHud) lines.Add("FPS   " + plugin.FramesPerSecond.ToString("0"));
            if (effects.ClockHud) lines.Add("TIME  " + DateTime.Now.ToString("HH:mm:ss"));
            if (effects.PositionHud)
            {
                var p = plugin.Game.Position;
                lines.Add("XYZ   " + p.x.ToString("0.0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0.0"));
            }
            if (effects.HeadingHud) lines.Add("YAW   " + plugin.Game.Heading.ToString("0") + " degrees");
            if (lines.Count == 0) return;

            float height = 17f + lines.Count * 24f;
            GUI.Box(new Rect(Screen.width - 310f, 18f, 295f, height), "", windowStyle);
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(Screen.width - 292f, 24f + i * 24f, 270f, 24f), lines[i], textStyle);
        }

        private void DrawWindow(int id)
        {
            Color savedColor = GUI.color;
            GUI.color = plugin.Accent;
            GUI.DrawTexture(new Rect(0f, 0f, window.width, 5f), white);
            GUI.color = savedColor;
            GUI.Label(new Rect(20f, 13f, 225f, 33f), "VOLAS", titleStyle);
            GUI.Label(new Rect(window.width - 192f, 21f, 170f, 25f), "GTAG / BepInEx 5 / v1.0", smallStyle);
            GUI.Label(new Rect(22f, 52f, window.width - 43f, 22f), plugin.Game.RoomSummary(), smallStyle);

            GUILayout.BeginArea(new Rect(16f, 82f, window.width - 32f, window.height - 143f));
            var categories = plugin.Features.Categories;
            if (categories.Count > 0)
            {
                selectedCategory = Mathf.Clamp(selectedCategory, 0, categories.Count - 1);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < categories.Count; i++)
                {
                    if (GUILayout.Button(categories[i], i == selectedCategory ? enabledStyle : disabledStyle,
                        GUILayout.Height(35f)))
                        selectedCategory = i;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10f);
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (var feature in plugin.Features.InCategory(categories[selectedCategory]))
                {
                    string state = feature.ActionOnly ? "RUN" : feature.Enabled ? "ON" : "OFF";
                    string label = feature.Label + "    /    " + state;
                    GUIStyle style = feature.Enabled ? enabledStyle : lineStyle;
                    if (GUILayout.Button(new GUIContent(label, feature.Description), style, GUILayout.Height(42f)))
                        plugin.Features.Activate(feature);
                    GUILayout.Label(feature.Description, smallStyle, GUILayout.Height(27f));
                    GUILayout.Space(4f);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            GUI.Label(new Rect(18f, window.height - 51f, window.width - 36f, 18f),
                plugin.Features.LastMessage, smallStyle);
            GUI.Label(new Rect(18f, window.height - 29f, window.width - 55f, 20f),
                "F6 hide  |  End reset  |  Left Y VR menu", smallStyle);
            GUI.Label(new Rect(window.width - 20f, window.height - 25f, 18f, 18f), "+", textStyle);

            var grip = new Rect(window.width - 31f, window.height - 32f, 31f, 32f);
            var e = Event.current;
            if (e.type == EventType.MouseDown && grip.Contains(e.mousePosition) && e.button == 0)
            {
                resizing = true;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && resizing)
            {
                window.width = Mathf.Clamp(window.width + e.delta.x, 390f, 980f);
                window.height = Mathf.Clamp(window.height + e.delta.y, 355f, 880f);
                e.Use();
            }
            if (e.type == EventType.MouseUp)
                resizing = false;
            GUI.DragWindow(new Rect(0f, 0f, window.width - 34f, 45f));
        }

        private void InitializeStyles()
        {
            if (windowStyle != null) return;
            background = Pixel(new Color(0.025f, 0.03f, 0.04f, 0.98f));
            row = Pixel(new Color(0.075f, 0.09f, 0.12f));
            lit = Pixel(new Color(0.085f, 0.20f, 0.24f));
            hover = Pixel(new Color(0.15f, 0.21f, 0.27f));
            white = Pixel(Color.white);

            windowStyle = new GUIStyle(GUI.skin.box);
            windowStyle.normal.background = background;
            windowStyle.border = new RectOffset(1, 1, 1, 1);
            windowStyle.padding = new RectOffset(0, 0, 0, 0);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontSize = 26;
            titleStyle.fontStyle = FontStyle.Bold;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 11;
            smallStyle.normal.textColor = new Color(0.65f, 0.72f, 0.76f);
            smallStyle.clipping = TextClipping.Clip;

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 13;
            textStyle.normal.textColor = Color.white;

            lineStyle = new GUIStyle(GUI.skin.button);
            lineStyle.normal.background = row;
            lineStyle.hover.background = hover;
            lineStyle.active.background = lit;
            lineStyle.normal.textColor = Color.white;
            lineStyle.hover.textColor = Color.white;
            lineStyle.fontSize = 13;
            lineStyle.alignment = TextAnchor.MiddleLeft;
            lineStyle.padding = new RectOffset(13, 8, 5, 5);

            enabledStyle = new GUIStyle(lineStyle);
            enabledStyle.normal.background = lit;
            enabledStyle.normal.textColor = new Color(0.47f, 0.94f, 1f);
        }

        private static Texture2D Pixel(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        internal void Dispose()
        {
            if (background != null) UnityEngine.Object.Destroy(background);
            if (row != null) UnityEngine.Object.Destroy(row);
            if (lit != null) UnityEngine.Object.Destroy(lit);
            if (hover != null) UnityEngine.Object.Destroy(hover);
            if (white != null) UnityEngine.Object.Destroy(white);
        }
    }
}
