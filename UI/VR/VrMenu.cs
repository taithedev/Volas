using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    /// <summary>
    /// Physical hand-following VR tablet based on the supplied wrist-menu references.
    /// All buttons are 3D geometry; interaction uses the opposite controller's tip.
    /// It does not require UI prefabs, Canvas objects, or extra VR packages.
    /// </summary>
    internal sealed class VrMenu
    {
        private const int PageSize = 7;
        private readonly VolasPlugin plugin;
        private readonly List<TouchButton> buttons = new List<TouchButton>();
        private GameObject root;
        private Material black;
        private Material charcoal;
        private Material row;
        private Material hover;
        private Material white;
        private Material dim;
        private int categoryIndex = 0;
        private int page = 0;
        private int currentRevision = -1;
        private int hoveredButton = -1;
        private int lastPressedButton = -1;
        private float nextAllowedPress;
        private bool lastTrigger;

        private sealed class TouchButton
        {
            internal float X, Y, Width, Height;
            internal int Id;
            internal Action Click;
            internal Renderer Surface;
            internal Material Resting;
        }

        internal VrMenu(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            Transform left = plugin.Game.LeftHand;
            Transform right = plugin.Game.RightHand;
            Camera head = plugin.Game.HeadCamera;
            if (!plugin.VrOpen || left == null || head == null)
            {
                if (root != null) root.SetActive(false);
                hoveredButton = -1;
                lastPressedButton = -1;
                lastTrigger = false;
                return;
            }

            if (root == null)
            {
                if (!CreateTablet()) return;
                currentRevision = -1;
            }
            root.SetActive(true);
            // The tablet is physically attached to the left controller's position,
            // with a readable orientation facing the player's headset.
            root.transform.position = left.TransformPoint(new Vector3(
                plugin.Settings.VrOffsetX.Value,
                plugin.Settings.VrOffsetY.Value,
                plugin.Settings.VrOffsetZ.Value));
            Vector3 towardsHead = head.transform.position - root.transform.position;
            if (towardsHead.sqrMagnitude > 0.01f)
                root.transform.rotation = Quaternion.LookRotation(towardsHead.normalized, Vector3.up);
            root.transform.localScale = Vector3.one * plugin.Settings.VrScale.Value *
                (plugin.Compact ? 0.8f : 1f);

            if (currentRevision != plugin.Features.Revision) Rebuild();

            if (right == null) return;
            Vector3 tip = root.transform.InverseTransformPoint(
                right.TransformPoint(new Vector3(0f, -0.018f, 0.045f)));
            bool near = Mathf.Abs(tip.z - 0.041f) < 0.105f;
            int target = FindButton(tip.x, tip.y, near);

            bool trigger = false;
            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
                rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);

            // Optional laser-like selection for controller poses where direct touch
            // is awkward. The panel is still a physical 3D object attached to the wrist.
            if (target < 0 && rightDevice.isValid && trigger)
            {
                var ray = new Ray(right.position, right.forward);
                var plane = new Plane(root.transform.forward, root.transform.position +
                    root.transform.forward * 0.042f);
                float distance;
                if (plane.Raycast(ray, out distance) && distance > 0f && distance < 0.65f)
                {
                    Vector3 onTablet = root.transform.InverseTransformPoint(ray.GetPoint(distance));
                    target = FindButton(onTablet.x, onTablet.y, true);
                }
            }

            if (target != hoveredButton)
            {
                hoveredButton = target;
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i].Surface != null)
                        buttons[i].Surface.sharedMaterial = i == target ? hover : buttons[i].Resting;
                }
            }

            bool pressed = target >= 0 && ((near && target != lastPressedButton) ||
                (trigger && !lastTrigger));
            if (pressed && Time.unscaledTime >= nextAllowedPress)
            {
                Action click = buttons[target].Click;
                nextAllowedPress = Time.unscaledTime + 0.22f;
                lastPressedButton = target;
                if (click != null) click();
            }

            if (!near && !trigger) lastPressedButton = -1;
            lastTrigger = trigger;
        }

        private int FindButton(float x, float y, bool enabled)
        {
            if (!enabled) return -1;
            for (int i = 0; i < buttons.Count; i++)
            {
                TouchButton button = buttons[i];
                if (Mathf.Abs(x - button.X) <= button.Width * 0.5f &&
                    Mathf.Abs(y - button.Y) <= button.Height * 0.5f)
                    return i;
            }
            return -1;
        }

        private bool CreateTablet()
        {
            if (!EnsureMaterials()) return false;
            root = new GameObject("Volas physical wrist tablet");
            root.transform.localScale = Vector3.one;
            // Layered slabs and a visible frame make this a physical menu, not
            // a floating desktop overlay or a texture placed on the screen.
            Cube("Wrist mount", 0, -0.03f, -0.036f, 0.16f, 0.31f, 0.055f, charcoal);
            Cube("Tablet back", 0, 0, -0.014f, 0.50f, 0.785f, 0.034f, charcoal);
            Cube("Tablet outline", 0, 0, 0.004f, 0.492f, 0.776f, 0.004f, white);
            Cube("OLED screen", 0, 0, 0.010f, 0.48f, 0.765f, 0.003f, black);
            Cube("Header strip", 0, 0.343f, 0.021f, 0.46f, 0.066f, 0.005f, charcoal);
            Cube("White top line", 0, 0.379f, 0.025f, 0.45f, 0.005f, 0.006f, white);
            Cube("Footer divider", 0, -0.269f, 0.024f, 0.435f, 0.002f, 0.004f, dim);
            return true;
        }

        private bool EnsureMaterials()
        {
            if (black != null) return true;
            Shader shader = Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            if (shader == null)
            {
                plugin.Log.LogError("Volas tablet: a compatible Unity material shader is missing.");
                return false;
            }
            black = MakeMaterial(shader, new Color(0.017f, 0.018f, 0.025f));
            charcoal = MakeMaterial(shader, new Color(0.053f, 0.057f, 0.068f));
            row = MakeMaterial(shader, new Color(0.105f, 0.11f, 0.13f));
            hover = MakeMaterial(shader, new Color(0.30f, 0.31f, 0.34f));
            white = MakeMaterial(shader, new Color(0.97f, 0.97f, 1f));
            dim = MakeMaterial(shader, new Color(0.56f, 0.57f, 0.60f));
            return true;
        }

        private static Material MakeMaterial(Shader shader, Color color)
        {
            var result = new Material(shader);
            result.color = color;
            return result;
        }

        private Renderer Cube(string name, float x, float y, float z,
            float width, float height, float depth, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(width, height, depth);
            var collision = go.GetComponent<Collider>();
            if (collision != null) collision.enabled = false;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private void Rebuild()
        {
            if (root == null) return;

            // Preserve structural frame children, delete the old clickable pages.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name.StartsWith("UI ", StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
            buttons.Clear();
            hoveredButton = -1;
            lastPressedButton = -1;

            var categories = plugin.Features.Categories;
            if (categories.Count == 0) return;
            categoryIndex = Mathf.Clamp(categoryIndex, 0, categories.Count - 1);
            string category = categories[categoryIndex];
            var features = plugin.Features.InCategory(category);
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(features.Count / (float)PageSize));
            page = Mathf.Clamp(page, 0, totalPages - 1);

            Label("VOLAS", -0.213f, 0.343f, 0.018f, white.color);
            Label("v1.4", 0.113f, 0.343f, 0.011f, dim.color);
            AddButton("X", 0.207f, 0.345f, 0.04f, 0.046f,
                white, () => plugin.VrOpen = false, 1);

            AddButton("<", -0.189f, 0.267f, 0.069f, 0.053f,
                row, () => SwitchCategory(-1), 2);
            AddButton(">", 0.189f, 0.267f, 0.069f, 0.053f,
                row, () => SwitchCategory(1), 3);
            Label(Clip(category.ToUpperInvariant(), 15), -0.118f, 0.267f, 0.013f, white.color);

            if (features.Count == 0)
                Label("NO MODS HERE", -0.205f, 0.10f, 0.013f, dim.color);

            int offset = page * PageSize;
            for (int i = 0; i < PageSize && offset + i < features.Count; i++)
            {
                Feature feature = features[offset + i];
                float y = 0.197f - i * 0.065f;
                string name = Clip(feature.Label.ToUpperInvariant(), 19);
                string suffix = feature.ActionOnly ? " >" : feature.Enabled ? " ON" : "";
                AddButton(name + suffix, 0f, y, 0.427f, 0.057f,
                    feature.Enabled ? white : row,
                    () => plugin.Features.Activate(feature), 100 + offset + i);
            }

            AddButton("PREV", -0.128f, -0.307f, 0.178f, 0.054f,
                row, () => SwitchPage(-1, totalPages), 4);
            AddButton("NEXT", 0.128f, -0.307f, 0.178f, 0.054f,
                row, () => SwitchPage(1, totalPages), 5);
            Label("PAGE " + (page + 1) + "/" + totalPages, -0.209f, -0.359f, 0.010f, dim.color);
            Label("TOUCH WITH RIGHT HAND", -0.080f, -0.359f, 0.009f, dim.color);

            currentRevision = plugin.Features.Revision;
        }

        private void SwitchCategory(int amount)
        {
            var categories = plugin.Features.Categories;
            if (categories.Count == 0) return;
            categoryIndex = (categoryIndex + amount + categories.Count) % categories.Count;
            page = 0;
            currentRevision = -1;
        }

        private void SwitchPage(int amount, int count)
        {
            page = (page + amount + count) % count;
            currentRevision = -1;
        }

        private void AddButton(string title, float x, float y,
            float width, float height, Material material, Action callback, int id)
        {
            Renderer plate = Cube("UI Button " + id, x, y, 0.043f, width, height, 0.009f, material);
            string trimmed = Clip(title, 26);
            float fontSize = trimmed.Length > 21 ? 0.010f : 0.012f;
            Label(trimmed, x - width * 0.45f, y, fontSize,
                material == white ? Color.black : Color.white);
            buttons.Add(new TouchButton
            {
                X = x, Y = y, Width = width, Height = height,
                Id = id, Click = callback, Surface = plate, Resting = material
            });
        }

        private void Label(string value, float x, float y, float size, Color color)
        {
            var labelObject = new GameObject("UI Label " + value);
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(x, y, 0.057f);
            labelObject.transform.localRotation = Quaternion.identity;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleLeft;
            label.alignment = TextAlignment.Left;
            label.fontSize = 54;
            label.characterSize = size;
            label.color = color;
        }

        private static string Clip(string value, int count)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= count ? value : value.Substring(0, count - 2) + "..";
        }

        internal void Dispose()
        {
            buttons.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
            DestroyMaterial(ref black);
            DestroyMaterial(ref charcoal);
            DestroyMaterial(ref row);
            DestroyMaterial(ref hover);
            DestroyMaterial(ref white);
            DestroyMaterial(ref dim);
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null) return;
            UnityEngine.Object.Destroy(material);
            material = null;
        }
    }
}
