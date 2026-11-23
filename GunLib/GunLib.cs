using System;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    // Local-only targeting data. GunLib never sends network messages or modifies hit objects.
    public struct GunTarget
    {
        public bool HasHit { get; internal set; }
        public Vector3 Point { get; internal set; }
        public Vector3 Normal { get; internal set; }
        public Vector3 Direction { get; internal set; }
        public float Distance { get; internal set; }
        public Collider Collider { get; internal set; }
    }

    /// <summary>
    /// Reusable right-hand VR raycaster for Volas mods.
    /// Grip aims, trigger fires; game objects remain local and are cleaned up.
    /// </summary>
    public sealed class GunLib
    {
        private readonly VolasPlugin plugin;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[48];
        private GameObject reticle;
        private LineRenderer beam;
        private Material reticleMaterial;
        private Material beamMaterial;
        private bool previousTrigger;

        public static GunLib Instance { get; private set; }
        public bool IsAiming { get; private set; }
        public bool TriggerHeld { get; private set; }
        public bool FiredThisFrame { get; private set; }
        public GunTarget Target { get; private set; }
        public event Action<GunTarget> Fired;

        internal GunLib(VolasPlugin owner)
        {
            plugin = owner;
            Instance = this;
        }

        internal void Tick(bool enabled)
        {
            FiredThisFrame = false;
            IsAiming = false;
            TriggerHeld = false;

            var controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool grip = false;
            bool trigger = false;
            if (controller.isValid)
            {
                if (!controller.TryGetFeatureValue(CommonUsages.gripButton, out grip))
                {
                    float gripValue;
                    if (controller.TryGetFeatureValue(CommonUsages.grip, out gripValue))
                        grip = gripValue > 0.55f;
                }

                if (!controller.TryGetFeatureValue(CommonUsages.triggerButton, out trigger))
                {
                    float triggerValue;
                    if (controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
                        trigger = triggerValue > 0.55f;
                }
            }

            var hand = plugin.Game.RightHand;
            if (!enabled || !controller.isValid || hand == null || !grip)
            {
                HideVisuals();
                previousTrigger = trigger;
                return;
            }

            IsAiming = true;
            TriggerHeld = trigger;
            Vector3 origin = hand.position - hand.up * 0.025f;
            Vector3 direction = plugin.Settings.GunUseForward.Value ? hand.forward : -hand.up;
            direction.Normalize();
            float limit = plugin.Settings.GunDistance.Value;

            GunTarget target = new GunTarget
            {
                HasHit = false,
                Point = origin + direction * limit,
                Direction = direction,
                Normal = -direction,
                Distance = limit
            };

            var ray = new Ray(origin, direction);
            int count = Physics.RaycastNonAlloc(ray, hitBuffer, limit, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float nearest = limit;
            for (int i = 0; i < count; i++)
            {
                var candidate = hitBuffer[i];
                if (candidate.collider == null || candidate.distance <= 0.03f ||
                    candidate.distance >= nearest)
                    continue;
                if (IsLocalPlayerCollider(candidate.collider))
                    continue;
                // Never lock onto dynamic bodies such as other players or movable props.
                if (candidate.rigidbody != null)
                    continue;
                nearest = candidate.distance;
                target.HasHit = true;
                target.Point = candidate.point;
                target.Normal = candidate.normal;
                target.Distance = candidate.distance;
                target.Collider = candidate.collider;
            }

            Target = target;
            DrawPointer(origin, target);

            FiredThisFrame = trigger && !previousTrigger;
            previousTrigger = trigger;
            if (FiredThisFrame)
            {
                try { Fired?.Invoke(target); }
                catch (Exception ex) { plugin.Log.LogError("GunLib callback failed: " + ex); }
            }
        }

        private bool IsLocalPlayerCollider(Collider candidate)
        {
            var playerComponent = plugin.Game.Player as Component;
            if (playerComponent != null && candidate.transform.IsChildOf(playerComponent.transform))
                return true;
            var body = plugin.Game.Body;
            return body != null && candidate.transform.IsChildOf(body.transform);
        }

        private void DrawPointer(Vector3 origin, GunTarget target)
        {
            EnsureVisuals();
            if (beam == null || reticle == null)
                return;
            beam.gameObject.SetActive(true);
            reticle.SetActive(true);
            float width = plugin.Settings.GunBeamWidth.Value;
            beam.startWidth = width;
            beam.endWidth = width * 0.55f;
            beam.SetPosition(0, origin);
            beam.SetPosition(1, target.Point);
            Color color = target.HasHit ? plugin.Accent : new Color(1f, 0.45f, 0.45f);
            if (TriggerHeld) color = Color.white;
            beamMaterial.color = color;
            reticleMaterial.color = color;
            reticle.transform.position = target.Point + target.Normal * 0.015f;
            reticle.transform.localScale = Vector3.one * plugin.Settings.GunReticleSize.Value;
        }

        private void EnsureVisuals()
        {
            if (reticle != null && beam != null) return;
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null)
            {
                plugin.Log.LogWarning("GunLib: no compatible local shader available.");
                return;
            }

            if (reticle == null)
            {
                reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                reticle.name = "Volas GunLib reticle";
                var collider = reticle.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                reticleMaterial = new Material(shader);
                reticle.GetComponent<Renderer>().material = reticleMaterial;
            }

            if (beam == null)
            {
                var go = new GameObject("Volas GunLib beam");
                beam = go.AddComponent<LineRenderer>();
                beam.positionCount = 2;
                beam.useWorldSpace = true;
                beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beam.receiveShadows = false;
                beamMaterial = new Material(shader);
                beam.material = beamMaterial;
            }
        }

        private void HideVisuals()
        {
            if (reticle != null) reticle.SetActive(false);
            if (beam != null) beam.gameObject.SetActive(false);
        }

        internal void Reset()
        {
            previousTrigger = false;
            IsAiming = false;
            TriggerHeld = false;
            FiredThisFrame = false;
            HideVisuals();
        }

        internal void Dispose()
        {
            Reset();
            Fired = null;
            if (reticle != null) UnityEngine.Object.Destroy(reticle);
            if (beam != null) UnityEngine.Object.Destroy(beam.gameObject);
            if (reticleMaterial != null) UnityEngine.Object.Destroy(reticleMaterial);
            if (beamMaterial != null) UnityEngine.Object.Destroy(beamMaterial);
            reticle = null;
            beam = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
