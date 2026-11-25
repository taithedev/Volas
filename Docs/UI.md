# Volas desktop UI (reference-matched)

## Desktop

The desktop UI is implemented entirely in `UI/Desktop/DesktopMenu.cs` and opens with **F6** inside Gorilla Tag PCVR. It is not a separate executable.

The desktop UI now tracks the supplied Untitled reference: compact violet logo, small gray version label, understated search, close and settings icons, tiny horizontally scrollable tabs, a nearly black rounded panel and a flat three-column Home grid. Home entries are arranged in the same columns and order as the image. The bottom of Home stays uncluttered. Other pages use the same palette, subtle rows, favorite stars and small violet switches. The categories map to real Volas features: **Projectiles → Gun Mods**, **Players → Telemetry**, **Fun → Fun**, and **VR Rig → VR Rig**. Settings now has independent uGUI appearance controls and a linked Gun Studio. Screenshot-only headings without registered modules show an honest empty state rather than fake buttons.

For the closest proportions, the default window is 760 × 500 pixels. Older installations may have persisted another window size in BepInEx settings; resizing is available from the lower-right corner. F6 toggles the panel. Font rendering depends on the game's Unity font availability, so identical typography across game versions cannot be guaranteed. Window resizing proportionally scales all contents.

The interface uses programmatically constructed **Unity uGUI**: Canvas, CanvasScaler, Image, Text, Button, InputField, ScrollRect, Slider, RectMask2D, EventSystem and pointer-drag handlers. No IMGUI call draws the desktop window. The UI creates rounded 9-slice sprites at runtime and uses only the game's local Unity UI assemblies. True background blur is not currently present; translucent layers approximate the reference.

## Physical VR menu

The in-world menu lives in `UI/VR/VrMenu.cs`. It constructs a **rounded 3D OLED tablet** with a charcoal body, violet edge trim, clickable rounded physical button meshes, and world-space TextMesh labels. No IMGUI or Canvas is used for the physical wrist panel. Its content rebuilds whenever the registry changes and stays attached to the left tracked controller with smoothed tracking and headset-facing rotation.

- **Left Y:** open / close.
- **Right hand:** move the controller tip onto a raised button to press it, or aim at a button and press the right trigger. The pointer supports both common controller aim axes and shows a local beam and selection dot.
- **HOME / CATS:** show registered feature categories; tap a category to browse.
- **FAV / ON / ALL:** browse saved favorites, currently enabled entries, or the full feature catalog.
- **+ / *:** save or remove a feature from favorites.
- **< / >:** change pages. Each page displays up to 6 features with ON/OFF feedback.
- **RESET:** disable all active Volas features.
- Compatible controllers receive a short haptic pulse on selection; input is debounced to avoid rapid repeats.

F7 can open the physical menu from the PC keyboard, and F6 > Settings > OPEN VR MENU / TEST also opens it. A head-positioned preview is used when tracked left-hand position is unavailable. Native Gorilla Tag ControllerInputPoller input supports SteamVR as a fallback for left Y and right trigger, with Unity XR fallback. PC mouse clicks work for testing when visible in the camera view.

The menu uses only registered Volas features and does not synthesize fake mod entries. Configuration in BepInEx stores the menu's scale and wrist offsets, but the current desktop uGUI settings page exposes only compact sizing; other offset values can be adjusted in the BepInEx configuration file.

## Theme

The PC menu uses graphite-black translucent surfaces, low-contrast gray text, compact category rows and a restrained violet active-tab / action accent. Settings adjusts panel opacity, and Gun Studio edits live pointer configuration. The GunLib visual effects can still use their optional independent rainbow styling; this does not make the desktop layout purple or import Untitled themes.

## Building

The GUI source is part of the same `Volas.dll` as every other mod. `Volas.csproj` includes `UI/**/*.cs`, along with all the other source folders. To compile, pull the latest branch, open `Volas.sln` through **File → Open → Project/Solution**, configure the local PC Gorilla Tag/BepInEx references, and run `build.bat` or Visual Studio Build Solution.

**The new interface has not been compiled or play-tested against the user's local game DLLs in this session.** If the build fails, capture the first untruncated error from the Visual Studio Build Output window.

## Features and limitations

The project currently registers **72** feature entries: 55 original entries plus 17 local movement, visual, fun and safety entries. GunLib uses the selected controller **grip to aim and trigger to fire**. Solo/MODDED room checks are applied to local locomotion changes. Lobby-wide effects and moderation evasions are intentionally absent. Local build against UnityEngine.UI.dll and UnityEngine.UIModule.dll, plus in-game verification, are required before calling this a working binary.

## Desktop effects

