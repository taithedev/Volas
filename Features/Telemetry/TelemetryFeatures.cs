using System;
using UnityEngine;

namespace Volas
{
    internal static class TelemetryFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            registry.Add(new Feature("frame_time", "Local", "Frame time HUD", "Show smoothed frame duration in milliseconds.", p => p.Effects.FrameTimeHud = true, p => p.Effects.FrameTimeHud = false));
            registry.Add(new Feature("session_clock", "Local", "Session timer", "Show elapsed time since Volas loaded.", p => p.Effects.SessionHud = true, p => p.Effects.SessionHud = false));
            registry.Add(new Feature("tracking_status", "Local", "Tracking status", "Show whether your headset and controllers are detected by Unity XR.", p => p.Effects.TrackingHud = true, p => p.Effects.TrackingHud = false));
            registry.Add(new Feature("controller_battery", "Local", "Controller battery", "Show battery percentages when the XR runtime reports them.", p => p.Effects.BatteryHud = true, p => p.Effects.BatteryHud = false));
            registry.Add(new Feature("hud_reset", "Local", "Clear HUD", "Disable local telemetry overlays.", p => { foreach (var f in p.Features.All) if (f.Category == "Telemetry" || (f.Category == "Local" && f.Id != "hud_reset")) p.Features.Disable(f); }, actionOnly: true));
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
