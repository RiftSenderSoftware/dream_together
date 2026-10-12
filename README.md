# Dream Together

An early prototype of a social multiplayer 3D game built with Unity 6. Several players join a shared
location as anime-style characters and walk around it in first- or third-person view.

The focus so far is on making characters feel alive: the head, eyes and torso follow where the player
looks, the character blinks, feet adapt to slopes and stairs, and all of this is visible to other
players over the network.

There is no quest, item interaction, chat, UI or progression yet. `SampleScene` is a test level
(ground, cubes, stairs, lighting).

## Tech stack

| Area | Technology |
|---|---|
| Engine | Unity 6 (`6000.6.4f1`), URP (separate PC and Mobile profiles) |
| Networking | Netcode for GameObjects 2.11, Unity Transport, Multiplayer Play Mode |
| Input | Input System (keyboard + mouse, gamepad) |
| Characters | Rukha93 Modular Anime Character: modular body parts, ShaderGraph toon shaders, facial blend shapes |
| IK | Final IK (RootMotion): `IKSolverLimb` for legs |
| Editor tools | vHierarchy, UniOpenCLI (`Assets/vExtantions`) |

Project code lives in `Assets/_Project`.

## Getting started

1. Open the project in Unity `6000.6.4f1`.
2. Open `Assets/_Project/Scenes/SampleScene.unity` and press Play.
3. In the editor the main instance starts as host automatically, and Multiplayer Play Mode virtual
   players join as clients.
4. In a build, use the on-screen **Host** / **Client** buttons. The host listens on `0.0.0.0:7777`
   (LAN); clients connect to the address typed in the field.

### Controls

| Action | Input |
|---|---|
| Move | WASD / arrow keys / left stick |
| Look | Mouse |
| Switch first / third person | `V` |
| Zoom (third person) | Mouse wheel |

## Architecture

### Composition through contexts and explicit `Init`

Components don't look each other up in `Awake`. A root "context" wires them together and passes
dependencies through `Init(...)`.

- **`PlayerContext`** is the player root. It references `PlayerVisual`, `PlayerController`,
  `PlayerInputReader` and `CharacterController` (auto-filled in `OnValidate`) and has two modes:
  - `InitLocal(view)`: the player controlled on this machine (input + movement + visual);
  - `InitRemote(view)`: a player controlled elsewhere (visual only; input and controller disabled).
- **`CameraContext`** is the camera root. It connects `CameraController` with both camera views.

Entry points:

- `GameInitializer` for single-player scenes with a pre-placed player;
- `PlayerNetwork.OnNetworkSpawn` for networked scenes, where each spawned player initializes itself.

The key abstraction is the **`view` transform**: where the player is looking from and to. For the
local player it is the real camera transform. For a remote player it is an empty `RemoteView` object
rotated from network state. All visual code (gaze, body, eyes) reads only `view` and doesn't know
whether the player is local or remote, so it stays independent of networking.

### Player layers

```
PlayerInputReader ──MoveChanged(Vector2)──► PlayerController ──SetMovement(vel, speed)──► PlayerVisual
 (keyboard/gamepad)                          (CharacterController,                          (Animator blend tree,
                                              gravity, camera-relative                       smoothing)
                                              movement)                                          │
                                                                                                 ▼
                                                                                Body Parts (procedural animation)
```

- **Input** (`PlayerInputReader`) polls WASD, arrow keys and the gamepad stick, and raises an event
  only when the direction changes.
- **Controller** (`PlayerController`) moves the `CharacterController` relative to the camera yaw, with
  separate forward / backward / sideways speeds and gravity. A 0.12 s grace time ignores brief ground
  contact loss at seams and steps. It exposes `MoveVelocity` and `MoveSpeed`, which are what gets
  sent over the network.
- **Visual** (`PlayerVisual`) converts world velocity into local `MoveX` / `MoveZ` for a 2D strafe
  blend tree. Root motion is off; `CharacterController` owns movement. Exponential smoothing uses
  separate constants for starting, stopping and changing direction.

### Procedural animation (`Visual/Body Parts`)

Runs on top of the Animator in `LateUpdate`, with order fixed by `[DefaultExecutionOrder]`:

| Order | Component | Responsibility |
|---|---|---|
| -100 | `CameraLookTarget` | Places the look target at `head + view.forward * 10 m` |
| -50 | `PlayerBodyLook` | Body yaw. While moving, the body faces the camera direction. While standing, the head turns first, then the spine (Spine / Chest / UpperChest with weights), and past a threshold the character **turns in place** (`IsTurning` / `TurnDirection` Animator parameters, constant turn speed synced with the stepping animation) |
| -25 | `PlayerFootPlacement` | Foot IK on Final IK: raycast per foot, height and tilt from surface normal, pelvis lowered to the lower foot. Reverts its previous correction in `Update` so it never accumulates |
| 0 | `PlayerHead` | Rotates the neck towards the look target within left / right / up / down limits |
| 100 | `PlayerEye` | Eye direction via `eye_look_*` blend shapes, computed from the final head pose |
| — | `EyeBlink` | Periodic blinking via the `eyes_closed` blend shape |

Rotation limits cascade: head → spine → whole body.

### Camera

- `CameraController` owns a shared `Yaw → Pitch → CameraPoint` rig and toggles views with `V`, using
  a smooth transition (SmoothStep + Slerp, 0.35 s).
- `ICameraView` (`CameraPoint`, `Init`, `SetActive`, `UpdatePose`) has two implementations:
  - `FirstPersonCameraController`: eye height 1.6 m, pitch -80°..80°;
  - `ThirdPersonCameraController`: orbit camera, pitch -30°..70°, distance 1.25–8 m, wheel zoom.

### Networking

- **Topology**: direct-IP host / client via `ConnectionSetup`. Tick rate 30, the player prefab is
  spawned automatically.
- **Owner-authoritative**:
  - position is synced by an owner-authoritative `NetworkTransform`;
  - `PlayerNetwork` syncs three owner-writable `NetworkVariable`s:
    - `_look` (`Vector2`: camera pitch, yaw),
    - `_moveVelocity` (`Vector2`: horizontal velocity),
    - `_moveSpeed` (`float`).
- On remote machines `_remoteView` is slerped towards the synced look rotation, and the velocity is
  fed into `PlayerVisual.SetMovement`. All procedural animation (head, eyes, body, foot IK) is
  computed **locally on each client**; only these three values travel over the network.
- A `NetworkVariable` is marked dirty only when its value changes, so idle players send almost
  nothing.

Trade-off: the server doesn't validate movement. This is fine for a cooperative social game, but
not for a competitive one.

## Code conventions

Defined in `.claude/skills/unity-code-style`:

- Required references are not hidden behind null checks; a broken setup should fail loudly in the
  console.
- `[Tooltip]` only where a field's meaning is not obvious.
- Comments, tooltips and log messages in English only.
- Namespaces: `Project.Player`, `Project.Player.Visual`, `Project.Player.Network`,
  `Project.Player.Camera`, `Project.Network`.
