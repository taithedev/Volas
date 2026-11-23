using System;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    internal sealed class MovementEffects
    {
        private readonly VolasPlugin plugin;
        private object originalPlayer;
        private float originalSpeed;
        private float originalJump;
        private float originalMultiplier;
        private bool captured;

        internal bool SpeedBoost;
        internal bool HighJump;
        internal bool LowGravity;
        internal bool Fly;

        internal MovementEffects(VolasPlugin owner) { plugin = owner; }

        internal void RequireMovementPlayer()
        {
            if (plugin.Game.Body == null)
                throw new InvalidOperationException("Enter Gorilla Tag gameplay before enabling movement.");
            if (!plugin.Game.IsSoloOrModdedRoom())
                throw new InvalidOperationException("Available only in solo or recognized MODDED rooms.");
        }

        internal void RequireLocomotionFields()
        {
            RequireMovementPlayer();
            if (!TryCapture())
                throw new InvalidOperationException("This Gorilla Tag build does not expose the required movement fields.");
        }

        private bool TryCapture()
        {
            if (plugin.Game.Player == null) return false;
            if (captured && ReferenceEquals(originalPlayer, plugin.Game.Player))
                return true;
            captured = false;
            originalPlayer = plugin.Game.Player;
            float speed, jump, multiplier;
            if (!plugin.Game.TryFloat("velocityLimit", out speed) ||
                !plugin.Game.TryFloat("maxJumpSpeed", out jump) ||
                !plugin.Game.TryFloat("jumpMultiplier", out multiplier))
                return false;
            originalSpeed = speed;
            originalJump = jump;
            originalMultiplier = multiplier;
            captured = true;
            return true;
        }

        internal void Tick()
        {
            if (!SpeedBoost && !HighJump)
            {
                RestoreLocomotion();
                return;
            }
            if (!plugin.Game.IsSoloOrModdedRoom())
            {
                RestoreLocomotion();
                return;
            }
            if (!TryCapture()) return;

            plugin.Game.SetFloat("velocityLimit", originalSpeed * (SpeedBoost ? 1.4f : 1f));
            plugin.Game.SetFloat("maxJumpSpeed", originalJump * (HighJump ? 1.5f : 1f));
            plugin.Game.SetFloat("jumpMultiplier", originalMultiplier * (HighJump ? 1.35f : 1f));
        }

        internal void FixedTick()
        {
            if ((!LowGravity && !Fly) || !plugin.Game.IsSoloOrModdedRoom()) return;
            var body = plugin.Game.Body;
            if (body == null) return;

            if (LowGravity)
                body.AddForce(-Physics.gravity * 0.55f, ForceMode.Acceleration);

            if (Fly)
            {
                bool held = false;
                var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightDevice.isValid)
                    rightDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out held);
                if (held)
                {
                    var direction = plugin.Game.RightHand != null
                        ? plugin.Game.RightHand.forward
                        : (plugin.Game.HeadCamera != null ? plugin.Game.HeadCamera.transform.forward : Vector3.forward);
                    body.AddForce(direction.normalized * 16f, ForceMode.Acceleration);
                }
            }
        }

        internal void RestoreLocomotion()
        {
            if (!captured) return;
            if (ReferenceEquals(originalPlayer, plugin.Game.Player))
            {
                plugin.Game.SetFloat("velocityLimit", originalSpeed);
                plugin.Game.SetFloat("maxJumpSpeed", originalJump);
                plugin.Game.SetFloat("jumpMultiplier", originalMultiplier);
            }
            captured = false;
            originalPlayer = null;
        }

        internal void Reset()
        {
            SpeedBoost = false;
            HighJump = false;
            LowGravity = false;
            Fly = false;
            RestoreLocomotion();
        }
    }
}
