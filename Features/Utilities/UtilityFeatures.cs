using System;
using UnityEngine;

namespace Volas
{
    internal static class UtilityFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            float oldVolume = AudioListener.volume;
            int oldFrameRate = Application.targetFrameRate;
            int oldVsync = QualitySettings.vSyncCount;

            registry.Add(new Feature(
                            "mute", "Utilities", "Mute audio", "Mute local game audio, restoring the prior volume when disabled.",
                            p => { oldVolume = AudioListener.volume; AudioListener.volume = 0f; },
                            p => AudioListener.volume = oldVolume));

            registry.Add(new Feature(
                            "cap90", "Utilities", "90 FPS target", "Request a local 90 FPS application cap (VR runtime may override).",
                            p => { oldFrameRate = Application.targetFrameRate; Application.targetFrameRate = 90; },
                            p => Application.targetFrameRate = oldFrameRate));

            registry.Add(new Feature(
                            "vsync", "Utilities", "VSync off", "Disable desktop VSync locally (VR runtime may override).",
                            p => { oldVsync = QualitySettings.vSyncCount; QualitySettings.vSyncCount = 0; },
                            p => QualitySettings.vSyncCount = oldVsync));

            registry.Add(new Feature(
                            "copy_pos", "Utilities", "Copy coordinates", "Copy your current X,Y,Z position to the Windows clipboard.",
                            p =>
                            {
                                var point = p.Game.Position;
                                GUIUtility.systemCopyBuffer = point.x.ToString("F2") + ", " +
                                    point.y.ToString("F2") + ", " + point.z.ToString("F2");
                            },
                            actionOnly: true));

            registry.Add(new Feature(
                            "room_status", "Utilities", "Log room status", "Record the active room name/player count in BepInEx log.",
                            p => p.Log.LogInfo("Volas room status: " + p.Game.RoomSummary()),
                            actionOnly: true));
        }
    }
}
