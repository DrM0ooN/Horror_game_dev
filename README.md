# Horror Game - working title

A first-person, 4-player co-op horror game built in Unity. Players spawn in a central hub and must clear a set of lockable challenge rooms, each holding a self-contained mini-game. While players are off solving a room, danger escalates back in the hub - the core tension mechanic.

Status: early vertical slice, actively in development. Movement, one full room loop from start to solve or fail to respawn, and the first mini-game are working in single-player. Multiplayer networking is not wired in yet.

## Screenshot
<img width="1379" height="781" alt="image" src="https://github.com/user-attachments/assets/af772172-5a15-4858-bb58-0eee9419ef72" />

<img width="1388" height="774" alt="image" src="https://github.com/user-attachments/assets/08e2306c-1faa-4a84-81af-f40171047ebb" />


## Current features

- First-person player controller
- Lockable challenge room with a start button and a required-player-count trigger
- First mini-game: a flashlight mechanic. Find and collect glowing key fragments while avoiding a moving monster that can trigger a fail if it catches your beam
- Hub Threat System: shared danger state that escalates while players are away from the hub
- Win and fail flow, with respawn back to the hub on both outcomes
- Warm, high-contrast lighting pass and placeholder art direction matching a painterly horror mood board

## Tech stack

- Unity 6, URP
- C#
- Networking: Photon Fusion 2, integration in progress
- Voice: Photon Voice 2, planned, proximity based
- New Input System

## Roadmap

- Wire networking so the room and threat logic work across clients
- Additional mini-games and rooms
- Real art pass, current art is placeholder or procedural
- Sound pass

## Notes

Built solo, with AI coding assistants used throughout for implementation support and debugging.
