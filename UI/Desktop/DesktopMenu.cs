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
        #region State & UI references

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
        private readonly Text pageTitle;
        private readonly VolasDesktopEffects polish;
        private readonly CanvasGroup windowFade;
        private readonly CanvasGroup pageFade;
        private float fade = 0f;
        private float pageTransition = 1f;
        private int openGeneration;
        private bool waitingForBackdrop;
        private readonly Dictionary<Image, float> accentGlows = new Dictionary<Image, float>();
        private readonly List<Image> expiredGlows = new List<Image>();
        private float lastBloomAmount = -1f;
        private ScrollRect featureScroll;
        private string scrollKey = "";
        private readonly Text hudText;
        private readonly Image crosshairH;
        private readonly Image crosshairV;
        private readonly Font font;
        private readonly Sprite rounded;
        private readonly Sprite softRounded;
        private InputField searchBox;
        private string currentPage = "Home";
        private string currentQuery = "";
        private bool searchPending;
        private float searchDue;
        private readonly List<FeatureRow> featureRows = new List<FeatureRow>();
        private readonly Dictionary<string, Image> navButtons = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> navLabels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Image> navIndicators = new Dictionary<string, Image>();
        private sealed class FeatureRow
        {
            internal Feature Mod;
            internal Image Track, Knob, Glow;
            internal Text Favorite;
            internal bool Enabled, IsFavorite;
        }
        private Feature selected;
        private int drawnRevision = -1;
        private string drawnPage = "";
        private bool visible;
        private bool savedCursorVisible;
        private CursorLockMode savedLockMode;
        private float lastStatusUpdate;
        private float lastHUDUpdate;
        #endregion

        #region Theme & navigation

        private readonly Color ink = new Color(.022f, .026f, .050f, .99f);
        private readonly Color headerColor = new Color(.025f, .030f, .050f, .85f);
        private readonly Color panelColor = new Color(.039f, .044f, .068f, .88f);
        private readonly Color hoverColor = new Color(.065f, .080f, .147f, 1f);
        private readonly Color borderColor = new Color(.14f, .15f, .22f, .35f);
        private readonly Color accentGray = new Color(.37f, .41f, .83f, 1f);
        private readonly Color white = new Color(.86f, .87f, .90f, 1f);
        private readonly Color muted = new Color(.49f, .51f, .59f, 1f);

        private static readonly string[] Tabs =
        {
            "Home", "Players", "#MODS", "Movement", "Visuals", "Gamemode",
            "Fun", "Charms", "Charm Studio", "Projectiles", "Custom Categories", "Zones", "#POWER",
            "VR Rig", "Overpowered", "Master", "Global", "#LIBRARY",
            "Local", "Favorites", "Enabled", "All", "Plugin", "Safety"
        };

        #endregion

        #region Initialization

        internal DesktopMenu(VolasPlugin owner)
        {
            plugin = owner;
            font = ResolveFont();
            rounded = MakeRoundSprite(48, 13);
            softRounded = MakeRoundSprite(32, 5);

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
            windowFade = window.gameObject.AddComponent<CanvasGroup>();
            windowFade.alpha = 0f;
            windowFade.blocksRaycasts = false;
            windowFade.interactable = false;
            polish = new VolasDesktopEffects(plugin, window, rounded);

            windowBackground = MakeImage(window, "OLED graphite",
                new Color(ink.r, ink.g, ink.b, plugin.Settings.DesktopOpacity.Value),
                rounded, true);
            Stretch(windowBackground.rectTransform);
            var rim = MakeImage(window, "Subtle border", borderColor, rounded, true);
            StretchInset(rim.rectTransform, 0f);
            innerBackground = MakeImage(window, "Inner panel", new Color(ink.r, ink.g, ink.b, .98f),
                rounded, true);
            StretchInset(innerBackground.rectTransform, 1f);
            innerBackground.color = new Color(ink.r, ink.g, ink.b,
                plugin.Settings.DesktopOpacity.Value);
            polish.BuildBackdrop();
            var sidebar = MakeImage(window, "Sidebar", new Color(.028f, .029f, .050f, .83f));
            Place(sidebar.rectTransform, 1f, 1f, 149f, 498f);
            var sidebarLine = MakeImage(window, "Sidebar divider", borderColor);
            Place(sidebarLine.rectTransform, 149f, 1f, 1f, 498f);
            AddAccentGlow(sidebar.rectTransform, 4f, 2f, 83f, 37f, .36f);
            MakeText(sidebar.rectTransform, "volas", 13f, 9f, 120f, 22f, 16, accentGray, true);
            MakeText(sidebar.rectTransform, "v1.4.0", 14f, 30f, 120f, 14f, 10, muted);
            var brandLine = MakeImage(sidebar.rectTransform, "Brand divider", borderColor);
            Place(brandLine.rectTransform, 10f, 50f, 128f, 1f);

            var header = MakeImage(window, "Header strip", headerColor);
            Place(header.rectTransform, 150f, 1f, 609f, 42f);
            header.raycastTarget = true;
            pageTitle = MakeText(window, "home", 163f, 9f, 330f, 25f, 13, white, true);
            BuildSearch();
            MakeButton(window, "Close menu", "×", 730f, 7f, 23f, 27f,
                Color.clear, () => plugin.DesktopOpen = false, muted, 17);
            var topLine = MakeImage(window, "Header divider", borderColor);
            Place(topLine.rectTransform, 159f, 42f, 591f, 1f);

            RectTransform navViewport = MakeRect(sidebar.rectTransform, "Navigation viewport");
            Place(navViewport, 8f, 54f, 133f, 402f);
            navViewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .001f);
            navViewport.gameObject.AddComponent<RectMask2D>();
            var scroll = navViewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 24f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            navigationContent = MakeRect(navViewport, "Navigation buttons");
            scroll.viewport = navViewport;
            scroll.content = navigationContent;
            BuildNavigation();

            var bottomLine = MakeImage(sidebar.rectTransform, "Footer divider", borderColor);
            Place(bottomLine.rectTransform, 10f, 461f, 128f, 1f);
            MakeButton(sidebar.rectTransform, "Settings", "⚙", 10f, 469f, 25f, 24f,
                panelColor, () => Navigate("Settings"), muted, 17);
            MakeButton(sidebar.rectTransform, "Favorites", "★", 39f, 469f, 25f, 24f,
                panelColor, () => Navigate("Favorites"), muted, 14);
            MakeButton(sidebar.rectTransform, "Effects shortcut", "FX", 68f, 469f, 31f, 24f,
                panelColor, () => Navigate("UI Effects"), muted, 10, true);
            MakeButton(sidebar.rectTransform, "VR menu", "VR", 104f, 469f, 35f, 24f,
                hoverColor, () => { plugin.VrOpen = true; plugin.DesktopOpen = false; }, white, 10, true);

            pageRoot = MakeRect(window, "Page content");
            Place(pageRoot, 160f, 50f, 730f, 512f);
            pageRoot.localScale = new Vector3(.81f, .81f, 1f);
            pageFade = pageRoot.gameObject.AddComponent<CanvasGroup>();
            statusText = MakeText(window, "", 163f, 470f, 558f, 22f, 10, muted);
            MakeButton(window, "Resize grip", "⌟", 735f, 475f, 20f, 22f,
                Color.clear, () => { }, muted, 13);
            var drag = header.gameObject.AddComponent<VolasUIDrag>();
            drag.Target = window;
            drag.IsResize = false;
            drag.OnComplete = SavePosition;
            var resizeHitbox = MakeImage(window, "Resize handler", Color.clear);
            resizeHitbox.raycastTarget = true;
            Place(resizeHitbox.rectTransform, 731f, 472f, 29f, 28f);
            var resize = resizeHitbox.gameObject.AddComponent<VolasUIDrag>();
            resize.Target = window;
            resize.IsResize = true;
            resize.OnComplete = SavePosition;


            hudText = MakeText(canvasRect, "", 10f, 8f, 340f, 190f, 12, white);
            hudText.gameObject.SetActive(false);
            crosshairH = MakeImage(canvasRect, "Crosshair H", white);
            crosshairV = MakeImage(canvasRect, "Crosshair V", white);
            Center(crosshairH.rectTransform, 18f, 2f);
            Center(crosshairV.rectTransform, 2f, 18f);
            root.SetActive(false);
            window.gameObject.SetActive(false);
        }

        #endregion

        #region Font & input system

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

        #endregion

        #region Search & sidebar navigation

        private void BuildSearch()
        {
            Image fieldImage = MakeImage(window, "Search background",
                new Color(.055f, .063f, .089f, 1f), softRounded, true);
            Place(fieldImage.rectTransform, 515f, 8f, 198f, 26f);
            MakeText(fieldImage.rectTransform, "⌕", 8f, 0f, 19f, 26f, 15, muted);
            fieldImage.raycastTarget = true;
            searchBox = fieldImage.gameObject.AddComponent<InputField>();
            searchBox.transition = Selectable.Transition.None;
            Text fieldText = MakeText(fieldImage.rectTransform, "Input text",
                28f, 2f, 162f, 22f, 11, white);
            fieldText.supportRichText = false;
            fieldText.gameObject.name = "Search input text";
            searchBox.textComponent = fieldText;
            Text placeholder = MakeText(fieldImage.rectTransform, "search...",
                28f, 2f, 162f, 22f, 11, muted);
            placeholder.gameObject.name = "Search placeholder";
            searchBox.placeholder = placeholder;
            searchBox.onValueChanged.AddListener(value =>
            {
                currentQuery = value ?? "";
                searchPending = true;
                searchDue = Time.unscaledTime + .12f;
            });
        }

        private void BuildNavigation()
        {
            float y = 0f;
            foreach (string tab in Tabs)
            {
                if (tab.StartsWith("#", StringComparison.Ordinal))
                {
                    MakeText(navigationContent, tab.Substring(1), 6f, y + 3f, 120f, 17f, 9, muted, true);
                    y += 20f;
                    continue;
                }
                string chosen = tab;
                string label = tab == "Custom Categories" ? "custom" : tab.ToLowerInvariant();
                bool active = tab == currentPage;
                var btn = MakeButton(navigationContent, "Tab " + tab, label,
                    0f, y, 133f, 23f, active ? hoverColor : panelColor,
                    () => Navigate(chosen), active ? white : muted, 11, active);
                var indicator = MakeImage(btn.transform, "Active category indicator", accentGray, softRounded, true);
                Place(indicator.rectTransform, 0f, 4f, 3f, 15f);
                indicator.enabled = active;
                navButtons[tab] = btn.GetComponent<Image>();
                navIndicators[tab] = indicator;
                Text t = btn.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.alignment = TextAnchor.MiddleLeft;
                    Place(t.rectTransform, 9f, 0f, 120f, 23f);
                    navLabels[tab] = t;
                }
                y += 26f;
            }
            navigationContent.sizeDelta = new Vector2(133f, Mathf.Max(402f, y));
        }

        private void Navigate(string name)
        {
            if (currentPage != name) pageTransition = plugin.Settings.DesktopMotion.Value ? .18f : 1f;
            currentPage = name;
            currentQuery = "";
            searchPending = false;
            if (searchBox != null && searchBox.text.Length > 0)
                searchBox.SetTextWithoutNotify("");
            selected = null;
            BuildNavigationFresh();
            RedrawPage();
        }

        private void BuildNavigationFresh()
        {
            foreach (var item in navButtons)
            {
                bool active = item.Key == currentPage;
                item.Value.color = active ? hoverColor : panelColor;
                navIndicators[item.Key].enabled = active;
                if (navLabels.TryGetValue(item.Key, out var text))
                {
                    text.color = active ? white : muted;
                    text.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        #endregion

        #region Window lifecycle

        internal void Tick()
        {
            if (root == null) return;
            float alpha = plugin.Settings.DesktopOpacity.Value;
            if (Mathf.Abs(alpha - lastOpacity) > .001f)
            {
                lastOpacity = alpha;
                windowBackground.color = new Color(ink.r, ink.g, ink.b, alpha);
                innerBackground.color = new Color(ink.r, ink.g, ink.b, alpha);
            }
            bool open = plugin.DesktopOpen;
            if (open != visible)
            {
                visible = open;
                if (open)
                {
                    int generation = ++openGeneration;
                    waitingForBackdrop = plugin.Settings.DesktopGlass.Value && !window.gameObject.activeSelf;
                    savedCursorVisible = Cursor.visible;
                    savedLockMode = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    if (addedInputModule != null) addedInputModule.enabled = true;
                    root.SetActive(true);
                    windowFade.blocksRaycasts = true;
                    windowFade.interactable = true;
                    if (drawnPage != currentPage || drawnRevision != plugin.Features.Revision || searchPending)
                    {
                        searchPending = false;
                        RedrawPage();
                    }
                    polish.Play(2);
                    if (waitingForBackdrop)
                    {
                        plugin.StartCoroutine(polish.CaptureBackdrop(() => generation == openGeneration && visible, () =>
                        {
                            if (generation != openGeneration || !visible) return;
                            waitingForBackdrop = false;
                            window.gameObject.SetActive(true);
                        }));
                    }
                    else window.gameObject.SetActive(true);
                }
                else
                {
                    // Only restore the cursor state we captured on opening.
                    ++openGeneration;
                    waitingForBackdrop = false;
                    windowFade.blocksRaycasts = false;
                    windowFade.interactable = false;
                    polish.Play(3);
                    if (addedInputModule != null) addedInputModule.enabled = false;
                    Cursor.lockState = savedLockMode;
                    Cursor.visible = savedCursorVisible;
                }
            }
            UpdatePolish(open);
            if (open && searchPending && Time.unscaledTime >= searchDue)
            {
                searchPending = false;
                RedrawPage();
            }
            else if (open && !searchPending && plugin.Features.Revision != drawnRevision)
                RefreshFeatureRows();
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
                string message = currentPage == "Home" ? "" : plugin.Features.LastMessage;
                if (currentPage == "Settings" && plugin.Vr != null) message = plugin.Vr.Status + "  |  Y / F7";
                if (currentPage == "Player ESP") message = plugin.Markers.Status;
                if (statusText.text != message) statusText.text = message;
            }
        }

        #endregion

        #region Animation & bloom

        private void UpdatePolish(bool open)
        {
            float target = open && !waitingForBackdrop ? 1f : 0f;
            float speed = target > fade ? 7f : 10f;
            fade = plugin.Settings.DesktopMotion.Value
                ? Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime * speed) : target;
            float opacity = fade * fade * (3f - 2f * fade);
            if (windowFade.alpha != opacity) windowFade.alpha = opacity;
            if (!open && fade <= 0f && window.gameObject.activeSelf)
            {
                window.gameObject.SetActive(false);
                polish.ClearBackdrop();
            }
            if (open && !waitingForBackdrop)
            {
                polish.UpdateBackdrop();
                pageTransition = plugin.Settings.DesktopMotion.Value
                    ? Mathf.MoveTowards(pageTransition, 1f, Time.unscaledDeltaTime * 6f) : 1f;
                float pageOpacity = pageTransition * pageTransition * (3f - 2f * pageTransition);
                if (pageFade.alpha != pageOpacity) pageFade.alpha = pageOpacity;
            }
            float amount = plugin.Settings.DesktopBloom.Value ? plugin.Settings.DesktopBloomStrength.Value : 0f;
            if (Mathf.Abs(amount - lastBloomAmount) > .001f)
            {
                lastBloomAmount = amount;
                foreach (var entry in accentGlows)
                {
                    if (entry.Key == null) continue;
                    var tint = entry.Key.color;
                    tint.a = amount * entry.Value;
                    entry.Key.color = tint;
                }
            }
        }

        private Image AddAccentGlow(RectTransform parent, float x, float y, float w, float h, float alpha, bool behind = false)
        {
            var image = MakeImage(parent, alpha == .36f ? "Volas logo glow" : "Accent bloom",
                new Color(.32f, .39f, 1f, plugin.Settings.DesktopBloom.Value ? alpha * plugin.Settings.DesktopBloomStrength.Value : 0f),
                polish.GlowSprite, false);
            Place(image.rectTransform, x, y, w, h);
            if (behind) image.transform.SetAsFirstSibling();
            accentGlows.Add(image, alpha);
            return image;
        }

        private void PruneGlows()
        {
            expiredGlows.Clear();
            foreach (var entry in accentGlows)
                if (entry.Key == null || !entry.Key.transform.IsChildOf(window)) expiredGlows.Add(entry.Key);
            foreach (var image in expiredGlows) accentGlows.Remove(image);
        }

        #endregion

        #region HUD

        private void UpdateHUD()
        {
            string value = plugin.Effects.LocalTelemetry();
            bool hasText = !string.IsNullOrEmpty(value);
            hudText.gameObject.SetActive(hasText);
            if (hasText && hudText.text != value) hudText.text = value;
            crosshairH.gameObject.SetActive(plugin.Effects.DesktopCrosshair);
            crosshairV.gameObject.SetActive(plugin.Effects.DesktopCrosshair);
            if (!visible) root.SetActive(window.gameObject.activeSelf || polish.AudioPlaying || hasText || plugin.Effects.DesktopCrosshair);
        }

        #endregion

        #region Page routing & home

        private void RedrawPage()
        {
            if (pageRoot == null) return;
            drawnRevision = plugin.Features.Revision;
            drawnPage = currentPage;
            pageTitle.text = string.IsNullOrWhiteSpace(currentQuery) ? currentPage.ToLowerInvariant() : "search results";
            if (string.IsNullOrEmpty(currentQuery) && (currentPage == "Home" || currentPage == "Settings" || currentPage == "Wrist Layout" || currentPage == "Player ESP" || currentPage == "Gun Studio" || currentPage == "UI Effects" || currentPage == "Charm Studio"))
                featureScroll = null;
            featureRows.Clear();
            DestroyChildren(pageRoot);
            PruneGlows();
            if (string.IsNullOrEmpty(currentQuery) && currentPage == "Home")
                DrawHome();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Settings")
                DrawSettingsPage();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "Charm Studio")
                DrawCharmStudio();
            else if (string.IsNullOrEmpty(currentQuery) && currentPage == "UI Effects")
                DrawEffectsPage();
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
                    button.targetGraphic = highlight;
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
                    polish.Wire(button, () => Navigate(destination));
                    MakeText(highlight.rectTransform, label, 7f, 8f, 223f, 22f,
                        12, new Color(0.553f, 0.553f, 0.553f, 1f), true);
                    var line = MakeImage(pageRoot, "Row divider", borderColor);
                    Place(line.rectTransform, x + 2f, y + 38f, 232f, 1f);
                }
            }
        }

        #endregion

        #region Settings pages

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
            MakeText(appearance, "Visual theme: Midnight / indigo",
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
            MakeButton(appearance, "UI Effects", "UI EFFECTS / AUDIO", 14f, 297f, 207f, 31f, accentGray, () => Navigate("UI Effects"), white, 10, true);
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

        private void DrawEffectsPage()
        {
            var s = plugin.Settings;
            MakeText(pageRoot, "Desktop effects", 4f, 0f, 600f, 28f, 16, white, true);
            MakeText(pageRoot, "Local appearance only — no changes to gameplay or global post-processing.", 4f, 30f, 700f, 25f, 11, muted);
            var left = MakeImage(pageRoot, "Glass settings", panelColor, softRounded, true);
            Place(left.rectTransform, 0f, 65f, 354f, 350f);
            var right = MakeImage(pageRoot, "Sound and motion", panelColor, softRounded, true);
            Place(right.rectTransform, 368f, 65f, 354f, 350f);
            var a = left.rectTransform;
            var b = right.rectTransform;
            MakeText(a, "ACRYLIC & BLOOM", 16f, 12f, 320f, 26f, 12, white, true);
            MakeButton(a, "Acrylic", "ACRYLIC BLUR: " + (s.DesktopGlass.Value ? "ON" : "OFF"), 16f, 52f, 322f, 39f, hoverColor, () => { s.DesktopGlass.Value = !s.DesktopGlass.Value; RedrawPage(); }, white, 12);
            MakeText(a, "Glass blend", 16f, 108f, 310f, 20f, 11, muted);
            MakeSlider(a, "Glass blend", 16f, 133f, 315f, 0f, 1f, s.DesktopBlurStrength.Value, v => s.DesktopBlurStrength.Value = v);
            MakeButton(a, "Bloom", "BLOOM: " + (s.DesktopBloom.Value ? "ON" : "OFF"), 16f, 177f, 322f, 39f, hoverColor, () => { s.DesktopBloom.Value = !s.DesktopBloom.Value; RedrawPage(); }, white, 12);
            MakeText(a, "Bloom intensity", 16f, 231f, 310f, 20f, 11, muted);
            MakeSlider(a, "Bloom intensity", 16f, 256f, 315f, 0f, 1f, s.DesktopBloomStrength.Value, v => s.DesktopBloomStrength.Value = v);
            MakeText(a, "Reopen F6 to refresh the scene behind the glass.", 16f, 292f, 320f, 31f, 10, muted);
            MakeText(b, "MOTION & AUDIO", 16f, 12f, 320f, 26f, 12, white, true);
            MakeButton(b, "Motion", "ANIMATIONS: " + (s.DesktopMotion.Value ? "ON" : "OFF"), 16f, 52f, 322f, 39f, hoverColor, () => { s.DesktopMotion.Value = !s.DesktopMotion.Value; RedrawPage(); }, white, 12);
            MakeButton(b, "Audio", "UI SOUNDS: " + (s.DesktopSounds.Value ? "ON" : "OFF"), 16f, 106f, 322f, 39f, hoverColor, () => { s.DesktopSounds.Value = !s.DesktopSounds.Value; RedrawPage(); }, white, 12);
            MakeText(b, "Sound volume", 16f, 160f, 310f, 20f, 11, muted);
            MakeSlider(b, "Sound volume", 16f, 185f, 315f, 0f, 1f, s.DesktopSoundVolume.Value, v => s.DesktopSoundVolume.Value = v);
            MakeButton(b, "Test audio", "TEST SOUND", 16f, 228f, 322f, 39f, panelColor, () => polish.Play(2), white, 11);
            MakeButton(b, "Reset effects", "RESET EFFECTS", 16f, 280f, 322f, 39f, panelColor, () =>
            {
                s.DesktopGlass.Value = true; s.DesktopBlurStrength.Value = .72f;
                s.DesktopBloom.Value = true; s.DesktopBloomStrength.Value = .55f;
                s.DesktopMotion.Value = true; s.DesktopSounds.Value = true; s.DesktopSoundVolume.Value = .25f;
                RedrawPage();
            }, white, 11);
            MakeButton(pageRoot, "Back", "BACK TO SETTINGS", 0f, 435f, 220f, 37f, panelColor, () => Navigate("Settings"), white, 11);
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
            MakeButton(pageRoot, "Preview", plugin.Markers.Preview ? "STOP LOCAL PREVIEW" : "TEST LOCAL PREVIEW", 390f, 150f, 250f, 40f, hoverColor, () => { plugin.Features.Activate(plugin.Features.Find("marker_preview")); RedrawPage(); }, white, 11);
            MakeText(pageRoot, "Depth-tested markers only.", 390f, 206f, 300f, 34f, 11, muted);
        }

        private void DrawCharmStudio()
        {
            var s = plugin.Settings;
            MakeText(pageRoot, "Charm studio / local cosmetics", 12f, 5f, 680f, 30f, 15, white, true);
            MakeText(pageRoot, "Size", 14f, 52f, 320f, 20f, 12, muted);
            MakeSlider(pageRoot, "Charm size", 14f, 76f, 320f, .5f, 1.8f, s.CharmScale.Value, v => s.CharmScale.Value = v);
            MakeText(pageRoot, "Color hue", 14f, 118f, 320f, 20f, 12, muted);
            MakeSlider(pageRoot, "Charm hue", 14f, 142f, 320f, 0f, 1f, s.CharmHue.Value, v => s.CharmHue.Value = v);
            MakeText(pageRoot, "Animation speed", 14f, 184f, 320f, 20f, 12, muted);
            MakeSlider(pageRoot, "Charm motion", 14f, 208f, 320f, 0f, 2f, s.CharmSpeed.Value, v => s.CharmSpeed.Value = v);
            MakeButton(pageRoot, "Hand", s.CharmRightHand.Value ? "HAND: RIGHT" : "HAND: LEFT", 370f, 52f, 330f, 40f, panelColor, () => { s.CharmRightHand.Value = !s.CharmRightHand.Value; RedrawPage(); }, white, 12);
            MakeButton(pageRoot, "Rainbow", s.CharmRainbow.Value ? "RAINBOW ON" : "RAINBOW OFF", 370f, 105f, 330f, 40f, panelColor, () => { s.CharmRainbow.Value = !s.CharmRainbow.Value; RedrawPage(); }, white, 12);
            MakeButton(pageRoot, "Charms", "OPEN CHARM COLLECTION", 370f, 158f, 330f, 40f, hoverColor, () => Navigate("Charms"), white, 12);
            MakeButton(pageRoot, "Reset", "RESET CHARM SETTINGS", 370f, 211f, 330f, 40f, panelColor, () => { s.CharmScale.Value = 1f; s.CharmHue.Value = .65f; s.CharmSpeed.Value = 1f; s.CharmRainbow.Value = false; s.CharmRightHand.Value = false; RedrawPage(); }, white, 12);
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

        #endregion

        #region Slider controls

        private void MakeSlider(Transform parent, string title,
            float x, float y, float width,
            float min, float max, float value, Action<float> onValue)
        {
            var rt = MakeRect(parent, title + " slider");
            Place(rt, x, y, width, 28f);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            float railWidth = width - 52f;
            var track = MakeImage(rt, "Track", new Color(.10f, .11f, .17f, 1f), softRounded, true);
            Place(track.rectTransform, 7f, 11f, railWidth - 14f, 6f);
            var fillArea = MakeRect(rt, "Fill area");
            Place(fillArea, 7f, 11f, railWidth - 14f, 6f);
            var fill = MakeImage(fillArea, "Fill", accentGray, softRounded, true);
            Stretch(fill.rectTransform);

            var handleArea = MakeRect(rt, "Handle travel");
            Place(handleArea, 7f, 0f, railWidth - 14f, 28f);
            var handle = MakeImage(handleArea, "Handle", white, rounded, false);
            var h = handle.rectTransform;
            h.anchorMin = new Vector2(0f, .5f); h.anchorMax = h.anchorMin;
            h.pivot = new Vector2(.5f, .5f); h.sizeDelta = new Vector2(14f, 14f);
            h.anchoredPosition = Vector2.zero;
            var number = MakeText(rt, value.ToString("0.##"), railWidth + 4f, 0f, 48f, 28f, 11, white, true);
            number.alignment = TextAnchor.MiddleRight;

            var slider = rt.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v =>
            {
                number.text = v.ToString("0.##");
                onValue(v);
            });
        }

        #endregion

        #region Feature browser

        private void DrawFeaturePage()
        {
            var matches = GetVisibleFeatures();
            string key = currentPage + "|" + currentQuery;
            float oldScroll = featureScroll != null && scrollKey == key
                ? featureScroll.verticalNormalizedPosition : 1f;
            scrollKey = key;
            RectTransform viewport = MakeRect(pageRoot, "Feature scroll viewport");
            Place(viewport, 0f, 0f, 730f, 508f);
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            featureScroll = viewport.gameObject.AddComponent<ScrollRect>();
            featureScroll.vertical = true;
            featureScroll.horizontal = false;
            featureScroll.movementType = ScrollRect.MovementType.Clamped;
            featureScroll.scrollSensitivity = 32f;
            RectTransform list = MakeRect(viewport, "Features");
            list.sizeDelta = new Vector2(730f, Mathf.Max(508f, ((matches.Count + 1) / 2) * 42f));
            featureScroll.viewport = viewport;
            featureScroll.content = list;

            for (int i = 0; i < matches.Count; i++)
            {
                Feature mod = matches[i];
                float x = (i % 2) * 366f;
                float y = (i / 2) * 42f;
                var state = new FeatureRow { Mod = mod, Enabled = mod.Enabled, IsFavorite = plugin.Features.IsFavorite(mod) };
                featureRows.Add(state);
                var row = MakeButton(list, "Mod " + mod.Id, "", x, y, 358f, 36f,
                    panelColor, () =>
                    {
                        plugin.Features.Activate(mod);
                        selected = mod;
                        RefreshFeatureRows();
                    });
                var rt = row.transform as RectTransform;
                var label = MakeText(rt, Truncate(mod.Label, 34), 12f, 0f, 282f, 36f, 13, white, true);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (mod.ActionOnly)
                {
                    MakeText(rt, "›", 333f, 0f, 20f, 36f, 17, muted, true);
                }
                else
                {
                    var track = MakeImage(rt, "Switch track",
                        mod.Enabled ? accentGray : new Color(.085f, .093f, .145f, 1f),
                        rounded, false);
                    Place(track.rectTransform, 307f, 9f, 40f, 18f);
                    state.Track = track;
                    state.Glow = AddAccentGlow(rt, 299f, 2f, 56f, 32f, .48f);
                    state.Glow.enabled = mod.Enabled;
                    var knob = MakeImage(rt, "Switch knob",
                        mod.Enabled ? white : new Color(.39f, .41f, .51f, 1f),
                        rounded, false);
                    Place(knob.rectTransform, mod.Enabled ? 330f : 309f, 11f, 14f, 14f);
                    state.Knob = knob;
                }
                var favorite = MakeButton(rt, "Favorite " + mod.Id, plugin.Features.IsFavorite(mod) ? "★" : "☆",
                    282f, 5f, 23f, 26f, Color.clear, () =>
                    {
                        plugin.Features.ToggleFavorite(mod);
                        RefreshFeatureRows();
                    }, plugin.Features.IsFavorite(mod) ? accentGray : muted, 13);
                state.Favorite = favorite.GetComponentInChildren<Text>();
            }
            if (matches.Count == 0)
                MakeText(list, "No features available in this category.", 12f, 12f, 680f, 36f, 13, muted);
            featureScroll.StopMovement();
            list.anchoredPosition = new Vector2(0f, (1f - Mathf.Clamp01(oldScroll)) * Mathf.Max(0f, list.sizeDelta.y - 508f));
        }

        private void RefreshFeatureRows()
        {
            drawnRevision = plugin.Features.Revision;
            if (featureScroll == null) return;
            var matches = GetVisibleFeatures();
            if (matches.Count != featureRows.Count) { RedrawPage(); return; }
            for (int i = 0; i < matches.Count; i++)
                if (matches[i] != featureRows[i].Mod) { RedrawPage(); return; }
            foreach (var row in featureRows)
            {
                bool enabled = row.Mod.Enabled;
                bool favorite = plugin.Features.IsFavorite(row.Mod);
                if (row.Enabled != enabled && row.Track != null)
                {
                    row.Track.color = enabled ? accentGray : new Color(.085f, .093f, .145f, 1f);
                    row.Knob.color = enabled ? white : new Color(.39f, .41f, .51f, 1f);
                    row.Knob.rectTransform.anchoredPosition = new Vector2(enabled ? 330f : 309f, -11f);
                    row.Glow.enabled = enabled;
                }
                if (row.IsFavorite != favorite && row.Favorite != null)
                {
                    row.Favorite.text = favorite ? "★" : "☆";
                    row.Favorite.color = favorite ? accentGray : muted;
                }
                row.Enabled = enabled;
                row.IsFavorite = favorite;
            }
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
                case "Local": return plugin.Features.All.FindAll(f => (f.Category == "Local" || f.Category == "Telemetry") && !f.NeedsModdedRoom);
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

        #endregion

        #region Window positioning

        private void ClampWindow()
        {
            Vector2 p = window.anchoredPosition;
            Rect area = canvasRect.rect;
            p.x = Mathf.Clamp(p.x, -window.sizeDelta.x * window.localScale.x + 160f, Mathf.Max(0f, area.width - 160f));
            p.y = Mathf.Clamp(p.y, -Mathf.Max(0f, area.height - 42f), 0f);
            if (window.anchoredPosition != p) window.anchoredPosition = p;
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

        #endregion

        #region Resource cleanup

        internal void Dispose()
        {
            if (visible)
            {
                Cursor.lockState = savedLockMode;
                Cursor.visible = savedCursorVisible;
            }
            ++openGeneration;
            visible = false;
            accentGlows.Clear();
            expiredGlows.Clear();
            polish.Dispose();
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

        #endregion

        #region UI building utilities

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
                child.gameObject.SetActive(false);
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
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock tint = button.colors;
            tint.normalColor = Color.white;
            tint.highlightedColor = new Color(.84f, .90f, 1f, 1f);
            tint.pressedColor = new Color(.70f, .70f, .70f, 1f);
            tint.fadeDuration = plugin.Settings.DesktopMotion.Value ? .16f : 0f;
            button.colors = tint;
            polish.Wire(button, callback);
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

        #endregion
    }

    // Handles moving/resizing without intercepting unrelated feature buttons.
    internal sealed class VolasUIDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Drag state

        internal RectTransform Target;
        internal bool IsResize;
        internal Action OnComplete;
        private Vector2 start;
        private Vector2 originalPosition;
        private Vector2 originalSize;
        private Vector3 originalScale;

        #endregion

        #region Pointer drag lifecycle

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

        #endregion
    }
}
