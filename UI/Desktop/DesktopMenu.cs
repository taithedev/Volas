using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Volas
{
    // Runtime Unity uGUI menu. No IMGUI, prefabs, external asset bundles or editor code.
    // All feature actions go through the existing FeatureRegistry.
    internal sealed class DesktopMenu
    {
        private readonly VolasPlugin plugin;
        private readonly GameObject root;
        private readonly Canvas canvas;
        private readonly Image windowBackground;
        private readonly Image innerBackground;
        private float lastOpacity = -1f;
        private StandaloneInputModule addedInputModule;
        private GameObject createdEventSystem;
        private readonly RectTransform canvasRect;
        private readonly RectTransform window;
        private readonly RectTransform pageRoot;
        private readonly RectTransform navigationContent;
        private readonly Text statusText;
        private readonly Text hudText;
        private readonly Image crosshairH;
        private readonly Image crosshairV;
        private readonly Font font;
        private readonly Sprite rounded;
        private readonly Sprite softRounded;
        private InputField searchBox;
        private string currentPage = "Home";
        private string currentQuery = "";
        private Feature selected;
        private int drawnRevision = -1;
        private string drawnPage = "";
        private bool visible;
        private bool savedCursorVisible;
        private CursorLockMode savedLockMode;
        private float lastStatusUpdate;
        private float lastHUDUpdate;
        private readonly Color ink = new Color(0.026f, 0.026f, 0.026f, .96f);
        private readonly Color headerColor = new Color(0.039f, 0.039f, 0.039f, .49f);
        private readonly Color panelColor = new Color(0.052f, 0.052f, 0.052f, .60f);
        private readonly Color hoverColor = new Color(0.116f, 0.116f, 0.116f, .45f);
        private readonly Color borderColor = new Color(0.537f, 0.537f, 0.537f, .105f);
        private readonly Color accentGray = new Color(0.536f, 0.536f, 0.536f, 1f);
        private readonly Color white = new Color(0.897f, 0.897f, 0.897f, 1f);
        private readonly Color muted = new Color(0.463f, 0.463f, 0.463f, 1f);

        private static readonly string[] Tabs =
        {
            "Settings", "Home", "Players", "VR Rig", "Movement", "Visuals",
            "Overpowered", "Master", "Global", "Gamemode", "Fun", "Plugin",
            "Zones", "Safety", "Projectiles", "Favorites", "Enabled", "All"
        };

        internal DesktopMenu(VolasPlugin owner)
        {
            plugin = owner;
            font = ResolveFont();
            rounded = MakeRoundSprite(48, 13);
            softRounded = MakeRoundSprite(48, 8);

            root = new GameObject("Volas Unity UI", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(943f, 704f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            canvasRect = root.GetComponent<RectTransform>();
            EnsureEventSystem();

            var shade = MakeImage(canvasRect, "Window Shadow", new Color(0.000f, 0.000f, 0.000f, .22f),
                rounded, true);
            Place(shade.rectTransform, 209f, 63f, 767f, 500f);
            // The shadow lives behind the main window, but moves with the window.
            window = MakeRect(canvasRect, "Volas Window");
            Place(window, owner.Settings.WindowX.Value, owner.Settings.WindowY.Value, 760f, 500f);
            window.localScale = new Vector3(
                Mathf.Clamp(owner.Settings.WindowWidth.Value / 760f, .75f, 1.65f),
                Mathf.Clamp(owner.Settings.WindowHeight.Value / 500f, .75f, 1.65f), 1f);
            shade.transform.SetParent(window, false);
            Place(shade.rectTransform, 6f, 8f, 760f, 500f);
            shade.transform.SetAsFirstSibling();

            windowBackground = MakeImage(window, "OLED graphite",
                new Color(ink.r, ink.g, ink.b, plugin.Settings.DesktopOpacity.Value),
                rounded, true);
            Stretch(windowBackground.rectTransform);
            var rim = MakeImage(window, "Subtle border", borderColor, rounded, true);
            StretchInset(rim.rectTransform, 0f);
            innerBackground = MakeImage(window, "Inner panel", new Color(ink.r, ink.g, ink.b, .98f),
                rounded, true);
            StretchInset(innerBackground.rectTransform, 1f);
            innerBackground.color = new Color(0.034f, 0.034f, 0.034f,
                plugin.Settings.DesktopOpacity.Value * .85f);
            var header = MakeImage(window, "Header strip", headerColor, softRounded, true);
            header.raycastTarget = true;
            Place(header.rectTransform, 4f, 3f, 752f, 38f);

            MakeText(window, "volas", 14f, 8f, 62f, 20f, 15, accentGray, true);
            MakeText(window, "v1.4.0", 79f, 11f, 72f, 18f, 10, muted);
            MakeText(window, "⌕", 531f, 9f, 17f, 22f, 15, accentGray);
            BuildSearch();
            MakeText(window, "⚙", 706f, 9f, 18f, 22f, 17, muted);
            MakeButton(window, "Settings hitbox", "", 697f, 6f, 30f, 28f,
                Color.clear, () => Navigate("Settings"));
            MakeText(window, "×", 734f, 9f, 18f, 20f, 17, muted);
            MakeButton(window, "Close menu", "", 728f, 4f, 25f, 28f,
                Color.clear, () => plugin.DesktopOpen = false);

            MakeImage(window, "Separator 1", borderColor, null, true).rectTransform
                .SetAsLastSibling();
            var sep1 = window.Find("Separator 1") as RectTransform;
            Place(sep1, 8f, 39f, 744f, 1f);

            RectTransform navViewport = MakeRect(window, "Navigation viewport");
            Place(navViewport, 13f, 43f, 735f, 32f);
            navViewport.gameObject.AddComponent<Image>().color = new Color(0.000f, 0.000f, 0.000f, .001f);
            var navMask = navViewport.gameObject.AddComponent<RectMask2D>();
            var scroll = navViewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            navigationContent = MakeRect(navViewport, "Navigation buttons");
            AnchorTopLeft(navigationContent);
            scroll.viewport = navViewport;
            scroll.content = navigationContent;
            BuildNavigation();

            var separator = MakeImage(window, "Separator 2", borderColor, null, true);
            Place(separator.rectTransform, 8f, 76f, 744f, 1f);

            pageRoot = MakeRect(window, "Page content");
            Place(pageRoot, 15f, 86f, 730f, 387f);
            statusText = MakeText(window, "", 17f, 473f, 704f, 19f, 9, muted);
            MakeButton(window, "Resize grip", "⌟", 736f, 474f, 19f, 23f,
                Color.clear, () => { });
            var drag = header.gameObject.AddComponent<VolasUIDrag>();
            drag.Target = window;
            drag.IsResize = false;
            drag.OnComplete = SavePosition;
            // Text and controls sit above the header. The uncovered header still drags.
            var resizeHitbox = MakeImage(window, "Resize handler", Color.clear);
            resizeHitbox.raycastTarget = true;
            Place(resizeHitbox.rectTransform, 728f, 468f, 32f, 32f);
            var resize = resizeHitbox.gameObject.AddComponent<VolasUIDrag>();
            resize.Target = window;
            resize.IsResize = true;
            resize.OnComplete = SavePosition;

            hudText = MakeText(canvasRect, "", 10f, 8f, 320f, 102f, 12, white);
            hudText.gameObject.SetActive(false);
            crosshairH = MakeImage(canvasRect, "Crosshair H", white);
            crosshairV = MakeImage(canvasRect, "Crosshair V", white);
            Center(crosshairH.rectTransform, 18f, 2f);
            Center(crosshairV.rectTransform, 2f, 18f);
            root.SetActive(false);
            window.gameObject.SetActive(false);
        }

        private static Font ResolveFont()
        {
            Font result = null;
            try { result = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
            if (result == null)
            {
                try { result = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { }
            }
            return result ?? Font.CreateDynamicFontFromOSFont("Arial", 13);
        }

        private void EnsureEventSystem()
        {
            var events = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (events == null)
            {
                createdEventSystem = new GameObject("Volas UI EventSystem",
                    typeof(EventSystem));
                UnityEngine.Object.DontDestroyOnLoad(createdEventSystem);
                events = createdEventSystem.GetComponent<EventSystem>();
            }
            // VR EventSystems do not always have a mouse input module.
            // Enable our added module only while the PC menu is visible.
            if (events.GetComponent<StandaloneInputModule>() == null)
            {
                addedInputModule = events.gameObject.AddComponent<StandaloneInputModule>();
                addedInputModule.enabled = false;
            }
        }

        private void BuildSearch()
        {
            Image fieldImage = MakeImage(window, "Search background",
                new Color(0.082f, 0.082f, 0.082f, .73f), softRounded, true);
            Place(fieldImage.rectTransform, 545f, 7f, 140f, 24f);
            fieldImage.raycastTarget = true;
            searchBox = fieldImage.gameObject.AddComponent<InputField>();
            searchBox.transition = Selectable.Transition.None;
            Text fieldText = MakeText(fieldImage.rectTransform, "Input text",
                9f, 3f, 121f, 20f, 11, white);
            fieldText.supportRichText = false;
            fieldText.gameObject.name = "Search input text";
            searchBox.textComponent = fieldText;
            Text placeholder = MakeText(fieldImage.rectTransform, "search...",
                9f, 3f, 120f, 20f, 11, muted);
            placeholder.gameObject.name = "Search placeholder";
            searchBox.placeholder = placeholder;
            searchBox.onValueChanged.AddListener(value =>
            {
                currentQuery = value ?? "";
                RedrawPage();
            });
        }

        private void BuildNavigation()
        {
            float x = 0f;
            foreach (string tab in Tabs)
            {
                string chosen = tab;
                float width = Mathf.Max(29f, tab.Length * 5.6f + 9f);
                bool active = tab == currentPage;
                var btn = MakeButton(navigationContent, "Tab " + tab,
                    tab.ToLowerInvariant(), x, 2f, width, 29f,
                    Color.clear, () => Navigate(chosen), active ? white : muted,
                    active ? 10 : 9, active);
                Text label = btn.GetComponentInChildren<Text>();
                if (label != null) label.alignment = TextAnchor.MiddleCenter;
                if (active)
                {
                    var under = MakeImage(btn.transform as RectTransform,
                        "Active underline", accentGray);
                    Place(under.rectTransform, 5f, 27f, width - 10f, 2f);
                }
                x += width + 1f;
            }
            navigationContent.sizeDelta = new Vector2(x, 32f);
        }

        private void Navigate(string name)
        {
            currentPage = name;
            currentQuery = "";
            if (searchBox != null && searchBox.text.Length > 0)
                searchBox.SetTextWithoutNotify("");
            selected = null;
            BuildNavigationFresh();
            RedrawPage();
        }

        private void BuildNavigationFresh()
        {
            DestroyChildren(navigationContent);
            BuildNavigation();
        }

        internal void Tick()
        {
            if (root == null) return;
            float alpha = plugin.Settings.DesktopOpacity.Value;
            if (Mathf.Abs(alpha - lastOpacity) > .001f)
            {
                lastOpacity = alpha;
                windowBackground.color = new Color(ink.r, ink.g, ink.b, alpha);
                innerBackground.color = new Color(0.034f, 0.034f, 0.034f, alpha * .85f);
            }
            bool open = plugin.DesktopOpen;
            if (open != visible)
            {
                visible = open;
                if (open)
                {
                    savedCursorVisible = Cursor.visible;
                    savedLockMode = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    if (addedInputModule != null) addedInputModule.enabled = true;
                    root.SetActive(true);
                    window.gameObject.SetActive(true);
                    RedrawPage();
                }
                else
                {
                    // Only restore the cursor state we captured on opening.
                    window.gameObject.SetActive(false);
                    if (addedInputModule != null) addedInputModule.enabled = false;
                    Cursor.lockState = savedLockMode;
                    Cursor.visible = savedCursorVisible;
                }
            }
            if (open && plugin.Features.Revision != drawnRevision)
                RedrawPage();
            if (open)
            {
                ClampWindow();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Time.unscaledTime >= lastHUDUpdate + .24f)
            {
                lastHUDUpdate = Time.unscaledTime;
                UpdateHUD();
            }
            if (open && Time.unscaledTime >= lastStatusUpdate + .4f)
            {
                lastStatusUpdate = Time.unscaledTime;
                statusText.text = plugin.Features.LastMessage;
                if (currentPage == "Home") statusText.text = "";
                else if (currentPage == "Settings" && plugin.Vr != null)
                    statusText.text = plugin.Vr.Status + "  |  Y / F7";
            }
        }

        private void UpdateHUD()
        {
            var messages = new List<string>();
            if (plugin.Effects.FpsHud)
                messages.Add("FPS  " + plugin.FramesPerSecond.ToString("0"));
            if (plugin.Effects.ClockHud)
                messages.Add("TIME " + DateTime.Now.ToString("HH:mm:ss"));
            if (plugin.Effects.PositionHud)
            {
                Vector3 p = plugin.Game.Position;
                messages.Add("XYZ " + p.x.ToString("0.0") + " " + p.y.ToString("0.0") +
                    " " + p.z.ToString("0.0"));
            }
            if (plugin.Effects.HeadingHud)
                messages.Add("YAW  " + plugin.Game.Heading.ToString("0"));
            hudText.gameObject.SetActive(messages.Count != 0);
            if (messages.Count > 0) hudText.text = string.Join("\n", messages.ToArray());
            crosshairH.gameObject.SetActive(plugin.Effects.DesktopCrosshair);
            crosshairV.gameObject.SetActive(plugin.Effects.DesktopCrosshair);
            // HUD remains visible even when the menu is closed.
            if (!visible) root.SetActive(messages.Count != 0 || plugin.Effects.DesktopCrosshair);
        }

        private void RedrawPage()
        {
            if (pageRoot == null) return;
            drawnRevision = plugin.Features.Revision;
            drawnPage = currentPage;
            DestroyChildren(pageRoot);
            if (string.IsNullOrEmpty(currentQuery) && currentPage == "Home")
                DrawHome();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Settings")
                DrawSettingsPage();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Wrist Layout")
                DrawWristLayout();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Player ESP")
                DrawMarkerSettings();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Gun Studio")
                DrawGunStudio();
            else
                DrawFeaturePage();
        }

        private void DrawHome()
        {
            string[][] cols =
            {
                new [] { "Settings", "Players", "Visuals", "Global", "Plugin", "Projectiles" },
                new [] { "Favorites", "VR Rig", "Overpowered", "Gamemode", "Zone Locked", "Custom Categories" },
                new [] { "Enabled", "Movement", "Masterclient", "Fun", "Help And Safety" }
            };
            for (int col = 0; col < 3; col++)
            {
                float x = col * 247f;
                for (int i = 0; i < cols[col].Length; i++)
                {
                    string label = cols[col][i];
                    float y = 1f + 43f * i;
                    Image highlight = MakeImage(pageRoot, "Category " + label,
                        new Color(0.087f, 0.087f, 0.087f, .10f));
                    highlight.raycastTarget = true;
                    Place(highlight.rectTransform, x, y, 237f, 38f);
                    Button button = highlight.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.ColorTint;
                    ColorBlock colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.730f, 0.730f, 0.730f, 1f);
                    colors.pressedColor = new Color(0.547f, 0.547f, 0.547f, 1f);
                    button.colors = colors;
                    string destination = label == "Masterclient" ? "Master" :
                        label == "Help And Safety" ? "Safety" :
                        label == "Zone Locked" ? "Zones" :
                        label;
                    button.onClick.AddListener(() => Navigate(destination));
                    MakeText(highlight.rectTransform, label, 7f, 8f, 223f, 22f,
                        12, new Color(0.553f, 0.553f, 0.553f, 1f), true);
                    var line = MakeImage(pageRoot, "Row divider", borderColor);
                    Place(line.rectTransform, x + 2f, y + 38f, 232f, 1f);
                }
            }
        }

        private void DrawSettingsPage()
        {
            MakeText(pageRoot, "Settings", 4f, 0f, 420f, 27f, 14, white, true);
            string[] names = { "Appearance", "Controls", "Safety" };
            float[] positions = { 0f, 247f, 494f };
            for (int i = 0; i < 3; i++)
            {
                var panel = MakeImage(pageRoot, "Settings " + names[i],
                    panelColor, softRounded, true);
                Place(panel.rectTransform, positions[i], 34f, 236f, 337f);
                MakeText(panel.rectTransform, names[i].ToLowerInvariant(),
                    13f, 9f, 208f, 23f, 11, white, true);
            }

            RectTransform appearance = pageRoot.Find("Settings Appearance") as RectTransform;
            RectTransform controls = pageRoot.Find("Settings Controls") as RectTransform;
            RectTransform safety = pageRoot.Find("Settings Safety") as RectTransform;

            MakeText(appearance, "Panel opacity", 14f, 48f, 195f, 19f, 11, muted);
            MakeSlider(appearance, "Opacity", 15f, 73f, 206f,
                .72f, 1f, plugin.Settings.DesktopOpacity.Value,
                v => plugin.Settings.DesktopOpacity.Value = v);
            MakeText(appearance, "Visual theme: OLED / grayscale",
                14f, 115f, 207f, 27f, 10, muted);
            MakeButton(appearance, "Rainbow accents",
                plugin.RainbowTheme ? "RAINBOW ON" : "RAINBOW OFF",
                14f, 161f, 207f, 33f, panelColor,
                () => { plugin.RainbowTheme = !plugin.RainbowTheme; RedrawPage(); },
                white, 10);
            MakeButton(appearance, "Restore size", "RESET WINDOW SIZE",
                14f, 207f, 207f, 32f, panelColor,
                () =>
                {
                    window.localScale = Vector3.one;
                    window.anchoredPosition = new Vector2(55f, -55f);
                    SavePosition();
                    RedrawPage();
                }, white, 10);

            MakeText(controls, "Modern: " + plugin.Settings.DesktopShortcut.Value + " / Legacy: " + plugin.Settings.LegacyDesktopShortcut.Value, 13f, 51f, 204f, 22f, 11, muted);
            MakeText(controls, "Disable all mods: End", 13f, 85f, 204f, 22f, 11, muted);
            MakeButton(controls, "Legacy menu", "OPEN LEGACY IMGUI",
                14f, 119f, 207f, 33f, panelColor,
                () => { plugin.DesktopOpen = false; plugin.LegacyDesktopOpen = true; }, white, 10);
            MakeButton(appearance, "Wrist layout", "WRIST MENU SETTINGS", 14f, 255f, 207f, 37f, panelColor, () => Navigate("Wrist Layout"), white, 10, true);
            MakeButton(controls, "Compact VR",
                plugin.Compact ? "COMPACT VR ON" : "COMPACT VR OFF",
                14f, 184f, 207f, 33f, panelColor,
                () => { plugin.Compact = !plugin.Compact; RedrawPage(); },
                white, 10);
            MakeButton(controls, "Gun styles", "OPEN GUN STUDIO",
                14f, 241f, 207f, 37f, accentGray,
                () => Navigate("Gun Studio"), white, 11, true);
            MakeButton(controls, "Physical menu", "OPEN VR MENU / TEST",
                14f, 288f, 207f, 33f, panelColor,
                () =>
                {
                    plugin.VrOpen = true;
                    plugin.DesktopOpen = false;
                }, white, 10, true);

            MakeText(safety, "Local-only features by default.",
                14f, 48f, 203f, 42f, 11, muted);
            MakeText(safety, "Movement mods require solo or a recognized MODDED room.",
                14f, 100f, 203f, 66f, 11, muted);
            MakeButton(safety, "ESP settings", "PLAYER ESP SETTINGS", 14f, 252f, 206f, 36f, panelColor, () => Navigate("Player ESP"), white, 10, true);
            MakeButton(safety, "Disable all", "PANIC / DISABLE ALL",
                14f, 197f, 206f, 40f, new Color(0.283f, 0.283f, 0.283f, .9f),
                () => { plugin.Features.DisableAll(); RedrawPage(); }, white, 11, true);
        }

        private void DrawWristLayout()
        {
            var s = plugin.Settings;
            MakeText(pageRoot, "Wrist layout", 12f, 4f, 680f, 30f, 15, white, true);
            MakeText(pageRoot, "Physical size", 14f, 50f, 320f, 22f, 12, muted);
            MakeSlider(pageRoot, "Size", 14f, 76f, 320f, .65f, 1.30f, s.VrScale.Value, v => s.VrScale.Value = v);
            MakeText(pageRoot, "Text size", 14f, 121f, 320f, 22f, 12, muted);
            MakeSlider(pageRoot, "Text", 14f, 147f, 320f, .85f, 1.25f, s.VrTextScale.Value, v => s.VrTextScale.Value = v);
            MakeText(pageRoot, "Wrist height offset", 14f, 192f, 320f, 22f, 12, muted);
            MakeSlider(pageRoot, "Height", 14f, 218f, 320f, -.24f, .24f, s.VrOffsetY.Value, v => s.VrOffsetY.Value = v);
            MakeText(pageRoot, "Wrist depth offset", 14f, 263f, 320f, 22f, 12, muted);
            MakeSlider(pageRoot, "Depth", 14f, 289f, 320f, .02f, .42f, s.VrOffsetZ.Value, v => s.VrOffsetZ.Value = v);
            MakeButton(pageRoot, "Ray", "POINTER RAY: " + (s.VrPointerRay.Value ? "ON" : "OFF"), 365f, 50f, 340f, 42f, panelColor, () => { s.VrPointerRay.Value = !s.VrPointerRay.Value; RedrawPage(); }, white, 11);
            MakeButton(pageRoot, "Notices", "NOTIFICATIONS: " + (s.VrNotifications.Value ? "ON" : "OFF"), 365f, 100f, 340f, 42f, panelColor, () => { s.VrNotifications.Value = !s.VrNotifications.Value; RedrawPage(); }, white, 11);
            MakeButton(pageRoot, "Reset", "RESET WRIST LAYOUT", 365f, 150f, 340f, 42f, panelColor, () => { s.VrScale.Value = 1f; s.VrTextScale.Value = 1f; s.VrOffsetX.Value = 0f; s.VrOffsetY.Value = .06f; s.VrOffsetZ.Value = .16f; plugin.Compact = false; RedrawPage(); }, white, 11);
            MakeButton(pageRoot, "Back", "BACK", 365f, 200f, 340f, 42f, panelColor, () => Navigate("Settings"), white, 11);
        }

        private void DrawMarkerSettings()
        {
            MakeText(pageRoot, "Player ESP / MODDED rooms only", 12f, 5f, 680f, 28f, 15, white, true);
            MakeText(pageRoot, "Maximum range (5–60 m)", 14f, 52f, 330f, 22f, 11, muted);
            MakeSlider(pageRoot, "Range", 14f, 78f, 330f, 5f, 60f, plugin.Settings.MarkerRange.Value, v => plugin.Settings.MarkerRange.Value = v);
            MakeText(pageRoot, "Refresh interval (.10–.75 s)", 14f, 122f, 330f, 22f, 11, muted);
            MakeSlider(pageRoot, "Refresh", 14f, 148f, 330f, .10f, .75f, plugin.Settings.MarkerRefresh.Value, v => plugin.Settings.MarkerRefresh.Value = v);
            MakeText(pageRoot, "Marker limit (1–12)", 14f, 192f, 330f, 22f, 11, muted);
            MakeSlider(pageRoot, "Limit", 14f, 218f, 330f, 1f, 12f, plugin.Settings.MarkerLimit.Value, v => plugin.Settings.MarkerLimit.Value = Mathf.RoundToInt(v));
            MakeText(pageRoot, "Line width (.005–.04 m)", 14f, 262f, 330f, 22f, 11, muted);
            MakeSlider(pageRoot, "Width", 14f, 288f, 330f, .005f, .04f, plugin.Settings.MarkerWidth.Value, v => plugin.Settings.MarkerWidth.Value = v);
            MakeButton(pageRoot, "Back", "BACK TO SETTINGS", 390f, 50f, 250f, 40f, panelColor, () => Navigate("Settings"), white, 11);
            MakeButton(pageRoot, "Visuals", "OPEN VISUAL FEATURES", 390f, 100f, 250f, 40f, panelColor, () => Navigate("Visuals"), white, 11);
        }

        private void DrawGunStudio()
        {
            MakeText(pageRoot, "Gun Studio", 4f, 0f, 420f, 27f, 14, white, true);
            RectTransform[] panels = new RectTransform[3];
            string[] headings = { "Pointer", "Reticle", "Controls" };
            for (int i = 0; i < 3; i++)
            {
                Image bg = MakeImage(pageRoot, headings[i], panelColor, softRounded, true);
                Place(bg.rectTransform, i * 247f, 34f, 236f, 337f);
                panels[i] = bg.rectTransform;
                MakeText(panels[i], headings[i], 14f, 9f, 207f, 24f, 12, white, true);
            }

            var cfg = plugin.Settings;
            MakeText(panels[0], "Maximum pointer range", 14f, 42f, 205f, 20f,
                11, muted);
            MakeSlider(panels[0], "Range", 14f, 70f, 206f,
                10f, 250f, cfg.GunDistance.Value, v => cfg.GunDistance.Value = v);
            MakeText(panels[0], "Beam width", 14f, 110f, 205f, 20f, 11, muted);
            MakeSlider(panels[0], "Beam width", 14f, 137f, 206f,
                .002f, .06f, cfg.GunBeamWidth.Value,
                v => cfg.GunBeamWidth.Value = v);
            MakeButton(panels[0], "Beam style", "BEAM " +
                new [] { "STRAIGHT", "WAVE", "SPIRAL" }[cfg.GunBeamStyle.Value],
                14f, 198f, 208f, 36f, panelColor,
                () =>
                {
                    cfg.GunBeamStyle.Value = (cfg.GunBeamStyle.Value + 1) % 3;
                    RedrawPage();
                }, white, 11);

            MakeText(panels[1], "Reticle size", 14f, 42f, 205f, 20f, 11, muted);
            MakeSlider(panels[1], "Reticle size", 14f, 69f, 207f,
                .025f, .20f, cfg.GunReticleSize.Value,
                v => cfg.GunReticleSize.Value = v);
            MakeButton(panels[1], "Reticle style", "RETICLE " +
                new [] { "ORB", "BOX", "RING", "GEM" }[cfg.GunReticleStyle.Value],
                14f, 124f, 208f, 36f, panelColor,
                () =>
                {
                    cfg.GunReticleStyle.Value = (cfg.GunReticleStyle.Value + 1) % 4;
                    RedrawPage();
                }, white, 11);
            MakeButton(panels[1], "Rainbow pointer",
                cfg.GunRainbow.Value ? "RAINBOW GUN ON" : "RAINBOW GUN OFF",
                14f, 178f, 208f, 36f,
                cfg.GunRainbow.Value ? accentGray : panelColor,
                () => { cfg.GunRainbow.Value = !cfg.GunRainbow.Value; RedrawPage(); },
                white, 11);
            MakeButton(panels[1], "Glow",
                cfg.GunGlow.Value ? "GLOW ON" : "GLOW OFF",
                14f, 232f, 208f, 32f, panelColor,
                () => { cfg.GunGlow.Value = !cfg.GunGlow.Value; RedrawPage(); },
                white, 10);

            MakeButton(panels[2], "Left hand",
                cfg.GunLeftHand.Value ? "LEFT HAND" : "RIGHT HAND",
                14f, 53f, 208f, 36f, panelColor,
                () => { cfg.GunLeftHand.Value = !cfg.GunLeftHand.Value; RedrawPage(); },
                white, 11);
            MakeButton(panels[2], "Forward aim",
                cfg.GunUseForward.Value ? "FORWARD AIM" : "DOWNHAND AIM",
                14f, 109f, 208f, 36f, panelColor,
                () => { cfg.GunUseForward.Value = !cfg.GunUseForward.Value; RedrawPage(); },
                white, 11);
            MakeButton(panels[2], "Pulse effect",
                cfg.GunPulse.Value ? "PULSE ON" : "PULSE OFF",
                14f, 165f, 208f, 36f, panelColor,
                () => { cfg.GunPulse.Value = !cfg.GunPulse.Value; RedrawPage(); },
                white, 11);
            MakeText(panels[2], "Hold the selected hand grip to aim, then fire with the trigger.",
                14f, 222f, 204f, 75f, 11, muted);
        }

                private void MakeSlider(Transform parent, string title,
            float x, float y, float width,
            float min, float max, float value, Action<float> onValue)
        {
            var rt = MakeRect(parent, title + " slider");
            Place(rt, x, y, width, 24f);
            var track = MakeImage(rt, "Track", new Color(0.143f, 0.143f, 0.143f, .9f),
                softRounded, true);
            Place(track.rectTransform, 0f, 9f, width, 6f);
            var fill = MakeImage(rt, "Fill", accentGray, softRounded, true);
            Place(fill.rectTransform, 0f, 9f, width, 6f);
            var handle = MakeImage(rt, "Handle", white, rounded, true);
            Place(handle.rectTransform, 0f, 5f, 14f, 14f);
            handle.raycastTarget = true;
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onValue(v));
        }

                private void DrawFeaturePage()
        {
            string pageName = string.IsNullOrWhiteSpace(currentQuery)
                ? currentPage : "Search results";
            var matches = GetVisibleFeatures();
            MakeText(pageRoot, pageName, 4f, 0f, 410f, 27f, 14, white, true);
            MakeText(pageRoot, matches.Count + " available", 607f, 7f, 115f, 20f,
                10, muted);

            Image leftPanel = MakeImage(pageRoot, "Mods list", panelColor, softRounded, true);
            Place(leftPanel.rectTransform, 0f, 34f, 459f, 340f);
            Image rightPanel = MakeImage(pageRoot, "Feature details", panelColor, softRounded, true);
            Place(rightPanel.rectTransform, 470f, 34f, 260f, 340f);

            MakeText(leftPanel.rectTransform, "mods", 12f, 8f, 200f, 20f, 10, muted);
            MakeText(rightPanel.rectTransform, "details", 12f, 8f, 190f, 20f, 10, muted);

            RectTransform viewport = MakeRect(leftPanel.rectTransform, "Feature scroll viewport");
            Place(viewport, 7f, 35f, 445f, 298f);
            viewport.gameObject.AddComponent<Image>().color = new Color(0.000f, 0.000f, 0.000f, .001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroller = viewport.gameObject.AddComponent<ScrollRect>();
            scroller.vertical = true;
            scroller.horizontal = false;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity = 20f;
            RectTransform list = MakeRect(viewport, "Features");
            AnchorTopLeft(list);
            list.sizeDelta = new Vector2(440f, Mathf.Max(298f, matches.Count * 57f));
            scroller.viewport = viewport;
            scroller.content = list;

            if (selected == null || !matches.Contains(selected))
                selected = matches.Count > 0 ? matches[0] : null;
            for (int i = 0; i < matches.Count; i++)
            {
                Feature mod = matches[i];
                float y = i * 57f;
                var row = MakeImage(list, "Mod " + mod.Id,
                    selected == mod ? hoverColor : new Color(0.062f, 0.062f, 0.062f, .33f),
                    softRounded, true);
                Place(row.rectTransform, 2f, y + 1f, 431f, 53f);
                row.raycastTarget = true;
                Button choose = row.gameObject.AddComponent<Button>();
                choose.transition = Selectable.Transition.None;
                choose.onClick.AddListener(() =>
                {
                    selected = mod;
                    RedrawPage();
                });
                if (mod.Enabled)
                {
                    Image activeMark = MakeImage(row.rectTransform, "Active", accentGray);
                    Place(activeMark.rectTransform, 0f, 9f, 2f, 34f);
                }
                MakeText(row.rectTransform, mod.Label, 12f, 5f, 272f, 20f,
                    11, white, true);
                MakeText(row.rectTransform, Truncate(mod.Description, 65),
                    12f, 27f, 295f, 20f, 10, muted);
                var fav = MakeButton(row.rectTransform, "Favorite",
                    plugin.Features.IsFavorite(mod) ? "★" : "☆",
                    316f, 9f, 31f, 31f, Color.clear, () =>
                    {
                        plugin.Features.ToggleFavorite(mod);
                        RedrawPage();
                    }, plugin.Features.IsFavorite(mod) ? accentGray : muted, 17);
                MakeButton(row.rectTransform, "Toggle " + mod.Id,
                    mod.ActionOnly ? "RUN" : mod.Enabled ? "ON" : "OFF",
                    353f, 13f, 63f, 28f,
                    mod.Enabled ? accentGray : new Color(0.123f, 0.123f, 0.123f, 1f),
                    () =>
                    {
                        plugin.Features.Activate(mod);
                        selected = mod;
                        RedrawPage();
                    }, white, 10, true);
            }
            if (matches.Count == 0)
                MakeText(leftPanel.rectTransform, "No working mods registered in this category.",
                    17f, 70f, 410f, 50f, 12, muted);
            DrawDetails(rightPanel.rectTransform);
        }

        private void DrawDetails(RectTransform container)
        {
            if (selected == null)
            {
                MakeText(container, "Choose a category with available features.",
                    14f, 55f, 230f, 90f, 12, muted);
            }
            else
            {
                MakeText(container, selected.Label, 14f, 43f, 230f, 48f,
                    14, white, true);
                Text description = MakeText(container, selected.Description,
                    14f, 92f, 231f, 105f, 11, muted);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                MakeText(container, selected.ActionOnly ? "INSTANT ACTION" :
                        selected.Enabled ? "ENABLED" : "DISABLED",
                    14f, 210f, 216f, 22f, 11,
                    selected.Enabled ? accentGray : muted, true);
                MakeButton(container, "Action",
                    selected.ActionOnly ? "RUN FEATURE" :
                    selected.Enabled ? "DISABLE" : "ENABLE",
                    13f, 250f, 234f, 36f,
                    accentGray, () =>
                    {
                        plugin.Features.Activate(selected);
                        RedrawPage();
                    }, white, 12, true);
            }

            if (currentPage == "Settings")
            {
                MakeButton(container, "Disable all", "PANIC / DISABLE ALL",
                    13f, 294f, 234f, 32f,
                    new Color(0.163f, 0.163f, 0.163f, 1f),
                    () =>
                    {
                        plugin.Features.DisableAll();
                        RedrawPage();
                    }, white, 10, true);
            }
        }

        private List<Feature> GetVisibleFeatures()
        {
            var all = plugin.Features.All;
            if (!string.IsNullOrWhiteSpace(currentQuery))
            {
                string s = currentQuery.Trim();
                return all.FindAll(f =>
                    f.Label.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.Id.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.Category.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.Description.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            switch (currentPage)
            {
                case "All": return new List<Feature>(all);
                case "Favorites": return plugin.Features.InCategory("Favorites");
                case "Enabled": return all.FindAll(f => f.Enabled);
                case "Players": return plugin.Features.InCategory("Telemetry");
                case "Projectiles": return plugin.Features.InCategory("Gun Mods");
                case "Fun": return plugin.Features.InCategory("Fun");
                case "VR Rig": return plugin.Features.InCategory("VR Rig");
                case "Master": return plugin.Features.InCategory("Master");
                case "Safety": return plugin.Features.InCategory("Safety");
                case "Plugin": return plugin.Features.InCategory("Settings");
                default: return plugin.Features.InCategory(currentPage);
            }
        }

        private void ClampWindow()
        {
            Vector2 p = window.anchoredPosition;
            Rect area = canvasRect.rect;
            p.x = Mathf.Clamp(p.x, -window.sizeDelta.x * window.localScale.x + 160f, Mathf.Max(0f, area.width - 160f));
            p.y = Mathf.Clamp(p.y, -Mathf.Max(0f, area.height - 42f), 0f);
            window.anchoredPosition = p;
        }

        private void SavePosition()
        {
            ClampWindow();
            Vector2 position = window.anchoredPosition;
            plugin.Settings.WindowX.Value = position.x;
            plugin.Settings.WindowY.Value = -position.y;
            plugin.Settings.WindowWidth.Value = window.sizeDelta.x * window.localScale.x;
            plugin.Settings.WindowHeight.Value = window.sizeDelta.y * window.localScale.y;
        }

        internal void Dispose()
        {
            if (visible)
            {
                Cursor.lockState = savedLockMode;
                Cursor.visible = savedCursorVisible;
            }
            if (root != null) UnityEngine.Object.Destroy(root);
            if (createdEventSystem != null)
                UnityEngine.Object.Destroy(createdEventSystem);
            else if (addedInputModule != null)
                UnityEngine.Object.Destroy(addedInputModule);
            if (rounded != null)
            {
                var texture = rounded.texture;
                UnityEngine.Object.Destroy(rounded);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
            if (softRounded != null)
            {
                var texture = softRounded.texture;
                UnityEngine.Object.Destroy(softRounded);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }

        private static string Truncate(string s, int length)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > length ? s.Substring(0, length - 3) + "..." : s;
        }

        private static void DestroyChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static RectTransform MakeRect(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            AnchorTopLeft(rt);
            return rt;
        }

        private static void AnchorTopLeft(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.localScale = Vector3.one;
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            AnchorTopLeft(rt);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchInset(RectTransform rt, float inset)
        {
            Stretch(rt);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = new Vector2(.5f, .5f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        private static Image MakeImage(Transform parent, string name, Color color,
            Sprite sprite = null, bool sliced = false)
        {
            RectTransform rect = MakeRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null && sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private Text MakeText(Transform parent, string value,
            float x, float y, float w, float h,
            int fontSize, Color color, bool bold = false)
        {
            var rect = MakeRect(parent, "Text " + value);
            Place(rect, x, y, w, h);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button MakeButton(Transform parent, string name, string title,
            float x, float y, float w, float h, Color background,
            Action callback, Color? textColor = null, int size = 11, bool bold = false)
        {
            var image = MakeImage(parent, name, background, softRounded, true);
            Place(image.rectTransform, x, y, w, h);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock tint = button.colors;
            tint.normalColor = Color.white;
            tint.highlightedColor = new Color(.88f, .88f, .88f, 1f);
            tint.pressedColor = new Color(.70f, .70f, .70f, 1f);
            tint.fadeDuration = .10f;
            button.colors = tint;
            if (callback != null) button.onClick.AddListener(() => callback());
            if (!string.IsNullOrEmpty(title))
            {
                var t = MakeText(image.rectTransform, title, 0f, 0f,
                    w, h, size, textColor ?? white, bold);
                t.alignment = TextAnchor.MiddleCenter;
            }
            return button;
        }

        private static Sprite MakeRoundSprite(int size, float radius)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            float half = (size - 1f) / 2f;
            float inside = half - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - half) - inside);
                    float dy = Mathf.Max(0f, Mathf.Abs(y - half) - inside);
                    float alpha = Mathf.Clamp01(radius + .5f -
                        Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, new Color(1.000f, 1.000f, 1.000f, alpha));
                }
            }
            t.Apply();
            float border = Mathf.Ceil(radius + 2f);
            return Sprite.Create(t, new Rect(0, 0, size, size),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }
    }

    // Handles moving/resizing without intercepting unrelated feature buttons.
    internal sealed class VolasUIDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal RectTransform Target;
        internal bool IsResize;
        internal Action OnComplete;
        private Vector2 start;
        private Vector2 originalPosition;
        private Vector2 originalSize;
        private Vector3 originalScale;

        public void OnBeginDrag(PointerEventData e)
        {
            if (Target == null) return;
            start = e.position;
            originalPosition = Target.anchoredPosition;
            originalSize = Target.sizeDelta;
            originalScale = Target.localScale;
        }

        public void OnDrag(PointerEventData e)
        {
            if (Target == null) return;
            float scale = Target.GetComponentInParent<Canvas>().scaleFactor;
            if (scale < .01f) scale = 1f;
            Vector2 delta = (e.position - start) / scale;
            if (IsResize)
            {
                Target.localScale = new Vector3(
                    Mathf.Clamp((originalSize.x * originalScale.x + delta.x) /
                        originalSize.x, .75f, 1.65f),
                    Mathf.Clamp((originalSize.y * originalScale.y - delta.y) /
                        originalSize.y, .75f, 1.65f), 1f);
            }
            else
            {
                Target.anchoredPosition = originalPosition + delta;
            }
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (OnComplete != null) OnComplete();
        }
    }
}
