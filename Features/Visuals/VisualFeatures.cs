using System;
using UnityEngine;

namespace Volas
{
    internal static class VisualFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            registry.Add(new Feature(
                            "orbs", "Visuals", "Hand orbs", "Render bright local orbs at both hands.",
                            p => p.Effects.Orbs = true,
                            p => p.Effects.Orbs = false));

            registry.Add(new Feature(
                            "rainbow_orbs", "Visuals", "Rainbow hands", "Cycle the hand orb colors.",
                            p => p.Effects.RainbowOrbs = true,
                            p => p.Effects.RainbowOrbs = false));

            registry.Add(new Feature(
                            "trails", "Visuals", "Hand trails", "Draw fading trails behind both hands.",
                            p => p.Effects.Trails = true,
                            p => p.Effects.Trails = false));

            registry.Add(new Feature(
                            "lights", "Visuals", "Palm lights", "Attach local point lights to both hands.",
                            p => p.Effects.HandLights = true,
                            p => p.Effects.HandLights = false));

            registry.Add(new Feature(
                            "laser", "Visuals", "Pointer beam", "Draw a raycast-based local right hand beam.",
                            p => p.Effects.Laser = true,
                            p => p.Effects.Laser = false));

            registry.Add(new Feature(
                            "halo", "Visuals", "Head halo", "Show a local light-colored ring above the headset.",
                            p => p.Effects.Halo = true,
                            p => p.Effects.Halo = false));

            registry.Add(new Feature(
                            "breadcrumb", "Visuals", "Path markers", "Leave a temporary trail of local glowing spheres.",
                            p => p.Effects.Breadcrumbs = true,
                            p => p.Effects.Breadcrumbs = false));
            registry.Add(new Feature(
                "headlamp", "Visuals", "VR headlamp", "A bright local spotlight follows your headset.",
                p => p.ExtraVisuals.Headlamp = true,
                p => p.ExtraVisuals.Headlamp = false));

            registry.Add(new Feature(
                "orbiting", "Visuals", "Orbiting orbs", "Three glowing spheres orbit locally around the headset.",
                p => p.ExtraVisuals.OrbitingOrbs = true,
                p => p.ExtraVisuals.OrbitingOrbs = false));

            registry.Add(new Feature(
                "body_aura", "Visuals", "Body aura", "A glowing local ring follows your body.",
                p => p.ExtraVisuals.BodyAura = true,
                p => p.ExtraVisuals.BodyAura = false));

            registry.Add(new Feature(
                "hand_axes", "Visuals", "Hand axis guide", "Show orientation axes on the right controller.",
                p => p.ExtraVisuals.HandAxes = true,
                p => p.ExtraVisuals.HandAxes = false));
        }
    }
}
