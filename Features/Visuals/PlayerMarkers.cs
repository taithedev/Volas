using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Volas
{
    // Local, depth-tested overlays; only recognized modded multiplayer rooms.
    internal sealed class PlayerMarkers
    {
        private readonly VolasPlugin plugin;
        internal bool Boxes, Labels;
        private readonly List<Marker> pool = new List<Marker>(12);
        private readonly List<Component> targets = new List<Component>(12);
        private Type rigType;
        private Material material;
        private float nextScan, nextRefresh;
        private bool active, faultLogged;
        private sealed class Marker
        {
            internal GameObject Root;
            internal LineRenderer Line;
            internal TextMesh Label;
            internal Component Target;
        }

        internal PlayerMarkers(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            if (!Boxes && !Labels) { if (active) Hide(); return; }
            if (!plugin.Game.IsInModdedRoom()) { if (active) Hide(); return; }
            Camera camera = plugin.Game.HeadCamera;
            if (camera == null) { if (active) Hide(); return; }
            try
            {
                active = true;
                float now = Time.unscaledTime;
                if (now >= nextScan)
                {
                    nextScan = now + 1.25f;
                    Scan();
                }
                if (now >= nextRefresh)
                {
                    nextRefresh = now + plugin.Settings.MarkerRefresh.Value;
                    Refresh(camera);
                }
                for (int i = 0; i < pool.Count; i++)
                {
                    Marker m = pool[i];
                    if (!m.Root.activeSelf) continue;
                    if (m.Target == null || !m.Target.gameObject.activeInHierarchy)
                    { m.Root.SetActive(false); continue; }
                    m.Root.transform.position = m.Target.transform.position;
                    m.Label.transform.rotation = camera.transform.rotation;
                }
            }
            catch (Exception ex)
            {
                Hide();
                if (!faultLogged) { faultLogged = true; plugin.Log.LogWarning("Player marker compatibility: " + ex.Message); }
                plugin.Features.Disable(plugin.Features.Find("marker_boxes"));
                plugin.Features.Disable(plugin.Features.Find("marker_labels"));
            }
        }

        private void Scan()
        {
            targets.Clear();
            if (rigType == null) rigType = GameBridge.FindType("VRRig");
            if (rigType == null || !typeof(Component).IsAssignableFrom(rigType)) return;
            foreach (UnityEngine.Object obj in UnityEngine.Object.FindObjectsOfType(rigType))
            {
                Component rig = obj as Component;
                if (rig == null || !rig.gameObject.activeInHierarchy) continue;
                object offline = GameBridge.GetMember(rigType, rig, "isOfflineVRRig", "isLocal");
                if (object.Equals(offline, true)) continue;
                object owner = GameBridge.GetMember(rigType, rig, "creator", "netPlayer", "Owner");
                object local = owner == null ? null : GameBridge.GetMember(owner.GetType(), owner, "IsLocal", "isLocal");
                object view = GameBridge.GetMember(rigType, rig, "photonView");
                if (!(local is bool) && view != null)
                    local = GameBridge.GetMember(view.GetType(), view, "IsMine");
                // Do not guess whether unknown rigs are remote.
                if (!object.Equals(local, false)) continue;
                targets.Add(rig);
            }
        }

        private void Refresh(Camera camera)
        {
            Vector3 center = camera.transform.position;
            float max = plugin.Settings.MarkerRange.Value;
            float maxSq = max * max;
            int limit = plugin.Settings.MarkerLimit.Value;
            int used = 0;
            for (int i = 0; i < targets.Count && used < limit; i++)
            {
                Component target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                float d = (target.transform.position - center).sqrMagnitude;
                if (d > maxSq) continue;
                if (used == pool.Count) pool.Add(Create());
                Marker m = pool[used++];
                m.Target = target;
                m.Root.transform.position = target.transform.position;
                m.Line.enabled = Boxes;
                m.Line.startWidth = m.Line.endWidth = plugin.Settings.MarkerWidth.Value;
                m.Line.startColor = m.Line.endColor = plugin.Accent;
                m.Label.gameObject.SetActive(Labels);
                if (Labels) { m.Label.text = Mathf.Sqrt(d).ToString("0.0") + " m"; m.Label.color = plugin.Accent; }
                m.Root.SetActive(true);
            }
            for (int i = used; i < pool.Count; i++) { pool[i].Target = null; pool[i].Root.SetActive(false); }
        }

        private Marker Create()
        {
            if (material == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("Depth-tested marker shader unavailable.");
                material = new Material(shader) { color = Color.white };
            }
            GameObject root = new GameObject("Volas player marker");
            root.SetActive(false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 16;
            float w = .34f, h = .55f;
            Vector3 a = new Vector3(-w,-h,-w), b = new Vector3(w,-h,-w), c = new Vector3(w,-h,w), d = new Vector3(-w,-h,w);
            Vector3 e = new Vector3(-w,h,-w), f = new Vector3(w,h,-w), g = new Vector3(w,h,w), k = new Vector3(-w,h,w);
            line.SetPositions(new[] { a,b,c,d,a,e,f,b,f,g,c,g,k,d,k,e });
            var labelObject = new GameObject("Distance");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f,.78f,0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = .014f;
            return new Marker { Root = root, Line = line, Label = label };
        }

        private void Hide()
        {
            active = false;
            targets.Clear();
            nextScan = nextRefresh = 0f;
            foreach (Marker m in pool) { m.Target = null; if (m.Root != null) m.Root.SetActive(false); }
        }

        internal void Dispose()
        {
            Hide();
            foreach (Marker m in pool) if (m.Root != null) UnityEngine.Object.Destroy(m.Root);
            pool.Clear();
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
