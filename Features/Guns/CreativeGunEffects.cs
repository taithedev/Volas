using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    // Decorative local-only GunLib modes: no game-network messages or damage.
    internal sealed class CreativeGunEffects
    {
        internal bool Grid;
        internal bool Spiral;
        internal bool Arrow;
        internal bool Sign;
        internal bool Sparkles;
        internal bool Checkerboard;
        private readonly bool[] shapes = new bool[6];
        internal void SetShape(int index,bool enabled) { if(index>=0&&index<shapes.Length)shapes[index]=enabled; }

        private readonly VolasPlugin plugin;
        private readonly List<EffectItem> active = new List<EffectItem>();
        private sealed class EffectItem
        {
            internal GameObject Root;
            internal float Expiry;
        }

        internal bool AnyEnabled { get { if (Grid || Spiral || Arrow || Sign || Sparkles || Checkerboard) return true; foreach(bool enabled in shapes) if(enabled)return true; return false; } }

        internal CreativeGunEffects(VolasPlugin owner)
        {
            plugin = owner;
            plugin.Gun.Fired += OnFire;
        }

        private void OnFire(GunTarget hit)
        {
            if (!hit.HasHit || !AnyEnabled) return;
            if (Grid) MakeGrid(hit);
            if (Spiral) MakeSpiral(hit);
            if (Arrow) MakeArrow(hit);
            if (Sign) MakeSign(hit);
            if (Sparkles) MakeSparkles(hit);
            if (Checkerboard) MakeCheckerboard(hit);
            for(int i=0;i<shapes.Length;i++) if(shapes[i]) MakeOutline(hit,i);
        }

        internal void Tick()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Root == null || Time.time >= active[i].Expiry)
                {
                    DestroyGroup(active[i].Root);
                    active.RemoveAt(i);
                }
            }
        }

        private void MakeOutline(GunTarget target,int index)
        {
            var root=NewRoot("Decorative outline",target);
            int style=index==0?6:index==1?7:index==2?8:index==3?9:index==4?10:11;
            int count=PointerStyles.OutlineCount(style);
            var line=MakeLine(root,"Outline",count,.014f);
            line.loop=true;
            float size=.28f*plugin.Settings.GunMarkScale.Value;
            for(int i=0;i<count;i++)line.SetPosition(i,PointerStyles.OutlinePoint(style,i,count)*size);
            Track(root);
        }

        private void MakeGrid(GunTarget target)
        {
            var root = NewRoot("Surface grid", target);
            float scale = plugin.Settings.GunMarkScale.Value;
            for (int i = -3; i <= 3; i++)
            {
                float offset = i * 0.12f * scale;
                var horizontal = MakeLine(root, "Grid horizontal", 2, 0.008f);
                horizontal.SetPosition(0, new Vector3(-0.36f * scale, offset, 0f));
                horizontal.SetPosition(1, new Vector3(0.36f * scale, offset, 0f));
                var vertical = MakeLine(root, "Grid vertical", 2, 0.008f);
                vertical.SetPosition(0, new Vector3(offset, -0.36f * scale, 0f));
                vertical.SetPosition(1, new Vector3(offset, 0.36f * scale, 0f));
            }
            Track(root);
        }

        private void MakeSpiral(GunTarget target)
        {
            var root = NewRoot("Spiral stamp", target);
            float scale = plugin.Settings.GunMarkScale.Value;
            var line = MakeLine(root, "Spiral", 65, 0.012f);
            for (int i = 0; i < 65; i++)
            {
                float t = i / 64f;
                float angle = t * 7f * Mathf.PI;
                float radius = t * 0.47f * scale;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
            Track(root);
        }

        private void MakeArrow(GunTarget target)
        {
            var root = NewRoot("Direction arrow", target);
            float scale = plugin.Settings.GunMarkScale.Value;
            var line = MakeLine(root, "Arrow", 4, 0.019f);
            line.SetPosition(0, new Vector3(0f, -0.38f * scale, 0f));
            line.SetPosition(1, new Vector3(0f, 0.38f * scale, 0f));
            line.SetPosition(2, new Vector3(-0.16f * scale, 0.19f * scale, 0f));
            line.SetPosition(3, new Vector3(0.16f * scale, 0.19f * scale, 0f));
            var rightHead = MakeLine(root, "Right arrow edge", 2, 0.019f);
            rightHead.SetPosition(0, new Vector3(0f, 0.38f * scale, 0f));
            rightHead.SetPosition(1, new Vector3(0.16f * scale, 0.19f * scale, 0f));
            Track(root);
        }

        private void MakeSign(GunTarget target)
        {
            var root = NewRoot("Volas sign", target);
            var go = new GameObject("Volas floating sign");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.up * 0.17f;
            var label = go.AddComponent<TextMesh>();
            label.text = "VOLAS";
            label.fontSize = 60;
            label.characterSize = 0.012f * plugin.Settings.GunMarkScale.Value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = plugin.Accent;
            Track(root);
        }

        private void MakeSparkles(GunTarget target)
        {
            var root = NewRoot("Sparkle burst", target);
            float scale = plugin.Settings.GunMarkScale.Value;
            for (int i = 0; i < 14; i++)
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "Volas local sparkle";
                dot.transform.SetParent(root.transform, false);
                dot.transform.localPosition = UnityEngine.Random.insideUnitSphere * 0.35f * scale;
                dot.transform.localScale = Vector3.one * 0.025f * scale;
                var collision = dot.GetComponent<Collider>();
                if (collision != null) collision.enabled = false;
                dot.GetComponent<Renderer>().sharedMaterial = NewMaterial(Color.HSVToRGB(
                    UnityEngine.Random.value, 0.72f, 1f));
            }
            Track(root);
        }

        private void MakeCheckerboard(GunTarget target)
        {
            var root = NewRoot("Checker stamp", target);
            float scale = plugin.Settings.GunMarkScale.Value;
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if ((x + y) % 2 != 0) continue;
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Volas checker tile";
                    cube.transform.SetParent(root.transform, false);
                    cube.transform.localPosition = new Vector3((x - 2.5f) * 0.09f * scale,
                        (y - 2.5f) * 0.09f * scale, 0f);
                    cube.transform.localScale = new Vector3(0.085f, 0.085f, 0.008f) * scale;
                    var collider = cube.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                    cube.GetComponent<Renderer>().sharedMaterial = NewMaterial(plugin.Accent);
                }
            }
            Track(root);
        }

        private GameObject NewRoot(string name, GunTarget target)
        {
            var root = new GameObject("Volas GunLib " + name);
            root.transform.position = target.Point + target.Normal * 0.035f;
            root.transform.rotation = Quaternion.LookRotation(target.Normal, Vector3.up);
            return root;
        }

        private LineRenderer MakeLine(GameObject parent, string name, int count, float width)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = count;
            line.startWidth = width * plugin.Settings.GunMarkScale.Value;
            line.endWidth = line.startWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = NewMaterial(plugin.Accent);
            return line;
        }

        private static Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Volas: required shader is missing.");
            return new Material(shader) { color = color, name = "Volas decorative material" };
        }

        private void Track(GameObject root)
        {
            active.Add(new EffectItem
            {
                Root = root,
                Expiry = Time.time + plugin.Settings.GunMarkLifetime.Value
            });
            while (active.Count > 36)
            {
                DestroyGroup(active[0].Root);
                active.RemoveAt(0);
            }
        }

        private static void DestroyGroup(GameObject group)
        {
            if (group == null) return;
            foreach (var renderer in group.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Volas decorative material")
                    UnityEngine.Object.Destroy(renderer.sharedMaterial);
            UnityEngine.Object.Destroy(group);
        }

        internal void Reset()
        {
            foreach (var item in active) DestroyGroup(item.Root);
            active.Clear();
        }

        internal void Dispose()
        {
            plugin.Gun.Fired -= OnFire;
            Reset();
        }
    }
}
