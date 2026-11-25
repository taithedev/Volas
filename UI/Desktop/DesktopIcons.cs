using System;
using System.Collections.Generic;
using UnityEngine;

namespace Volas
{
    // Cached procedural line icons, independent of the installed UI font.
    internal sealed class DesktopIcons
    {
        #region Cache
        private readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        internal Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var existing)) return existing;
            const int size = 32;
            var pixels = new Color32[size * size];
            var segments = new List<Vector4>();
            void Line(float x, float y, float a, float b) { segments.Add(new Vector4(x,y,a,b)); }
            void Path(params float[] p) { for (int i=0; i+3<p.Length; i+=2) Line(p[i],p[i+1],p[i+2],p[i+3]); }
            void Ring(float x,float y,float r)
            {
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI/12f,b=(i+1)*Mathf.PI/12f;
                    Line(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r);
                }
            }
            switch(name)
            {
                case "Volas": Path(4,7,15,26,28,7); Path(11,7,16,16,21,7); break;
                case "Home": Path(4,14,16,4,28,14); Path(7,12,7,27,25,27,25,12); Path(13,27,13,19,19,19,19,27); break;
                case "Players": Ring(16,9,5); Path(5,27,5,22,10,17,22,17,27,22,27,27); break;
                case "Search": Ring(13,13,8); Line(19,19,27,27); break;
                case "Visuals": case "Player ESP": Path(3,16,9,9,16,7,23,9,29,16,23,23,16,25,9,23,3,16); Ring(16,16,4); break;
                case "Movement": Path(3,18,11,18,15,7,20,25,24,14,29,14); break;
                case "Favorites": case "Charms": case "Charm Studio": Path(16,3,20,11,29,12,22,19,24,28,16,24,8,28,10,19,3,12,12,11,16,3); break;
                case "Enabled": Path(5,16,12,23,27,7); break;
                case "Owner": case "Safety": Path(16,3,27,7,26,19,22,25,16,29,10,25,6,19,5,7,16,3); Path(11,16,15,20,22,12); break;
                case "UI Effects": case "Fun": Path(16,3,16,29); Line(3,16,29,16); Line(7,7,25,25); Line(7,25,25,7); break;
                case "Settings": case "Gun Studio": Ring(16,16,9); Ring(16,16,4); for(int i=0;i<8;i++){float a=i*Mathf.PI/4f;Line(16+9*Mathf.Cos(a),16+9*Mathf.Sin(a),16+13*Mathf.Cos(a),16+13*Mathf.Sin(a));} break;
                case "Projectiles": case "Local": Ring(16,16,10); Ring(16,16,4); break;
                default: Path(5,5,13,5,13,13,5,13,5,5); Path(19,5,27,5,27,13,19,13,19,5); Path(5,19,13,19,13,27,5,27,5,19); Path(19,19,27,19,27,27,19,27,19,19); break;
            }
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var point=new Vector2(x+.5f,size-y-.5f);
                float distance=100f;
                foreach(var s in segments)
                {
                    var a=new Vector2(s.x,s.y);var b=new Vector2(s.z,s.w);var ab=b-a;
                    float t=Mathf.Clamp01(Vector2.Dot(point-a,ab)/Mathf.Max(.001f,ab.sqrMagnitude));
                    distance=Mathf.Min(distance,(point-a-ab*t).magnitude);
                }
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(1.65f-distance)));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            texture.SetPixels32(pixels);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect);
            cache.Add(name,sprite);
            return sprite;
        }
        internal void Dispose()
        {
            foreach(var sprite in cache.Values) if(sprite!=null)
            {
                var texture=sprite.texture;
                UnityEngine.Object.Destroy(sprite);
                if(texture!=null) UnityEngine.Object.Destroy(texture);
            }
            cache.Clear();
        }
        #endregion
    }
}
