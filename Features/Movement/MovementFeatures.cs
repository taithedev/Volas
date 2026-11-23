using System;
using UnityEngine;

namespace Volas
{
    internal static class MovementFeatures
    {
        internal static void Register(FeatureRegistry registry)
        {
            registry.Add(new Feature(
                            "speed", "Movement", "Speed boost", "Local 40% velocity cap boost.",
                            p => { p.Movement.RequireLocomotionFields(); p.Movement.SpeedBoost = true; },
                            p => p.Movement.SpeedBoost = false,
                            needsModdedRoom: true));

            registry.Add(new Feature(
                            "jump", "Movement", "High jump", "Higher local jump power.",
                            p => { p.Movement.RequireLocomotionFields(); p.Movement.HighJump = true; },
                            p => p.Movement.HighJump = false,
                            needsModdedRoom: true));

            registry.Add(new Feature(
                            "gravity", "Movement", "Float gravity", "Reduces effective gravity on your own Rigidbody.",
                            p => { p.Movement.RequireMovementPlayer(); p.Movement.LowGravity = true; },
                            p => p.Movement.LowGravity = false,
                            needsModdedRoom: true));

            registry.Add(new Feature(
                            "fly", "Movement", "Hand flight", "Hold right B in solo or MODDED rooms to fly in hand direction.",
                            p => { p.Movement.RequireMovementPlayer(); p.Movement.Fly = true; },
                            p => p.Movement.Fly = false,
                            needsModdedRoom: true));
        }
    }
}
