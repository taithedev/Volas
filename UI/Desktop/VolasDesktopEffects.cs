using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Volas
{
    internal sealed class VolasDesktopEffects
    {
        #region State & resources

        private readonly VolasPlugin plugin;
        private readonly RectTransform window;
        private readonly Sprite maskSprite;
        private readonly Texture2D glowTexture, grainTexture;
        private readonly AudioSource audio;
        private readonly AudioClip[] sounds;
        private readonly Vector3[] corners = new Vector3[4];
        private RawImage glass, grain;
        private RenderTexture blurred;
        private Rect lastUv;
        private float lastBlend = -1f;
        private bool lastGlassEnabled;
        private static readonly MethodInfo CaptureIntoTexture = ResolveCaptureMethod();
        private bool disposed;
        private float lastHover = -10f;
        private float lastClick = -10f;
        internal Sprite GlowSprite { get; private set; }
        internal bool AudioPlaying { get { return audio != null && audio.isPlaying; } }

        #endregion

        #region Procedural asset initialization

        internal VolasDesktopEffects(VolasPlugin owner, RectTransform target, Sprite mask)
        {
            plugin = owner; window = target; maskSprite = mask;
            glowTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            glowTexture.wrapMode = TextureWrapMode.Clamp;
            glowTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float radius = dx * dx + dy * dy;
                float falloff = Mathf.Max(0f, Mathf.Exp(-radius * 4.5f) - .011f);
                pixels[y * 64 + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(falloff) * 255f));
            }
            glowTexture.SetPixels32(pixels); glowTexture.Apply();
            GlowSprite = Sprite.Create(glowTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            grainTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            grainTexture.wrapMode = TextureWrapMode.Repeat; grainTexture.filterMode = FilterMode.Point;
            var random = new System.Random(417);
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)random.Next(45, 211);
                pixels[i] = new Color32(v, v, v, 255);
            }
            grainTexture.SetPixels32(pixels); grainTexture.Apply(false, true);

            // Separate local 2D source; never touches game or Photon voice audio.
            var obj = new GameObject("Volas UI audio");
            obj.transform.SetParent(plugin.transform, false);
            audio = obj.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.loop = false; audio.spatialBlend = 0f;
            audio.dopplerLevel = 0f; audio.ignoreListenerPause = true;
            sounds = new[] {
                Tone("Hover", .035f, 570f, 660f),
                Tone("Click", .065f, 760f, 430f),
                Tone("Open", .16f, 390f, 820f),
                Tone("Close", .13f, 660f, 300f)
            };
        }

        #endregion

        #region Acrylic UI construction

        internal void BuildBackdrop()
        {
            var holder = new GameObject("Acrylic clipping mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            var rt = (RectTransform)holder.transform;
            rt.SetParent(window, false); Fill(rt, 2f);
            var shape = holder.GetComponent<Image>();
            shape.sprite = maskSprite; shape.type = Image.Type.Sliced; shape.raycastTarget = false;
            holder.GetComponent<Mask>().showMaskGraphic = false;
            glass = MakeRaw(rt, "Blurred scene");
            glass.enabled = false;
            grain = MakeRaw(rt, "Acrylic microtexture");
            grain.texture = grainTexture; grain.uvRect = new Rect(0f, 0f, 11f, 7f);
            grain.color = new Color(1f, 1f, 1f, .035f);
        }

        private static RawImage MakeRaw(RectTransform parent, string name)
        {
            var o = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)o.transform;
            rt.SetParent(parent, false); Fill(rt, 0f);
            var result = o.GetComponent<RawImage>(); result.raycastTarget = false;
            return result;
        }

        private static void Fill(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            rt.localScale = Vector3.one;
        }

        #endregion

        #region Backdrop capture & rendering

        private static MethodInfo ResolveCaptureMethod()
        {
            try
            {
                Type type = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule")
                    ?? Type.GetType("UnityEngine.ScreenCapture, UnityEngine");
                return type?.GetMethod("CaptureScreenshotIntoRenderTexture", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(RenderTexture) }, null);
            }
            catch { return null; }
        }

        // GPU-only capture avoids synchronous Texture2D pixel readback. If the
        // Unity version has no capture API, keep a lightweight frosted surface.
        internal IEnumerator CaptureBackdrop(Func<bool> stillOpen, Action ready)
        {
            yield return new WaitForEndOfFrame();
            if (disposed || !stillOpen()) yield break;
            ClearBackdrop();
            RenderTexture screen = null, small = null;
            try
            {
                if (CaptureIntoTexture != null && plugin.Settings.DesktopGlass.Value)
                {
                    int w = Mathf.Max(4, Screen.width), h = Mathf.Max(4, Screen.height);
                    screen = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                    screen.filterMode = FilterMode.Bilinear;
                    CaptureIntoTexture.Invoke(null, new object[] { screen });
                    small = RenderTexture.GetTemporary(Mathf.Max(4, w / 8), Mathf.Max(4, h / 8), 0, RenderTextureFormat.ARGB32);
                    small.filterMode = FilterMode.Bilinear;
                    blurred = new RenderTexture(Mathf.Max(4, w / 24), Mathf.Max(4, h / 24), 0, RenderTextureFormat.ARGB32);
                    blurred.filterMode = FilterMode.Bilinear; blurred.wrapMode = TextureWrapMode.Clamp;
                    blurred.Create();
                    Graphics.Blit(screen, small);
                    Graphics.Blit(small, blurred);
                }
            }
            catch (Exception ex)
            {
                ClearBackdrop();
                plugin.Log.LogDebug("Acrylic fallback: " + ex.GetType().Name);
            }
            finally
            {
                if (screen != null) RenderTexture.ReleaseTemporary(screen);
                if (small != null) RenderTexture.ReleaseTemporary(small);
                if (!disposed && stillOpen()) { UpdateBackdrop(); ready(); }
            }
        }

        internal void UpdateBackdrop()
        {
            if (disposed || glass == null) return;
            bool enabled = plugin.Settings.DesktopGlass.Value;
            float blend = plugin.Settings.DesktopBlurStrength.Value;
            if (lastGlassEnabled != enabled || lastBlend != blend)
            {
                lastGlassEnabled = enabled; lastBlend = blend;
                grain.enabled = enabled && blend > 0f;
                grain.color = new Color(1f, 1f, 1f, .035f * blend);
                glass.color = new Color(.65f, .72f, 1f, .62f * blend);
            }
            bool show = enabled && blurred != null && blend > 0f;
            if (glass.enabled != show) glass.enabled = show;
            if (!show) return;
            if (glass.texture != blurred) glass.texture = blurred;
            window.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            var uv = new Rect(min.x / w, min.y / h, (max.x - min.x) / w, (max.y - min.y) / h);
            if (uv != lastUv || glass.uvRect != uv) { lastUv = uv; glass.uvRect = uv; }
        }

        internal void ClearBackdrop()
        {
            if (glass != null) { glass.texture = null; glass.enabled = false; }
            if (blurred != null) { blurred.Release(); UnityEngine.Object.Destroy(blurred); blurred = null; }
        }

        #endregion

        #region Procedural audio

        private static AudioClip Tone(string name, float seconds, float start, float end)
        {
            const int rate = 24000;
            int n = Mathf.CeilToInt(rate * seconds);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n, f = Mathf.Lerp(start, end, t);
                phase += 2f * Mathf.PI * f / rate;
                float envelope = Mathf.Min(1f, t * 30f) * Mathf.Pow(1f - t, 2.8f);
                data[i] = (Mathf.Sin(phase) + .18f * Mathf.Sin(phase * 2f)) * envelope * .38f;
            }
            AudioClip clip = AudioClip.Create("Volas " + name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        internal void Play(int index)
        {
            if (disposed || audio == null || !plugin.Settings.DesktopSounds.Value || index < 0 || index >= sounds.Length) return;
            float now = Time.unscaledTime;
            if (index == 0 && (now - lastHover < .09f || now - lastClick < .09f)) return;
            if (index == 0) lastHover = now; else lastClick = now;
            audio.PlayOneShot(sounds[index], Mathf.Clamp01(plugin.Settings.DesktopSoundVolume.Value) * (index == 0 ? .45f : 1f));
        }

        #endregion

        #region Button integration

        internal void Wire(Button button, Action callback)
        {
            var feedback = button.gameObject.AddComponent<VolasDesktopButtonFeedback>();
            feedback.Initialize(this, plugin, button);
            button.onClick.AddListener(() => { Play(1); if (callback != null) callback(); });
        }

        #endregion

        #region Resource cleanup

        internal void Dispose()
        {
            disposed = true;
            ClearBackdrop();
            if (audio != null) { audio.Stop(); UnityEngine.Object.Destroy(audio.gameObject); }
            foreach (var clip in sounds) if (clip != null) UnityEngine.Object.Destroy(clip);
            if (GlowSprite != null) UnityEngine.Object.Destroy(GlowSprite);
            UnityEngine.Object.Destroy(glowTexture); UnityEngine.Object.Destroy(grainTexture);
        }

        #endregion
    }

    internal sealed class VolasDesktopButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Feedback state & setup

        private VolasDesktopEffects effects;
        private VolasPlugin plugin;
        private Button button;
        internal void Initialize(VolasDesktopEffects fx, VolasPlugin owner, Button target)
        {
            effects = fx; plugin = owner; button = target;
        }
        #endregion

        #region Pointer interaction

        public void OnPointerEnter(PointerEventData eventData) { if (button != null && button.IsInteractable()) effects.Play(0); }
        public void OnPointerExit(PointerEventData eventData) { ResetPress(); }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && button != null && button.IsInteractable() && plugin.Settings.DesktopMotion.Value)
                transform.localScale = new Vector3(.985f, .985f, 1f);
        }
        public void OnPointerUp(PointerEventData eventData) { ResetPress(); }
        #endregion

        #region Motion & reset

        private void ResetPress() { if (transform.localScale != Vector3.one) transform.localScale = Vector3.one; }
        private void OnDisable() { ResetPress(); }

        #endregion
    }
}