F6 retains the existing sidebar and compact two-column layout. Settings → UI Effects / Audio controls acrylic, bloom, motion and sound separately. Acrylic captures a local in-memory desktop scene snapshot before the window opens, downsamples it on the GPU, and crops it to the window. The snapshot is refreshed on reopening and discarded on closing. When capture is unsupported, the menu uses the frosted grain background instead. Bloom is a procedural UI-only soft glow, not HDR scene bloom. Hover/click/open/close tones are synthesized locally; no sound files, global audio changes or Photon voice changes. All controls are saved in the existing BepInEx configuration.

## GUI performance pass

The current desktop menu uses in-place feature row and sidebar state updates, debounced search, conditional canvas property writes, event-driven button feedback and GPU-only acrylic snapshot capture. If GPU screenshot capture is unavailable, the frosted tint remains without synchronous CPU pixel readback. Favorite/Enabled filters rebuild only when membership changes. Real frame times require testing in the installed game.

## Charm collection and marker preview

Nine procedural wrist charms are available under Charms and in the physical menu categories. Charm Studio controls size, color, hand, rainbow and animation speed. Local-only cosmetics do not modify physics, network state or inventory. The marker preview is a stationary test shape in solo or permitted modded rooms. Remote markers remain depth-tested and restricted to recognized modded rooms. The status footer reports rig discovery. Runtime verification is still required.

## Volas 1.5 desktop workspace

Procedural cached line icons replace font-dependent navigation glyphs. Home provides shortcuts to active feature categories and tools. Owner is a local installation diagnostics workspace (not an authenticated service or remote administration console). It displays frame timing, registered and active features, marker status, latest feature fault and a catalog-handler check. The check validates registration only and does not prove game compatibility. The marker pool survives scene transitions and explicitly resolves a compatible runtime label font; actual rendering remains to be tested in permitted modded rooms. Existing room restrictions remain unchanged.

## Volas 1.6 licensing

Fresh game starts require a valid license. The client stores its session token in memory, sends license credentials over HTTPS to the dedicated Volas Neon Function, and checks validity every 45 seconds; the session expires after 12 hours, or fails closed after 135 seconds without verification. F6 opens the login gate; F8 opens session controls, including owner-only announcement publishing and aggregate license/opt-in presence counts. The server validates roles from the database on each authenticated request. No private room codes, player identities or automatic joining are collected. Presence is off by default and can be disabled at any time. Client-side DLL modifications can bypass a local UI gate; cloud actions remain server-authorized. No secrets belong in Git. Local compilation and live endpoint testing are still required.

## Volas 1.7 community effects

The Community page offers optional Aurora, Pulse and Orbit cosmetics. Licensed owners publish a five-minute schedule or stop it from the Owner session panel or Community page; authorization and expiry are checked by the Neon backend. Participating clients fetch the schedule on heartbeat and render a local wrist effect only in solo or permitted modded rooms. This does not modify Gorilla Tag's servers, send game RPCs, reveal other players' private room codes, or affect their physics. The updated client also blocks menu hotkeys while the license panel is open and clears the session locally immediately on sign-out. Existing licenses remain valid. The native public API needs an edge rate-limit layer and load testing before commercial release.

## Volas 1.8 performance and reliability

Desktop feature browsing is paged at 16 rows, including search and favorites. It renders only the visible page, preserves in-place switch updates, and shows feature descriptions and scope labels. The home dashboard shows live FPS and active feature counts. Account controls are available directly from the sidebar; the feature registry validates registrations, indexes IDs, and checks the authenticated session at activation. License input is validated locally and transient network failures use bounded retry backoff. Existing community, owner and room restrictions are preserved. No new database deployment is needed for this client-only release. Runtime compatibility is not established by these source changes.

## 1.9.1 search and availability

Modern and legacy menus share a ranked, multi-keyword catalog search. Search spans feature names, IDs, categories, descriptions and common aliases, with direct navigation to a matching page. The default Available Now filter hides unavailable room-dependent features while retaining local cosmetics and telemetry. All Features exposes the full catalog without changing any activation permissions. The VR Local tab includes all unrestricted features. The old unused desktop scroll fields have been removed. Existing game-room and license restrictions remain intact.

## 1.9.2 native search input

The modern search bar uses an IMGUI text field over the existing desktop header, independent of the game's uGUI input module. Click the bar or use Ctrl+F to focus it; Enter/Escape leaves the field, and the clear button resets the query. The field supports native keyboard editing and clipboard shortcuts. End does not reset features while modern search is focused. Both desktop catalogs now show all entries by default; the available-only filter remains optional. Lobby-specific activation restrictions are unchanged. Test typing, paste, page selection and clearing in the installed game.
