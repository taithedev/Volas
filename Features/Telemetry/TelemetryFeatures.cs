using System;
using UnityEngine;

namespace Volas
{
    internal static class TelemetryFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            registry.Add(new Feature(
                            "fps_hud", "Telemetry", "FPS HUD", "Show smoothed frames per second in VR and on desktop.",
                            p => p.Effects.FpsHud = true,
                            p => p.Effects.FpsHud = false));

            registry.Add(new Feature(
                            "clock_hud", "Telemetry", "Clock HUD", "Display the local 24-hour time.",
                            p => p.Effects.ClockHud = true,
                            p => p.Effects.ClockHud = false));

            registry.Add(new Feature(
                            "pos_hud", "Telemetry", "Position HUD", "Show your current X / Y / Z location.",
                            p => p.Effects.PositionHud = true,
                            p => p.Effects.PositionHud = false));

            registry.Add(new Feature(
                            "heading_hud", "Telemetry", "Heading HUD", "Show camera yaw in degrees.",
                            p => p.Effects.HeadingHud = true,
                            p => p.Effects.HeadingHud = false));

            registry.Add(new Feature(
                            "crosshair", "Telemetry", "PC crosshair", "Show a simple non-VR crosshair in the middle of the game window.",
                            p => p.Effects.DesktopCrosshair = true,
                            p => p.Effects.DesktopCrosshair = false));
        }
    }
}
