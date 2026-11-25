using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Volas
{
    internal static class CharmFeatures
    {
        #region Registration
        internal static void Register(FeatureRegistry registry)
        {
            string[] names = { "Star charm", "Diamond charm", "Heart charm", "Moon charm", "Lightning charm", "Orbit charm", "Mini crown", "Crystal charm", "Comet charm" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                registry.Add(new Feature("charm_" + i, "Charms", names[i], "Local animated wrist cosmetic. Customize in Charm Studio.",
                    p => p.Charms.Set(index, true), p => p.Charms.Set(index, false)));
            }
        }
        #endregion
    }

    internal sealed class LocalCharms
    {
        #region State
        private readonly VolasPlugin plugin;
        private readonly bool[] enabled = new bool[9];
        private readonly LineRenderer[] charms = new LineRenderer[9];
        private Material material;
        private GameObject root;
        private bool faultLogged;
        internal LocalCharms(VolasPlugin owner) { plugin = owner; }
        internal void Set(int index, bool value) { if (index >= 0 && index < enabled.Length) enabled[index] = value; }
        #endregion

        #region Runtime
        internal void Tick()
        {
            bool any = false;
            foreach (bool state in enabled) if (state) { any = true; break; }
            if (!any) { if (root != null && root.activeSelf) root.SetActive(false); return; }
            try
            {
                var hand = plugin.Settings.CharmRightHand.Value ? plugin.Game.RightHand : plugin.Game.LeftHand;
                if (hand == null) { if (root != null && root.activeSelf) root.SetActive(false); return; }
                if (root == null) root = new GameObject("Volas local charms");
                if (root.transform.parent != hand) root.transform.SetParent(hand, false);
                root.transform.localPosition = new Vector3(0f, -.045f, .10f);
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one * plugin.Settings.CharmScale.Value;
                if (!root.activeSelf) root.SetActive(true);
                float time = Time.unscaledTime * plugin.Settings.CharmSpeed.Value;
                Color color = Color.HSVToRGB(plugin.Settings.CharmRainbow.Value ? Mathf.Repeat(time * .15f, 1f) : plugin.Settings.CharmHue.Value, .65f, 1f);
                int slot = 0;
                for (int i = 0; i < enabled.Length; i++)
                {
                    var line = charms[i];
                    if (!enabled[i]) { if (line != null && line.gameObject.activeSelf) line.gameObject.SetActive(false); continue; }
                    if (line == null) charms[i] = line = Build(i);
                    if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
                    float angle = slot++ * 2.399963f;
                    line.transform.localPosition = new Vector3(Mathf.Cos(angle) * .07f, Mathf.Sin(angle) * .07f, Mathf.Sin(time * 2f + i) * .008f);
                    line.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 2f + i) * 22f, Mathf.Sin(time * 2f + i) * 12f);
                    if (line.startColor != color) line.startColor = line.endColor = color;
                }
            }
            catch (Exception ex)
            {
                if (!faultLogged) { faultLogged = true; plugin.Log.LogWarning("Local charms unavailable: " + ex.Message); }
                for (int i = 0; i < enabled.Length; i++) if (enabled[i]) plugin.Features.Disable(plugin.Features.Find("charm_" + i));
                Dispose();
            }
        }
        #endregion

        #region Geometry
        private LineRenderer Build(int index)
        {
            if (material == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("No compatible charm shader");
                material = new Material(shader) { color = Color.white };
            }
            var obj = new GameObject("Volas charm " + index);
            obj.transform.SetParent(root.transform, false);
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false; line.loop = true;
            line.startWidth = line.endWidth = .003f;
            line.numCornerVertices = 2; line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            var points = Shape(index);
            line.positionCount = points.Length; line.SetPositions(points);
            return line;
        }

        private static Vector3[] Shape(int index)
        {
            if (index == 1) return new[] { P(0,1), P(.75f,0), P(0,-1), P(-.75f,0) };
            if (index == 4) return new[] { P(.2f,1), P(-.8f,-.1f), P(-.1f,-.1f), P(-.45f,-1), P(.9f,.3f), P(.1f,.3f) };
            if (index == 6) return new[] { P(-1,-.6f), P(-1,.7f), P(-.4f,0), P(0,1), P(.4f,0), P(1,.7f), P(1,-.6f) };
            if (index == 7) return new[] { P(-.6f,.6f), P(0,1), P(.6f,.6f), P(.6f,-.5f), P(0,-1), P(-.6f,-.5f) };
            int n = index == 0 ? 10 : 48;
            var points = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                float x = Mathf.Cos(a), y = Mathf.Sin(a);
                if (index == 0) { float r = i % 2 == 0 ? 1f : .45f; x = Mathf.Sin(a) * r; y = Mathf.Cos(a) * r; }
                else if (index == 2) { float v = Mathf.Sin(a); x = v*v*v; y = (13f*Mathf.Cos(a)-5f*Mathf.Cos(2*a)-2f*Mathf.Cos(3*a)-Mathf.Cos(4*a))/17f; }
                else if (index == 3) { if (i >= n/2) { x = .55f*Mathf.Cos(a) + .35f; y = .9f*Mathf.Sin(a); } }
                else if (index == 5) { x *= 1.2f; y *= .55f; }
                else if (index == 8) { x *= 1f + .3f*Mathf.Cos(a); y *= .55f; }
                points[i] = P(x,y);
            }
            return points;
        }
        private static Vector3 P(float x, float y) { return new Vector3(x*.028f,y*.028f,0f); }
        #endregion

        #region Cleanup
        internal void Dispose()
        {
            Array.Clear(enabled,0,enabled.Length);
            if (root != null) UnityEngine.Object.Destroy(root);
            root = null;
            Array.Clear(charms,0,charms.Length);
            if (material != null) UnityEngine.Object.Destroy(material);
            material = null;
        }
        #endregion
    }
}
