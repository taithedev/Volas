using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    /// <summary>Local-only GunLib effects. Never sends Photon events or edits other players.</summary>
    internal sealed class GunEffects
    {
        private readonly VolasPlugin plugin;
        private readonly List<Transient> objects = new List<Transient>();
        private readonly List<Pulse> pulses = new List<Pulse>();
        private GameObject root;
        private TextMesh display;
        private GameObject spotlightObject;
        private float nextTrail;
        private Vector3 lastTrail;
        private bool hasLastTrail;
        private bool firstMeasurePoint;
        private Vector3 measureStart;
        private bool grappleLocked;
        private Vector3 grapplePoint;

        internal bool Marker;
        internal bool Paint;
        internal bool Beacon;
        internal bool Ring;
        internal bool PulseGun;
        internal bool Confetti;
        internal bool Trail;
        internal bool Inspector;
        internal bool Ruler;
        internal bool Distance;
        internal bool Coordinates;
        internal bool Spotlight;
        internal bool Grapple;
        internal bool Dash;

        private sealed class Transient
        {
            internal GameObject Object;
            internal float ExpiresAt;
        }

        private sealed class Pulse
        {
            internal LineRenderer Line;
            internal Vector3 Origin;
            internal Quaternion Orientation;
            internal float CreatedAt;
            internal float Duration;
        }

        internal GunEffects(VolasPlugin owner)
        {
            plugin = owner;
            plugin.Gun.Fired += OnFire;
        }

        internal bool AnyEnabled
        {
            get
            {
                return Marker || Paint || Beacon || Ring || PulseGun || Confetti || Trail ||
                       Inspector || Ruler || Distance || Coordinates || Spotlight || Grapple || Dash;
            }
        }

        private void OnFire(GunTarget target)
        {
            if (!AnyEnabled) return;

            if (Dash && plugin.Game.IsSoloOrModdedRoom())
            {
                var body = plugin.Game.Body;
                if (body != null)
                    body.AddForce(target.Direction * 5.5f, ForceMode.VelocityChange);
            }

            if (!target.HasHit) return;

            if (Marker) MakeDot("Marker", target.Point + target.Normal * 0.05f, 0.13f, plugin.Accent, 45f);
            if (Paint) PlacePaint(target);
            if (Beacon) PlaceBeacon(target);
            if (Ring) PlaceRing(target, 0.36f, 35f);
            if (PulseGun) MakePulse(target, plugin.Accent);
            if (Confetti) PlaceConfetti(target);

            if (Ruler)
            {
                if (!firstMeasurePoint)
                {
                    firstMeasurePoint = true;
                    measureStart = target.Point;
                    MakeDot("Measure start", measureStart, 0.09f, Color.yellow, 45f);
                }
                else
                {
                    DrawRuler(measureStart, target.Point);
                    firstMeasurePoint = false;
                }
            }

            // Movement-changing modes are opt-in and restricted to solo or explicitly MODDED rooms.
            if (Grapple && plugin.Game.IsSoloOrModdedRoom() && plugin.Game.Body != null)
            {
                grapplePoint = target.Point;
                grappleLocked = true;
            }

        }

        internal void Tick()
        {
            CullExpired();
            AnimatePulses();

            if (!AnyEnabled)
            {
                Reset();
                return;
            }

            var gun = plugin.Gun;
            var target = gun.Target;
            bool hit = gun.IsAiming && target.HasHit;
            if (hit && Trail && gun.TriggerHeld && Time.time >= nextTrail &&
                (!hasLastTrail || Vector3.Distance(lastTrail, target.Point) > 0.17f))
            {
                MakeDot("Gun trail", target.Point + target.Normal * 0.025f, 0.045f, plugin.Accent, 15f);
                nextTrail = Time.time + 0.07f;
                lastTrail = target.Point;
                hasLastTrail = true;
            }
            if (!gun.TriggerHeld || !gun.IsAiming)
            {
                hasLastTrail = false;
                grappleLocked = false;
            }

            UpdateSpotlight(hit, target);
            UpdateText(hit, target);
        }

        internal void FixedTick()
        {
            if (!Grapple || !grappleLocked || !plugin.Gun.IsAiming || !plugin.Gun.TriggerHeld ||
                !plugin.Game.IsSoloOrModdedRoom())
            {
                grappleLocked = false;
                return;
            }
            Rigidbody body = plugin.Game.Body;
            if (body == null) return;
            var delta = grapplePoint - body.worldCenterOfMass;
            if (delta.sqrMagnitude < 1f) return;
            body.AddForce(delta.normalized * 25f, ForceMode.Acceleration);
        }

        private void PlacePaint(GunTarget target)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Volas local paint";
            disc.transform.SetParent(Root().transform, true);
            disc.transform.position = target.Point + target.Normal * 0.01f;
            disc.transform.rotation = Quaternion.FromToRotation(Vector3.up, target.Normal);
            disc.transform.localScale = new Vector3(0.15f, 0.002f, 0.15f);
            SetUnlit(disc, Color.HSVToRGB(Mathf.Repeat(Time.time * 0.13f, 1f), 0.8f, 1f));
            Track(disc, 55f);
        }

        private void PlaceBeacon(GunTarget target)
        {
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "Volas local beacon";
            rod.transform.SetParent(Root().transform, true);
            rod.transform.position = target.Point + Vector3.up * 1.25f;
            rod.transform.localScale = new Vector3(0.035f, 1.25f, 0.035f);
            SetUnlit(rod, plugin.Accent);
            Track(rod, 40f);
            MakeDot("Beacon tip", target.Point + Vector3.up * 2.5f, 0.15f, plugin.Accent, 40f);
        }

        private void PlaceRing(GunTarget target, float radius, float seconds)
        {
            var line = MakeLine("Volas surface ring", 40, 0.017f, plugin.Accent, true);
            var orientation = Quaternion.FromToRotation(Vector3.up, target.Normal);
            Vector3 center = target.Point + target.Normal * 0.02f;
            for (int i = 0; i < 40; i++)
            {
                float phase = i * Mathf.PI * 2f / 40f;
                var local = new Vector3(Mathf.Cos(phase) * radius, 0f, Mathf.Sin(phase) * radius);
                line.SetPosition(i, center + orientation * local);
            }
            Track(line.gameObject, seconds);
        }

        private void MakePulse(GunTarget target, Color color)
        {
            var line = MakeLine("Volas expanding pulse", 36, 0.016f, color, true);
            pulses.Add(new Pulse
            {
                Line = line,
                Origin = target.Point + target.Normal * 0.025f,
                Orientation = Quaternion.FromToRotation(Vector3.up, target.Normal),
                CreatedAt = Time.time,
                Duration = 1.35f
            });
            Track(line.gameObject, 1.4f);
        }

        private void AnimatePulses()
        {
            for (int j = pulses.Count - 1; j >= 0; j--)
            {
                var pulse = pulses[j];
                if (pulse.Line == null || Time.time > pulse.CreatedAt + pulse.Duration)
                {
                    pulses.RemoveAt(j);
                    continue;
                }
                float t = Mathf.Clamp01((Time.time - pulse.CreatedAt) / pulse.Duration);
                float radius = Mathf.Lerp(0.10f, 1.7f, t);
                for (int i = 0; i < 36; i++)
                {
                    float phase = i * 2f * Mathf.PI / 36f;
                    pulse.Line.SetPosition(i, pulse.Origin + pulse.Orientation *
                        new Vector3(Mathf.Cos(phase) * radius, 0f, Mathf.Sin(phase) * radius));
                }
                pulse.Line.startWidth = Mathf.Lerp(0.025f, 0.002f, t);
                pulse.Line.endWidth = pulse.Line.startWidth;
            }
        }

        private void PlaceConfetti(GunTarget target)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.32f + target.Normal * 0.15f;
                MakeDot("Volas confetti", target.Point + offset, 0.035f,
                    Color.HSVToRGB(UnityEngine.Random.value, 0.85f, 1f), 1.8f);
            }
        }

        private void DrawRuler(Vector3 start, Vector3 end)
        {
            var line = MakeLine("Volas ruler", 2, 0.012f, plugin.Accent, false);
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Track(line.gameObject, 45f);
            MakeWorldText(Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.12f,
                Vector3.Distance(start, end).ToString("0.00") + " m", 45f);
        }

        private void UpdateSpotlight(bool hit, GunTarget target)
        {
            if (!Spotlight || !hit)
            {
                if (spotlightObject != null) spotlightObject.SetActive(false);
                return;
            }
            if (spotlightObject == null)
            {
                spotlightObject = new GameObject("Volas target spotlight");
                spotlightObject.transform.SetParent(Root().transform, true);
                var light = spotlightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 3f;
                light.intensity = 1.9f;
            }
            spotlightObject.SetActive(true);
            spotlightObject.transform.position = target.Point + target.Normal * 0.10f;
            spotlightObject.GetComponent<Light>().color = plugin.Accent;
        }

        private void UpdateText(bool hit, GunTarget target)
        {
            if (!hit || (!Inspector && !Distance && !Coordinates))
            {
                if (display != null) display.gameObject.SetActive(false);
                return;
            }
            if (display == null)
            {
                var go = new GameObject("Volas GunLib target readout");
                go.transform.SetParent(Root().transform, true);
                display = go.AddComponent<TextMesh>();
                display.anchor = TextAnchor.LowerCenter;
                display.alignment = TextAlignment.Center;
                display.fontSize = 52;
                display.characterSize = 0.0085f;
            }
            display.gameObject.SetActive(true);
            var lines = new List<string>();
            if (Distance) lines.Add("RANGE  " + target.Distance.ToString("0.00") + " m");
            if (Coordinates)
                lines.Add("XYZ  " + target.Point.x.ToString("0.0") + " / " +
                    target.Point.y.ToString("0.0") + " / " + target.Point.z.ToString("0.0"));
            if (Inspector && target.Collider != null)
            {
                string label = target.Collider.gameObject.name;
                if (label.Length > 35) label = label.Substring(0, 35);
                lines.Add("OBJECT  " + label);
                lines.Add("LAYER  " + LayerMask.LayerToName(target.Collider.gameObject.layer));
            }
            display.text = string.Join("\n", lines.ToArray());
            display.color = plugin.Accent;
            display.transform.position = target.Point + target.Normal * 0.18f;
            var head = plugin.Game.HeadCamera;
            if (head != null)
                display.transform.rotation = Quaternion.LookRotation(
                    (head.transform.position - display.transform.position).normalized, Vector3.up);
        }

        private GameObject MakeDot(string name, Vector3 position, float size, Color color, float lifetime)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Volas " + name;
            go.transform.SetParent(Root().transform, true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            SetUnlit(go, color);
            Track(go, lifetime);
            return go;
        }

        private void MakeWorldText(Vector3 position, string value, float lifetime)
        {
            var go = new GameObject("Volas measurement text");
            go.transform.SetParent(Root().transform, true);
            go.transform.position = position;
            var label = go.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 52;
            label.characterSize = 0.009f;
            label.color = plugin.Accent;
            var head = plugin.Game.HeadCamera;
            if (head != null)
                go.transform.rotation = Quaternion.LookRotation(
                    (head.transform.position - go.transform.position).normalized, Vector3.up);
            Track(go, lifetime);
        }

        private LineRenderer MakeLine(string name, int points, float width, Color color, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root().transform, true);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = points;
            line.useWorldSpace = true;
            line.loop = loop;
            line.startWidth = width;
            line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = NewMaterial(color);
            return line;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Volas: compatible shader unavailable.");
            return new Material(shader) { color = color };
        }

        private static void SetUnlit(GameObject go, Color color)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material = NewMaterial(color);
        }

        private GameObject Root()
        {
            if (root == null) root = new GameObject("Volas local GunLib effects");
            return root;
        }

        private void Track(GameObject go, float seconds)
        {
            objects.Add(new Transient { Object = go, ExpiresAt = Time.time + seconds });
            if (objects.Count > 150) DestroyTransient(objects[0]);
            if (objects.Count > 150) objects.RemoveAt(0);
        }

        private void CullExpired()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i].Object != null && Time.time < objects[i].ExpiresAt)
                    continue;
                DestroyTransient(objects[i]);
                objects.RemoveAt(i);
            }
        }

        private static void DestroyTransient(Transient transient)
        {
            if (transient.Object == null) return;
            foreach (var renderer in transient.Object.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.sharedMaterial != null)
                    UnityEngine.Object.Destroy(renderer.sharedMaterial);
            UnityEngine.Object.Destroy(transient.Object);
        }

        internal void Reset()
        {
            foreach (var obj in objects) DestroyTransient(obj);
            objects.Clear();
            pulses.Clear();
            firstMeasurePoint = false;
            grappleLocked = false;
            hasLastTrail = false;
            if (display != null) UnityEngine.Object.Destroy(display.gameObject);
            display = null;
            if (spotlightObject != null) UnityEngine.Object.Destroy(spotlightObject);
            spotlightObject = null;
            // The shared pointer is reset by FeatureRegistry only when disabling all features.
        }

        internal void Dispose()
        {
            plugin.Gun.Fired -= OnFire;
            Reset();
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
        }
    }
}
