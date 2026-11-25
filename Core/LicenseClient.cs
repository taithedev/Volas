using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Volas
{
    internal sealed class LicenseClient
    {
        #region State
        private const string Endpoint = "https://br-flat-glitter-bd1gbj6a-volaslicense.compute.c-8.us-east-2.aws.neon.tech/";
        private readonly VolasPlugin plugin;
        private string token = "", role = "", key = "", status = "Enter your Volas license key.";
        private string announcement = "", announcementId = "", draft = "";
        private string licensed = "0", revoked = "0", online = "0";
        private string eventPreset = "";
        private float eventExpires;
        internal bool CommunityEffects { get; set; }
        internal string ActiveEvent { get { return Authenticated && Time.realtimeSinceStartup < eventExpires ? eventPreset : ""; } }
        internal int EventRemaining { get { return Mathf.Max(0, Mathf.CeilToInt(eventExpires - Time.realtimeSinceStartup)); } }
        internal string CloudStatus { get { return status; } }
        internal string LicenseStats { get { return "Valid: " + licensed + " / revoked: " + revoked + " / opted-in online: " + online; } }
        private bool busy, sharePresence, panel = true, ownsCursor;
        private bool savedCursor;
        private CursorLockMode savedLock;
        private float nextHeartbeat, verifiedAt, toastUntil;
        private int generation;
        private Rect bounds;
        internal bool Authenticated { get { return token.Length != 0 && Time.realtimeSinceStartup - verifiedAt <= 135f; } }
        internal bool IsOwner { get { return Authenticated && role == "owner"; } }
        internal bool PanelOpen { get { return panel; } }
        private int failures;
        private GUIStyle announcementStyle;
        internal void OpenAccountPanel() { if (Authenticated) OpenPanel(); }
        internal LicenseClient(VolasPlugin owner) { plugin = owner; }
        #endregion

        #region Lifecycle
        internal void Tick()
        {
            if (token.Length != 0 && !Authenticated) LoseSession("Connection verification expired. Sign in again.");
            if (Authenticated)
            {
                if (MenuInput.Pressed(KeyCode.F8)) { if (panel) HidePanel(); else OpenPanel(); }
                if (!busy && Time.realtimeSinceStartup >= nextHeartbeat) Start("heartbeat", "");
            }
            else if (MenuInput.Shortcut(plugin.Settings.DesktopShortcut.Value, KeyCode.F6)) panel = !panel;
            // Do not take the cursor until the desktop menu has restored its own cursor state.
            if (panel && !ownsCursor && !plugin.DesktopOpen && !plugin.LegacyDesktopOpen)
            {
                savedCursor = Cursor.visible; savedLock = Cursor.lockState; ownsCursor = true;
            }
            if (panel && ownsCursor) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
            else if (!panel) RestoreCursor();
        }
        internal void OpenOwnerPanel() { if (IsOwner) OpenPanel(); }
        private void OpenPanel()
        {
            plugin.DesktopOpen = false; plugin.LegacyDesktopOpen = false; plugin.VrOpen = false;
            panel = true; status = "Session verified.";
        }
        private void HidePanel() { panel = false; RestoreCursor(); }
        private void RestoreCursor()
        {
            if (!ownsCursor) return;
            Cursor.visible = savedCursor; Cursor.lockState = savedLock; ownsCursor = false;
        }
        private void LoseSession(string message)
        {
            ++generation; token = ""; role = ""; key = ""; busy = false;
            draft = ""; announcement = ""; announcementId = ""; toastUntil = 0f;
            eventPreset = ""; eventExpires = 0f; CommunityEffects = false;
            RestoreCursor();
            plugin.CloseLicensedMenus();
            panel = true; status = message;
        }
        #endregion

        #region Network
        private void Start(string action, string body)
        {
            if (busy) return;
            if (action == "login" && !ValidKey(body)) { status = "Enter a complete VOLAS license key."; return; }
            if (action != "login" && !Authenticated) return;
            if ((action == "announce" || action == "event") && !IsOwner) return;
            busy = true;
            status = action == "login" ? "Verifying key..." : action == "announce" ? "Sending announcement..." : status;
            nextHeartbeat = Time.realtimeSinceStartup + (CommunityEffects ? 20f : 45f) + UnityEngine.Random.Range(0f, 5f);
            plugin.StartCoroutine(Request(action, body, generation));
        }
        private static bool ValidKey(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 70 || !value.StartsWith("VOLAS-", StringComparison.OrdinalIgnoreCase)) return false;
            for (int i = 6; i < value.Length; i++) if (!Uri.IsHexDigit(value[i])) return false;
            return true;
        }

        private IEnumerator Request(string action, string body, int attempt)
        {
            using (var request = new UnityWebRequest(Endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body ?? ""));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 12;
                request.redirectLimit = 0;
                request.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");
                request.SetRequestHeader("X-Volas-Action", action);
                request.SetRequestHeader("X-Volas-Presence", sharePresence ? "true" : "false");
                if (action != "login") request.SetRequestHeader("Authorization", "Bearer " + token);
                yield return request.SendWebRequest();
                if (attempt != generation) yield break;
                busy = false;
                if (action == "logout") { LoseSession("Signed out."); yield break; }
                if (request.responseCode == 401)
                {
                    LoseSession(action == "login" ? "Invalid or revoked key." : "Session expired or revoked.");
                    yield break;
                }
                if (request.responseCode != 200)
                {
                    failures = Mathf.Min(failures + 1, 4);
                    status = request.responseCode == 429 ? "Service busy. Retrying verification." : "Network request failed. Try again.";
                    nextHeartbeat = Time.realtimeSinceStartup + Mathf.Min(45f, 5f * (1 << failures));
                    yield break;
                }
                string[] fields = request.downloadHandler.text.Split('\n');
                if (fields.Length < 8 || fields[0] != "OK" || (fields[1] != "owner" && fields[1] != "member"))
                {
                    status = "Invalid response from license service."; yield break;
                }
                if (action == "login")
                {
                    if (fields[2].Length != 64) { status = "Invalid session response."; yield break; }
                    token = fields[2]; key = "";
                }
                failures = 0;
                role = fields[1]; verifiedAt = Time.realtimeSinceStartup;
                if (action == "login") { HidePanel(); plugin.OnLicenseVerified(); }
                licensed = fields[5]; revoked = fields[6]; online = fields[7];
                int remaining;
                if (fields.Length >= 10 && int.TryParse(fields[9], out remaining) && remaining > 0 && (fields[8] == "aurora" || fields[8] == "pulse" || fields[8] == "orbit"))
                { eventPreset = fields[8]; eventExpires = Time.realtimeSinceStartup + Mathf.Clamp(remaining, 0, 300); }
                else { eventPreset = ""; eventExpires = 0f; }
                if (fields[3].Length != 0 && fields[3] != announcementId)
                {
                    try { announcement = Encoding.UTF8.GetString(Convert.FromBase64String(fields[4])); announcementId = fields[3]; toastUntil = Time.realtimeSinceStartup + 12f; }
                    catch (FormatException) { }
                }
                status = action == "announce" ? "Announcement published." : action == "event" ? "Community event updated." : "Session verified.";
                if (action == "announce") draft = "";
            }
        }
        #endregion

        internal void SetCommunityEffects(bool enabled) { CommunityEffects = enabled; if (enabled) nextHeartbeat = 0f; }
        internal void PublishEvent(string preset) { if (IsOwner && !busy) Start("event", preset); }
        internal void RefreshCloud() { if (Authenticated && !busy) Start("heartbeat", ""); }

        #region Interface
        internal void Draw()
        {
            if (panel)
            {
                float width = Mathf.Min(450f, Screen.width - 24f), height = IsOwner ? 550f : 380f;
                bounds = new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height);
                GUI.Box(bounds, "");
                GUILayout.BeginArea(new Rect(bounds.x+18f,bounds.y+16f,bounds.width-36f,bounds.height-32f));
                GUILayout.Label(Authenticated ? (IsOwner ? "VOLAS / OWNER CONTROL" : "VOLAS / SESSION") : "VOLAS / LICENSE REQUIRED");
                GUILayout.Label("Volas 1.9.1  |  Secure session");
                GUILayout.Space(9f);
                if (!Authenticated)
                {
                    GUILayout.Label("A valid key is required each time the game starts.");
                    key = GUILayout.PasswordField(key, '*', 80);
                    GUILayout.Space(4f);
                    sharePresence = GUILayout.Toggle(sharePresence, "Share anonymous online presence (optional)");
                    GUI.enabled = !busy;
                    if (GUILayout.Button("Verify license", GUILayout.Height(34f))) Start("login", key.Trim());
                    if (busy) GUILayout.Label("Connecting to Volas license service...");
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("Role: " + role);
                    if (GUILayout.Button("Open desktop menu")) { HidePanel(); plugin.OpenVerifiedDesktop(); }
                    if (GUILayout.Button("Open legacy menu")) { HidePanel(); plugin.OpenVerifiedLegacy(); }
                    bool consent = GUILayout.Toggle(sharePresence, "Share anonymous online presence (optional)");
                    if (consent != sharePresence) { sharePresence = consent; nextHeartbeat = 0f; }
                    bool participate = GUILayout.Toggle(CommunityEffects, "Participate in community cosmetic events");
                    if (participate != CommunityEffects) SetCommunityEffects(participate);
                    GUILayout.Label(ActiveEvent.Length == 0 ? "No active community event" : ActiveEvent + " / " + EventRemaining + "s left");
                    if (IsOwner)
                    {
                        GUILayout.Space(6f);
                        GUILayout.Label("Valid licenses: " + licensed + "   Revoked: " + revoked);
                        GUILayout.Label("Online (opted in): " + online);
                        GUILayout.Space(8f);
                        GUILayout.Label("Global announcement (up to 500 characters)");
                        draft = GUILayout.TextArea(draft, 500, GUILayout.Height(65f));
                        GUI.enabled = !busy;
                        if (GUILayout.Button("Publish announcement", GUILayout.Height(32f)) && !string.IsNullOrWhiteSpace(draft)) Start("announce", draft);
                        if (GUILayout.Button("Refresh stats")) Start("heartbeat", "");
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Aurora")) PublishEvent("aurora");
                        if (GUILayout.Button("Pulse")) PublishEvent("pulse");
                        if (GUILayout.Button("Orbit")) PublishEvent("orbit");
                        if (GUILayout.Button("Stop")) PublishEvent("stop");
                        GUILayout.EndHorizontal();
                        GUI.enabled = true;
                    }
                    GUI.enabled = !busy;
                    if (GUILayout.Button("Sign out")) { Start("logout", ""); LoseSession("Signed out."); }
                    GUI.enabled = true;
                }
                GUILayout.Label(status);
                if (GUILayout.Button(Authenticated ? "Close (F8)" : "Hide (F6 reopens)")) HidePanel();
                GUILayout.EndArea();
            }
            else if (Authenticated && Time.realtimeSinceStartup < toastUntil && announcement.Length != 0)
            {
                float width = Mathf.Min(480f, Screen.width-24f);
                GUI.Box(new Rect((Screen.width-width)*.5f,18f,width,85f), "VOLAS ANNOUNCEMENT");
                if (announcementStyle == null) announcementStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false, alignment = TextAnchor.UpperCenter };
                GUI.Label(new Rect((Screen.width-width)*.5f+12f,43f,width-24f,54f),announcement,announcementStyle);
            }
        }
        internal void Dispose() { ++generation; token = ""; key = ""; RestoreCursor(); }
        #endregion
    }
}
