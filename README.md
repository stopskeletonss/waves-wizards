# Waves & Wizards

A first-person, wave-based co-op shooter built in **Unity 6000.3.9f1 (URP)**. Players use magic staffs to fight waves of enemies that scale exponentially each round.

Multiplayer runs on **Unity Netcode for GameObjects (NGO) 2.11.0** over the **Unity Transport (UTP)** package.



## Table of Contents

1. [Quick Start Guide](#quick-start-guide)
2. [Project Structure](#project-structure)
3. [Networking](#networking)
4. [Scripts](#script)
5. [Asset Inventory](#asset-inventory)
6. [Known Issues / WIP](#known-issues--WIP)



## Quick Start Guide

1. Open the project in **Unity 6000.3.9f1**. Anything older will not open the scenes.
2. Open [Assets/Scenes/NetworkTest.unity](Assets/Scenes/NetworkTest.unity). This is the only scene with a `NetworkManager` in it, and therefore the only scene multiplayer currently works in.
3. Press **Play**.
4. Click **Start Host** in the top-left on-screen menu (drawn by `NetworkStartButtons`). This signs in anonymously to Unity Gaming Services, creates a **Relay** allocation, and displays a **join code**. Press `C` to copy the code to the clipboard.
5. On another machine (or a Multiplayer Play Mode virtual player), enter the join code in the text field and click **Join Game**. Up to 3 players can join the host (4 total).

Because connections go through Unity Relay, players do not need to be on the same network and no ports need to be opened. The project must be linked to a Unity Cloud project (**Edit → Project Settings → Services**) for Relay sign-in to work.



### Controls

| Input | Action |
|---|---|
| `WASD` | Move |
| `Mouse` | Look |
| `Left Shift` | Sprint |
| `Space` | Jump |
| `Left Mouse` | Fire equipped staff |
| `R` | Refill staff ammo |
| `E` | Interact with shop |
| `Scroll Wheel / 1 / 2` | Switch between staffs |
| `C` | Copy the Relay join code (host) |
| `Esc` | Back out of a main menu sub-page |



### Scenes

- `Assets/Scenes/TheTavern.unity`: The original tavern scene that will be used for the tutorial.
- `Assets/Scenes/Ian'sTavern.unity`: The updated tavern scene that will be the first playable level.
- `Assets/Scenes/MainMenuScene.unity`: The main menu, set inside of the original tavern.
- `Assets/Scenes/Hub.unity`: Lobby hub to invite other players before starting a level. It will have various sections, including target practice, an area to view achievements, and more. 
- `Assets/Scenes/NetworkTest.unity`: Currently the only scene with a working network manager. Now contains Ian's tavern, the health UI overlay, and the character assignment manager.
- `Assets/Scenes/LocalTesting/LocalTavern.unity`: Non-networked copy of the tavern for testing single-player features (e.g. the health UI) without starting a host.
- `Assets/Scenes/KevinVFXStuff.unity`: Sandbox scene for VFX/projectile work.
- `Assets/_Recovery/`: Unity auto-recovery scenes. Not used; safe to delete.

Scenes in the Build Settings:

- `Assets/Scenes/TheTavern.unity` (disabled)
- `Assets/Scenes/NetworkTest.unity` (**enabled**, the only scene in the build)

Only enabled scenes in the Build Settings will be bundled when the game is exported. The first enabled scene is the one that loads when the game starts.

---


## Project Structure

```
Assets/
├── Decimate/Grid Master/     Third-party greybox/blockout kit
├── Extras/TutorialInfo/      Leftover Unity template assets
├── Models/
│   ├── Characters/           Hex
│   ├── Level_1/              Tavern, Blacksmith, Carnival, MagicShop, Stable, FishingHut
│   ├── Level_2/              Cemetery (tombstones, coffins, candles, braziers)
│   ├── Level_3/              Castle
│   ├── ModularAssets/        Reusable models: walls, windows, doors, towers, fences, vendor stalls
│   ├── OutDoors/             Fountain set (duplicated under ModularAssets/OutDoors)
│   ├── Projectiles/          Staff/enemy projectiles
│   ├── Staffs/               staff models, most with a base + upgraded variant
│   ├── TavernLevel/          Clock tower + dock models
│   └── The_Hub/              Training dummies and practice targets
├── Prefabs/
│   ├── Enemies/
│   ├── Player/
│   ├── Projectiles/
│   ├── Staffs/
│   └── VFX/
├── Samples/                  Imported VFX Graph sample content (Lightning, Smoke, Sparks)
├── Scenes/                   Hub, Ian'sTavern KevinVFXStuff, MainMenuScene, NetworkTest, TheTavern, LocalTesting/LocalTavern
├── Scripts/                  All gameplay code (MainMenu/, Player/, EnemyScripts/, Rounds/)
├── Skyboxes/TavernSky/       Night sky skybox material + texture
├── TempForPitch/             Placeholder materials/textures used for the pitch build
├── _Recovery/                Unity auto-recovery scenes (unused)
└── DefaultNetworkPrefabs.asset   ← NGO network prefab registry
```



### Packages

| Package | Version | Used for |
|---|---|---|
| `com.unity.netcode.gameobjects` | 2.11.0 | All multiplayer |
| `com.unity.multiplayer.center` | 1.0.1 | Multiplayer setup tooling |
| `com.unity.services.multiplayer` | 2.4.0 | Unity Gaming Services auth + Relay (join codes) |
| `com.unity.multiplayer.playmode` | 2.0.2 | Multiplayer Play Mode (virtual players in the editor) |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering |
| `com.unity.visualeffectgraph` | 17.3.0 | Projectile and portal VFX |
| `com.unity.ai.navigation` | 2.0.10 | Enemy NavMesh pathfinding |
| `com.unity.inputsystem` | 1.18.0 | Installed, but gameplay currently uses legacy `Input.GetAxis` |
| `com.unity.probuilder` | 6.0.8 | Level greyboxing |

---



## Networking

We use **Netcode for GameObjects (NGO)** with a host-authoritative topology. One machine runs as **host** (server + client in the same process). The host owns all enemy spawning, enemy health, and round progression. Each connected client owns exactly one `Player1` instance and drives its own movement locally. Firing and reloading are requested from the server via `ServerRpc`s. The server spawns the networked projectile, resolves hits, and changes enemy and player HP, which replicate to everyone through `NetworkVariable`s.



### Current Network Configuration

Set on the `NetworkManager` GameObject in [Assets/Scenes/NetworkTest.unity](Assets/Scenes/NetworkTest.unity):

| Setting | Value | Notes |
|---|---|---|
| Transport | `UnityTransport` | Relay server data is set at runtime by `NetworkStartButtons` (DTLS) |
| Address / Port | `127.0.0.1` : `7777` | Only used for direct (non-Relay) connections, e.g. the NetworkManager inspector buttons |
| Max joining players | 3 | `NetworkStartButtons.maxJoiningPlayers` (host + 3 = 4 characters) |
| Tick Rate | 30 | Network ticks per second |
| Player Prefab | `Prefabs/Player/Player1.prefab` | Auto-spawned per connected client |
| `AutoSpawnPlayerPrefabClientSide` | true | You do not need to spawn players manually |
| `EnableSceneManagement` | true | Server drives scene loads for clients |
| Connection Approval | off | Anyone with the join code can join |
| Network Topology | ClientServer | Not distributed authority |



### Network Prefab Registry

`Assets/DefaultNetworkPrefabs.asset` is the list NGO uses to match prefab hashes between server and client. It currently contains four entries:

1. `Prefabs/Player/Player1.prefab`
2. `Prefabs/Enemies/EnemyTest.prefab`
3. `Prefabs/Enemies/EnemyTest 2.prefab` (Sentinel enemy; this is the one `RoundSystem` spawns in `NetworkTest`)
4. `Prefabs/Projectiles/TempProjectile1.prefab`

> **Rule:** any prefab you intend to `Spawn()` over the network must have a `NetworkObject` component **and** be registered in this list. `ForceSamePrefabs` is enabled, so a prefab present on the host but missing on a client will cause the connection to error out rather than silently desync.



### Local vs. Server Ownership

| Thing | Authority | Mechanism |
|---|---|---|
| Player movement & look | **Client (owner)** | `FirstPersonController` stops immediately if `!IsOwner` is true; an owner-authoritative `NetworkTransform` on the prefab replicates the movement for other players |
| Player animations | **Owner → Server → everyone** | Owner sends walk/sprint/grounded via `ServerRpc`; server writes `NetworkVariable<bool>`s that every peer applies to the Animator |
| Character assignment / model | **Server** | `CharacterAssignmentManager` writes `PlayerStatus.Character`; `PlayerModelAssignment` enables the matching model on every peer |
| Player camera & audio listener | **Client (owner)** | Enabled only when `IsOwner` in `OnNetworkSpawn` |
| Player HP | **Server** | `PlayerStatus.TakeDamage` → `ApplyDamageServerRpc` → `NetworkVariable<float> CurrentHP` |
| Enemy spawning | **Server only** | `RoundSystem.OnNetworkSpawn` returns early unless `IsServer` |
| Enemy AI / targeting | **Server** | `EnemyAITest.Update` only runs on the server; the `NavMeshAgent` is disabled on clients |
| Enemy HP / death | **Server** | `EnemyAITest.ApplyDamage` → `NetworkVariable<float>` → `NetworkObject.Despawn()` |
| Round progression | **Server** | `RoundSystem` counts despawns via `EnemyRoundTracker` |
| Projectiles | **Server** | `StaffFireInput.FireStaffServerRpc` spawns a networked projectile; `ProjectileStats` handles hits and despawning on the server only |
| Ammo / reload | **Server** | `StaffManager.ReloadStaffServerRpc` refills ammo on the server's copy of the staff |
| Staff models / shops / UI | **Local only** | Plain `MonoBehaviour`s; `PlayerHealthUI` reads replicated `PlayerStatus` values |



### Ownership Pattern

Every networked behaviour in this project follows the same pattern. Use it for new code:

```csharp
public class MyThing : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Runs instead of Start() for networked objects.
        // Do per-instance setup here, and branch on IsOwner / IsServer.
        if (!IsServer) return;   // server-only setup
    }

    private void Update()
    {
        if (!IsOwner) return;    // owner-only input
    }

    [ServerRpc]
    public void DoThingServerRpc(int value)
    {
        // Runs on the server, called from a client.
        // Method name MUST end in "ServerRpc".
    }
}
```

Rules:

1. **Use `OnNetworkSpawn()`, not `Start()`**, on any `NetworkBehaviour`. `Start()` can run before the object is spawned on the network, at which point `IsOwner` / `IsServer` are not yet meaningful (which is why we don't want to use it). `FirstPersonController`, `PlayerStatus`, `StaffManager`, `StaffFireInput`, `EnemyAITest`, `CharacterAssignmentManager`, `PlayerModelAssignment`, and `RoundSystem` all do this correctly.
2. **Use `IsOwner` for player-specifc actions and `IsServer` for shared game state.** `IsOwner` means "this is my player." `IsServer` means "I am allowed to change shared game state." A client that runs server logic will either be ignored or cause a desync.
3. **Follow the naming convention for RPCs.** A method marked with `[ServerRpc]` must end in `ServerRpc`, and a method marked with `[ClientRpc]` must end in `ClientRpc`. If a method is renamed and the suffix is lost, the project fails to compile.

By default a `[ServerRpc]` can only be called by the object's **owner**. Use `[ServerRpc(RequireOwnership = false)]` when a non-owner needs to call it, as `PlayerStatus.ApplyDamageServerRpc` (called by enemies) and `EnemyAITest.TakeDamageServerRpc` do.



### Transform Replication

`Player1.prefab` uses Netcode's stock **`NetworkTransform`** with `AuthorityMode: 1` (**owner authoritative**) and `Interpolate` on. The owning client moves its own Rigidbody in `FirstPersonController` and the transform is sent to everyone else, so the server and client no longer fight over the player's position. This replaced the old `ClientNetworkTransform` script, which has been deleted (NGO 2.x exposes the same behaviour through the `AuthorityMode` setting).

The Sentinel enemy (`EnemyTest 2.prefab`) and the projectile (`TempProjectile1.prefab`) also have a `NetworkTransform`, so their movement replicates to clients.



### Adding a New Networked Object

1. Add a **`NetworkObject`** component to the prefab root.
2. If it moves, add **`NetworkTransform`** and set its Authority Mode to **Server** (server drives it) or **Owner** (owning client drives it).
3. Make its scripts extend **`NetworkBehaviour`** instead of `MonoBehaviour`.
4. Register the prefab in **`Assets/DefaultNetworkPrefabs.asset`**.
5. Spawn it **on the server only**: `Instantiate(prefab).GetComponent<NetworkObject>().Spawn();`
6. Despawn it **on the server only**: `GetComponent<NetworkObject>().Despawn();`
7. Test as host **and** as a second connected client.

`RoundSystem.SpawnEnemy()` and `StaffFireInput.FireStaffServerRpc()` are reference implementations of step 5, and `ProjectileStats.DespawnProjectile()` of step 6.



### Replicating State to Clients

Shared state uses `NetworkVariable<T>` with server write permission. Current examples: `PlayerStatus.CurrentHP`, `PlayerStatus.Character`, `EnemyAITest.networkCurrentHP`, and the animation flags in `FirstPersonController`. Round number and points are still local values. When we wire up shared round UI, follow the same pattern:

```csharp
// Server writes, everyone reads.
private NetworkVariable<int> currentRound = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

// React to changes on every peer:
currentRound.OnValueChanged += (oldVal, newVal) => roundText.text = $"Round {newVal}";
```

Remember to unsubscribe from `OnValueChanged` in `OnNetworkDespawn`. `RoundSystem` already exposes `GetCurrentRound()` and `GetEnemiesAlive()` as hooks for this.



### Testing Remote Multiplayer

Remote play uses **Unity Relay** through `NetworkStartButtons` (see the [Quick Start Guide](#quick-start-guide)). The host creates an allocation and shares the join code, and clients enter it to connect. No port forwarding or LAN IP is needed.

For a direct connection without Relay (e.g. on the same LAN), you can still use the NetworkManager inspector buttons: set `ServerListenAddress` to `0.0.0.0` on the host, set `Address` to the host's LAN IP on clients, and open UDP port `7777` in the host firewall.

---



## Script Reference

All gameplay code lives in [Assets/Scripts/](Assets/Scripts/). Scripts marked **networked** extend `NetworkBehaviour`.



### Player

#### [FirstPersonController.cs](Assets/Scripts/Player/FirstPersonController.cs): *networked*
The current, multiplayer-ready first-person controller. Rigidbody-based.

- Runs setup in `OnNetworkSpawn`: Configures the Rigidbody (interpolation on, continuous-dynamic collision, rotation frozen, zero damping), auto-finds the child camera and `AudioListener`, and **enables them only for `IsOwner`** so remote players don't hijack your view or audio.
- `Update` (owner only) reads mouse look into `yaw`/`pitch`, movement into `moveInput`, and timestamps jump presses.
- `FixedUpdate` (owner only) runs ground check → look → movement → gravity/jump.
- `DoGroundCheck` uses a **`SphereCast`** rather than a raycast because a raycast misses on ledges and can start inside geometry. Rejects surfaces steeper than `maxSlopeAngle` (60°).
- Jump feel: **coyote time** (0.1s grace after leaving ground) and a **jump buffer** (0.1s of pre-press forgiveness). Gravity is applied manually, not via Rigidbody gravity.
- Horizontal velocity is smoothed with `acceleration` / `deceleration` (`Vector3.MoveTowards`), so the player no longer slides after releasing input.
- **Head bobbing** on the owner's camera in `LateUpdate`, scaled up while sprinting and eased back to rest when stopped.
- **Networked animation:** finds the active character model's `Animator`, and the owner sends `Walking` / `Sprinting` / `Grounded` to the server only when they change. The server writes them to `NetworkVariable`s that all peers apply. The owner's own model is hidden so it doesn't block the camera.
- Exposes a `PlayerCamera` property (used by `StaffFireInput` for aiming).
- Locks and hides the cursor for the owner, restores it in `OnDisable`.



#### [PlayerStatus.cs](Assets/Scripts/Player/PlayerStatus.cs): *networked*
Networked HP and character container.

- `CurrentHP` (`NetworkVariable<float>`, server-write) is set to `maxHP` (100) by the server in `OnNetworkSpawn`.
- `Character` (`NetworkVariable<CharacterType>`) holds which wizard this player is. On spawn the server asks `CharacterAssignmentManager` to assign one.
- `TakeDamage(float)` forwards to `ApplyDamageServerRpc` (`RequireOwnership = false`, so enemies can call it), which clamps HP at 0.
- `OnCharacterChanged` / `OnHealthChanged` hooks exist (currently debug logs) for future model/portrait reactions.



#### [CharacterAssignmentManager.cs](Assets/Scripts/Player/CharacterAssignmentManager.cs): *networked*
Server-only singleton (`Instance`) that also defines the `CharacterType` enum: `Unassigned`, `Hex`, `Cyrus`, `Snickerdoodle`, `Orion`. Tracks a `clientId → CharacterType` dictionary and gives each connecting client the first unused character in order. Listens to `OnClientConnectedCallback` / `OnClientDisconnectCallback` and frees a character when its player leaves. Logs a warning if a fifth player connects. Players can't pick their own character yet (TODO in code).



#### [PlayerModelAssignment.cs](Assets/Scripts/Player/PlayerModelAssignment.cs): *networked*
On the player prefab. Holds references to the four character models and enables only the one matching `PlayerStatus.Character`, re-applying whenever the value changes so every peer sees the right model.



#### [PlayerHealthUI.cs](Assets/Scripts/Player/PlayerHealthUI.cs)
Scene HUD (plain `MonoBehaviour`). Each frame it finds all `PlayerStatus` components, shows the **local** player's health bar and portrait in the top-left, and fills the bottom-left team list with each teammate's health (one row per character; rows are hidden for unused characters and for the local player).



#### [PlayerController.cs](Assets/Scripts/PlayerController.cs): *legacy, single-player*
The older `CharacterController`-based controller. **Not networked.** Used in `LocalTavern`. Handles movement with acceleration/deceleration smoothing, mouse look, head bobbing, drives `Walking` / `Sprinting` / `Grounded` Animator parameters, has a `K` debug key that damages the attached `PlayerStatus`, and holds the player's health and points, including the TextMeshPro (TMP) HUD text and `AddPoints` / `SpendPoints` / `TakeDamage` API. `StaffShop` depends on this for the points economy, so it can't be deleted until points move onto `PlayerStatus`. Keep in mind that `Player1.prefab` uses `FirstPersonController`, not this. So **the shop's points system does not work on the networked player prefab**.



### Staffs & Combat

#### [StaffManager.cs](Assets/Scripts/StaffManager.cs): *networked*
Two-slot staff inventory mounted on the player. Instantiates `starterStaffPrefab` under `weaponHoldPoint` in `OnNetworkSpawn` (on every peer, so everyone sees the staff). For the owner only, it handles slot switching (`1`, `2`, scroll wheel) and reload (`R` → `ReloadStaffServerRpc`, so ammo is refilled on the server's copy). It no longer handles firing. Exposes `AddStaffToSlot`, `ReplaceActiveStaff`, `RefillAmmoForStaff`, and `GetCurrentStaff`. Fires an `onStaffEquipped` event so other systems can react to a weapon swap. Slot switching works by toggling `SetActive` on the staff GameObjects.



#### [StaffController.cs](Assets/Scripts/StaffController.cs)
Per-staff behaviour: ammo, fire force, projectile prefab, and the TMP mana readout. Its `Fire()` method is no longer called by anything, since firing now goes through `StaffFireInput`.

- At `Start` it **unparents itself from the weapon hold point** and caches that transform, then in `LateUpdate` smoothly follows it with exponential position smoothing and rotational slerp. This produces the weapon-sway/lag feel instead of the staff being rigidly welded to the camera.
- `Fire()` computes aim by raycasting from the **centre of the screen** (`Camera.main`), so the projectile converges on the crosshair rather than firing along the staff's laggy forward vector. Falls back to hold-point forward, then transform forward, if no camera is found.
- Spawns the projectile and sets `Rigidbody.linearVelocity` directly, decrements ammo, updates UI. `RefillAmmo()` restores ammo to the value it started with.



#### [StaffFireInput.cs](Assets/Scripts/StaffFireInput.cs): *networked*
The single fire path. Subscribes to `StaffManager.onStaffEquipped` and locates a child named `ProjectileOrigin` on the equipped staff. On `LMB` (owner only) it raycasts from the centre of `playerCamera` to find the aim point, then calls `FireStaffServerRpc(position, direction)`. The server re-checks the staff and ammo, instantiates the projectile, calls `NetworkObject.Spawn()`, and sets its velocity (`fireForce` along the aim direction plus `upwardForce`).



#### [ProjectileStats.cs](Assets/Scripts/ProjectileStats.cs)
On the (networked) projectile prefab. Collision and lifetime run **on the server only**. On hitting an object tagged `Enemy` it damages the `EnemyAITest` and despawns itself. It also despawns after `lifetime` (10s) if it never hits anything. Despawning on the server removes it from every client. Also declares `pointsOnHit` / `pointsOnKill`, which isn't fully implemented yet.



#### [StaffStore.cs](Assets/Scripts/StaffStore.cs): Class name is `StaffShop`
In-world shop. Note the **file name and class name differ**--the component is `StaffShop`.

- Requires a child GameObject named `BuyBoundary` carrying a trigger collider.
- Polls every frame for a `Player` tagged object inside `triggerCollider.bounds` (bounds containment, not actual trigger callbacks).
- On entry, finds a TMP object tagged `BuyTMP` in the player's hierarchy and shows either `Buy {staff}: {price}` or, if the player already owns the staff, `Conjure Mana: {cost}`.
- `E` spends points via `PlayerController.SpendPoints` and either adds the staff to slot 1 / replaces the active staff, or refills that staff's ammo.



### Enemies

#### [EnemyAITest.cs](Assets/Scripts/EnemyScripts/EnemyAITest.cs): *networked*
Server-driven enemy AI that supports multiple players. All logic runs on the server; clients disable their `NavMeshAgent` and just display the replicated transform and health.

- Waits 2.5s after spawning, then every `targetRefInterval` (0.25s) retargets to the **closest living player** (ignoring dead/inactive players). Stops if there's no valid target.
- Outside `AttackDistance` it chases with `SetDestination`. Inside, it stops and starts an `AttackTarget` coroutine: triggers the `Attack` animation, waits `AttackDelay`, re-checks that the target is alive and still in range, then calls `PlayerStatus.TakeDamage(AttackDamage)`. `attackCooldown` (1.5s) gates the next swing.
- HP is a server-write `NetworkVariable<float>`, and every peer updates its health bar in `OnValueChanged`. `TakeDamageServerRpc` (`RequireOwnership = false`) and `ApplyDamage` clamp HP at 0 and despawn the enemy when it dies.



#### [TestHealthBar.cs](Assets/Scripts/EnemyScripts/TestHealthBar.cs)
Sets a UI `Image.fillAmount` from `currentHP / maxHP`. Called by `EnemyAITest`. Does not automatically rotate to face the camera, which is something that should be discussed, given there are multiple player cameras in multiplayer.



#### [EnemySpawnerTest.cs](Assets/Scripts/EnemyScripts/EnemySpawnerTest.cs)
The original, **non-networked** spawner. Plain `Instantiate` up to `MaxEnemies`, with `spawnTimer` counted in frames rather than seconds. Superseded by `RoundSystem`, so it's safe to delete once nothing references the prefab.



### Rounds

#### [RoundSystem.cs](Assets/Scripts/Rounds/RoundSystem.cs): *networked*
The wave director, and currently the cleanest networked code in the project. **Server only**: `OnNetworkSpawn` returns immediately on clients.

- Round *n* spawns `round1EnemyCount * exponentialMultiplier^(n-1)` enemies, rounded to an int and floored at 1. Defaults: 5 enemies on round 1, ×1.29 per round.
- `SpawnLoop` is a coroutine that spawns one enemy every `spawnInterval` seconds, yielding without spawning while `enemiesAlive >= maxAliveEnemies` (default 30) so the server never floods itself.
- `SpawnEnemy` instantiates at a random `spawnPoints` transform, ensures an `EnemyRoundTracker` is attached (adding one if the prefab lacks it), calls `NetworkObject.Spawn()`, then `Init`s the tracker with a back-reference.
- `NotifyEnemyDespawned` decrements the alive count. When everything for the round has spawned and nothing is alive, it stops the coroutine and schedules `StartNextRound` after `nextRoundDelay`.
- `GetCurrentRound()` / `GetEnemiesAlive()` exist as UI hooks.



#### [EnemyRoundTracker.cs](Assets/Scripts/Rounds/EnemyRoundTracker.cs): *networked*
Goes on the enemy prefab. Holds a reference to the owning `RoundSystem` and, in `OnNetworkDespawn` (server only), calls `NotifyEnemyDespawned`. This is how round completion is detected: by despawn, which means an enemy despawned for *any* reason counts toward round completion.



### Environment & Presentation

#### [CandleBounce.cs](Assets/Scripts/CandleBounce.cs)
Candle/torch flicker. Collects all child `Light`s and `Renderer`s, drives intensity from a rolling average of random values (queue length = `Smoothing`) so the flicker is organic rather than strobing, lerps emissive material colour to match, and adds slight vertical jitter to light positions. Also runs a `FadeInLightRange` coroutine that ramps light range from 0 to `targetRange` after a delay, so lights don't pop in on scene load.



#### [ClockTowerHover.cs](Assets/Scripts/ClockTowerHover.cs)
Sine-wave vertical bob for floating props. `RandomOffset` desynchronises multiple instances so a group of floating objects doesn't move together.



### Main Menu

Scripts in [Assets/Scripts/MainMenu/](Assets/Scripts/MainMenu/) for the book-style menu in `MainMenuScene`.

#### [MainMenuController.cs](Assets/Scripts/MainMenu/MainMenuController.cs)
Drives the menu. Moves/rotates the camera between anchors (Play, Tutorial, Credits, Settings, Quit, Main) with idle sway, slides the menu panel on screen after `introDelay`, opens the Settings/Credits/Quit pages, plays sub-menu sounds, animates the book, and handles the lobby/joining page. `Esc` returns from a sub-menu. Replaces the old `MainMenuCamera.cs`.



#### [MainMenuButtonEffect.cs](Assets/Scripts/MainMenu/MainMenuButtonEffect.cs)
Hover effect for menu buttons: scales up to `hoverScale` and plays `hoverSound` on pointer enter, then scales back on exit.



#### [MenuMusicFade.cs](Assets/Scripts/MainMenu/MenuMusicFade.cs)
Starts the menu music after `startDelay` and fades it up to `targetVolume` over `fadeTime`.



#### [ConstantSpin.cs](Assets/Scripts/MainMenu/ConstantSpin.cs)
Rotates an object at `rotationSpeed` degrees per second.



### Networking UI

#### [NetworkStartButtons.cs](Assets/Scripts/NetworkStartButtons.cs)
Temporary `OnGUI` connection menu. Initializes Unity Services and signs in anonymously, then either creates a Relay allocation and starts the host (showing the join code; `C` copies it) or joins an existing allocation from an entered code. Shows connection status in the corner.

---



## Asset Inventory

### Scenes

| Scene | Purpose | In Build |
|---|---|---|
| `Scenes/NetworkTest.unity` | **The multiplayer scene.** Contains the only `NetworkManager`, `NetworkStartButtons`, `CharacterAssignmentManager`, a `RoundSystem`, Ian's tavern, and the health HUD | ✅ |
| `Scenes/TheTavern.unity` | Tutorial level | ❌ (disabled) |
| `Scenes/LocalTesting/LocalTavern.unity` | Non-networked tavern test scene | ❌ |
| `Scenes/KevinVFXStuff.unity` | VFX sandbox | ❌ |
| `Scenes/Hub.unity` | Hub area for creating a lobby before starting a level | ❌ |
| `Scenes/Ian'sTavern.unity` | Tavern level (level 1) | ❌ |
| `Scenes/MainMenuScene.unity` | Tavern main menu | ❌ |



### Gameplay Prefabs

| Prefab | Networked | Notes |
|---|---|---|
| `Prefabs/Player/Player1.prefab` | ✅ | `NetworkObject` + owner-authoritative `NetworkTransform` + `FirstPersonController` + `PlayerStatus` + `PlayerModelAssignment` + `StaffManager` + `StaffFireInput`. Contains the four character models (Hex, Cyrus, Snickerdoodle, Orion). Children include `Camera`, `StaffHoldPoint`, `StaffFIrePoint` (note the typo in the object name). Uses the `NoFriction` physic material |
| `Prefabs/Enemies/EnemyTest 2.prefab` | ✅ | **Sentinel** enemy spawned by `RoundSystem` in `NetworkTest`. `NetworkObject` + `NetworkTransform` + `EnemyAITest` + `NavMeshAgent` + `Animator` + health bar |
| `Prefabs/Enemies/EnemyTest.prefab` | ✅ | Original skeleton test enemy. **No `NetworkTransform`**, so its movement does not replicate |
| `Prefabs/Enemies/EnemySpawnerTest.prefab` | ❌ | Legacy non-networked spawner |
| `Prefabs/Projectiles/TempProjectile1.prefab` | ✅ | `NetworkObject` + `NetworkTransform` + Rigidbody + `ProjectileStats`. Spawned by the server |
| `Prefabs/Projectiles/Snowflake.prefab` | ❌ | Ice projectile visual |
| `Prefabs/VFX/Basic_Projectile.prefab` | ❌ | VFX-driven projectile |



### Staff Prefabs

Base: `BaseStaff`, `DefaultStaff/`, `ElectricStaff`, `GravityStaff`, `HealthStaff`, `IceStaff`, `LightStaff/`, `NecromancerStaff`, `SandStaff`, `ShatteredStaff`, `TimeStaff`, `UndeadStaff`, `Temp2`.

Upgraded: `UpgradedDarkStaff`, `UpgradedElectricStaff`, `UpgradedGravityStaff`, `UpgradedIceStaff`, `UpgradedSandStaff`, `UpgradedShatteredStaff`, `UpgradedStoneStaff`, `UpgradedUndeadStaff`.

These are all in `Prefabs/Staffs/`. The staffs themselves aren't networked: the player's `StaffManager` spawns them locally on every peer, and their projectiles are spawned by the server.



### Staff Models

Base staff, Dark, Default (wooden), Electric, Gravity, Health, Light, Necromancer, Sand, Shattered, Stone, Time, Undead. Most have a **base** and an **upgraded** variant FBX, which lines up with the shop's buy/upgrade flow.



### VFX

| Asset | Notes |
|---|---|
| `Prefabs/VFX/Dark_Projectile.vfx` | + `Dark_Projectile_Shader.shadergraph` |
| `Prefabs/VFX/Ice_Projectile.vfx` | + `Ice.shadergraph`, `Ice_Projectile_Shader.shadergraph`, `Icicle` mesh/textures, `Snowflake.png` |
| `Prefabs/VFX/Lightning_Projectile.vfx` | Lightning staff projectile |
| `Prefabs/VFX/Hit_Impact_Basic.vfx` | Generic impact burst |
| `Prefabs/VFX/Portal.vfx` | + `Portal.shadergraph`, `Tavern_PortalHDRI.shadergraph`, `PortalMask.png`, `PortalNoise.png`, `PortalPlane.fbx` |
| `Samples/.../Lightning|Smoke|Sparks.prefab` | Unity VFX Graph sample content |



### Environment Models by Set

- **Level 1 — Tavern:** barrels, bottles (7 + rack + broken variants), boxes/crates, bucket, chair, clock, clock tower, counter pieces, fireplace, harp, stairs, mugs, shelving, piano, portrait, round table, stool, table, "Tipsy Spell Caster" sign, wooden chandelier, wood planks, beer dispenser, ale barrel, bag
- **Level 1 — Blacksmith:** bellow, furnace, sign
- **Level 1 — Carnival:** arrow sign, balloon stand, bowling game + rack, carousel, ferris wheel, firework barrel, fortune booth, jester tent, shooting range, simple tent, strength game, ticket stand
- **Level 1 — MagicShop:** 4 books, floating ornament, hourglass, magic cage, sign, orb, 7 potions, stone planter, open/closed scrolls
- **Level 1 — Stable:** drinking trough, horseshoe, sign
- **Level 2 — Cemetery:** brazier, candle holders (plate, candelabra, single, wall), cemetery gate, casket, coffin, gothic chandelier, stone grave, 7 tombstones, torch holder
- **Level 3 — Castle:** guillotine
- **Modular kit:** town gate, bridge, wall pieces (4 + big wall + cross/triangle patterns), flag, wooden arc, clock tower, 3 door types, furniture (bookshelf, desk, cabinet), 5 towers, 4 vases, 5 window types, roof tile, stone arch, water wheel, 10 vendor-shop pieces, and an outdoor set (bell, benches, bulletin board, fences & gates, fountains, garbage bin, ladder, lanterns, sign hangers, lamps, planter, wagon, water tower, well, wheelbarrows)
- **The Hub:** practice target, 2 training dummies
- **Characters:** Hex, Cyrus, Snickerdoodle, and Orion models with animations, plus character portraits used by the health HUD

Texture convention across the models: each model ships alongside `*_Color.png`, `*_Normal.png`, `*_Roughness.png`, and sometimes `*_Emission.png`.

---



## Known Issues / WIP

1. **Two player controllers, split responsibilities.** `PlayerController` (legacy, non-networked) owns points/HUD and is what `StaffShop` talks to. `FirstPersonController` (networked) is what's actually on the prefab. Points need to migrate onto `PlayerStatus` for the shop to work in multiplayer.
2. **`StaffShop` isn't multiplayer-ready.** It polls every frame using `FindGameObjectWithTag` plus bounds containment rather than `OnTriggerEnter`/`Exit`, assumes a single player tagged `Player`, and spends points through `PlayerController`.
3. **Round number and points aren't replicated.** Player HP, enemy HP, characters, and animations now use `NetworkVariable`s, but `RoundSystem`'s round count and the points economy are still machine-local.
4. **Staff switching isn't replicated.** Each peer spawns the starter staff locally, but slot switches and shop purchases only happen on the owner, so other players won't see a staff change. The server's ammo count and the owner's mana UI can also drift apart.
5. **Characters are auto-assigned.** `CharacterAssignmentManager` hands out characters in order. There's no character select yet, and a fifth player gets no character.
6. **Connection UI is a temporary `OnGUI` overlay.** It should move into the main menu's lobby page. Relay also requires the project to be linked to Unity Cloud.
7. **`EnemyTest.prefab` (skeleton) has no `NetworkTransform`.** Only the Sentinel (`EnemyTest 2`) replicates movement.
8. **Stale Build Settings entry.** `Assets/Scenes/NetworkTavern.unity` is listed (disabled) but doesn't exist.
9. **`EnemySpawnerTest` counts in frames, not seconds** (`spawnTimer++` in `Update`), so its spawn rate is framerate-dependent. Superseded by `RoundSystem`.
10. **`StaffController.Fire()` is dead code** now that `StaffFireInput` handles firing.
11. **Naming inconsistencies to be aware of:** `StaffStore.cs` declares class `StaffShop`. The player prefab child is named `StaffFIrePoint`.
12. **Leftover folders:** `Extras/TutorialInfo/` (Unity template) and `_Recovery/` (auto-recovery scenes) can be deleted.
13. **Input System package is installed but unused**. All gameplay reads legacy `Input.GetAxis` / `Input.GetKey`. Migrating would also make controller support (which `FirstPersonController` was written with in mind) straightforward.
