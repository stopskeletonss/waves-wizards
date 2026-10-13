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
4. Select the **NetworkManager** object in the Hierarchy on the left-hand side of the screen in the **DontDestroyOnLoad** dropdown. In the Inspector, Netcode creates **Start Host / Start Server / Start Client** buttons during play mode. Click **Start Host**.
5. To test with a second player, either build a standalone player and run it alongside the editor, or use Unity's Multiplayer Play Mode (virtual players). Clients connect to `127.0.0.1:7777` by default.

There is no in-game connection UI yet. Hosting/joining is done through the NetworkManager inspector buttons.



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



### Scenes

- `Assets/Scenes/TheTavern.unity`: The original tavern scene that will be used for the tutorial.
- `Assets/Scenes/Ian'sTavern.unity`: The updated tavern scene that will be the first playable level.
- `Assets/Scenes/MainMenuScene.unity`: The main menu, set inside of the original tavern.
- `Assets/Scenes/Hub.unity`: Lobby hub to invite other players before starting a level. It will have various sections, including target practice, an area to view achievements, and more. 
- `Assets/Scenes/NetworkTest.unity`: Currently the only scene with a working network manager. Used for testing/debugging.

Scenes in the Build Settings:

- `Assets/Scenes/TheTavern.unity` (index 0)
- `Assets/Scenes/NetworkTest.unity` (index 1)

