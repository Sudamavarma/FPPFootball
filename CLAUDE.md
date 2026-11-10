# FPPFootball: Project Brief for Claude

## Game
- First-person multiplayer football game. Each player is a real person seeing the pitch through their player's eyes.
- Start: 3v3 (small pitch, with bots filling empty slots). Later scale up toward 7v7.
- Mobile first (Android), then PC and other platforms.
- A "look around" button lets players check the pitch while dribbling.
- Target: finish version 1 in about 1 year.

## Tech stack
- Unity 6000.3.28f1 (6.3 LTS). Do NOT suggest upgrading the editor.
- Universal Render Pipeline (URP), Universal 3D template. Mobile-friendly settings only, no HDRP features.
- New Input System package (touch controls: virtual joystick and buttons).
- Networking: TBD (deciding between Photon Fusion, Netcode for GameObjects, Mirror). Prototype a 1v1 ball test first.
- Server-authoritative ball physics with client-side prediction.
- Source control: Git + Git LFS on GitHub (private repo).

## Rules for code
- C# only. Keep scripts small and single-purpose.
- Scripts go in Assets/Scripts/ under subfolders: Player, Ball, Networking, UI.
- Prefer prefabs over editing shared scenes.
- Never commit Library/, Temp/, Logs/, or Builds/.
- Performance target: smooth on a mid-range Android phone. Avoid per-frame allocations, avoid heavy shaders.
- Explain what a script needs to be attached to in the Unity Editor (GameObject, components, inspector fields).

## Workflow
- One branch per task, merge to main through pull requests.
- Only one person edits a given scene at a time.
- Pull before starting. Commit small and often.

## Team
- [Name]: [role, e.g. code / networking]
- [Name]: [role, e.g. art / animation]
- [Name]: [role, e.g. design / UI]

## Current status
- Project created, pushed to GitHub with LFS, Android build profile set.
- Next: grey-box pitch, ball, FPP player controller with touch input.