using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    /// <summary>
    /// Desktop dashboard modeled after the supplied three-column reference:
    /// wide glass-black surface, compact top tabs, search, favorites and enabled lists.
    /// All controls call the same FeatureRegistry as the VR wrist menu.
    /// </summary>
    internal sealed class DesktopMenu
    {
        private readonly VolasPlugin plugin;
        private Rect window;
        private Vector2 navigationScroll;
        private Vector2 featureScroll;
        private Vector2 rightScroll;
        private string page = "Home";
        private string search = "";
        private Feature selectedFeature;
        private bool resizing;
        private float underlineX = -1f;
        private float underlineWidth = 42f;
        private float lastAppliedOpacity = -1f;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Texture2D whitePixel;
        private Texture2D windowTexture;
        private GUIStyle backgroundStyle;
        private GUIStyle textSmall;
        private GUIStyle textDim;
        private GUIStyle textNormal;
        private GUIStyle textBold;
        private GUIStyle textTitle;
        private GUIStyle textMono;
        private GUIStyle searchField;
        private GUIStyle noStyle;

        private static readonly Color Ink = new Color(0.022f, 0.024f, 0.031f, 0.96f);
        private static readonly Color Header = new Color(0.035f, 0.037f, 0.046f, 0.98f);
        private static readonly Color Surface = new Color(0.052f, 0.056f, 0.068f, 0.90f);
        private static readonly Color Hover = new Color(0.12f, 0.125f, 0.145f, 0.97f);
        private static readonly Color Border = new Color(1f, 1f, 1f, 0.11f);
        private static readonly Color Muted = new Color(0.59f, 0.60f, 0.64f);
        private static readonly Color White = new Color(0.98f, 0.98f, 1f);
        private static readonly Color Soft = new Color(0.82f, 0.83f, 0.86f);

        internal DesktopMenu(VolasPlugin owner)
        {
            plugin = owner;
            window = new Rect(
                owner.Settings.WindowX.Value, owner.Settings.WindowY.Value,
                Mathf.Max(770f, owner.Settings.WindowWidth.Value),
                Mathf.Max(520f, owner.Settings.WindowHeight.Value));
        }

        internal void Draw()
        {
            EnsureStyles();
            UpdateOpacity();
            DrawTelemetry();
            if (plugin.Effects.DesktopCrosshair)
            {
                Fill(new Rect(Screen.width / 2f - 1, Screen.height / 2f - 9, 2, 18), White);
                Fill(new Rect(Screen.width / 2f - 9, Screen.height / 2f - 1, 18, 2), White);
            }

            if (!plugin.DesktopOpen) return;

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                window.width = Mathf.Min(window.width, Mathf.Max(550, Screen.width - 18));
                window.height = Mathf.Min(window.height, Mathf.Max(360, Screen.height - 18));
            }

            window.x = Mathf.Clamp(window.x, 0f, Mathf.Max(0f, Screen.width - 90f));
            window.y = Mathf.Clamp(window.y, 0f, Mathf.Max(0f, Screen.height - 42f));
            window = GUI.Window(89273, window, WindowContents, "", backgroundStyle);
            if (Event.current.type == EventType.MouseUp) SaveWindow();
        }

        private void WindowContents(int id)
        {
            float w = window.width;
            float h = window.height;
            Fill(new Rect(0f, 0f, w, 36f), Header);
            Fill(new Rect(0f, 0f, w, 2f), White);
            DrawText(new Rect(18f, 7f, 98f, 27f), "volas", textTitle);
            DrawText(new Rect(110f, 11f, 104f, 21f), "v1.4  /  PC", textDim);

            float rightSpace = 205f;
            DrawText(new Rect(w - rightSpace - 23, 11, 20, 20), "⌕", textNormal);
            search = GUI.TextField(new Rect(w - rightSpace, 6, 145, 25), search ?? "", searchField);
            if (Button(new Rect(w - 57f, 6f, 26f, 24f), "SET", true))
                Navigate("Settings");
            if (Button(new Rect(w - 29f, 6f, 23f, 24f), "x", true))
                plugin.DesktopOpen = false;

            Fill(new Rect(0, 37, w, 1), Border);
            DrawNavigation(new Rect(16, 41, w - 32, 30));
            Fill(new Rect(0, 74, w, 1), Border);

            Rect body = new Rect(15, 85, w - 30, h - 118);
            if (!string.IsNullOrWhiteSpace(search))
                DrawCategoryView(body, "Search", SearchResults());
            else if (page == "Home")
                DrawHome(body);
            else if (page == "Enabled")
                DrawCategoryView(body, page, EnabledFeatures());
            else
                DrawCategoryView(body, page, plugin.Features.InCategory(page));

            Fill(new Rect(0, h - 29, w, 1), Border);
            DrawText(new Rect(16, h - 24, w - 176, 20),
                plugin.Features.LastMessage, textDim);
            DrawText(new Rect(w - 182, h - 24, 164, 20),
                "F6 HIDE  /  END RESET", textDim);
            Fill(new Rect(w - 16, h - 9, 11, 1), Soft);
            Fill(new Rect(w - 11, h - 14, 1, 11), Soft);

            var grip = new Rect(w - 30f, h - 29f, 30f, 29f);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && grip.Contains(e.mousePosition))
            {
                resizing = true;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && resizing)
            {
                window.width = Mathf.Clamp(window.width + e.delta.x, 550f, Mathf.Max(550, Screen.width - 12));
                window.height = Mathf.Clamp(window.height + e.delta.y, 360f, Mathf.Max(360, Screen.height - 12));
                e.Use();
            }
            if (e.type == EventType.MouseUp)
                resizing = false;

            GUI.DragWindow(new Rect(0, 0, w - 230f, 36f));
        }

        private void DrawNavigation(Rect area)
        {
            var tabs = new List<string> { "Home", "All", "Favorites", "Enabled" };
            foreach (string name in plugin.Features.Categories)
                if (!tabs.Contains(name)) tabs.Add(name);
            float total = 6;
            foreach (var tab in tabs) total += Mathf.Max(61f, tab.Length * 7.1f + 24f);

            navigationScroll = GUI.BeginScrollView(area, navigationScroll,
                new Rect(0, 0, Mathf.Max(total, area.width - 2), 29), false, false);
            float x = 0;
            float targetX = 0;
            float targetWidth = 48;
            foreach (string tab in tabs)
            {
                float width = Mathf.Max(61f, tab.Length * 7.1f + 24f);
                bool active = string.IsNullOrEmpty(search) && page == tab;
                if (GUI.Button(new Rect(x, 0, width - 5, 26), tab.ToLowerInvariant(), noStyle))
                    Navigate(tab);
                DrawText(new Rect(x + 7, 1, width - 10, 21), tab.ToLowerInvariant(),
                    active ? textBold : textDim);
                if (active)
                {
                    targetX = x + 7;
                    targetWidth = width - 19;
                }
                x += width;
            }
            float blend = Mathf.Clamp01(Time.unscaledDeltaTime * 13f);
            if (underlineX < 0f) { underlineX = targetX; underlineWidth = targetWidth; }
            underlineX = Mathf.Lerp(underlineX, targetX, blend);
            underlineWidth = Mathf.Lerp(underlineWidth, targetWidth, blend);
            if (string.IsNullOrWhiteSpace(search))
                Fill(new Rect(underlineX, 27, underlineWidth, 2), White);
            GUI.EndScrollView();
        }

        private void DrawHome(Rect area)
        {
            DrawText(new Rect(area.x + 5, area.y - 2, 400, 29), "Home", textBold);
            DrawText(new Rect(area.x + 5, area.y + 27, 500, 23),
                "Your Volas dashboard. Choose a category or toggle a saved mod.", textDim);

            float top = area.y + 60f;
            float gap = 10;
            float colWidth = (area.width - gap * 2f) / 3f;
            Rect left = new Rect(area.x, top, colWidth, area.height - 63);
            Rect middle = new Rect(area.x + colWidth + gap, top, colWidth, area.height - 63);
            Rect right = new Rect(area.x + 2f * (colWidth + gap), top, colWidth, area.height - 63);
            Panel(left);
            Panel(middle);
            Panel(right);

            ColumnTitle(left, "CATEGORIES", plugin.Features.Categories.Count.ToString());
            float y = left.y + 43;
            foreach (string name in plugin.Features.Categories)
            {
                if (name == "All" || name == "Favorites") continue;
                if (y + 34 > left.yMax - 5) break;
                if (HomeRow(new Rect(left.x + 8, y, left.width - 16, 34), name, ">"))
                    Navigate(name);
                y += 43;
            }

            var favorites = plugin.Features.InCategory("Favorites");
            ColumnTitle(middle, "FAVORITES", favorites.Count.ToString());
            y = middle.y + 43;
            if (favorites.Count == 0)
            {
                DrawText(new Rect(middle.x + 15, y + 8, middle.width - 30, 48),
                    "Star mods to pin them here.", textDim);
            }
            foreach (var f in favorites)
            {
                if (y + 34 > middle.yMax - 5) break;
                if (HomeRow(new Rect(middle.x + 8, y, middle.width - 16, 34),
                    f.Label, f.Enabled ? "ON" : "OFF"))
                    plugin.Features.Activate(f);
                y += 43;
            }

            var enabled = EnabledFeatures();
            ColumnTitle(right, "ENABLED", enabled.Count.ToString());
            y = right.y + 43;
            if (enabled.Count == 0)
            {
                DrawText(new Rect(right.x + 15, y + 8, right.width - 30, 48),
                    "No active mods.", textDim);
            }
            foreach (var f in enabled)
            {
                if (y + 34 > right.yMax - 5) break;
                if (HomeRow(new Rect(right.x + 8, y, right.width - 16, 34),
                    f.Label, "ON"))
                    plugin.Features.Activate(f);
                y += 43;
            }

            if (GUI.Button(new Rect(right.x + 8, right.yMax - 46, right.width - 16, 31),
                "DISABLE ALL", noStyle))
                plugin.Features.DisableAll();
            DrawText(new Rect(right.x + 17, right.yMax - 43, right.width - 34, 24),
                "DISABLE ALL", textSmall);
        }

        private void DrawCategoryView(Rect area, string category, List<Feature> features)
        {
            if (selectedFeature == null || !features.Contains(selectedFeature))
                selectedFeature = features.Count > 0 ? features[0] : null;

            DrawText(new Rect(area.x + 4, area.y - 2, area.width - 10, 30),
                category == "Search" ? "Search results" : category, textBold);
            DrawText(new Rect(area.x + 4, area.y + 27, area.width - 10, 21),
                features.Count + " available  /  click a mod to toggle it", textDim);

            float top = area.y + 57;
            float gap = 12;
            float leftWidth = area.width * 0.61f;
            Rect list = new Rect(area.x, top, leftWidth, area.height - 58);
            Rect details = new Rect(list.xMax + gap, top, area.width - leftWidth - gap, area.height - 58);
            Panel(list);
            Panel(details);
            ColumnTitle(list, "MODS", features.Count.ToString());
            ColumnTitle(details, "DETAILS", "VOLAS");

            float rowHeight = 69f;
            Rect listViewport = new Rect(list.x + 7, list.y + 37, list.width - 14, list.height - 44);
            featureScroll = GUI.BeginScrollView(listViewport, featureScroll,
                new Rect(0, 0, listViewport.width - 16, Mathf.Max(listViewport.height, features.Count * rowHeight + 3)));
            float rowWidth = Mathf.Max(220, listViewport.width - 18);
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                float y = i * rowHeight + 2;
                Rect row = new Rect(2, y, rowWidth - 7, 61);
                Fill(row, f.Enabled ? new Color(0.14f, 0.145f, 0.16f, 0.88f) : Surface);
                if (GUI.Button(new Rect(row.x, row.y, row.width - 83, row.height), "", noStyle))
                {
                    selectedFeature = f;
                    plugin.Features.Activate(f);
                }
                DrawText(new Rect(row.x + 11, y + 6, row.width - 99, 21), f.Label, textNormal);
                DrawText(new Rect(row.x + 11, y + 30, row.width - 101, 22),
                    ClipText(f.Description, 51), textDim);
                if (GUI.Button(new Rect(row.xMax - 76, y + 9, 30, 43), "", noStyle))
                    plugin.Features.ToggleFavorite(f);
                DrawText(new Rect(row.xMax - 69, y + 13, 28, 20),
                    plugin.Features.IsFavorite(f) ? "*" : "+", textBold);
                if (GUI.Button(new Rect(row.xMax - 42, y + 8, 37, 44), "", noStyle))
                {
                    selectedFeature = f;
                    plugin.Features.Activate(f);
                }
                Fill(new Rect(row.xMax - 41, y + 19, 33, 22),
                    f.Enabled ? White : new Color(0.19f, 0.20f, 0.22f, 1f));
                DrawText(new Rect(row.xMax - 38, y + 18, 32, 24),
                    f.ActionOnly ? "GO" : f.Enabled ? "ON" : "OFF",
                    f.Enabled ? textMono : textSmall);
            }
            GUI.EndScrollView();
            DrawDetails(details, category);
        }

        private void DrawDetails(Rect box, string category)
        {
            Rect area = new Rect(box.x + 13, box.y + 43, box.width - 26, box.height - 55);
            rightScroll = GUI.BeginScrollView(area, rightScroll,
                new Rect(0, 0, area.width - 15, 850));
            float w = Mathf.Max(130, area.width - 18);
            float y = 0;
            if (selectedFeature != null)
            {
                DrawText(new Rect(0, y, w, 28), selectedFeature.Label, textBold); y += 30;
                DrawText(new Rect(0, y, w, 44), selectedFeature.Description, textSmall); y += 50;
                DrawText(new Rect(0, y, w, 23),
                    "STATUS  " + (selectedFeature.Enabled ? "ENABLED" : "DISABLED"), textDim); y += 30;
                if (ClickLabel(new Rect(0, y, w, 34),
                    selectedFeature.ActionOnly ? "RUN FEATURE" :
                    selectedFeature.Enabled ? "DISABLE FEATURE" : "ENABLE FEATURE", true))
                    plugin.Features.Activate(selectedFeature);
                y += 43;
            }
            if (category == "Gun Mods" || category == "Settings")
            {
                DrawText(new Rect(0, y, w, 23), "APPEARANCE & CONTROLS", textSmall); y += 26;
                if (category == "Gun Mods")
                {
                    y = SettingsSlider(y, w, "POINTER RANGE", 10f, 250f,
                        plugin.Settings.GunDistance.Value, v => plugin.Settings.GunDistance.Value = v);
                    y = SettingsSlider(y, w, "BEAM WIDTH", 0.002f, 0.06f,
                        plugin.Settings.GunBeamWidth.Value, v => plugin.Settings.GunBeamWidth.Value = v);
                    y = SettingsSlider(y, w, "RETICLE SIZE", 0.025f, 0.20f,
                        plugin.Settings.GunReticleSize.Value, v => plugin.Settings.GunReticleSize.Value = v);
                    y = SettingsSlider(y, w, "STAMP SIZE", 0.5f, 2.5f,
                        plugin.Settings.GunMarkScale.Value, v => plugin.Settings.GunMarkScale.Value = v);
                    y = SettingsSlider(y, w, "STAMP LIFETIME", 3f, 120f,
                        plugin.Settings.GunMarkLifetime.Value, v => plugin.Settings.GunMarkLifetime.Value = v);
                    DrawText(new Rect(0, y, w, 22), "BEAM STYLE", textDim); y += 22;
                    y = Options(y, w, new [] { "LINE", "WAVE", "SPIRAL" },
                        plugin.Settings.GunBeamStyle.Value, v => plugin.Settings.GunBeamStyle.Value = v);
                    DrawText(new Rect(0, y, w, 22), "RETICLE", textDim); y += 22;
                    y = Options(y, w, new [] { "ORB", "BOX", "RING", "GEM" },
                        plugin.Settings.GunReticleStyle.Value, v => plugin.Settings.GunReticleStyle.Value = v);
                    y = Toggle(y, w, "BEAM GLOW", plugin.Settings.GunGlow.Value,
                        v => plugin.Settings.GunGlow.Value = v);
                    y = Toggle(y, w, "PULSE", plugin.Settings.GunPulse.Value,
                        v => plugin.Settings.GunPulse.Value = v);
                    y = Toggle(y, w, "RAINBOW GUN", plugin.Settings.GunRainbow.Value,
                        v => plugin.Settings.GunRainbow.Value = v);
                    y = Toggle(y, w, "LEFT HAND", plugin.Settings.GunLeftHand.Value,
                        v => plugin.Settings.GunLeftHand.Value = v);
                    y = Toggle(y, w, "FORWARD AIM", plugin.Settings.GunUseForward.Value,
                        v => plugin.Settings.GunUseForward.Value = v);
                }
                else
                {
                    y = SettingsSlider(y, w, "PANEL OPACITY", 0.72f, 1f,
                        plugin.Settings.DesktopOpacity.Value, v => plugin.Settings.DesktopOpacity.Value = v);
                    y = SettingsSlider(y, w, "VR SIZE", 0.65f, 1.3f,
                        plugin.Settings.VrScale.Value, v => plugin.Settings.VrScale.Value = v);
                    y = SettingsSlider(y, w, "WRIST LEFT/RIGHT", -0.24f, 0.24f,
                        plugin.Settings.VrOffsetX.Value, v => plugin.Settings.VrOffsetX.Value = v);
                    y = SettingsSlider(y, w, "WRIST UP/DOWN", -0.24f, 0.24f,
                        plugin.Settings.VrOffsetY.Value, v => plugin.Settings.VrOffsetY.Value = v);
                    y = SettingsSlider(y, w, "WRIST FORWARD", 0.02f, 0.42f,
                        plugin.Settings.VrOffsetZ.Value, v => plugin.Settings.VrOffsetZ.Value = v);
                    y = Toggle(y, w, "COMPACT VR", plugin.Compact, v => plugin.Compact = v);
                    y = Toggle(y, w, "RAINBOW EFFECTS", plugin.RainbowTheme,
                        v => plugin.RainbowTheme = v);
                }
            }
            GUI.EndScrollView();
        }

        private float Options(float y, float w, string[] values, int selected, Action<int> onChange)
        {
            float segment = w / values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                int item = i;
                Rect rect = new Rect(segment * i + 1f, y, segment - 3, 29);
                Fill(rect, selected == item ? White : Surface);
                if (GUI.Button(rect, "", noStyle)) onChange(item);
                DrawText(new Rect(rect.x + 4, y + 4, rect.width - 8, 22), values[i],
                    selected == item ? textMono : textSmall);
            }
            return y + 39;
        }

        private float SettingsSlider(float y, float w, string name, float min, float max,
            float current, Action<float> onChange)
        {
            DrawText(new Rect(0, y, w, 24), name + "   " + current.ToString("0.00"), textDim);
            float v = GUI.HorizontalSlider(new Rect(4, y + 30, w - 12, 17), current, min, max);
            if (Mathf.Abs(v - current) > (max - min) * 0.001f) onChange(v);
            return y + 62;
        }

        private float Toggle(float y, float w, string label, bool value, Action<bool> onChange)
        {
            Rect rect = new Rect(0, y, w, 31);
            Fill(rect, Surface);
            if (GUI.Button(rect, "", noStyle)) onChange(!value);
            DrawText(new Rect(8, y + 4, w - 67, 22), label, textSmall);
            Fill(new Rect(w - 43, y + 7, 34, 17), value ? White : Hover);
            DrawText(new Rect(w - 40, y + 5, 35, 22), value ? "ON" : "OFF",
                value ? textMono : textSmall);
            return y + 38;
        }

        private List<Feature> SearchResults()
        {
            string term = search.Trim();
            return plugin.Features.All.FindAll(f =>
                f.Label.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.Id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.Category.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.Description.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private List<Feature> EnabledFeatures()
        {
            return plugin.Features.All.FindAll(f => f.Enabled);
        }

        private void Navigate(string name)
        {
            page = name;
            search = "";
            featureScroll = Vector2.zero;
            rightScroll = Vector2.zero;
            selectedFeature = null;
        }

        private void Panel(Rect area)
        {
            Fill(area, Surface);
            Fill(new Rect(area.x, area.y, area.width, 1), Border);
            Fill(new Rect(area.x, area.yMax - 1, area.width, 1), Border);
        }

        private void ColumnTitle(Rect area, string title, string number)
        {
            DrawText(new Rect(area.x + 13, area.y + 11, area.width - 90, 22), title, textSmall);
            DrawText(new Rect(area.xMax - 51, area.y + 11, 41, 22), number, textDim);
            Fill(new Rect(area.x + 10, area.y + 34, area.width - 20, 1), Border);
        }

        private bool HomeRow(Rect rect, string label, string status)
        {
            Vector2 mouse = Event.current.mousePosition;
            Fill(rect, rect.Contains(mouse) ? Hover : new Color(0.07f, 0.075f, 0.087f, 0.80f));
            bool clicked = GUI.Button(rect, "", noStyle);
            DrawText(new Rect(rect.x + 9, rect.y + 7, rect.width - 47, 23),
                ClipText(label, Mathf.Max(10, (int)((rect.width - 55) / 8))), textSmall);
            DrawText(new Rect(rect.xMax - 41, rect.y + 7, 39, 23), status, textDim);
            return clicked;
        }

        private bool Button(Rect rect, string label, bool subtle)
        {
            Fill(rect, subtle ? Surface : White);
            bool click = GUI.Button(rect, "", noStyle);
            DrawText(new Rect(rect.x + 3, rect.y + 2, rect.width - 5, rect.height - 2), label, textSmall);
            return click;
        }

        private bool ClickLabel(Rect rect, string label, bool solid)
        {
            Fill(rect, solid ? White : Surface);
            bool clicked = GUI.Button(rect, "", noStyle);
            DrawText(new Rect(rect.x + 12, rect.y + 7, rect.width - 24, 22),
                label, solid ? textMono : textSmall);
            return clicked;
        }

        private static string ClipText(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length > max ? value.Substring(0, Mathf.Max(2, max - 3)) + "..." : value;
        }

        private void DrawTelemetry()
        {
            var lines = new List<string>();
            if (plugin.Effects.FpsHud) lines.Add("FPS  " + plugin.FramesPerSecond.ToString("0"));
            if (plugin.Effects.ClockHud) lines.Add("TIME " + DateTime.Now.ToString("HH:mm:ss"));
            if (plugin.Effects.PositionHud)
            {
                var pos = plugin.Game.Position;
                lines.Add("XYZ " + pos.x.ToString("0.0") + " " + pos.y.ToString("0.0") + " " + pos.z.ToString("0.0"));
            }
            if (plugin.Effects.HeadingHud) lines.Add("YAW " + plugin.Game.Heading.ToString("0"));
            if (lines.Count == 0) return;
            Rect card = new Rect(Mathf.Max(0, Screen.width - 239), 12, 226, lines.Count * 22 + 14);
            Fill(card, Ink);
            for (int i = 0; i < lines.Count; i++)
                DrawText(new Rect(card.x + 9, card.y + 6 + i * 22, card.width - 16, 20),
                    lines[i], textSmall);
        }

        private void EnsureStyles()
        {
            if (whitePixel != null) return;
            whitePixel = Pixel(Color.white);
            windowTexture = Pixel(Ink);
            backgroundStyle = new GUIStyle(GUI.skin.window);
            backgroundStyle.normal.background = windowTexture;
            backgroundStyle.border = new RectOffset(0, 0, 0, 0);
            backgroundStyle.padding = new RectOffset(0, 0, 0, 0);
            textSmall = NewText(11, Soft);
            textDim = NewText(11, Muted);
            textNormal = NewText(13, White);
            textBold = NewText(15, White, true);
            textTitle = NewText(21, White, true);
            textMono = NewText(11, Color.black, true);
            noStyle = new GUIStyle(GUIStyle.none);
            searchField = new GUIStyle(GUI.skin.textField);
            searchField.fontSize = 12;
            searchField.normal.textColor = White;
            searchField.focused.textColor = White;
            searchField.normal.background = Pixel(new Color(0.09f, 0.095f, 0.11f, 1f));
            searchField.focused.background = searchField.normal.background;
            searchField.padding = new RectOffset(8, 7, 4, 4);
        }

        private static GUIStyle NewText(int size, Color color, bool bold = false)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = size;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.normal.textColor = color;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            return style;
        }

        private static Texture2D Pixel(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whitePixel);
            GUI.color = before;
        }

        private static void DrawText(Rect rect, string value, GUIStyle style)
        {
            GUI.Label(rect, value ?? "", style);
        }

        private void UpdateOpacity()
        {
            float value = plugin.Settings.DesktopOpacity.Value;
            if (Mathf.Abs(value - lastAppliedOpacity) < 0.004f) return;
            lastAppliedOpacity = value;
            windowTexture.SetPixel(0, 0, new Color(0.022f, 0.024f, 0.031f, value));
            windowTexture.Apply();
        }

        private void SaveWindow()
        {
            if (Mathf.Abs(plugin.Settings.WindowX.Value - window.x) > 1f)
                plugin.Settings.WindowX.Value = window.x;
            if (Mathf.Abs(plugin.Settings.WindowY.Value - window.y) > 1f)
                plugin.Settings.WindowY.Value = window.y;
            if (Mathf.Abs(plugin.Settings.WindowWidth.Value - window.width) > 1f)
                plugin.Settings.WindowWidth.Value = window.width;
            if (Mathf.Abs(plugin.Settings.WindowHeight.Value - window.height) > 1f)
                plugin.Settings.WindowHeight.Value = window.height;
        }

        internal void Dispose()
        {
            if (whitePixel != null) UnityEngine.Object.Destroy(whitePixel);
            if (windowTexture != null) UnityEngine.Object.Destroy(windowTexture);
            if (searchField != null && searchField.normal.background != null &&
                searchField.normal.background != whitePixel)
                UnityEngine.Object.Destroy(searchField.normal.background);
            whitePixel = null;
            windowTexture = null;
        }
    }
}