Only scenes in the Build Settings will be bundled when the game is exported. The scene at index 0 will be the first scene to load when the game starts.

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
├── Networking Stuff/         
├── Prefabs/
│   ├── Enemies/
│   ├── Player/
│   ├── Projectiles/
│   ├── Staffs/
│   └── VFX/
├── Samples/                  Imported VFX Graph sample content (Lightning, Smoke, Sparks)
├── Scenes/                   Hub, Ian'sTavern, MainMenuScene, NetworkTest, TheTavern
├── Scripts/                  All gameplay code
├── Skyboxes/TavernSky/       Night sky skybox material + texture
├── TempForPitch/             Placeholder materials/textures used for the pitch build
├── DefaultNetworkPrefabs.asset   ← NGO network prefab registry
└── FinishedAttack.cs         Animator StateMachineBehaviour
```



### Packages

| Package | Version | Used for |
|---|---|---|
| `com.unity.netcode.gameobjects` | 2.11.0 | All multiplayer |
| `com.unity.multiplayer.center` | 1.0.1 | Multiplayer setup tooling |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering |
| `com.unity.visualeffectgraph` | 17.3.0 | Projectile and portal VFX |
| `com.unity.ai.navigation` | 2.0.10 | Enemy NavMesh pathfinding |
| `com.unity.inputsystem` | 1.18.0 | Installed, but gameplay currently uses legacy `Input.GetAxis` |
| `com.unity.probuilder` | 6.0.8 | Level greyboxing |

---



## Networking

We use **Netcode for GameObjects (NGO)** with a host-authoritative topology. One machine runs as **host** (server + client in the same process). The host owns all enemy spawning, enemy health, and round progression. Each connected client owns exactly one `Player1` instance and drives its own movement locally. Damage from a projectile is reported to the server via a `ServerRpc`, and the server mutates enemy HP and despawns the enemy when it dies.



### Current Network Configuration

Set on the `NetworkManager` GameObject in [Assets/Scenes/NetworkTest.unity](Assets/Scenes/NetworkTest.unity):

| Setting | Value | Notes |
|---|---|---|
| Transport | `UnityTransport` (UDP) | `m_ProtocolType: 0` = Unity Transport, not Relay |
| Address / Port | `127.0.0.1` : `7777` | Localhost only — change for LAN/online testing |
| Server listen address | `127.0.0.1` | **Must be changed to `0.0.0.0` to accept remote clients** |
| Tick Rate | 30 | Network ticks per second |
| Player Prefab | `Prefabs/Player/Player1.prefab` | Auto-spawned per connected client |
| `AutoSpawnPlayerPrefabClientSide` | true | You do not need to spawn players manually |
| `EnableSceneManagement` | true | Server drives scene loads for clients |
| Connection Approval | off | Anyone who can reach the port joins |
| Network Topology | ClientServer | Not distributed authority |



### Network Prefab Registry

`Assets/DefaultNetworkPrefabs.asset` is the list NGO uses to match prefab hashes between server and client. It currently contains exactly two entries:

1. `Prefabs/Player/Player1.prefab`
2. `Prefabs/Enemies/EnemyTest.prefab`

> **Rule:** any prefab you intend to `Spawn()` over the network must have a `NetworkObject` component **and** be registered in this list. `ForceSamePrefabs` is enabled, so a prefab present on the host but missing on a client will cause the connection to error out rather than silently desync.



### Local vs. Server Ownership

| Thing | Authority | Mechanism |
|---|---|---|
| Player movement & look | **Client (owner)** | `FirstPersonController` stops immediately if `!IsOwner` is true; a `NetworkTransform` on the prefab replicates the movement for other players (so everyone can see each other moving) |
| Player camera & audio listener | **Client (owner)** | Enabled only when `IsOwner` in `OnNetworkSpawn` |
| Player HP | Server-side object, but currently mutated locally | `PlayerStatus.takeDamage` is a plain public method, rather than a Remote Procedure Call (RPC) |
| Enemy spawning | **Server only** | `RoundSystem.OnNetworkSpawn` returns early unless `IsServer` |
| Enemy HP / death | **Server** | `EnemyAITest.TakeDamageServerRpc` → `NetworkObject.Despawn()` |
| Round progression | **Server** | `RoundSystem` counts despawns via `EnemyRoundTracker` |
| Projectiles | **Local only** | Instantiated with plain `Instantiate`, not networked at all (players can't see each other's projectiles) |
| Staffs / shops / UI | **Local only** | All plain `MonoBehaviour` |



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

1. **Use `OnNetworkSpawn()`, not `Start()`**, on any `NetworkBehaviour`. `Start()` can run before the object is spawned on the network, at which point `IsOwner` / `IsServer` are not yet meaningful (which is why we don't want to use it). `FirstPersonController`, `PlayerStatus`, `EnemyAITest`, and `RoundSystem` all do this correctly.
2. **Use `IsOwner` for player-specifc actions and `IsServer` for shared game state.** `IsOwner` means "this is my player." `IsServer` means "I am allowed to change shared game state." A client that runs server logic will either be ignored or cause a desync.
3. **Follow the naming convention for RPCs.** A method marked with `[ServerRpc]` must end in `ServerRpc`, and a method marked with `[ClientRpc]` must end in `ClientRpc`. If a method is renamed and the suffix is lost, the project fails to compile.

By default a `[ServerRpc]` can only be called by the object's **owner**. `EnemyAITest.TakeDamageServerRpc` is called from `ProjectileStats` on a client that does *not* own the enemy (the server does), so it will be rejected with an ownership warning. See [Known issues](#known-issues--WIP). The fix is `[ServerRpc(RequireOwnership = false)]`.



### Transform Replication

`Player1.prefab` uses Netcode's stock **`NetworkTransform`** with `AuthorityMode: 0` (**server authoritative**), `Interpolate` on, and full position/rotation/scale sync. This means the server is responsible for controlling and
synchronizing the players' position and rotation. 

There is also a **`ClientNetworkTransform`** in [Assets/Networking Stuff/ClientNetworkTransform.cs](Assets/Networking%20Stuff/ClientNetworkTransform.cs):

```csharp
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
```

This changes the `NetworkTransform` so the **owning client** is responsible for its own transform and sends those changes to the other clients. This is a better approach for the first-person controller because `FirstPersonController` moves the player's Rigidbody locally on the owning client. However, `ClientNetworkTransform` **is not currently attached to `Player1.prefab`**, which still uses the server-authoritative `NetworkTransform`. Since `FirstPersonController` moves the Rigidbody locally on the owner, a server-authoritative transform will fight that movement and produce rubber-banding for non-host clients. Swapping the `NetworkTransform` for `ClientNetworkTransform` on `Player1.prefab` is the best fix available right now.



### Adding a New Networked Object

1. Add a **`NetworkObject`** component to the prefab root.
2. If it moves, add **`NetworkTransform`** (server drives it) or **`ClientNetworkTransform`** (owner drives it).
3. Make its scripts extend **`NetworkBehaviour`** instead of `MonoBehaviour`.
4. Register the prefab in **`Assets/DefaultNetworkPrefabs.asset`**.
5. Spawn it **on the server only**: `Instantiate(prefab).GetComponent<NetworkObject>().Spawn();`
6. Despawn it **on the server only**: `GetComponent<NetworkObject>().Despawn();`
7. Test as host **and** as a second connected client.

`RoundSystem.SpawnEnemy()` is the reference implementation of steps 5 and 6.



### Replicating State to Clients

Nothing in the project currently uses `NetworkVariable<T>` or `[ClientRpc]`. Enemy HP, player HP, round number, and points are all local values. That's why health bars and round counters will not agree between players. When we wire up shared UI, the pattern to follow is:

```csharp
// Server writes, everyone reads.
private NetworkVariable<int> currentRound = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

