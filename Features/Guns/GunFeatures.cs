namespace Volas
{
    internal static class GunFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            string[] outlines = { "Triangle", "Hexagon", "Star", "Heart", "Flower", "Octagon" };
            for(int i=0;i<outlines.Length;i++)
            {
                int index=i;
                registry.Add(new Feature("pointer_outline_"+i,"Gun Mods",outlines[i]+" stamp","Place a temporary local decorative outline.",p=>p.CreativeGun.SetShape(index,true),p=>p.CreativeGun.SetShape(index,false)));
            }
            registry.Add(new Feature("gun_preview", "Gun Mods", "Pointer preview", "Test the local grip-controlled pointer without placing objects.", p => p.Gun.Preview = true, p => p.Gun.Preview = false));
            registry.Add(new Feature(
                "gun_marker", "Gun Mods", "Marker gun", "Place a local luminous marker on a surface.",
                p => { p.Guns.Marker = true; },
                p => p.Guns.Marker = false));

            registry.Add(new Feature(
                "gun_paint", "Gun Mods", "Paint gun", "Place a local colored paint disc on a surface.",
                p => { p.Guns.Paint = true; },
                p => p.Guns.Paint = false));

            registry.Add(new Feature(
                "gun_beacon", "Gun Mods", "Beacon gun", "Create a temporary local beacon at the surface.",
                p => { p.Guns.Beacon = true; },
                p => p.Guns.Beacon = false));

            registry.Add(new Feature(
                "gun_ring", "Gun Mods", "Ring gun", "Draw a local circle at the surface normal.",
                p => { p.Guns.Ring = true; },
                p => p.Guns.Ring = false));

            registry.Add(new Feature(
                "gun_pulse", "Gun Mods", "Pulse gun", "Create an expanding local pulse on impact.",
                p => { p.Guns.PulseGun = true; },
                p => p.Guns.PulseGun = false));

            registry.Add(new Feature(
                "gun_confetti", "Gun Mods", "Confetti gun", "Spawn a temporary local confetti burst.",
                p => { p.Guns.Confetti = true; },
                p => p.Guns.Confetti = false));

            registry.Add(new Feature(
                "gun_trail", "Gun Mods", "Trail gun", "Hold trigger to draw temporary surface dots.",
                p => { p.Guns.Trail = true; },
                p => p.Guns.Trail = false));

            registry.Add(new Feature(
                "gun_inspect", "Gun Mods", "Inspect gun", "Show the hit object name and layer.",
                p => { p.Guns.Inspector = true; },
                p => p.Guns.Inspector = false));

            registry.Add(new Feature(
                "gun_ruler", "Gun Mods", "Ruler gun", "Click two points to measure distance.",
                p => { p.Guns.Ruler = true; },
                p => p.Guns.Ruler = false));

            registry.Add(new Feature(
                "gun_range", "Gun Mods", "Range finder", "Display the target distance in meters.",
                p => { p.Guns.Distance = true; },
                p => p.Guns.Distance = false));

            registry.Add(new Feature(
                "gun_coordinates", "Gun Mods", "Coordinate gun", "Display the target X/Y/Z location.",
                p => { p.Guns.Coordinates = true; },
                p => p.Guns.Coordinates = false));

            registry.Add(new Feature(
                "gun_spotlight", "Gun Mods", "Spotlight gun", "Light up the target surface locally.",
                p => { p.Guns.Spotlight = true; },
                p => p.Guns.Spotlight = false));

            registry.Add(new Feature(
                "gun_grapple", "Gun Mods", "Grapple gun", "Hold trigger to pull toward an anchored surface (MODDED/solo).",
                p => { p.Movement.RequireMovementPlayer(); p.Guns.Grapple = true; },
                p => p.Guns.Grapple = false,
                needsModdedRoom: true));

            registry.Add(new Feature(
                "gun_dash", "Gun Mods", "Dash gun", "Apply a local impulse in the aim direction (MODDED/solo).",
                p => { p.Movement.RequireMovementPlayer(); p.Guns.Dash = true; },
                p => p.Guns.Dash = false,
                needsModdedRoom: true));
            registry.Add(new Feature(
                "gun_grid", "Gun Mods", "Surface grid", "Draw a glowing grid on a local surface.",
                p => p.CreativeGun.Grid = true,
                p => p.CreativeGun.Grid = false));

            registry.Add(new Feature(
                "gun_spiral", "Gun Mods", "Spiral stamp", "Draw a luminous spiral decal.",
                p => p.CreativeGun.Spiral = true,
                p => p.CreativeGun.Spiral = false));

            registry.Add(new Feature(
                "gun_arrow", "Gun Mods", "Arrow marker", "Mark a surface with a glowing arrow.",
                p => p.CreativeGun.Arrow = true,
                p => p.CreativeGun.Arrow = false));

            registry.Add(new Feature(
                "gun_sign", "Gun Mods", "Volas sign", "Place a local VOLAS sign on the surface.",
                p => p.CreativeGun.Sign = true,
                p => p.CreativeGun.Sign = false));

            registry.Add(new Feature(
                "gun_sparkles", "Gun Mods", "Sparkle tool", "Scatter colorful local particles at the aimed surface.",
                p => p.CreativeGun.Sparkles = true,
                p => p.CreativeGun.Sparkles = false));

            registry.Add(new Feature(
                "gun_checker", "Gun Mods", "Checkerboard stamp", "Place a local neon checkerboard decal.",
                p => p.CreativeGun.Checkerboard = true,
                p => p.CreativeGun.Checkerboard = false));
        }
    }
}
