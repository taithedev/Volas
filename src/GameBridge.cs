using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Volas
{
    // Avoids hard-coded Gorilla Tag class references so menu/visual modules survive game type renames.
    internal sealed class GameBridge
    {
        private Type playerType;
        private Type photonType;
        internal object Player { get; private set; }

        internal GameBridge()
        {
            playerType = FindType("GorillaLocomotion.GTPlayer") ?? FindType("GorillaLocomotion.Player");
            photonType = FindType("Photon.Pun.PhotonNetwork");
        }

        internal void Refresh()
        {
            if (playerType == null)
                playerType = FindType("GorillaLocomotion.GTPlayer") ?? FindType("GorillaLocomotion.Player");
            if (photonType == null)
                photonType = FindType("Photon.Pun.PhotonNetwork");

            if (playerType != null)
            {
                Player = GetMember(playerType, null, "Instance", "instance");
                if (Player is UnityEngine.Object && (UnityEngine.Object)Player == null)
                    Player = null;
            }
        }

        internal Camera HeadCamera { get { return Camera.main; } }

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
                return GetTransform("leftControllerTransform", "leftHandTransform", "leftHand");
            }
        }

        internal Transform RightHand
        {
            get
            {
                return GetTransform("rightControllerTransform", "rightHandTransform", "rightHand");
            }
        }

        private Transform GetTransform(params string[] names)
        {
            if (Player == null) return null;
            var value = GetMember(Player.GetType(), Player, names);
            return value as Transform ?? (value as GameObject)?.transform;
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
                var value = (pair.Key == null ? "" : pair.Key.ToString()) + " " +
                            (pair.Value == null ? "" : pair.Value.ToString());
                if (value.IndexOf("MODDED", StringComparison.OrdinalIgnoreCase) >= 0)
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