// React to changes on every peer:
currentRound.OnValueChanged += (oldVal, newVal) => roundText.text = $"Round {newVal}";
```

`RoundSystem` already exposes `GetCurrentRound()` and `GetEnemiesAlive()` as hooks for exactly this.



### Testing Remote Multiplayer

1. On the host, change `ServerListenAddress` to `0.0.0.0` so the socket binds all interfaces.
2. On clients, set `Address` to the host's LAN IP.
3. Open UDP port `7777` in the host firewall.
4. For play over the Internet, swap `UnityTransport`'s protocol to **Relay** and add Unity Gaming Services authentication. The `com.unity.multiplayer.center` package walks through this setup.

---



## Script Reference

All gameplay code lives in [Assets/Scripts/](Assets/Scripts/), except `FinishedAttack.cs` which sits at the `Assets/` root. Scripts marked **networked** extend `NetworkBehaviour`.



### Player

#### [FirstPersonController.cs](Assets/Scripts/Player/FirstPersonController.cs): *networked*
The current, multiplayer-ready first-person controller. Rigidbody-based.

- Runs setup in `OnNetworkSpawn`: Configures the Rigidbody (interpolation on, continuous-dynamic collision, rotation frozen, zero damping), auto-finds the child camera and `AudioListener`, and **enables them only for `IsOwner`** so remote players don't hijack your view or audio.
- `Update` (owner only) reads mouse look into `yaw`/`pitch`, movement into `moveInput`, and timestamps jump presses.
- `FixedUpdate` (owner only) runs ground check → look → movement → gravity/jump.
- `DoGroundCheck` uses a **`SphereCast`** rather than a raycast because a raycast misses on ledges and can start inside geometry. Rejects surfaces steeper than `maxSlopeAngle` (60°).
- Jump feel: **coyote time** (0.1s grace after leaving ground) and a **jump buffer** (0.1s of pre-press forgiveness). Gravity is applied manually, not via Rigidbody gravity.
- Locks and hides the cursor for the owner, restores it in `OnDisable`.



#### [PlayerStatus.cs](Assets/Scripts/Player/PlayerStatus.cs): *networked*
HP container. Sets `currentHP = maxHP` in `OnNetworkSpawn` and exposes `takeDamage(float)`. Intended to grow into the home for ammo/buffs/debuffs (whatever affects the player). Note that `takeDamage` is a plain method, so damage applied by an enemy currently only registers on whichever machine ran the enemy AI.



#### [PlayerController.cs](Assets/Scripts/PlayerController.cs): *legacy, single-player*
The older `CharacterController`-based controller. **Not networked.** Handles movement with acceleration/deceleration smoothing, mouse look, head bobbing, and holds the player's health and points, including the TextMeshPro (TMP) HUD text and `AddPoints` / `SpendPoints` / `TakeDamage` API. `StaffShop` depends on this for the points economy, so it can't be deleted until points move onto `PlayerStatus`. Keep in mind that `Player1.prefab` uses `FirstPersonController`, not this. So **the shop's points system does not work on the networked player prefab**.



### Staffs & Combat

#### [StaffManager.cs](Assets/Scripts/StaffManager.cs)
Two-slot staff inventory mounted on the player. Instantiates `starterStaffPrefab` under `weaponHoldPoint` at `Start`, handles slot switching (`1`, `2`, scroll wheel), fire input (`LMB` → `StaffController.Fire()`), reload (`R`), and exposes `AddStaffToSlot`, `ReplaceActiveStaff`, `RefillAmmoForStaff`, and `GetCurrentStaff`. Fires an `onStaffEquipped` event so other systems can react to a weapon swap. Slot switching works by toggling `SetActive` on the staff GameObjects.



#### [StaffController.cs](Assets/Scripts/StaffController.cs)
Per-staff behaviour: ammo, fire force, projectile prefab, and the TMP mana readout.

- At `Start` it **unparents itself from the weapon hold point** and caches that transform, then in `LateUpdate` smoothly follows it with exponential position smoothing and rotational slerp. This produces the weapon-sway/lag feel instead of the staff being rigidly welded to the camera.
- `Fire()` computes aim by raycasting from the **centre of the screen** (`Camera.main`), so the projectile converges on the crosshair rather than firing along the staff's laggy forward vector. Falls back to hold-point forward, then transform forward, if no camera is found.
- Spawns the projectile and sets `Rigidbody.linearVelocity` directly, decrements ammo, updates UI. `RefillAmmo()` restores ammo to the value it started with.



#### [StaffFireInput.cs](Assets/Scripts/StaffFireInput.cs)
A **second, parallel** fire path. Subscribes to `StaffManager.onStaffEquipped`, locates a child named `ProjectileOrigin` on the equipped staff, and on `LMB` instantiates a projectile from that point along `firePoint.forward`. Because `StaffManager.HandleInput` also fires on `LMB`, having both components active on the player means **each click fires twice and consumes two ammo**. We need to pick one path and remove the other (`StaffController.Fire()` is the more capable of the two).



#### [ProjectileStats.cs](Assets/Scripts/ProjectileStats.cs)
On the projectile prefab. On `OnTriggerEnter` with an object tagged `Enemy`, calls `EnemyAITest.TakeDamageServerRpc(damage)` and destroys itself 0.1s later. Also declares `pointsOnHit` / `pointsOnKill`, which isn't fully implemented yet.



#### [StaffStore.cs](Assets/Scripts/StaffStore.cs): Class name is `StaffShop`
In-world shop. Note the **file name and class name differ**--the component is `StaffShop`.

- Requires a child GameObject named `BuyBoundary` carrying a trigger collider.
- Polls every frame for a `Player` tagged object inside `triggerCollider.bounds` (bounds containment, not actual trigger callbacks).
- On entry, finds a TMP object tagged `BuyTMP` in the player's hierarchy and shows either `Buy {staff}: {price}` or, if the player already owns the staff, `Conjure Mana: {cost}`.
- `E` spends points via `PlayerController.SpendPoints` and either adds the staff to slot 1 / replaces the active staff, or refills that staff's ammo.



### Enemies

#### [EnemyAITest.cs](Assets/Scripts/EnemyScripts/EnemyAITest.cs): *networked*
In `OnNetworkSpawn` it grabs the `NavMeshAgent` and `Animator`, initialises HP, and auto-targets the first object tagged `Player`. Each `Update` it measures distance to the target. If the target is outside `AttackDistance`, it sets the destination to the player. If the target is inside `AttackDistance`, it halts and triggers the `Attack` animation, applying `AttackDamage` to the target's `PlayerStatus` exactly once per swing (gated by the `applyDamage` flag). `TakeDamageServerRpc` subtracts damage from the enemy HP, refreshes the health bar, and calls `NetworkObject.Despawn()` at zero HP.



#### [FinishedAttack.cs](Assets/FinishedAttack.cs)
An Animator `StateMachineBehaviour` attached to the enemy's attack state. On `OnStateExit` it sets `EnemyAITest.applyDamage = true`, which is what re-arms the enemy for its next hit. This prevents an enemy from dealing damage every frame it's in range.



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



#### [MainMenuCamera.cs](Assets/Scripts/MainMenuCamera.cs)
Idle camera sway for the main menu. Layered sine/cosine on both position and rotation at different speeds to avoid an obvious loop.

---



## Asset Inventory

### Scenes

| Scene | Purpose | In Build |
|---|---|---|
| `Scenes/NetworkTest.unity` | **The multiplayer scene.** Contains the only `NetworkManager`, a `RoundSystem`, floors, a sphere, lighting | ✅ |
| `Scenes/TheTavern.unity` | Tutorial level | ✅ |
| `Scenes/Hub.unity` | Hub area for creating a lobby before starting a level | ❌ |
| `Scenes/Ian'sTavern.unity` | Tavern level (level 1) | ❌ |
| `Scenes/MainMenuScene.unity` | Tavern main menu | ❌ |



