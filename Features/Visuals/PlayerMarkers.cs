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
        internal bool Boxes, Labels, Preview;
        internal string Status { get; private set; } = "Markers disabled";
        private Marker preview;
        private Vector3 previewPosition;
        private readonly List<Marker> pool = new List<Marker>(12);
        private readonly List<Component> targets = new List<Component>(12);
        private Type rigType;
        private Material material;
        private Font labelFont;
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
            Camera camera = (Boxes || Labels || Preview) ? plugin.Game.HeadCamera : null;
            try { UpdatePreview(camera); } catch (Exception ex) { Preview = false; HidePreview(); plugin.Features.Disable(plugin.Features.Find("marker_preview")); plugin.Log.LogWarning("Marker preview: " + ex.Message); }
            if (!Boxes && !Labels) { if (active) Hide(); Status = Preview ? "Local preview active" : "Markers disabled"; return; }
            if (!plugin.Game.IsInModdedRoom()) { if (active) Hide(); Status = "Recognized MODDED room required"; return; }
            if (camera == null) { if (active) Hide(); Status = "Waiting for camera"; return; }
            try
            {
                active = true;
                float now = Time.unscaledTime;
                if (now >= nextScan)
                {
                    nextScan = now + 1.25f;
                    Scan();
                    nextRefresh = 0f;
                }
                if (now >= nextRefresh)
                {
                    nextRefresh = now + plugin.Settings.MarkerRefresh.Value;
                    Refresh(camera);
                }
                for (int i = 0; i < pool.Count; i++)
                {
                    Marker m = pool[i];
                    if (m.Root == null || !m.Root.activeSelf) continue;
                    if (m.Target == null || !m.Target.gameObject.activeInHierarchy || (m.Target.transform.position - camera.transform.position).sqrMagnitude > plugin.Settings.MarkerRange.Value * plugin.Settings.MarkerRange.Value)
                    { m.Root.SetActive(false); continue; }
                    m.Root.transform.position = m.Target.transform.position;
                    if (Labels) m.Label.transform.rotation = camera.transform.rotation;
                }
            }
            catch (Exception ex)
            {
                Hide();
                Status = "Marker error: " + ex.GetBaseException().Message;
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
                if (object.Equals(GameBridge.GetMember(rigType, rig, "isOfflineVRRig"), true) ||
                    object.Equals(GameBridge.GetMember(rigType, rig, "isLocal"), true)) continue;
                object owner = GameBridge.GetMember(rigType, rig, "creator") ?? GameBridge.GetMember(rigType, rig, "netPlayer") ?? GameBridge.GetMember(rigType, rig, "Owner");
                object local = owner == null ? null : GameBridge.GetMember(owner.GetType(), owner, "IsLocal", "isLocal");
                object view = GameBridge.GetMember(rigType, rig, "photonView");
                object mine = view == null ? null : GameBridge.GetMember(view.GetType(), view, "IsMine");
                // Exclude unknown and conflicting ownership signals.
                if (object.Equals(local, true) || object.Equals(mine, true)) continue;
                if (!object.Equals(local, false) && !object.Equals(mine, false)) continue;
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
            pool.RemoveAll(m => m.Root == null);
            targets.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);
            targets.Sort((a,b) => (a.transform.position-center).sqrMagnitude.CompareTo((b.transform.position-center).sqrMagnitude));
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
            for (int i = used; i < pool.Count; i++) { pool[i].Target = null; if (pool[i].Root.activeSelf) pool[i].Root.SetActive(false); }
            Status = rigType == null ? "VRRig type unavailable" : used + " in-range markers / " + targets.Count + " recognized rigs";
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
            UnityEngine.Object.DontDestroyOnLoad(root);
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
            if (labelFont == null)
            {
                try { labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (labelFont == null) try { labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { }
            }
            if (labelFont != null)
            {
                label.font = labelFont;
                labelObject.GetComponent<MeshRenderer>().sharedMaterial = labelFont.material;
            }
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = .014f;
            return new Marker { Root = root, Line = line, Label = label };
        }

        #region Local preview
        private void UpdatePreview(Camera camera)
        {
            if (!Preview || camera == null || !plugin.Game.IsSoloOrModdedRoom()) { HidePreview(); return; }
            if (preview == null || preview.Root == null) preview = Create();
            if (!preview.Root.activeSelf)
            {
                previewPosition = camera.transform.position + camera.transform.forward * 2.5f;
                preview.Root.SetActive(true);
                preview.Label.text = "PREVIEW";
            }
            preview.Root.transform.position = previewPosition;
            preview.Line.startWidth = preview.Line.endWidth = plugin.Settings.MarkerWidth.Value;
            preview.Line.startColor = preview.Line.endColor = Color.white;
            preview.Label.transform.rotation = camera.transform.rotation;
        }
        private void HidePreview() { if (preview != null && preview.Root != null && preview.Root.activeSelf) preview.Root.SetActive(false); }
        #endregion

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
            if (preview != null && preview.Root != null) UnityEngine.Object.Destroy(preview.Root);
            preview = null;
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
