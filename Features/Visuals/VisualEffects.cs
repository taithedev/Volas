using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Text;

namespace Volas
{
    internal sealed class VisualEffects
    {
        private readonly VolasPlugin plugin;
        internal bool Orbs;
        internal bool RainbowOrbs;
        internal bool Trails;
        internal bool HandLights;
        internal bool Laser;
        internal bool Halo;
        internal bool Breadcrumbs;
        internal bool FpsHud;
        internal bool FrameTimeHud, SessionHud, TrackingHud, BatteryHud;
        private readonly StringBuilder telemetry = new StringBuilder(256);
        private float nextTelemetry;
        private string cachedTelemetry = "";
        internal bool ClockHud;
        internal bool PositionHud;
        internal bool HeadingHud;
        internal bool DesktopCrosshair;

        private GameObject leftOrb;
        private GameObject rightOrb;
        private GameObject leftTrailObject;
        private GameObject rightTrailObject;
        private GameObject leftLightObject;
        private GameObject rightLightObject;
        private LineRenderer beam;
        private LineRenderer headRing;
        private TextMesh hud;
        private float nextHudUpdate;
        private Vector3 lastPathPos;
        private bool hasPathPos;
        private readonly List<PathDot> path = new List<PathDot>();

        private sealed class PathDot
        {
            internal GameObject Object;
            internal float Expires;
        }

        internal VisualEffects(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            bool hands = Orbs || RainbowOrbs || Trails || HandLights;
            var left = hands ? plugin.Game.LeftHand : null;
            var right = hands || Laser ? plugin.Game.RightHand : null;
            var accent = plugin.Accent;

            if ((Orbs || RainbowOrbs) && left != null && right != null)
            {
                if (leftOrb == null) leftOrb = Sphere("Left hand orb", 0.065f);
                if (rightOrb == null) rightOrb = Sphere("Right hand orb", 0.065f);
                leftOrb.transform.position = left.position;
                rightOrb.transform.position = right.position;
                Color tint = RainbowOrbs
                    ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.25f, 1f), 0.85f, 1f)
                    : accent;
                Tint(leftOrb, tint);
                Tint(rightOrb, tint);
            }
            else { DestroyObject(ref leftOrb); DestroyObject(ref rightOrb); }

            if (Trails && left != null && right != null)
            {
                if (leftTrailObject == null) leftTrailObject = Trail("Left trail", left);
                if (rightTrailObject == null) rightTrailObject = Trail("Right trail", right);
                if (leftTrailObject.transform.parent != left) leftTrailObject.transform.SetParent(left, false);
                if (rightTrailObject.transform.parent != right) rightTrailObject.transform.SetParent(right, false);
                leftTrailObject.GetComponent<TrailRenderer>().startColor = accent;
                rightTrailObject.GetComponent<TrailRenderer>().startColor = accent;
            }
            else { DestroyObject(ref leftTrailObject); DestroyObject(ref rightTrailObject); }

            if (HandLights && left != null && right != null)
            {
                if (leftLightObject == null) leftLightObject = LightObject("Left palm light", left);
                if (rightLightObject == null) rightLightObject = LightObject("Right palm light", right);
                if (leftLightObject.transform.parent != left) leftLightObject.transform.SetParent(left, false);
                if (rightLightObject.transform.parent != right) rightLightObject.transform.SetParent(right, false);
                leftLightObject.GetComponent<Light>().color = accent;
                rightLightObject.GetComponent<Light>().color = accent;
            }
            else { DestroyObject(ref leftLightObject); DestroyObject(ref rightLightObject); }

            if (Laser && right != null)
            {
                if (beam == null) beam = Line("Volas pointer beam", 2, 0.009f, false);
                beam.startColor = accent;
                beam.endColor = accent;
                beam.SetPosition(0, right.position);
                var ray = new Ray(right.position, right.forward);
                RaycastHit hit;
                beam.SetPosition(1, Physics.Raycast(ray, out hit, 15f) ? hit.point : ray.GetPoint(15f));
            }
            else DestroyLine(ref beam);