### Gameplay Prefabs

| Prefab | Networked | Notes |
|---|---|---|
| `Prefabs/Player/Player1.prefab` | ✅ | `NetworkObject` + `NetworkTransform` + `FirstPersonController` + `PlayerStatus` + `StaffManager` + `StaffFireInput`. Children: `Camera`, `StaffHoldPoint`, `StaffFIrePoint` (note the typo in the object name) |
| `Prefabs/Enemies/EnemyTest.prefab` | ✅ | `NetworkObject` + `EnemyAITest` + `NavMeshAgent` + `Animator` + world-space `Healthbar Canvas` with `TestHealthBar`. **No `NetworkTransform`**, so enemy movement does not replicate |
| `Prefabs/Enemies/EnemySpawnerTest.prefab` | ❌ | Legacy non-networked spawner |
| `Prefabs/Projectiles/TempProjectile1.prefab` | ❌ | Plain Rigidbody projectile, local only |
| `Prefabs/VFX/Basic_Projectile.prefab` | ❌ | VFX-driven projectile |



### Staff Prefabs

`DarkStaff`, `DefaultStaff`, `ElectricStaff`, `FireStaff`, `GravityStaff`, `IceStaff`, `LightStaff`, `RockStaff`, `ShatteredStaff`, `Temp2`. These are all in `Prefabs/Staffs/`, none are networked. Staffs are currently cosmetic + local-fire only.



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
- **Characters:** `Hex.fbx` (single placeholder character)

