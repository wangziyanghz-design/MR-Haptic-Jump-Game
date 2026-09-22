# Contributing

Thank you for contributing to MR Haptic Jump Game. Keep changes focused, preserve the hardware-free workflow, and describe how your contribution was tested.

## Development Setup

```bash
git clone https://github.com/wangziyanghz-design/MR-Haptic-Jump-Game.git
cd MR-Haptic-Jump-Game
```

Add the project in Unity Hub and open it with **Unity 2022.3.30f1**, as recorded in `ProjectSettings/ProjectVersion.txt`. Wait for package resolution/import, then open `Assets/Scenes/MainMenuScene.unity`. Do not upgrade Unity or packages as an incidental part of an unrelated change.

Start with **KEYBOARD** (hold SPACE to charge, release to jump), or **KNOB -> SIMULATION** (D / Right to rotate, A / Left to reverse, SPACE to release). See [README.md](README.md) for verified parameters, UDP configuration, and current hardware limitations.

## Branch Workflow

**Do not develop directly on main.** Use descriptive branches:

- `feature/<name>`: `feature/haptic-feedback`, `feature/gameplay`, `feature/ui`
- `fix/<name>`: `fix/udp-connection`
- `docs/<name>`: `docs/readme-update`

Keep unrelated work on separate branches. Do not force-push shared branches or rewrite another contributor's history.

## Before Starting Work

Check `git status` first. Commit or otherwise safely preserve your work before switching branches; do not discard someone else's local changes.

```bash
git checkout main
git pull
git checkout -b feature/example
```

Check existing work and discuss overlapping changes with the team before editing shared assets.

## Unity Collaboration Rules

1. Never commit generated caches or local outputs: `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/`, `Builds/`, `UserSettings/`, `MemoryCaptures/`, `.vs/`, generated `*.csproj` / `*.sln`, or build ZIPs. Respect `.gitignore`; do not bypass it with `git add -f`.
2. Commit Unity `.meta` files with the assets/folders they describe. Keep asset moves and their metadata together so GUID references remain intact.
3. Do not manually delete or regenerate another contributor's `.meta` files. Prefer moving/renaming assets within Unity.
4. Avoid simultaneous edits to the same `.unity` Scene.
5. Avoid simultaneous edits to the same large Prefab.
6. Notify the team before modifying shared Scenes or Prefabs; identify those assets in the PR.
7. Prefer independent C# components and focused Prefabs where appropriate to reduce merge conflicts. Extend existing systems instead of duplicating gameplay or hardware logic.
8. Review the diff for accidental scene serialization, ProjectSettings, or package changes. Include such changes only when required and explained.
9. Never commit tokens, passwords, API keys, `.env` files, or private hardware credentials. Inspect new large assets before staging; discuss Git LFS before adding files near GitHub's single-file limit.

## Commit Guidelines

Use small, coherent commits with clear messages. Conventional Commit-style prefixes are recommended, not mandatory:

```text
feat: add ...
fix: fix ...
docs: update ...
refactor: ...
```

Before committing, inspect `git status`, `git diff`, and `git diff --cached`. Run `git diff --check` and `git diff --cached --check` for whitespace problems. Stage only intended files.

## Pull Requests

```text
branch -> commit -> push -> Pull Request -> review -> merge
```

Push your branch, for example:

```bash
git push -u origin feature/example
```

Open a Pull Request targeting `main`. Include:

- **What changed** and the affected components/assets.
- **Why** the change is needed, with an issue reference if available.
- **How tested**, including Unity version, input mode, scenarios, and Console results.
- **Whether hardware was required** and whether it was actually available.
- **Whether Keyboard Simulation was tested**; distinguish KEYBOARD hold-to-charge from KNOB -> SIMULATION.

Include screenshots for UI/visual changes and relevant non-sensitive diagnostics for communication changes. Request review before merging; this guide describes the team workflow and does not itself configure GitHub branch protection.

## Hardware-related Changes

Changes to UDP, encoder handling, haptic mapping, calibration, or external communication must state:

- **Hardware tested / simulation only / loopback only**. Do not equate loopback packets with physical haptic validation.
- **Relevant configuration**: listen/output ports, destination, send rate, encoder range, and damping/calibration settings. Redact sensitive infrastructure details.
- **Fallback behaviour**: no data, malformed packets, unavailable ports, disconnects, mode changes, and shutdown.

Keep Keyboard and Knob Simulation paths usable. Verify that Simulation does not send hardware commands, Haptic OFF produces the intended minimum in Real Hardware mode, and Return/respawn reset compression and damping. Do not assume that the current calibration UI drives a physical resistance ramp; document any new wiring and its device-side safety requirements explicitly.

## Testing

At minimum, verify and report:

- The project opens in Unity 2022.3.30f1 without compilation errors.
- **Console Error = 0** during the exercised scenarios. Record existing errors instead of hiding them.
- MainMenuScene starts and launches gameplay.
- Keyboard simulation works where applicable, with the exact tested input path identified.

For gameplay changes, check short-jump Return followed by another attempt, a target Success/Perfect, Fail/respawn, and Game Over/Restart. Confirm that Return preserves lives, score, combo, current pair, and progression. Exercise both Experience and Endless when shared progression is affected.

For hardware changes, add malformed/no-data checks and loopback transport tests; test on the actual device where needed. Report anything not tested. New deterministic rules should have targeted tests where practical; the repository currently has Test Framework installed but no tracked automated suite.

Documentation-only PRs should check technical claims against source/configuration, verify links and formatting, and confirm that no Unity assets changed. A documentation check is not a new gameplay or hardware test.
