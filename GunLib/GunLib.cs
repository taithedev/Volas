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
        private LineRenderer haloBeam;
        private Material haloMaterial;
        private LineRenderer reticleCircle;
        private Material circleMaterial;
        private int currentReticleStyle = -1;
        private bool previousTrigger, wasAiming, triggerArmed;
        private readonly System.Collections.Generic.List<InputDevice> controllers = new System.Collections.Generic.List<InputDevice>(4);
        internal bool Preview { get; set; }
        internal string Status { get; private set; } = "Inactive";
        private float nextShaderWarning;

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
            Target = default(GunTarget);

            if (!enabled)
            {
                Reset();
                Status = "Inactive";
                return;
            }

            bool left = plugin.Settings.GunLeftHand.Value;
            bool grip, trigger;
            bool inputAvailable = ReadController(left, out grip, out trigger);
            Transform hand = left ? plugin.Game.LeftHand : plugin.Game.RightHand;
            if (!inputAvailable || hand == null || !grip)
            {
                HideVisuals();
                wasAiming = false;
                triggerArmed = false;
                previousTrigger = trigger;
                Status = hand == null ? "Waiting for tracked hand" : !inputAvailable ? "Waiting for controller input" : "Hold grip to aim";
                return;
            }

            if (!wasAiming)
            {
                wasAiming = true;
                triggerArmed = !trigger;
                previousTrigger = trigger;
            }
            if (!trigger) triggerArmed = true;
            IsAiming = true;
            TriggerHeld = trigger;
            Vector3 direction = plugin.Settings.GunUseForward.Value ? hand.forward : -hand.up;
            Vector3 origin = hand.position + direction * 0.025f;
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
            if (target.HasHit || plugin.Settings.GunShowMiss.Value) DrawPointer(origin, target);
            else HideVisuals();

            Status = target.HasHit ? "Aiming at surface" : "No surface hit";
            FiredThisFrame = triggerArmed && trigger && !previousTrigger;
            previousTrigger = trigger;
            if (FiredThisFrame && target.HasHit && Fired != null)
            {
                foreach (Delegate callback in Fired.GetInvocationList())
                {
                    try { ((Action<GunTarget>)callback)(target); }
                    catch (Exception ex) { plugin.Log.LogError("GunLib callback failed: " + ex); }
                }
            }
        }

        #region Controller input

        private bool ReadController(bool left, out bool grip, out bool trigger)
        {
            grip = false; trigger = false;
            string prefix = left ? "leftController" : "rightController";
            bool gripKnown = plugin.Game.TryControllerButton(prefix + "GripButton", out grip);
            bool triggerKnown = plugin.Game.TryControllerButton(prefix + "TriggerButton", out trigger);
            float value;
            if (plugin.Game.TryControllerFloat(prefix + "GripFloat", out value))
            { grip = grip || value >= .55f; gripKnown = true; }
            if (plugin.Game.TryControllerFloat(prefix + "IndexFloat", out value))
            { trigger = trigger || value >= .55f; triggerKnown = true; }
            InputDevice device = InputDevices.GetDeviceAtXRNode(left ? XRNode.LeftHand : XRNode.RightHand);
            if (!device.isValid)
            {
                controllers.Clear();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller |
                    (left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right), controllers);
                foreach (var candidate in controllers) if (candidate.isValid) { device = candidate; break; }
            }
            if (device.isValid)
            {
                bool pressed;
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out pressed)) { grip |= pressed; gripKnown = true; }
                if (device.TryGetFeatureValue(CommonUsages.grip, out value)) { grip |= value >= .55f; gripKnown = true; }
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out pressed)) { trigger |= pressed; triggerKnown = true; }
                if (device.TryGetFeatureValue(CommonUsages.trigger, out value)) { trigger |= value >= .55f; triggerKnown = true; }
            }
            return gripKnown && triggerKnown;
        }

        #endregion

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
            if (beam == null || reticle == null || haloBeam == null || reticleCircle == null) return;
            var cfg = plugin.Settings;
            float t = Time.unscaledTime * cfg.GunAnimationSpeed.Value;
            float pulse = cfg.GunPulse.Value ? .82f + .18f * Mathf.Sin(t * 4f) : 1f;
            float width = cfg.GunBeamWidth.Value * pulse;
            Color color = PointerStyles.ColorFor(cfg, plugin.Accent, target.HasHit, t);
            beam.startWidth = width; beam.endWidth = width * cfg.GunEndWidth.Value;
            beamMaterial.color = Color.white;
            beam.startColor = beam.endColor = color;

            int style = Mathf.Clamp(cfg.GunBeamStyle.Value, 0, PointerStyles.Beams.Length - 1);
            bool showBeam = cfg.GunShowBeam.Value;
            bool showGlow = showBeam && cfg.GunGlow.Value && cfg.GunGlowAmount.Value > 0f;
            beam.gameObject.SetActive(showBeam);
            haloBeam.gameObject.SetActive(showGlow);
            if (showBeam)
            {
                int count = style == 0 ? 2 : Mathf.Clamp(cfg.GunSegments.Value, 12, 64);
                if (beam.positionCount != count) beam.positionCount = count;
                if (showGlow && haloBeam.positionCount != count) haloBeam.positionCount = count;
                Vector3 delta = target.Point - origin;
                Vector3 axis = delta.sqrMagnitude > .0001f ? delta.normalized : Vector3.forward;
                Vector3 side = Vector3.Cross(axis, Vector3.up);
                if (side.sqrMagnitude < .002f) side = Vector3.Cross(axis, Vector3.right);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, axis).normalized;
                for (int i = 0; i < count; i++)
                {
                    float u = i / (count - 1f);
                    Vector3 point = Vector3.Lerp(origin, target.Point, u) + PointerStyles.BeamOffset(style, u, t, cfg.GunWaveAmount.Value, side, up);
                    beam.SetPosition(i, point);
                    if (showGlow) haloBeam.SetPosition(i, point);
                }
                if (showGlow)
                {
                    haloBeam.startWidth = width * 3.3f; haloBeam.endWidth = width * cfg.GunEndWidth.Value * 3f;
                    haloMaterial.color = Color.white;
                    haloBeam.startColor = haloBeam.endColor = new Color(color.r,color.g,color.b,color.a * cfg.GunGlowAmount.Value);
                }
            }

            int reticleStyle = Mathf.Clamp(cfg.GunReticleStyle.Value, 0, PointerStyles.Reticles.Length - 1);
            if (reticleStyle != currentReticleStyle) ConfigureReticle(reticleStyle);
            if (reticle == null || reticleMaterial == null) return;
            bool outline = reticleStyle == 2 || reticleStyle >= 6;
            bool show = cfg.GunShowReticle.Value;
            Vector3 pointOnSurface = target.Point + target.Normal * .015f;
            reticle.SetActive(show && !outline);
            reticleCircle.gameObject.SetActive(show && outline);
            if (!show) return;
            float size = cfg.GunReticleSize.Value * pulse;
            if (!outline)
            {
                reticle.transform.position = pointOnSurface;
                reticle.transform.localScale = Vector3.one * size;
                reticleMaterial.color = color;
                reticle.transform.rotation = reticleStyle == 3 ? Quaternion.Euler(45f, cfg.GunRotateReticle.Value ? t * 38f : 0f, 45f) :
                    (cfg.GunRotateReticle.Value ? Quaternion.Euler(0f,t*38f,0f) : Quaternion.identity);
            }
            else
            {
                circleMaterial.color = Color.white;
                reticleCircle.startColor = reticleCircle.endColor = color;
                reticleCircle.startWidth = reticleCircle.endWidth = Mathf.Max(.002f, size * .07f);
                int count = PointerStyles.OutlineCount(reticleStyle);
                if (reticleCircle.positionCount != count) reticleCircle.positionCount = count;
                Vector3 normal = target.Normal.sqrMagnitude > .0001f ? target.Normal.normalized : Vector3.forward;
                Quaternion rotation = Quaternion.LookRotation(normal, Mathf.Abs(Vector3.Dot(normal,Vector3.up)) > .98f ? Vector3.right : Vector3.up);
                if (cfg.GunRotateReticle.Value) rotation *= Quaternion.Euler(0f,0f,t*38f);
                for (int i=0;i<count;i++)
                    reticleCircle.SetPosition(i, pointOnSurface + rotation * PointerStyles.OutlinePoint(reticleStyle,i,count) * (size*.6f));
            }
        }

        private void EnsureVisuals()
        {
            if (reticle != null && beam != null && haloBeam != null && reticleCircle != null) return;
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null)
            {
                if (Time.unscaledTime >= nextShaderWarning)
                {
                    nextShaderWarning = Time.unscaledTime + 10f;
                    plugin.Log.LogWarning("GunLib: no compatible local shader available.");
                }
                return;
            }

            if (reticle == null)
            {
                if (reticleMaterial == null) reticleMaterial = new Material(shader);
                ConfigureReticle(plugin.Settings.GunReticleStyle.Value);
            }

            if (beam == null)
            {
                beam = CreateLine("Volas GunLib luminous beam", shader, false, 2);
                if (beamMaterial != null) UnityEngine.Object.Destroy(beamMaterial);
                beamMaterial = beam.sharedMaterial;
            }
            if (haloBeam == null)
            {
                haloBeam = CreateLine("Volas GunLib soft glow", shader, false, 2);
                if (haloMaterial != null) UnityEngine.Object.Destroy(haloMaterial);
                haloMaterial = haloBeam.sharedMaterial;
            }
            if (reticleCircle == null)
            {
                reticleCircle = CreateLine("Volas GunLib ring reticle", shader, true, 40);
                reticleCircle.startWidth = 0.009f;
                reticleCircle.endWidth = 0.009f;
                if (circleMaterial != null) UnityEngine.Object.Destroy(circleMaterial);
                circleMaterial = reticleCircle.sharedMaterial;
                reticleCircle.gameObject.SetActive(false);
            }
        }

        private static LineRenderer CreateLine(string name, Shader shader, bool loop, int points)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = points;
            line.loop = loop;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = new Material(shader);
            go.SetActive(false);
            return line;
        }

        private void ConfigureReticle(int style)
        {
            if (reticle != null) { reticle.SetActive(false); UnityEngine.Object.Destroy(reticle); reticle = null; }
            Shader shader = Shader.Find("Sprites/Default") ??
                            Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) return;
            reticle = GameObject.CreatePrimitive(style == 1 || style == 3 ? PrimitiveType.Cube : style == 4 ? PrimitiveType.Capsule : style == 5 ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            reticle.name = "Volas GunLib decorative reticle";
            var collider = reticle.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            if (reticleMaterial == null) reticleMaterial = new Material(shader);
            reticle.GetComponent<Renderer>().sharedMaterial = reticleMaterial;
            reticle.SetActive(false);
            currentReticleStyle = style;
        }

        private void HideVisuals()
        {
            if (reticle != null && reticle.activeSelf) reticle.SetActive(false);
            if (beam != null && beam.gameObject.activeSelf) beam.gameObject.SetActive(false);
            if (haloBeam != null && haloBeam.gameObject.activeSelf) haloBeam.gameObject.SetActive(false);
            if (reticleCircle != null && reticleCircle.gameObject.activeSelf) reticleCircle.gameObject.SetActive(false);
        }

        internal void Reset()
        {
            previousTrigger = false;
            wasAiming = false; triggerArmed = false;
            Target = default(GunTarget);
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
            if (haloBeam != null) UnityEngine.Object.Destroy(haloBeam.gameObject);
            if (reticleCircle != null) UnityEngine.Object.Destroy(reticleCircle.gameObject);
            if (reticleMaterial != null) UnityEngine.Object.Destroy(reticleMaterial);
            if (beamMaterial != null) UnityEngine.Object.Destroy(beamMaterial);
            if (haloMaterial != null) UnityEngine.Object.Destroy(haloMaterial);
            if (circleMaterial != null) UnityEngine.Object.Destroy(circleMaterial);
            reticle = null;
            beam = null;
            haloBeam = null;
            reticleCircle = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