Texture convention across the models: each model ships alongside `*_Color.png`, `*_Normal.png`, `*_Roughness.png`, and sometimes `*_Emission.png`.

---



## Known Issues / WIP

1. **`Player1.prefab` has the wrong transform component.** It uses server-authoritative `NetworkTransform` while `FirstPersonController` moves the Rigidbody client-side. `ClientNetworkTransform` exists precisely for this and isn't wired up. Expect rubber-banding on non-host clients until it's swapped.
2. **`TakeDamageServerRpc` will be rejected for non-owners.** `ServerRpc` defaults to `RequireOwnership = true`, but `ProjectileStats` calls it from a client that doesn't own the enemy. It needs `[ServerRpc(RequireOwnership = false)]`.
3. **Projectiles aren't networked.** They're locally instantiated, so remote players never see each other's shots. Making them `NetworkObject`s (or, cheaper, spawning them visually via a `ClientRpc` and keeping hit detection server-side) is a combat milestone.
4. **Double-fire on the player prefab.** Both `StaffManager` and `StaffFireInput` listen for `LMB` and both spawn a projectile. Remove one.
5. **Two player controllers, split responsibilities.** `PlayerController` (legacy, non-networked) owns health/points/HUD and is what `StaffShop` talks to. `FirstPersonController` (networked) is what's actually on the prefab. Points and health need to migrate onto `PlayerStatus` for the shop to work in multiplayer.
6. **`EnemyTest.prefab` has no `NetworkTransform`.** Enemies pathfind on the server, but clients won't see them move.
7. **No shared state replication.** No `NetworkVariable` or `ClientRpc` anywhere. Enemy HP, player HP, round number, and points are all machine-local and will disagree between peers.
8. **`PlayerStatus.takeDamage` isn't an RPC**, so enemy damage doesn't propagate.
9. **No connection UI.** Host/join is inspector-button only.
10. **Localhost-locked transport.** `ServerListenAddress` is `127.0.0.1`. Remote clients cannot connect without changing it.
11. **`StaffShop` polls every frame** using `FindGameObjectWithTag` plus bounds containment rather than using `OnTriggerEnter`/`Exit`. It also assumes a single player tagged `Player`, which breaks in multiplayer.
12. **`EnemySpawnerTest` counts in frames, not seconds** (`spawnTimer++` in `Update`), so its spawn rate is framerate-dependent. Superseded by `RoundSystem`.
13. **Duplicate fountain assets** exist at both `Models/OutDoors/Fountain/` and `Models/ModularAssets/OutDoors/Foutine/` (note the misspelling).
14. **Naming inconsistencies to be aware of:** `StaffStore.cs` declares class `StaffShop`. The player prefab child is named `StaffFIrePoint`. `Assets/FinishedAttack.cs` sits outside `Scripts/`. The folder `Networking Stuff/` contains a space, which is awkward in shell paths.
15. **`Extras/TutorialInfo/`** is a leftover Unity template and can be deleted.
16. **Input System package is installed but unused**. All gameplay reads legacy `Input.GetAxis` / `Input.GetKey`. Migrating would also make controller support (which `FirstPersonController` was written with in mind) straightforward.