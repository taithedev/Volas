using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    internal sealed class VrMenu
    {
        private const int PageSize = 7;
        private readonly VolasPlugin plugin;
        private readonly List<TouchButton> buttons = new List<TouchButton>();
        private readonly List<Renderer> accentSurfaces = new List<Renderer>();
        private GameObject root;
        private int categoryIndex;
        private int page;
        private int seenRevision = -1;
        private bool touchingLastFrame;

        private sealed class TouchButton
        {
            internal float X, Y, Width, Height;
            internal Action Click;
        }

        internal VrMenu(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            var left = plugin.Game.LeftHand;
            var camera = plugin.Game.HeadCamera;
            if (!plugin.VrOpen || left == null || camera == null)
            {
                if (root != null) root.SetActive(false);
                touchingLastFrame = false;
                return;
            }

            if (root == null)
            {
                root = new GameObject("Volas VR Menu");
                seenRevision = -1;
            }

            root.SetActive(true);
            root.transform.position = left.position + left.right * 0.18f + left.up * 0.09f;
            Vector3 towardsEyes = camera.transform.position - root.transform.position;
            if (towardsEyes.sqrMagnitude > 0.001f)
                root.transform.rotation = Quaternion.LookRotation(towardsEyes.normalized, Vector3.up);
            root.transform.localScale = Vector3.one * plugin.Settings.VrScale.Value * (plugin.Compact ? 0.8f : 1f);

            if (seenRevision != plugin.Features.Revision)
                Rebuild();

            foreach (var surface in accentSurfaces)
                if (surface != null) surface.material.color = plugin.Accent;

            var right = plugin.Game.RightHand;
            if (right == null) return;
            var point = root.transform.InverseTransformPoint(right.position);
            bool hitSomething = false;

            if (Mathf.Abs(point.z) < 0.13f)
            {
                foreach (var button in buttons)
                {
                    if (Mathf.Abs(point.x - button.X) > button.Width * 0.5f ||
                        Mathf.Abs(point.y - button.Y) > button.Height * 0.5f)
                        continue;

                    hitSomething = true;
                    if (!touchingLastFrame)
                        button.Click();
                    break;
                }
            }
            touchingLastFrame = hitSomething;
        }

        private void Rebuild()
        {
            if (root == null) return;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(root.transform.GetChild(i).gameObject);
            buttons.Clear();
            accentSurfaces.Clear();

            var categories = plugin.Features.Categories;
            if (categories.Count == 0) return;
            categoryIndex = Mathf.Clamp(categoryIndex, 0, categories.Count - 1);
            string category = categories[categoryIndex];
            var features = plugin.Features.InCategory(category);
            int pages = Mathf.Max(1, Mathf.CeilToInt(features.Count / (float)PageSize));
            page = Mathf.Clamp(page, 0, pages - 1);

            Box("Main panel", 0f, 0f, 0.52f, 0.63f, new Color(0.025f, 0.028f, 0.036f, 1f), 0.0f);
            accentSurfaces.Add(Box("Accent stripe", 0f, 0.293f, 0.52f, 0.008f, plugin.Accent, 0.018f));
            Text("VOLAS  1.2   /   PCVR", -0.22f, 0.255f, 0.018f, Color.white);
            Text(plugin.Game.RoomSummary(), -0.22f, 0.219f, 0.010f, new Color(0.60f, 0.65f, 0.73f));

            AddButton("<", -0.185f, 0.166f, 0.065f, 0.053f, null, () =>
            {
                categoryIndex = (categoryIndex - 1 + categories.Count) % categories.Count;
                page = 0;
                seenRevision = -1;
            });
            Text(category.ToUpperInvariant(), -0.12f, 0.166f, 0.018f, Color.white);
            AddButton(">", 0.183f, 0.166f, 0.065f, 0.053f, null, () =>
            {
                categoryIndex = (categoryIndex + 1) % categories.Count;
                page = 0;
                seenRevision = -1;
            });

            if (features.Count == 0)
                Text("NO FAVORITES YET - USE PC MENU", -0.22f, 0.045f, 0.012f, Color.white);

            int start = page * PageSize;
            for (int i = 0; i < PageSize && start + i < features.Count; i++)
            {
                var feature = features[start + i];
                float y = 0.113f - i * 0.053f;
                AddButton(
                    feature.Label + (feature.ActionOnly ? "  >" : feature.Enabled ? "  [ON]" : "  [OFF]"),
                    0f, y, 0.44f, 0.047f, feature, () => plugin.Features.Activate(feature));
            }

            AddButton("PREV", -0.132f, -0.267f, 0.15f, 0.053f, null, () =>
            {
                page = (page - 1 + pages) % pages;
                seenRevision = -1;
            });
            AddButton("NEXT", 0.132f, -0.267f, 0.15f, 0.053f, null, () =>
            {
                page = (page + 1) % pages;
                seenRevision = -1;
            });

            Text("PAGE " + (page + 1) + "/" + pages + "     TOUCH WITH RIGHT HAND", -0.22f, -0.303f, 0.010f, new Color(0.61f, 0.65f, 0.71f));
            seenRevision = plugin.Features.Revision;
        }

        private void AddButton(string label, float x, float y, float w, float h, Feature feature, Action onClick)
        {
            Color background = feature != null && feature.Enabled
                ? new Color(0.07f, 0.23f, 0.29f, 1f)
                : new Color(0.095f, 0.105f, 0.135f, 1f);
            Box("Volas button " + label, x, y, w, h, background, 0.014f);
            Text(label, x - w * 0.46f, y, 0.0145f,
                feature != null && feature.Enabled ? plugin.Accent : Color.white);
            buttons.Add(new TouchButton { X = x, Y = y, Width = w, Height = h, Click = onClick });
        }

        private Renderer Box(string name, float x, float y, float w, float h, Color color, float z)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(root.transform, false);
            obj.transform.localPosition = new Vector3(x, y, z);
            obj.transform.localScale = new Vector3(w, h, 0.012f);
            var collision = obj.GetComponent<Collider>();
            if (collision != null) collision.enabled = false;
            var renderer = obj.GetComponent<Renderer>();
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            renderer.material = new Material(shader) { color = color };
            return renderer;
        }

        private void Text(string value, float x, float y, float size, Color color)
        {
            var go = new GameObject("Volas text");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(x, y, 0.033f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.anchor = TextAnchor.MiddleLeft;
            mesh.alignment = TextAlignment.Left;
            mesh.fontSize = 52;
            mesh.characterSize = size;
            mesh.color = color;
        }

        internal void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
            buttons.Clear();
            accentSurfaces.Clear();
        }
    }
}
