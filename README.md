# Delivery Impossible

A cooperative first-person delivery game made in **Unity** with **Mirror** networking. You and your partner work as couriers: pick up cargo, carry it through narrow doorways without dropping or smashing it, and place it in the delivery zone. Heavy items need two people to lift, and fragile ones break if you hit them too hard.

> The in-game interface is in Russian.

<!-- Add a screenshot or GIF here:
![Gameplay](images/gameplay.png)
-->

## Features

- **Online co-op for 2 players.** One player hosts, the other joins by IP address.
- **Solo test mode.** The host can start a shift alone to try the game without a partner.
- **Lobby system.** Players get numbers (Courier 1, Courier 2), mark themselves as ready, and the host starts the shift.
- **Physics-based cargo carrying.** Grab and carry objects with `E`. Heavy cargo needs two couriers; with only one, the carrier is too weak to lift it properly.
- **Fragile cargo.** Impacts above a safe speed damage the cargo. It changes color as it gets damaged and breaks at high speed.
- **Delivery orders.** Each order goes through pickup, transit through a doorway checkpoint, delivery to a marked zone, and final confirmation.
- **Grading and score.** Every delivery gets a grade from S to C, and the shift ends with a total score.
- **Main menu and HUD.** Play / Join menus, lobby screen, objective hints, integrity indicator and placement progress.

## How to Play

1. One player chooses **Play → Host**. The other chooses **Join**, enters the host's IP address and connects.
2. In the lobby, everyone presses **Ready**, then the host starts the shift. (A single player can start a solo test.)
3. For each order:
   1. **Pick up** the cargo (walk up to it and press `E`).
   2. **Carry** it through the doorway checkpoint. Heavy cargo needs both couriers; fragile cargo must not be hit.
   3. **Place** it inside the green delivery zone.
   4. **Don't touch** the cargo until the placement progress bar fills up and the delivery is confirmed.
4. After the last order the shift summary shows how many orders you completed and your total score. Then a new shift starts.

### Controls

| Key | Action |
|-----|--------|
| `W` `A` `S` `D` | Move |
| Mouse | Look around |
| `Space` | Jump |
| `Ctrl` / `C` | Crouch |
| `E` | Grab / release cargo |
| `Esc` | Release the mouse cursor |
| Left mouse button | Capture the cursor again |
| `F1` | Show or hide the network debug panel |

### Grading

| Cargo type | S (1000 pts) | A (750 pts) | B (500 pts) | C (250 pts) |
|------------|--------------|-------------|-------------|-------------|
| Normal / heavy | no drops and ≤ 60 s | ≤ 1 drop and ≤ 90 s | ≤ 2 drops | otherwise |
| Fragile | integrity ≥ 95% and no drops | integrity ≥ 80% | integrity ≥ 50% | otherwise |

## Tech Stack

| Category | Technology |
|----------|------------|
| Engine | Unity 6 (6000.3.26f1) |
| Language | C# |
| Networking | [Mirror](https://github.com/MirrorNetworking/Mirror) |
| Input | Unity Input System |
| Rendering | Universal Render Pipeline (URP) |
| UI | Unity UI + TextMesh Pro |

## Getting Started

### Requirements

- [Unity Hub](https://unity.com/download) and Unity Editor **6000.3.26f1** (the version the project was made with)
- Git

### Open the project

```bash
git clone https://github.com/DiaaaP/Game-.git
```

1. In Unity Hub choose **Add → Add project from disk** and select the `game` folder inside the cloned repository (the one that contains `Assets`, `Packages` and `ProjectSettings`).
2. Open the project with the Unity version listed above. The first import may take a few minutes.
3. Open the scene `Assets/DeliveryImpossible/Scenes/MovementTest.unity`.
4. Press **Play**.

### Test multiplayer on one computer

- Build the game (**File → Build Profiles / Build Settings**) and run the build as a client, or run two instances of the game.
- One instance: **Play → Host**. The other: **Join** with address `127.0.0.1`.
- The build also accepts the launch argument `--auto-client`, which connects the client to `127.0.0.1` automatically.

### Play over a network

- The host creates a room and shares their computer's IP address.
- Both computers must be on the same network, and the host's firewall must allow the game to accept connections.

## Project Structure

```
game/
├── Assets/
│   ├── DeliveryImpossible/
│   │   ├── Scripts/      # Game code (C#)
│   │   ├── Prefabs/      # Player, cargo, fragile TV, network bootstrap
│   │   ├── Scenes/       # MovementTest scene
│   │   ├── UI/           # Menu background and UI assets
│   │   ├── Materials/    # Cargo, delivery zone, damage materials
│   │   ├── Fonts/        # Oswald and Rubik (OFL license)
│   │   └── Editor/       # Editor tools for setting up prototypes
│   └── Mirror/           # Mirror networking library
├── Packages/
└── ProjectSettings/
```

### Main scripts

| Script | Purpose |
|--------|---------|
| `NetworkFirstPersonController` | First-person movement, jumping, crouching, mouse look |
| `NetworkGrabber` / `NetworkGrabbable` | Grabbing, carrying and releasing objects; two-person lifting for heavy cargo |
| `NetworkFragileCargo` | Impact damage and integrity of fragile items |
| `NetworkDeliveryOrder` | Order flow, timing, drops, grading and score |
| `DeliveryCheckpoint` / `DeliveryZone` | Doorway checkpoint and delivery area detection |
| `NetworkSessionState` | Session state: required players, solo test, shift start and restart |
| `NetworkLobbyPlayer` | Player numbers and ready status (synced over the network) |
| `NetworkLobbyUi` / `NetworkOrderHud` | Menus, lobby screen and in-game HUD |
| `DevelopmentNetworkLauncher` | Developer helpers: network debug panel and launch arguments |
