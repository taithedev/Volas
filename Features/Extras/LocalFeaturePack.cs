using System;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    // Explicitly local/solo gameplay features. No Photon RPCs, room kicks or remote effects.
    internal static class LocalFeaturePack
    {
        private static Vector3 checkpoint;
        private static bool checkpointSaved;

        internal static void Register(FeatureRegistry registry)
        {
            RegisterMovement(registry);
            RegisterVisuals(registry);
            RegisterFun(registry);
            RegisterSafety(registry);
        }

        private static void RegisterMovement(FeatureRegistry r)
        {
            r.Add(new Feature(
                "checkpoint_save", "Movement", "Save checkpoint",
                "Save your current local position for later return (solo/MODDED).",
                p =>
                {
                    p.Movement.RequireMovementPlayer();
                    checkpoint = p.Game.Body.position;
                    checkpointSaved = true;
                    p.Log.LogInfo("Checkpoint saved: " + checkpoint);
                }, actionOnly: true, needsModdedRoom: true));

            r.Add(new Feature(
                "checkpoint_return", "Movement", "Return to checkpoint",
                "Teleport your own Rigidbody back to the saved position (solo/MODDED).",
                p =>
                {
                    p.Movement.RequireMovementPlayer();
                    if (!checkpointSaved)
                        throw new InvalidOperationException("Save a checkpoint first.");
                    Rigidbody body = p.Game.Body;
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.position = checkpoint;
                }, actionOnly: true, needsModdedRoom: true));

            r.Add(new Feature(
                "unstuck_local", "Movement", "Unstuck",
                "Move your own body up 1.5 meters (solo/MODDED).",
                p =>
                {
                    p.Movement.RequireMovementPlayer();
                    Rigidbody body = p.Game.Body;
                    body.velocity = Vector3.zero;
                    body.position += Vector3.up * 1.5f;
                }, actionOnly: true, needsModdedRoom: true));

            r.Add(new Feature(
                "brake_local", "Movement", "Stop momentum",
                "Zero your local Rigidbody velocity (solo/MODDED).",
                p =>
                {
                    p.Movement.RequireMovementPlayer();
                    Rigidbody body = p.Game.Body;
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }, actionOnly: true, needsModdedRoom: true));

            r.Add(new Feature(
                "grip_boost", "Movement", "Grip boost [rg]",
                "Hold right grip for gentle thrust along the right controller (solo/MODDED).",
                p => p.Movement.RequireMovementPlayer(),
                onFixedUpdate: p =>
                {
                    if (!RightGripHeld()) return;
                    Rigidbody body = p.Game.Body;
                    Transform hand = p.Game.RightHand;
                    if (body != null && hand != null)
                        body.AddForce(hand.forward * 6f, ForceMode.Acceleration);
                }, needsModdedRoom: true));

            r.Add(new Feature(
                "grip_glide", "Movement", "Grip glide [rg]",
                "Hold right grip to soften gravity while airborne (solo/MODDED).",
                p => p.Movement.RequireMovementPlayer(),
                onFixedUpdate: p =>
                {
                    if (!RightGripHeld()) return;
                    Rigidbody body = p.Game.Body;
                    if (body != null)
                        body.AddForce(-Physics.gravity * .35f, ForceMode.Acceleration);
                }, needsModdedRoom: true));

            r.Add(new Feature(
                "strafe_local", "Movement", "PC strafe assist",
                "A/D key input adds mild sideways acceleration (solo/MODDED).",
                p => p.Movement.RequireMovementPlayer(),
                onFixedUpdate: p =>
                {
                    Rigidbody body = p.Game.Body;
                    Camera camera = p.Game.HeadCamera;
                    if (body == null || camera == null) return;
                    float x = Input.GetAxisRaw("Horizontal");
                    Vector3 direction = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
                    body.AddForce(direction * x * 6f, ForceMode.Acceleration);
                }, needsModdedRoom: true));
        }

        private static void RegisterVisuals(FeatureRegistry r)
        {
            GameObject headMarker = null;
            r.Add(new Feature(
                "head_marker", "Visuals", "Head marker",
                "Small local marker hovers above your headset.",
                p =>
                {
                    if (p.Game.HeadCamera == null)
                        throw new InvalidOperationException("Head camera not ready.");
                    if (headMarker == null)
                    {
                        headMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        headMarker.name = "Volas local head marker";
                        headMarker.transform.localScale = Vector3.one * .12f;
                        Collider c = headMarker.GetComponent<Collider>();
                        if (c != null) c.enabled = false;
                    }
                },
                p =>
                {
                    if (headMarker != null) UnityEngine.Object.Destroy(headMarker);
                    headMarker = null;
                },
                onUpdate: p =>
                {
                    if (headMarker != null && p.Game.HeadCamera != null)
                        headMarker.transform.position =
                            p.Game.HeadCamera.transform.position + Vector3.up * .38f;
                }));

            GameObject rightMarker = null;
            r.Add(new Feature(
                "right_hand_marker", "VR Rig", "Right hand marker",
                "A small local sphere follows your right controller.",
                p =>
                {
                    if (p.Game.RightHand == null)
                        throw new InvalidOperationException("Right controller not tracked.");
                    rightMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    rightMarker.name = "Volas right hand marker";
                    rightMarker.transform.localScale = Vector3.one * .085f;
                    Collider c = rightMarker.GetComponent<Collider>();
                    if (c != null) c.enabled = false;
                },
                p => { if (rightMarker != null) UnityEngine.Object.Destroy(rightMarker); rightMarker = null; },
                onUpdate: p =>
                {
                    if (rightMarker != null && p.Game.RightHand != null)
                        rightMarker.transform.position = p.Game.RightHand.position;
                }));

            GameObject leftMarker = null;
            r.Add(new Feature(
                "left_hand_marker", "VR Rig", "Left hand marker",
                "A small local sphere follows your left controller.",
                p =>
                {
                    if (p.Game.LeftHand == null)
                        throw new InvalidOperationException("Left controller not tracked.");
                    leftMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leftMarker.name = "Volas left hand marker";
                    leftMarker.transform.localScale = Vector3.one * .085f;
                    Collider c = leftMarker.GetComponent<Collider>();
                    if (c != null) c.enabled = false;
                },
                p => { if (leftMarker != null) UnityEngine.Object.Destroy(leftMarker); leftMarker = null; },
                onUpdate: p =>
                {
                    if (leftMarker != null && p.Game.LeftHand != null)
                        leftMarker.transform.position = p.Game.LeftHand.position;
                }));

            float originalFov = 60f;
            Camera activeCamera = null;
            r.Add(new Feature(
                "zoom_pc", "Visuals", "PC camera zoom",
                "Zoom the non-VR camera to 45 degrees; disable to restore original FOV.",
                p =>
                {
                    if (InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid)
                        throw new InvalidOperationException("Camera zoom is desktop-only.");
                    activeCamera = p.Game.HeadCamera;
                    if (activeCamera == null) throw new InvalidOperationException("No camera.");
                    originalFov = activeCamera.fieldOfView;
                    activeCamera.fieldOfView = 45f;
                },
                p =>
                {
                    if (activeCamera != null) activeCamera.fieldOfView = originalFov;
                    activeCamera = null;
                }));
        }

        private static void RegisterFun(FeatureRegistry r)
        {
            r.Add(new Feature(
                "confetti_burst", "Fun", "Local confetti burst",
                "Spawn twelve short-lived decorative spheres around your headset.",
                p =>
                {
                    Camera camera = p.Game.HeadCamera;
                    if (camera == null) throw new InvalidOperationException("No camera.");
                    for (int i = 0; i < 12; i++)
                    {
                        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        ball.name = "Volas local confetti";
                        ball.transform.position = camera.transform.position +
                            UnityEngine.Random.insideUnitSphere * .8f +
                            camera.transform.forward * .9f;
                        ball.transform.localScale = Vector3.one * .028f;
                        Collider collider = ball.GetComponent<Collider>();
                        if (collider != null) collider.enabled = false;
                        UnityEngine.Object.Destroy(ball, 3f);
                    }
                }, actionOnly: true));

            r.Add(new Feature(
                "room_summary", "Fun", "Room information",
                "Show the current room status in the BepInEx log; no network changes.",
                p => p.Log.LogInfo("Volas: " + p.Game.RoomSummary()),
                actionOnly: true));
        }

        private static void RegisterSafety(FeatureRegistry r)
        {
            r.Add(new Feature(
                "panic_local", "Safety", "Panic: disable all",
                "Immediately disable all enabled Volas features and reset local gun visuals.",
                p => p.Features.DisableAll(), actionOnly: true));

            r.Add(new Feature(
                "clear_marks", "Safety", "Clear local gun effects",
                "Remove temporary Volas gun markers, beams and effects from this client.",
                p =>
                {
                    p.Guns.Reset();
                    p.CreativeGun.Reset();
                    p.Gun.Reset();
                }, actionOnly: true));

            r.Add(new Feature(
                "disable_movement", "Safety", "Disable movement mods",
                "Turn off active movement modifiers and restore standard locomotion.",
                p =>
                {
                    foreach (Feature feature in p.Features.All)
                        if (feature.Category == "Movement" && feature.Enabled)
                            p.Features.Disable(feature);
                    p.Movement.Reset();
                }, actionOnly: true));

            r.Add(new Feature(
                "disable_visuals", "Safety", "Disable visual mods",
                "Turn off Volas' local visual overlays and decoration features.",
                p =>
                {
                    foreach (Feature feature in p.Features.All)
                        if (feature.Category == "Visuals" && feature.Enabled)
                            p.Features.Disable(feature);
                }, actionOnly: true));
        }

        private static bool RightGripHeld()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid) return false;
            bool held;
            if (device.TryGetFeatureValue(CommonUsages.gripButton, out held)) return held;
            float analog;
            return device.TryGetFeatureValue(CommonUsages.grip, out analog) && analog > .55f;
        }
    }
}
