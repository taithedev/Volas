using System;
using UnityEngine;

namespace Volas
{
    // Lightweight, entirely local decorative systems for VR hands and the player's viewpoint.
    internal sealed class VisualDecorations
    {
        internal bool WristRings;
        internal bool PulsingWristRings;
        internal bool HeadCrown;
        internal bool FloorRing;
        internal bool RainbowFloor;
        internal bool GloveCubes;

        private readonly VolasPlugin plugin;
        private LineRenderer leftWrist;
        private LineRenderer rightWrist;
        private LineRenderer floor;
        private GameObject crown;
        private GameObject leftCube;
        private GameObject rightCube;

        internal VisualDecorations(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            bool wristEnabled = WristRings || PulsingWristRings;
            if (wristEnabled)
            {
                UpdateWrist(ref leftWrist, plugin.Game.LeftHand, "Left wrist");
                UpdateWrist(ref rightWrist, plugin.Game.RightHand, "Right wrist");
            }
            else
            {
                DestroyLine(ref leftWrist);
                DestroyLine(ref rightWrist);
            }

            var head = plugin.Game.HeadCamera;
            if (HeadCrown && head != null)
            {
                if (crown == null)
                {
                    crown = new GameObject("Volas crown");
                    for (int i = 0; i < 7; i++)
                    {
                        var jewel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        jewel.name = "Local crown jewel";
                        jewel.transform.SetParent(crown.transform, false);
                        jewel.transform.localScale = Vector3.one * 0.065f;
                        var collider = jewel.GetComponent<Collider>();
                        if (collider != null) collider.enabled = false;
                        jewel.GetComponent<Renderer>().material = NewMaterial(plugin.Accent);
                    }
                }

                crown.transform.position = head.transform.position + Vector3.up * 0.30f;
                for (int i = 0; i < crown.transform.childCount; i++)
                {
                    float phase = i * 2f * Mathf.PI / 7f + Time.unscaledTime * 0.55f;
                    var jewel = crown.transform.GetChild(i);
                    jewel.localPosition = new Vector3(
                        Mathf.Cos(phase) * 0.24f, 0.05f * Mathf.Sin(phase * 3f),
                        Mathf.Sin(phase) * 0.24f);
                    jewel.GetComponent<Renderer>().sharedMaterial.color = plugin.Accent;
                }
            }
            else DestroyGameObject(ref crown);

            if ((FloorRing || RainbowFloor) && head != null)
            {
                if (floor == null)
                    floor = NewLine("Volas floor beacon", 60, 0.018f, true);
                Vector3 pos = plugin.Game.Body != null
                    ? plugin.Game.Body.position + Vector3.down * 0.55f
                    : head.transform.position + Vector3.down * 1.55f;
                for (int i = 0; i < 60; i++)
                {
                    float phase = i * 2f * Mathf.PI / 60f;
                    floor.SetPosition(i, pos + new Vector3(
                        Mathf.Cos(phase) * 0.60f, 0f, Mathf.Sin(phase) * 0.60f));
                }
                floor.sharedMaterial.color = RainbowFloor
                    ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.16f, 1f), 0.85f, 1f)
                    : plugin.Accent;
            }
            else DestroyLine(ref floor);

            if (GloveCubes)
            {
                UpdateCube(ref leftCube, plugin.Game.LeftHand, "Left controller cube");
                UpdateCube(ref rightCube, plugin.Game.RightHand, "Right controller cube");
            }
            else
            {
                DestroyGameObject(ref leftCube);
                DestroyGameObject(ref rightCube);
            }
        }

        private void UpdateWrist(ref LineRenderer line, Transform hand, string title)
        {
            if (hand == null)
            {
                DestroyLine(ref line);
                return;
            }
            if (line == null)
                line = NewLine("Volas " + title + " neon ring", 48, 0.012f, true);
            line.transform.SetParent(hand, false);
            line.transform.localPosition = Vector3.zero;
            line.transform.localRotation = Quaternion.identity;
            float multiplier = PulsingWristRings
                ? 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.19f
                : 1f;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * 2f * Mathf.PI / 48f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.11f * multiplier,
                    Mathf.Sin(angle) * 0.11f * multiplier, 0f));
            }
            line.sharedMaterial.color = plugin.Accent;
        }

        private void UpdateCube(ref GameObject obj, Transform hand, string title)
        {
            if (hand == null) { DestroyGameObject(ref obj); return; }
            if (obj == null)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = "Volas " + title;
                obj.transform.localScale = Vector3.one * 0.09f;
                var collider = obj.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                obj.GetComponent<Renderer>().material = NewMaterial(plugin.Accent);
            }
            obj.transform.SetParent(hand, false);
            obj.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            obj.transform.localRotation = Quaternion.Euler(
                Time.unscaledTime * 38f, Time.unscaledTime * 66f, Time.unscaledTime * 22f);
            obj.GetComponent<Renderer>().sharedMaterial.color = plugin.Accent;
        }

        private LineRenderer NewLine(string title, int count, float width, bool loop)
        {
            var obj = new GameObject(title);
            var line = obj.AddComponent<LineRenderer>();
            line.positionCount = count;
            line.loop = loop;
            line.useWorldSpace = false;
            line.startWidth = width;
            line.endWidth = width;
            line.material = NewMaterial(plugin.Accent);
            return line;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Volas: no compatible shader.");
            return new Material(shader) { color = color };
        }

        private static void DestroyGameObject(ref GameObject obj)
        {
            if (obj == null) return;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.sharedMaterial != null)
                    UnityEngine.Object.Destroy(renderer.sharedMaterial);
            UnityEngine.Object.Destroy(obj);
            obj = null;
        }

        private static void DestroyLine(ref LineRenderer line)
        {
            if (line == null) return;
            var obj = line.gameObject;
            if (line.sharedMaterial != null) UnityEngine.Object.Destroy(line.sharedMaterial);
            UnityEngine.Object.Destroy(obj);
            line = null;
        }

        internal void Reset()
        {
            DestroyLine(ref leftWrist);
            DestroyLine(ref rightWrist);
            DestroyLine(ref floor);
            DestroyGameObject(ref crown);
            DestroyGameObject(ref leftCube);
            DestroyGameObject(ref rightCube);
        }

        internal void Dispose() { Reset(); }
    }
}
