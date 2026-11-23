using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    /// <summary>
    /// A physical left-hand VR menu. Real raised 3D button surfaces with
    /// right-hand proximity touch, controller ray selection and haptics.
    /// No IMGUI, prefabs, scene edits or network actions.
    /// </summary>
    internal sealed class VrMenu
    {
        private const int ItemsPerPage = 6;
        private const float ButtonPlane = 0.049f;
        private const float TouchPlane = 0.057f;

        private readonly VolasPlugin plugin;
        private readonly List<PhysicalButton> buttons = new List<PhysicalButton>();
        private readonly List<string> categories = new List<string>();
        private readonly List<InputDevice> rightControllerCandidates = new List<InputDevice>(4);
        private float nextMissingCameraLog;
        internal string Status { get; private set; } = "VR menu ready";
        private GameObject root;
        private Transform content;
        private GameObject pointerDot;
        private LineRenderer pointerBeam;
        private Material deepBlack;
        private Material body;
        private Material buttonRest;
        private Material buttonHover;
        private Material active;
        private Material activeHover;
        private Material accent;
        private Material gray;
        private Material light;
        private Material buttonOutline;

        private enum ScreenMode { Categories, Features, Favorites, Enabled, All }
        private ScreenMode screen = ScreenMode.Categories;
        private string currentCategory = "";
        private int page;
        private int displayedRevision = -1;
        private bool firstPosition = true;
        private bool previousTrigger;
        private bool touchArmed = true;
        private int hovered = -1;
        private float nextPressAt;
        private float lastPulseAt;
        private bool wasOpen;

        private sealed class PhysicalButton
        {
            internal float X;
            internal float Y;
            internal float Width;
            internal float Height;
            internal Action Action;
            internal Renderer Renderer;
            internal Material Normal;
            internal Material Hovered;
        }

        internal VrMenu(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            if (!plugin.VrOpen)
            {
                if (root != null) root.SetActive(false);
                HidePointer();
                if (wasOpen)
                {
                    previousTrigger = false;
                    touchArmed = false;
                    firstPosition = true;
                    wasOpen = false;
                    hovered = -1;
                }
                Status = "Closed - left Y, F7, or PC settings";
                return;
            }

            Camera camera = plugin.Game.HeadCamera;
            if (camera == null)
            {
                Status = "Cannot display VR menu: no active headset/game camera";
                if (root != null) root.SetActive(false);
                if (Time.unscaledTime >= nextMissingCameraLog)
                {
                    nextMissingCameraLog = Time.unscaledTime + 8f;
                    plugin.Log.LogWarning("Volas VR menu is open but no camera is available. Check your headset and game camera.");
                }
                return;
            }

            if (root == null && !CreateMenu())
            {
                Status = "VR menu material initialization failed - check BepInEx log";
                return;
            }
            root.SetActive(true);

            Transform anchor = plugin.Game.LeftHand;
            Transform head = camera.transform;
            Vector3 position;
            bool preview = anchor == null;
            if (!preview)
            {
                position = anchor.TransformPoint(new Vector3(
                    plugin.Settings.VrOffsetX.Value,
                    plugin.Settings.VrOffsetY.Value,
                    plugin.Settings.VrOffsetZ.Value));
                Vector3 offset = position - head.position;
                if (offset.sqrMagnitude < 0.075f || offset.sqrMagnitude > 2.0f ||
                    Vector3.Dot(head.forward, offset) < -0.25f)
                    preview = true;
            }
            if (preview)
            {
                // Do not silently hide the entire menu when a hand transform is
                // unavailable or outside the view. Show a head-anchored preview.
                position = head.position + head.forward * 0.75f -
                    head.right * 0.12f - head.up * 0.01f;
                Status = anchor == null
                    ? "Head preview: left controller pose unavailable"
                    : "Head preview: left controller is outside view";
            }
            else
            {
                Status = "Physical wrist menu active";
            }

            Vector3 facing = head.position - position;
            Quaternion orientation = facing.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(facing.normalized, head.up)
                : head.rotation;
            if (firstPosition)
            {
                root.transform.position = position;
                root.transform.rotation = orientation;
                firstPosition = false;
            }
            else
            {
                float t = 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime);
                root.transform.position = Vector3.Lerp(root.transform.position, position, t);
                root.transform.rotation = Quaternion.Slerp(root.transform.rotation, orientation, t);
            }

            root.transform.localScale = Vector3.one *
                plugin.Settings.VrScale.Value * (plugin.Compact ? 0.82f : 1f);
            if (!wasOpen)
            {
                wasOpen = true;
                displayedRevision = -1;
                plugin.Log.LogInfo("Volas physical menu rendered: " + Status);
            }
            if (displayedRevision != plugin.Features.Revision) Rebuild();

            Transform rightHand = plugin.Game.RightHand;
            InputDevice controller = GetRightController();
            bool trigger = ReadTrigger(controller);
            int touched = -1;
            bool touching = false;
            int pointed = -1;
            Vector3 hit = Vector3.zero;
            bool rayHit = false;

            if (rightHand != null)
            {
                Vector3 tip = root.transform.InverseTransformPoint(
                    rightHand.TransformPoint(new Vector3(0f, -0.018f, 0.047f)));
                bool hoverDepth = tip.z >= .005f && tip.z <= .16f;
                touched = FindButton(tip.x, tip.y, hoverDepth);
                // Only press when physically contacting the raised front,
                // not merely hovering a few centimeters above it.
                touching = touched >= 0 && tip.z <= .069f;
                if (touched == -1 || tip.z > .105f) touchArmed = true;
                rayHit = FindPointer(rightHand, out pointed, out hit);
            }
            else
            {
                touchArmed = false;
            }

            // In desktop preview, an ordinary mouse can click physical
            // menu buttons. This also works when headset controllers fail.
            int mouseTarget = -1;
            Vector3 mouseHit;
            bool mouseRay = FindMousePointer(camera, out mouseTarget, out mouseHit);
            bool mouseClick = false;
            try { mouseClick = mouseTarget >= 0 && Input.GetMouseButtonDown(0); }
            catch (Exception) { }

            if ((!rayHit || pointed == -1) && mouseRay)
            {
                rayHit = true;
                pointed = mouseTarget;
                hit = mouseHit;
            }
            if (rightHand != null && rayHit)
                UpdatePointer(rightHand, true, hit, controller);
            else if (mouseRay)
            {
                if (pointerDot != null)
                {
                    pointerDot.SetActive(true);
                    pointerDot.transform.position = mouseHit +
                        root.transform.forward * .013f;
                }
                if (pointerBeam != null) pointerBeam.gameObject.SetActive(false);
            }
            else
                HidePointer();

            int selection = touched >= 0 ? touched : pointed;
            if (mouseClick) selection = mouseTarget;
            SetHover(selection);

            bool touchPress = touching && touchArmed;
            bool triggerPress = pointed >= 0 && trigger && !previousTrigger;
            previousTrigger = trigger;
            if ((touchPress || triggerPress || mouseClick) &&
                Time.unscaledTime >= nextPressAt)
            {
                int index = mouseClick ? mouseTarget :
                    touchPress ? touched : pointed;
                if (index >= 0 && index < buttons.Count)
                {
                    touchArmed = false;
                    nextPressAt = Time.unscaledTime + .23f;
                    Action action = buttons[index].Action;
                    Haptic(controller);
                    if (action != null) action();
                    Status = "Button selected";
                }
            }
        }

        private InputDevice GetRightController()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid) return device;
            rightControllerCandidates.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightControllerCandidates);
            for (int i = 0; i < rightControllerCandidates.Count; i++)
                if (rightControllerCandidates[i].isValid)
                    return rightControllerCandidates[i];
            return device;
        }

        private static bool ReadTrigger(InputDevice device)
        {
            if (!device.isValid) return false;
            bool pressed;
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out pressed))
                return pressed;
            float analog;
            return device.TryGetFeatureValue(CommonUsages.trigger, out analog) &&
                analog >= .62f;
        }

        private bool FindMousePointer(Camera camera, out int index, out Vector3 point)
        {
            index = -1;
            point = Vector3.zero;
            if (camera == null) return false;
            try
            {
                return IntersectMenu(camera.ScreenPointToRay(Input.mousePosition),
                    4.0f, out index, out point);
            }
            catch (Exception) { return false; }
        }

        private bool CreateMenu()
        {
            if (!CreateMaterials()) return false;
            root = new GameObject("Volas physical VR menu");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.transform.localScale = Vector3.one;

            // Solid physical back with an OLED front, raised navigation and buttons.
            Cube(root.transform, "Grip mount", 0f, -0.083f, -0.035f,
                0.135f, 0.29f, 0.046f, body);
            Cube(root.transform, "Backplate", 0f, 0f, -0.016f,
                0.508f, 0.806f, 0.039f, body);
            Cube(root.transform, "Accent edge", 0f, 0f, 0.003f,
                0.498f, 0.795f, 0.008f, accent);
            Cube(root.transform, "OLED inset", 0f, 0f, 0.011f,
                0.487f, 0.780f, 0.008f, deepBlack);
            Cube(root.transform, "Header band", 0f, 0.343f, 0.023f,
                0.474f, 0.074f, 0.008f, body);
            Cube(root.transform, "Footer band", 0f, -0.345f, 0.024f,
                0.473f, 0.088f, 0.008f, body);

            content = new GameObject("Volas runtime buttons and labels").transform;
            content.SetParent(root.transform, false);
            BuildPointer();
            Rebuild();
            return true;
        }

        private bool CreateMaterials()
        {
            if (deepBlack != null) return true;
            Shader shader = Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            if (shader == null)
            {
                plugin.Log.LogError("Volas physical menu: no supported Unity shader found.");
                return false;
            }
            deepBlack = CreateMaterial(shader, new Color(.018f, .015f, .028f));
            body = CreateMaterial(shader, new Color(.055f, .047f, .072f));
            buttonRest = CreateMaterial(shader, new Color(.105f, .094f, .14f));
            buttonHover = CreateMaterial(shader, new Color(.25f, .17f, .35f));
            active = CreateMaterial(shader, new Color(.47f, .23f, .81f));
            activeHover = CreateMaterial(shader, new Color(.64f, .39f, .97f));
            accent = CreateMaterial(shader, new Color(.40f, .19f, .74f));
            gray = CreateMaterial(shader, new Color(.58f, .55f, .65f));
            light = CreateMaterial(shader, new Color(.93f, .91f, .97f));
            buttonOutline = CreateMaterial(shader, new Color(.14f, .12f, .19f));
            return true;
        }

        private static Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static Renderer Cube(Transform parent, string name,
            float x, float y, float z, float width, float height,
            float depth, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, z);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(width, height, depth);
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private void BuildPointer()
        {
            GameObject lineObject = new GameObject("Volas menu selection ray");
            lineObject.transform.SetParent(root.transform, false);
            pointerBeam = lineObject.AddComponent<LineRenderer>();
            pointerBeam.useWorldSpace = true;
            pointerBeam.positionCount = 2;
            pointerBeam.startWidth = 0.0025f;
            pointerBeam.endWidth = 0.0025f;
            pointerBeam.sharedMaterial = accent;
            pointerBeam.startColor = new Color(1f, 1f, 1f, .55f);
            pointerBeam.endColor = new Color(1f, 1f, 1f, .15f);
            pointerBeam.gameObject.SetActive(false);

            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerDot.name = "Volas menu pointer dot";
            pointerDot.transform.SetParent(root.transform, false);
            pointerDot.transform.localScale = Vector3.one * .014f;
            pointerDot.GetComponent<Renderer>().sharedMaterial = activeHover;
            Collider collision = pointerDot.GetComponent<Collider>();
            if (collision != null) collision.enabled = false;
            pointerDot.SetActive(false);
        }

        private void UpdatePointer(Transform hand, bool visible,
            Vector3 point, InputDevice controller)
        {
            if (pointerDot == null || pointerBeam == null) return;
            // Pointer is only visible when an actual UI surface is pointed at.
            pointerDot.SetActive(visible);
            pointerBeam.gameObject.SetActive(visible);
            if (!visible) return;
            pointerDot.transform.position = point + root.transform.forward * .013f;
            pointerBeam.SetPosition(0, hand.position);
            pointerBeam.SetPosition(1, point);
        }

        private void HidePointer()
        {
            if (pointerDot != null) pointerDot.SetActive(false);
            if (pointerBeam != null) pointerBeam.gameObject.SetActive(false);
        }

        private bool FindPointer(Transform hand, out int index, out Vector3 point)
        {
            index = -1;
            point = Vector3.zero;
            // GTAG rigs commonly aim along either -up or forward.
            Vector3[] directions = { -hand.up, hand.forward };
            float closest = float.MaxValue;
            bool intersected = false;
            for (int i = 0; i < directions.Length; i++)
            {
                int candidate;
                Vector3 hit;
                if (!IntersectMenu(new Ray(hand.position, directions[i]), .95f,
                    out candidate, out hit)) continue;
                float distance = Vector3.Distance(hand.position, hit);
                if (distance >= closest) continue;
                closest = distance;
                point = hit;
                index = candidate;
                intersected = true;
                // Prefer a ray hitting an actual button over empty panel space.
                if (candidate >= 0) break;
            }
            return intersected;
        }

        private bool IntersectMenu(Ray ray, float maxDistance, out int index,
            out Vector3 point)
        {
            index = -1;
            point = Vector3.zero;
            Plane panelPlane = new Plane(root.transform.forward,
                root.transform.position + root.transform.forward *
                (ButtonPlane * root.transform.localScale.z));
            float distance;
            if (!panelPlane.Raycast(ray, out distance) ||
                distance < 0f || distance > maxDistance)
                return false;
            point = ray.GetPoint(distance);
            Vector3 local = root.transform.InverseTransformPoint(point);
            if (Mathf.Abs(local.x) > .242f || Mathf.Abs(local.y) > .384f)
                return false;
            index = FindButton(local.x, local.y, true);
            return true;
        }

        private int FindButton(float x, float y, bool eligible)
        {
            if (!eligible) return -1;
            // Newer buttons (favorites etc.) take priority over wider rows.
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                PhysicalButton item = buttons[i];
                if (Mathf.Abs(item.X - x) <= item.Width * .5f &&
                    Mathf.Abs(item.Y - y) <= item.Height * .5f)
                    return i;
            }
            return -1;
        }

        private void SetHover(int index)
        {
            if (hovered == index) return;
            hovered = index;
            for (int i = 0; i < buttons.Count; i++)
            {
                PhysicalButton item = buttons[i];
                if (item.Renderer != null)
                    item.Renderer.sharedMaterial =
                        index == i ? item.Hovered : item.Normal;
            }
        }

        private void Haptic(InputDevice controller)
        {
            if (!controller.isValid || Time.unscaledTime < lastPulseAt + .11f)
                return;
            lastPulseAt = Time.unscaledTime;
            try { controller.SendHapticImpulse(0u, .26f, .055f); }
            catch { /* Unsupported controllers still receive button input. */ }
        }

        private void Rebuild()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            buttons.Clear();
            hovered = -1;
            UpdateCategories();

            Label("VOLAS", -0.216f, 0.348f, .017f, activeHover.color, true);
            Label("v1.4", -0.074f, 0.348f, .009f, gray.color);
            AddButton("HOME", 0.119f, 0.348f, .092f, .048f,
                buttonRest, () => Open(ScreenMode.Categories), true);
            AddButton("X", .211f, .348f, .048f, .048f,
                active, () => plugin.VrOpen = false, true);

            AddButton("CATS", -0.177f, .277f, .092f, .047f,
                screen == ScreenMode.Categories ? active : buttonRest,
                () => Open(ScreenMode.Categories), true);
            AddButton("FAV", -.060f, .277f, .092f, .047f,
                screen == ScreenMode.Favorites ? active : buttonRest,
                () => Open(ScreenMode.Favorites), true);
            AddButton("ON", .057f, .277f, .092f, .047f,
                screen == ScreenMode.Enabled ? active : buttonRest,
                () => Open(ScreenMode.Enabled), true);
            AddButton("ALL", .174f, .277f, .092f, .047f,
                screen == ScreenMode.All ? active : buttonRest,
                () => Open(ScreenMode.All), true);

            var list = new List<Feature>();
            int count;
            string title;
            if (screen == ScreenMode.Categories)
            {
                count = categories.Count;
                title = "CATEGORIES";
            }
            else
            {
                switch (screen)
                {
                    case ScreenMode.Favorites:
                        title = "FAVORITES";
                        list = plugin.Features.InCategory("Favorites");
                        break;
                    case ScreenMode.Enabled:
                        title = "ENABLED";
                        list = plugin.Features.All.FindAll(f => f.Enabled);
                        break;
                    case ScreenMode.All:
                        title = "ALL MODS";
                        list = new List<Feature>(plugin.Features.All);
                        break;
                    default:
                        title = currentCategory.ToUpperInvariant();
                        list = plugin.Features.InCategory(currentCategory);
                        break;
                }
                count = list.Count;
            }
            Label(Trim(title, 21), -0.220f, .223f, .011f, gray.color);
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)ItemsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            int first = page * ItemsPerPage;

            for (int i = 0; i < ItemsPerPage && first + i < count; i++)
            {
                int rowIndex = first + i;
                float y = .172f - i * .073f;
                if (screen == ScreenMode.Categories)
                {
                    string name = categories[rowIndex];
                    AddButton(Trim(name, 25), 0f, y, .434f, .063f,
                        buttonRest, () => OpenCategory(name));
                    Label(">", .174f, y, .015f, gray.color);
                }
                else
                {
                    Feature feature = list[rowIndex];
                    AddButton(Trim(feature.Label, 19), -.033f, y,
                        .363f, .063f, feature.Enabled ? active : buttonRest,
                        () => ActivateFeature(feature));
                    if (!feature.ActionOnly)
                        Label(feature.Enabled ? "ON" : "OFF",
                            .090f, y, .009f,
                            feature.Enabled ? light.color : gray.color);
                    AddButton(plugin.Features.IsFavorite(feature) ? "*" : "+",
                        .193f, y, .056f, .063f,
                        plugin.Features.IsFavorite(feature) ? active : buttonRest,
                        () => FavoriteFeature(feature), true);
                }
            }

            if (count == 0)
                Label("NO ACTIVE ENTRIES", -.204f, .072f, .012f, gray.color);
            AddButton("<", -.182f, -.287f, .062f, .052f,
                page > 0 ? buttonRest : body,
                () => ChangePage(-1, pages), true);
            Label("PAGE " + (page + 1) + " / " + pages,
                -.118f, -.287f, .011f, light.color);
            AddButton(">", .184f, -.287f, .062f, .052f,
                page < pages - 1 ? buttonRest : body,
                () => ChangePage(1, pages), true);

            Label("RIGHT HAND: TOUCH OR TRIGGER", -.222f, -.355f, .0082f,
                gray.color);
            AddButton("RESET", .175f, -.355f, .086f, .04f,
                buttonRest, () =>
                {
                    plugin.Features.DisableAll();
                    displayedRevision = -1;
                }, true);

            displayedRevision = plugin.Features.Revision;
        }

        private void UpdateCategories()
        {
            categories.Clear();
            foreach (string category in plugin.Features.Categories)
            {
                if (category == "Favorites" || category == "All") continue;
                if (!categories.Contains(category)) categories.Add(category);
            }
        }

        private void Open(ScreenMode mode)
        {
            screen = mode;
            page = 0;
            displayedRevision = -1;
        }

        private void OpenCategory(string category)
        {
            currentCategory = category;
            screen = ScreenMode.Features;
            page = 0;
            displayedRevision = -1;
        }

        private void ChangePage(int direction, int pages)
        {
            page = Mathf.Clamp(page + direction, 0, pages - 1);
            displayedRevision = -1;
        }

        private void ActivateFeature(Feature feature)
        {
            plugin.Features.Activate(feature);
            displayedRevision = -1;
        }

        private void FavoriteFeature(Feature feature)
        {
            plugin.Features.ToggleFavorite(feature);
            displayedRevision = -1;
        }

        private void AddButton(string caption, float x, float y,
            float width, float height, Material material,
            Action action, bool centerLabel = false)
        {
            Cube(content, "Button border", x, y, .042f,
                width + .003f, height + .003f, .009f, buttonOutline);
            Renderer renderer = Cube(content, "Button " + caption,
                x, y, ButtonPlane, width, height, .010f, material);
            buttons.Add(new PhysicalButton
            {
                X = x, Y = y, Width = width, Height = height,
                Action = action,
                Renderer = renderer,
                Normal = material,
                Hovered = material == active ? activeHover : buttonHover
            });

            float font = height <= .051f ? .0105f : .0118f;
            float xLabel = centerLabel ? x : x - width * .5f + .015f;
            Color text = material == active ? light.color : light.color;
            Label(caption, xLabel, y, font, text, centerLabel);
        }

        private void Label(string value, float x, float y, float font,
            Color color, bool center = false)
        {
            GameObject go = new GameObject("UI Text " + value);
            go.transform.SetParent(content, false);
            go.transform.localPosition = new Vector3(x, y, .063f);
            go.transform.localRotation = Quaternion.identity;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.anchor = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            mesh.alignment = center ? TextAlignment.Center : TextAlignment.Left;
            mesh.fontSize = 54;
            mesh.characterSize = font;
            mesh.color = color;
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= max ? value :
                value.Substring(0, max - 2) + "..";
        }

        internal void Dispose()
        {
            buttons.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
            pointerDot = null;
            pointerBeam = null;
            Destroy(ref deepBlack);
            Destroy(ref body);
            Destroy(ref buttonRest);
            Destroy(ref buttonHover);
            Destroy(ref active);
            Destroy(ref activeHover);
            Destroy(ref accent);
            Destroy(ref gray);
            Destroy(ref light);
            Destroy(ref buttonOutline);
        }

        private static void Destroy(ref Material material)
        {
            if (material == null) return;
            UnityEngine.Object.Destroy(material);
            material = null;
        }
    }
}
