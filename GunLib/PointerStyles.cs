using UnityEngine;

namespace Volas
{
    // Procedural decorative geometry, shared by runtime and the configuration UI.
    internal static class PointerStyles
    {
        internal static readonly string[] Beams = { "Straight", "Wave", "Spiral", "Zigzag", "Ribbon", "Arc", "Braid", "Ripple" };
        internal static readonly string[] Reticles = { "Orb", "Box", "Ring", "Gem", "Capsule", "Cylinder", "Triangle", "Hexagon", "Star", "Heart", "Flower", "Octagon" };
        internal static readonly string[] Colors = { "Accent", "Custom", "Spectrum", "Surface" };
        internal static readonly string[] Presets = { "Minimal", "Aurora", "Crystal", "Retro", "Candy", "Midnight" };

        internal static Color ColorFor(VolasConfiguration s, Color accent, bool hit, float t)
        {
            Color c = accent;
            if (s.GunRainbow.Value || s.GunColorMode.Value == 2)
                c = Color.HSVToRGB(Mathf.Repeat(t*.08f,1f),s.GunSaturation.Value,1f);
            else if (s.GunColorMode.Value == 1)
                c = Color.HSVToRGB(s.GunHue.Value,s.GunSaturation.Value,1f);
            else if (s.GunColorMode.Value == 3)
                c = hit ? new Color(.45f,.88f,.73f) : new Color(.8f,.55f,.4f);
            c.a = s.GunOpacity.Value;
            return c;
        }

        internal static Vector3 BeamOffset(int style,float u,float t,float amount,Vector3 side,Vector3 up)
        {
            float taper = Mathf.Sin(Mathf.PI*u);
            float phase = u*24f-t*5f;
            switch(style)
            {
                case 1: return side*Mathf.Sin(phase)*amount*taper;
                case 2: return (side*Mathf.Cos(phase)+up*Mathf.Sin(phase))*amount*taper;
                case 3: return side*(Mathf.PingPong(u*12f-t*2f,1f)*2f-1f)*amount*taper;
                case 4: return (side*Mathf.Sin(phase*.5f)+up*Mathf.Sin(phase))*amount*taper;
                case 5: return up*amount*taper;
                case 6: return (side*Mathf.Sin(phase)+up*Mathf.Cos(phase*2f))*amount*taper;
                case 7: return (side+up)*Mathf.Sin(phase)*Mathf.Sin(phase*.35f)*amount*taper*.7f;
                default: return Vector3.zero;
            }
        }

        internal static int OutlineCount(int style)
        {
            switch(style) { case 6:return 3; case 7:return 6; case 8:return 10; case 11:return 8; default:return 40; }
        }

        internal static Vector3 OutlinePoint(int style,int i,int count)
        {
            float a = 2f*Mathf.PI*i/count;
            if (style == 9)
            {
                float v=Mathf.Sin(a);
                return new Vector3(v*v*v,(13f*Mathf.Cos(a)-5f*Mathf.Cos(2f*a)-2f*Mathf.Cos(3f*a)-Mathf.Cos(4f*a))/17f,0f);
            }
            float r = style == 8 ? (i%2==0 ? 1f : .45f) : style == 10 ? .76f+.24f*Mathf.Cos(a*5f) : 1f;
            return new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0f);
        }

        internal static void ApplyPreset(VolasConfiguration s,int preset)
        {
            int[] beams={0,2,5,3,4,7};
            int[] reticles={2,10,3,7,9,11};
            float[] hues={.57f,.48f,.64f,.13f,.92f,.71f};
            preset=Mathf.Clamp(preset,0,Presets.Length-1);
            s.GunBeamStyle.Value=beams[preset]; s.GunReticleStyle.Value=reticles[preset];
            s.GunHue.Value=hues[preset]; s.GunSaturation.Value=.8f; s.GunColorMode.Value=1;
            s.GunRainbow.Value=false; s.GunGlow.Value=preset!=0; s.GunPulse.Value=preset!=0;
            s.GunShowBeam.Value=true; s.GunShowReticle.Value=true; s.GunOpacity.Value=1f;
            s.GunRotateReticle.Value=preset!=0; s.GunBeamWidth.Value=.012f;
            s.GunReticleSize.Value=.07f; s.GunWaveAmount.Value=.045f; s.GunGlowAmount.Value=.17f;
            s.GunEndWidth.Value=.55f; s.GunSegments.Value=32; s.GunAnimationSpeed.Value=1f;
        }
    }
}