            var head = Halo || HasTelemetry ? plugin.Game.HeadCamera : null;
            if (Halo && head != null)
            {
                if (headRing == null) headRing = Line("Volas head halo", 48, 0.012f, true);
                headRing.startColor = accent;
                headRing.endColor = accent;
                Vector3 center = head.transform.position + Vector3.up * 0.28f;
                for (int i = 0; i < 48; i++)
                {
                    float angle = (i / 48f) * Mathf.PI * 2f;
                    headRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * 0.19f, 0f, Mathf.Sin(angle) * 0.19f));
                }
            }
            else DestroyLine(ref headRing);

            UpdatePath();
            UpdateHud(head);
        }

        private void UpdatePath()
        {
            if (!Breadcrumbs)
            {
                ClearPath();
                return;
            }

            Vector3 now = plugin.Game.Position;
            if (!hasPathPos || Vector3.Distance(lastPathPos, now) >= 0.8f)
            {
                lastPathPos = now;
                hasPathPos = true;
                var dot = Sphere("Volas breadcrumb", 0.045f);
                dot.transform.position = now;
                Tint(dot, plugin.Accent);
                path.Add(new PathDot { Object = dot, Expires = Time.time + 18f });
            }

            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (Time.time < path[i].Expires && path.Count <= 50) continue;
                DestroyObject(ref path[i].Object);
                path.RemoveAt(i);
            }
        }

        internal bool HasTelemetry
        {
            get { return FpsHud || ClockHud || PositionHud || HeadingHud || FrameTimeHud || SessionHud || TrackingHud || BatteryHud; }
        }

        internal string LocalTelemetry()
        {
            if (!HasTelemetry) { cachedTelemetry = ""; nextTelemetry = 0f; return ""; }
            if (Time.unscaledTime < nextTelemetry) return cachedTelemetry;
            nextTelemetry = Time.unscaledTime + .25f;
            telemetry.Length = 0;
            if (FpsHud) telemetry.Append("FPS ").Append(plugin.FramesPerSecond.ToString("0")).AppendLine();
            if (FrameTimeHud) telemetry.Append("FRAME ").Append(plugin.FrameMilliseconds.ToString("0.0")).AppendLine(" ms");
            if (ClockHud) telemetry.AppendLine(DateTime.Now.ToString("HH:mm:ss"));
            if (SessionHud) telemetry.Append("SESSION ").AppendLine(TimeSpan.FromSeconds(Mathf.Max(0f, Time.realtimeSinceStartup - plugin.SessionStarted)).ToString(@"hh\:mm\:ss"));
            if (PositionHud)
            {
                Vector3 p = plugin.Game.Position;
                telemetry.Append("XYZ ").Append(p.x.ToString("0.0")).Append(" / ").Append(p.y.ToString("0.0")).Append(" / ").AppendLine(p.z.ToString("0.0"));
            }
            if (HeadingHud) telemetry.Append("YAW ").AppendLine(plugin.Game.Heading.ToString("0"));
            if (TrackingHud || BatteryHud)
            {
                InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (TrackingHud)
                    telemetry.Append("XR H:").Append(InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid ? "OK" : "--")
                        .Append(" L:").Append(left.isValid ? "OK" : "--").Append(" R:").AppendLine(right.isValid ? "OK" : "--");
                if (BatteryHud) telemetry.Append("BAT L:").Append(Battery(left)).Append(" R:").AppendLine(Battery(right));
            }
            cachedTelemetry = telemetry.ToString().TrimEnd();
            return cachedTelemetry;
        }

        private static string Battery(InputDevice device)
        {
            float value;
            return device.isValid && device.TryGetFeatureValue(CommonUsages.batteryLevel, out value) ?
                (Mathf.Clamp01(value) * 100f).ToString("0") + "%" : "N/A";
        }

        private void UpdateHud(Camera head)
        {
            if (!HasTelemetry || head == null)
            {
                if (hud != null) { UnityEngine.Object.Destroy(hud.gameObject); hud = null; }
                return;
            }
            if (hud == null)
            {
                var go = new GameObject("Volas VR telemetry");
                go.transform.SetParent(head.transform, false);
                go.transform.localPosition = new Vector3(-.20f, .17f, .85f);
                hud = go.AddComponent<TextMesh>();
                hud.anchor = TextAnchor.UpperLeft;
                hud.alignment = TextAlignment.Left;
                hud.fontSize = 54;
                hud.characterSize = .0025f;
                hud.color = Color.white;
                hud.fontStyle = FontStyle.Bold;
            }
            if (hud.transform.parent != head.transform) hud.transform.SetParent(head.transform, false);
            if (Time.unscaledTime < nextHudUpdate) return;
            nextHudUpdate = Time.unscaledTime + .25f;
            string value = LocalTelemetry();
            if (hud.text != value) hud.text = value;
        }

        private static GameObject Sphere(string name, float diameter)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = Vector3.one * diameter;
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = NewMaterial(Color.white);
            return go;
        }

        private static GameObject Trail(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = NewMaterial(Color.white);
            trail.time = 0.7f;
            trail.startWidth = 0.025f;
            trail.endWidth = 0.001f;
            trail.minVertexDistance = 0.015f;
            trail.startColor = Color.cyan;
            trail.endColor = new Color(0f, 0.8f, 1f, 0f);
            return go;
        }

        private static GameObject LightObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.5f;
            light.intensity = 1.1f;
            return go;
        }

        private static LineRenderer Line(string name, int count, float width, bool loop)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = count;
            lr.loop = loop;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.sharedMaterial = NewMaterial(Color.white);
            return lr;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void Tint(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null) renderer.sharedMaterial.color = color;
        }

        private static void DestroyObject(ref GameObject obj)
        {
            if (obj == null) return;
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null) UnityEngine.Object.Destroy(renderer.sharedMaterial);
            UnityEngine.Object.Destroy(obj);
            obj = null;
        }

        private static void DestroyLine(ref LineRenderer line)
        {
            if (line == null) return;
            if (line.sharedMaterial != null) UnityEngine.Object.Destroy(line.sharedMaterial);
            UnityEngine.Object.Destroy(line.gameObject);
            line = null;
        }

        private void ClearPath()
        {
            foreach (var item in path)
                DestroyObject(ref item.Object);
            path.Clear();
            hasPathPos = false;
        }

        internal void Dispose()
        {
            DestroyObject(ref leftOrb);
            DestroyObject(ref rightOrb);
            DestroyObject(ref leftTrailObject);
            DestroyObject(ref rightTrailObject);
            DestroyObject(ref leftLightObject);
            DestroyObject(ref rightLightObject);
            DestroyLine(ref beam);
            DestroyLine(ref headRing);
            if (hud != null) UnityEngine.Object.Destroy(hud.gameObject);
            hud = null;
            ClearPath();
        }
    }
}
