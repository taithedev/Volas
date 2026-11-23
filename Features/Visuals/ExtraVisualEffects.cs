using System;
using UnityEngine;

namespace Volas
{
    // All effects are client-side; no RPCs or other player's objects are modified.
    internal sealed class ExtraVisualEffects
    {
        private readonly VolasPlugin plugin;
        internal bool Headlamp;
        internal bool OrbitingOrbs;
        internal bool BodyAura;
        internal bool HandAxes;

        private GameObject lamp;
        private GameObject[] orbs;
        private LineRenderer aura;
        private GameObject axes;
        private Renderer[] orbRenderers;
        private float nextColorUpdate;

        internal ExtraVisualEffects(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            UpdateLamp();
            UpdateOrbs();
            UpdateAura();
            UpdateAxes();
        }

        private void UpdateLamp()
        {
            Camera cam = plugin.Game.HeadCamera;
            if (!Headlamp || cam == null)
            {
                DestroyObject(ref lamp);
                return;
            }

            if (lamp == null)
            {
                lamp = new GameObject("Volas headlamp");
                lamp.transform.SetParent(cam.transform, false);
                lamp.transform.localPosition = new Vector3(0f, 0.09f, 0.045f);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = 16f;
                light.spotAngle = 48f;
                light.intensity = 1.8f;
            }
            if (lamp.transform.parent != cam.transform)
                lamp.transform.SetParent(cam.transform, false);
            lamp.GetComponent<Light>().color = plugin.Accent;
        }

        private void UpdateOrbs()
        {
            Camera head = plugin.Game.HeadCamera;
            if (!OrbitingOrbs || head == null)
            {
                ClearOrbs();
                return;
            }

            if (orbs == null)
            {
                orbs = new GameObject[3];
                orbRenderers = new Renderer[3];
                for (int i = 0; i < 3; i++)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.name = "Volas orbiting orb " + (i + 1);
                    obj.transform.localScale = Vector3.one * 0.095f;
                    var collider = obj.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                    var renderer = obj.GetComponent<Renderer>();
                    renderer.material = MaterialFor(plugin.Accent);
                    orbs[i] = obj;
                    orbRenderers[i] = renderer;
                }
            }

            Vector3 center = head.transform.position + Vector3.up * 0.09f;
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] == null) continue;
                float angle = Time.time * 1.7f + i * Mathf.PI * 2f / orbs.Length;
                orbs[i].transform.position = center +
                    new Vector3(Mathf.Cos(angle) * 0.43f, Mathf.Sin(angle * 2f) * 0.09f,
                        Mathf.Sin(angle) * 0.43f);
            }
            if (Time.time >= nextColorUpdate)
            {
                nextColorUpdate = Time.time + 0.12f;
                for (int i = 0; i < orbRenderers.Length; i++)
                    if (orbRenderers[i] != null) orbRenderers[i].sharedMaterial.color = plugin.Accent;
            }
        }

        private void UpdateAura()
        {
            var body = plugin.Game.Body;
            Camera cam = plugin.Game.HeadCamera;
            if (!BodyAura || (body == null && cam == null))
            {
                if (aura != null)
                {
                    var obj = aura.gameObject;
                    DestroyRendererMaterial(aura);
                    UnityEngine.Object.Destroy(obj);
                    aura = null;
                }
                return;
            }

            if (aura == null)
            {
                var obj = new GameObject("Volas local aura ring");
                aura = obj.AddComponent<LineRenderer>();
                aura.positionCount = 48;
                aura.loop = true;
                aura.useWorldSpace = true;
                aura.startWidth = 0.022f;
                aura.endWidth = 0.022f;
                aura.material = MaterialFor(plugin.Accent);
            }

            Vector3 center = body != null ? body.position : cam.transform.position - Vector3.up * 0.35f;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                aura.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * 0.5f,
                    0.1f + Mathf.Sin(Time.time * 2f) * 0.04f, Mathf.Sin(angle) * 0.5f));
            }
            aura.sharedMaterial.color = plugin.Accent;
        }

        private void UpdateAxes()
        {
            Transform hand = plugin.Game.RightHand;
            if (!HandAxes || hand == null)
            {
                DestroyObject(ref axes);
                return;
            }

            if (axes == null)
            {
                axes = new GameObject("Volas right-hand axis guide");
                axes.transform.SetParent(hand, false);
                CreateAxis("X", Vector3.right, Color.red);
                CreateAxis("Y", Vector3.up, Color.green);
                CreateAxis("Z", Vector3.forward, Color.blue);
            }
            if (axes.transform.parent != hand)
                axes.transform.SetParent(hand, false);
        }

        private void CreateAxis(string name, Vector3 direction, Color color)
        {
            var obj = new GameObject("Axis " + name);
            obj.transform.SetParent(axes.transform, false);
            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = 0.009f;
            line.endWidth = 0.003f;
            line.material = MaterialFor(color);
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, direction * 0.24f);
        }

        private static Material MaterialFor(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Volas visuals: shader unavailable.");
            return new Material(shader) { color = color };
        }

        private static void DestroyRendererMaterial(Renderer renderer)
        {
            if (renderer != null && renderer.sharedMaterial != null)
                UnityEngine.Object.Destroy(renderer.sharedMaterial);
        }

        private static void DestroyObject(ref GameObject obj)
        {
            if (obj == null) return;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
                DestroyRendererMaterial(renderer);
            UnityEngine.Object.Destroy(obj);
            obj = null;
        }

        private void ClearOrbs()
        {
            if (orbs != null)
                for (int i = 0; i < orbs.Length; i++)
                    DestroyObject(ref orbs[i]);
            orbs = null;
            orbRenderers = null;
        }

        internal void Reset()
        {
            DestroyObject(ref lamp);
            ClearOrbs();
            DestroyObject(ref axes);
            if (aura != null)
            {
                DestroyRendererMaterial(aura);
                UnityEngine.Object.Destroy(aura.gameObject);
                aura = null;
            }
        }

        internal void Dispose() { Reset(); }
    }
}
