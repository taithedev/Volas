using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Volas
{
    // Opt-in cosmetic renderer. It never sends game RPCs, room identifiers, or
    // avatar state. Neon owns the shared schedule; each client renders locally.
    internal sealed class CommunityEffects
    {
        private readonly VolasPlugin plugin;
        private LineRenderer ring;
        private Material material;
        private float nextRoomCheck;
        private bool roomAllowed;
        private bool faultLogged;
        internal CommunityEffects(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            string preset = plugin.Licensing.ActiveEvent;
            if (!plugin.Licensing.CommunityEffects || preset.Length == 0) { Hide(); return; }
            if (Time.unscaledTime >= nextRoomCheck)
            {
                nextRoomCheck = Time.unscaledTime + .5f;
                roomAllowed = plugin.Game.IsSoloOrModdedRoom();
            }
            if (!roomAllowed) { Hide(); return; }
            try
            {
                Transform hand = plugin.Game.LeftHand;
                if (hand == null) { Hide(); return; }
                if (ring == null) Build();
                if (ring.transform.parent != hand) ring.transform.SetParent(hand, false);
                float t = Time.unscaledTime;
                ring.transform.localPosition = new Vector3(0f, 0f, .055f);
                ring.transform.localRotation = preset == "orbit" ? Quaternion.Euler(t*35f,t*55f,0f) : Quaternion.identity;
                ring.transform.localScale = Vector3.one * (preset == "pulse" ? 1f + .16f*Mathf.Sin(t*3f) : 1f);
                Color color = preset == "aurora" ? Color.HSVToRGB(Mathf.Repeat(t*.08f,1f),.55f,1f) :
                    preset == "pulse" ? new Color(.5f,.7f,1f) : new Color(.75f,.5f,1f);
                ring.startColor = ring.endColor = color;
                if (!ring.gameObject.activeSelf) ring.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                if (!faultLogged) { faultLogged = true; plugin.Log.LogWarning("Community cosmetic unavailable: " + ex.Message); }
                plugin.Licensing.SetCommunityEffects(false); Hide();
            }
        }

        private void Build()
        {
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("No compatible cosmetic shader");
                material = new Material(shader) { color = Color.white };
            }
            var obj = new GameObject("Volas community wrist ring");
            ring = obj.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.loop = true; ring.useWorldSpace = false;
            ring.positionCount = 40;
            ring.startWidth = ring.endWidth = .005f;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i=0;i<40;i++)
            {
                float a = i*Mathf.PI*2f/40f;
                ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.12f,Mathf.Sin(a)*.12f,0f));
            }
        }
        internal void Hide() { if (ring != null && ring.gameObject.activeSelf) ring.gameObject.SetActive(false); }
        internal void Dispose()
        {
            if (ring != null) UnityEngine.Object.Destroy(ring.gameObject);
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
