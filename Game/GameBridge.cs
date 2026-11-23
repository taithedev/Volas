using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace Volas
{
    // Avoids hard-coded Gorilla Tag class references so menu/visual modules survive game type renames.
    internal sealed class GameBridge
    {
        private Type playerType;
        private Type photonType;
        private Type pollerType;
        private Type gorillaTaggerType;
        private float nextPollerLookup;
        private Camera cachedCamera;
        private float nextTypeLookup;
        private Transform fallbackLeft;
        private Transform fallbackRight;
        internal object Player { get; private set; }

        internal GameBridge()
        {
            playerType = FindType("GorillaLocomotion.GTPlayer") ?? FindType("GorillaLocomotion.Player");
            photonType = FindType("Photon.Pun.PhotonNetwork");
            pollerType = FindType("ControllerInputPoller");
            gorillaTaggerType = FindType("GorillaTagger");
        }

        internal void Refresh()
        {
            if (Time.unscaledTime >= nextTypeLookup && (playerType == null || photonType == null))
            {
                if (playerType == null)
                    playerType = FindType("GorillaLocomotion.GTPlayer") ?? FindType("GorillaLocomotion.Player");
                if (photonType == null)
                    photonType = FindType("Photon.Pun.PhotonNetwork");
                nextTypeLookup = Time.unscaledTime + 2f;
            }

            if (playerType != null)
            {
                Player = GetMember(playerType, null, "Instance", "instance");
                if (Player is UnityEngine.Object && (UnityEngine.Object)Player == null)
                    Player = null;
            }
        }

        // Camera.main can be null in VR games: the active headset camera
        // is often not tagged MainCamera. Prefer an active stereo camera.
        internal Camera HeadCamera
        {
            get
            {
                if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
                    return cachedCamera;

                Camera candidate = Camera.main;
                Camera[] cameras = Camera.allCameras;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera current = cameras[i];
                    if (current == null || !current.isActiveAndEnabled) continue;
                    if (current.stereoTargetEye != StereoTargetEyeMask.None)
                    {
                        cachedCamera = current;
                        return cachedCamera;
                    }
                    if (candidate == null) candidate = current;
                }
                if (candidate == null)
                    candidate = UnityEngine.Object.FindObjectOfType<Camera>();
                cachedCamera = candidate;
                return cachedCamera;
            }
        }

        internal Rigidbody Body
        {
            get
            {
                if (Player == null) return null;
                var rb = GetMember(Player.GetType(), Player, "playerRigidBody", "playerRigidbody", "bodyCollider") as Rigidbody;
                if (rb != null) return rb;
                var component = Player as Component;
                return component != null ? component.GetComponent<Rigidbody>() : null;
            }
        }

        internal Transform LeftHand
        {
            get
            {
                return GetTransform("leftControllerTransform", "leftHandTransform", "leftHand")
                    ?? TryTrackedHand(XRNode.LeftHand, ref fallbackLeft);
            }
        }

        internal Transform RightHand
        {
            get
            {
                return GetTransform("rightControllerTransform", "rightHandTransform", "rightHand")
                    ?? TryTrackedHand(XRNode.RightHand, ref fallbackRight);
            }
        }

        private Transform GetTransform(params string[] names)
        {
            if (Player != null)
            {
                object fromPlayer = GetMember(Player.GetType(), Player, names);
                Transform resolved = fromPlayer as Transform ??
                    (fromPlayer as GameObject)?.transform;
                if (resolved != null) return resolved;
            }

            // Some GT builds expose controller transforms on GorillaTagger
            // rather than on the locomotion Player singleton.
            if (gorillaTaggerType == null)
                gorillaTaggerType = FindType("GorillaTagger");
            if (gorillaTaggerType != null)
            {
                object tagger = GetMember(gorillaTaggerType, null,
                    "Instance", "instance");
                if (tagger != null)
                {
                    object fromTagger = GetMember(tagger.GetType(), tagger, names);
                    Transform hand = fromTagger as Transform ??
                        (fromTagger as GameObject)?.transform;
                    if (hand != null) return hand;
                }
            }
            return null;
        }

        // Uses Gorilla Tag's own input poller for SteamVR and Oculus/Meta,
        // without creating a hard compile-time dependency on game code.
        internal bool TryControllerButton(string memberName, out bool pressed)
        {
            pressed = false;
            object state = ReadPollerMember(memberName);
            if (!(state is bool)) return false;
            pressed = (bool)state;
            return true;
        }

        internal bool TryControllerFloat(string memberName, out float value)
        {
            value = 0f;
            object state = ReadPollerMember(memberName);
            if (state == null) return false;
            try { value = Convert.ToSingle(state); return true; }
            catch { return false; }
        }

        private object ReadPollerMember(string memberName)
        {
            if (pollerType == null && Time.unscaledTime >= nextPollerLookup)
            {
                pollerType = FindType("ControllerInputPoller");
                nextPollerLookup = Time.unscaledTime + 2f;
            }
            if (pollerType == null) return null;
            object instance = GetMember(pollerType, null, "instance", "Instance");
            if (instance == null) return null;
            return GetMember(instance.GetType(), instance, memberName);
        }

        // Some Gorilla Tag releases rename player hand fields. Resolve tracked
        // XR hand poses relative to the real headset as a non-invasive fallback.
        // This creates no visible meshes and never changes player movement.
        private Transform TryTrackedHand(XRNode node, ref Transform fallback)
        {
            Camera camera = HeadCamera;
            if (camera == null) return null;
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var controller = InputDevices.GetDeviceAtXRNode(node);
            if (!headset.isValid || !controller.isValid) return null;

            Vector3 localHead;
            Vector3 localHand;
            if (!headset.TryGetFeatureValue(CommonUsages.devicePosition, out localHead) ||
                !controller.TryGetFeatureValue(CommonUsages.devicePosition, out localHand))
                return null;

            Quaternion localHeadRotation;
            Quaternion localHandRotation;
            if (!headset.TryGetFeatureValue(CommonUsages.deviceRotation, out localHeadRotation))
                localHeadRotation = Quaternion.identity;
            if (!controller.TryGetFeatureValue(CommonUsages.deviceRotation, out localHandRotation))
                localHandRotation = Quaternion.identity;

            Quaternion originRotation = camera.transform.rotation *
                Quaternion.Inverse(localHeadRotation);
            if (fallback == null)
            {
                var go = new GameObject(node == XRNode.LeftHand
                    ? "Volas XR left fallback"
                    : "Volas XR right fallback");
                fallback = go.transform;
            }
            fallback.position = camera.transform.position +
                originRotation * (localHand - localHead);
            fallback.rotation = originRotation * localHandRotation;
            return fallback;
        }

        internal void Dispose()
        {
            if (fallbackLeft != null) UnityEngine.Object.Destroy(fallbackLeft.gameObject);
            if (fallbackRight != null) UnityEngine.Object.Destroy(fallbackRight.gameObject);
            fallbackLeft = null;
            fallbackRight = null;
        }

        internal Vector3 Position
        {
            get
            {
                var playerComponent = Player as Component;
                if (playerComponent != null) return playerComponent.transform.position;
                return HeadCamera != null ? HeadCamera.transform.position : Vector3.zero;
            }
        }

        internal float Heading
        {
            get
            {
                return HeadCamera != null ? HeadCamera.transform.eulerAngles.y : 0f;
            }
        }

        internal bool TryFloat(string name, out float value)
        {
            value = 0f;
            var source = Player;
            if (source == null) return false;
            var member = GetMember(source.GetType(), source, name);
            if (member == null) return false;
            try { value = Convert.ToSingle(member); return true; }
            catch { return false; }
        }

        internal bool SetFloat(string name, float value)
        {
            if (Player == null) return false;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = Player.GetType();
            var field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(Player, value);
                return true;
            }
            var property = type.GetProperty(name, flags);
            if (property != null && property.CanWrite && property.PropertyType == typeof(float))
            {
                property.SetValue(Player, value, null);
                return true;
            }
            return false;
        }

        internal bool IsSoloOrModdedRoom()
        {
            if (photonType == null) return false; // Fail closed if room state cannot be inspected.
            var inRoom = GetMember(photonType, null, "InRoom");
            if (!(inRoom is bool)) return false;
            if (!(bool)inRoom) return true;

            var currentRoom = GetMember(photonType, null, "CurrentRoom");
            if (currentRoom == null) return false;
            var properties = GetMember(currentRoom.GetType(), currentRoom, "CustomProperties") as IDictionary;
            if (properties == null) return false;
            foreach (DictionaryEntry pair in properties)
            {
                // A recognized MODDED room is required. An arbitrary private room is not enough.
                string key = pair.Key == null ? "" : pair.Key.ToString();
                string value = pair.Value == null ? "" : pair.Value.ToString();
                if ((key.Equals("gameMode", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("mode", StringComparison.OrdinalIgnoreCase)) &&
                    value.IndexOf("MODDED", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (key.Equals("MODDED", StringComparison.OrdinalIgnoreCase) &&
                    (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1"))
                    return true;
            }
            return false;
        }

        internal string RoomSummary()
        {
            if (photonType == null) return "Photon unavailable";
            var inRoom = GetMember(photonType, null, "InRoom");
            if (!(inRoom is bool) || !(bool)inRoom) return "Solo";
            var room = GetMember(photonType, null, "CurrentRoom");
            if (room == null) return "Room unknown";
            var name = GetMember(room.GetType(), room, "Name");
            var count = GetMember(room.GetType(), room, "PlayerCount");
            return (name == null ? "Unknown room" : name.ToString()) +
                (count == null ? "" : " / " + count + " players");
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static object GetMember(Type type, object instance, params string[] names)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (string name in names)
            {
                try
                {
                    var property = type.GetProperty(name, flags);
                    if (property != null) return property.GetValue(instance, null);
                    var field = type.GetField(name, flags);
                    if (field != null) return field.GetValue(instance);
                }
                catch { }
            }
            return null;
        }
    }
}
