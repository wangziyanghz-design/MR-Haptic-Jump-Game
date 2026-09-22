# MR Haptic Jump Game

A Unity-based interactive jumping game for magnetorheological haptic knob research and demonstration.

## Overview

MR Haptic Jump Game is a component-based 2D platform-jumping application for demonstrating and exploring interaction with a magnetorheological (MR) rotary knob. Knob rotation controls platform compression; the launch platform's stiffness affects jump velocity and the commanded haptic resistance. The player chooses how much to charge, then releases a jump toward the next platform.

The project supports physical knob input over UDP, a keyboard-driven encoder simulator, and a separate hold-to-charge keyboard mode. Developers without hardware can run the main gameplay and inspect the calculated haptic values.

## Key Features

- Rigidbody2D jumps with compression-dependent speed and a fixed launch angle.
- Five platform stiffness levels and five target-distance levels.
- Bidirectional encoder unwrapping and a new logical zero for each attempt.
- Success / Perfect / Fail evaluation, three lives, respawn, scoring, and Perfect combos.
- Neutral Return / short-jump retries when landing back on the launch platform.
- Experience mode with a stiffness tutorial and a shuffled set of 25 combinations; randomized Endless mode.
- Configurable UDP input/output and saved runtime connection settings.
- Haptic damping calculation, ON/OFF selection, and a saved calibration-value workflow.
- HUD, Game Over / Restart controls, landing feedback, procedural audio, and an F1 debug panel.

## System Architecture

```text
UDP encoder receiver OR keyboard knob simulator
    -> IKnobInputSource -> EncoderUnwrapper ----+
                                              |
KeyboardChargeInput --------------------------+-> CompressionController
                                                   |             |
                                                   |             +-> PlatformCompressor
Space press (Knob) / Space release (Keyboard)        v
    -------------------------------------> PlayerJumpController
                                              | uses JumpPhysics
                                              v
                                          Rigidbody2D
                                              v
                                      PlayerLandingDetector
                                      /                  \
                          Target / fall                  Current platform
                         LandingEvaluator                Return: reset attempt
                                |                        (no scoring event)
                     Score / Lives / PlatformGenerator
                                |
                       Tutorial / Experience / Endless

Compression + current platform + player/lives state + saved calibration
    -> HapticController -> UdpHapticOutput -> external control system
External control system -> UdpKnobInputReceiver -> encoder input chain
```

Gameplay, input, haptics, presentation, and mode progression are separate components under [Assets/Scripts](Assets/Scripts). `JumpPhysics` calculates velocity; `PlayerJumpController` applies it. UDP components transport values rather than implementing the jump or haptic formulas.

## Haptic Interaction

The implementation is a **gameplay-oriented haptic mapping**, not a validated material model or a calibrated torque model. Physics stiffness `K` and haptic stiffness `kh` are separate parameters associated with the same platform level.

For Knob input with Haptic Feedback ON, grounded charging uses:

```text
D = Dmin + (DmaxUser - Dmin) * kh * x
D = Clamp(D, Dmin, DmaxUser)
```

Here, `x` is normalized compression. Current defaults are `Dmin = 20` and fallback `DmaxUser = 80`. A saved calibration replaces the fallback, with the effective maximum never below `Dmin`. These numbers are command values, not specified physical units.

At the default bounds, `x = 0.5` produces `D = 26` for K1 (`kh = 0.2`) and `D = 50` for K5 (`kh = 1.0`). Higher levels therefore increase resistance more steeply for the same rotation. Visual compression does **not** depend on stiffness.

| Condition | Calculated damping |
|---|---|
| Ready / reset compression | `Dmin` |
| Charging in Knob + Haptic ON | Formula above |
| Accepted release / airborne in Knob + Haptic ON | `DmaxUser` |
| Successful landing, Return, or respawn after reset | `Dmin` |
| Game Over or jumping disabled | `Dmin` |
| Haptic OFF or Keyboard input | `Dmin` |

Hardware output is active only for **Knob + Real Hardware**. Haptic OFF in that configuration sends the minimum; Keyboard and Simulation do not actively send hardware UDP. Shutdown attempts a final minimum command before closing an existing output socket. UDP delivery is not guaranteed.

### Calibration

The calibration value starts at **20**, increases by **5 every 1 second**, and is capped at **130**. SPACE saves the displayed value as `DmaxUser` in `PlayerPrefs`. Settings provides Recalibrate. The first Play with Knob + Real Hardware + Haptic ON routes through calibration if no value is saved; Keyboard and Simulation skip that automatic requirement.

**Current integration limit:** `CalibrationScene` runs the value/UI workflow but has no UDP haptic-output component connected to the calibration ramp. Gameplay output reads `HapticController`, not `HapticCalibration.CurrentDamping`. Do not interpret the calibration screen alone as a verified physical-resistance calibration.

Sources: [HapticController.cs](Assets/Scripts/Haptics/HapticController.cs), [HapticSettings.cs](Assets/Scripts/Haptics/HapticSettings.cs), [HapticCalibration.cs](Assets/Scripts/Haptics/HapticCalibration.cs), and [UdpHapticOutput.cs](Assets/Scripts/Haptics/UdpHapticOutput.cs).

## Input Modes

### Hardware Mode

Select **KNOB -> REAL HARDWARE** in the main menu. Send one ASCII integer per UDP datagram, in the range **0-7999**. The receiver ignores empty, invalid, or out-of-range payloads. SPACE on the keyboard releases the jump; no hardware release-button protocol is implemented.

One revolution is **8000 counts = 360 degrees**. `EncoderUnwrapper` uses the shortest signed difference between successive consumed samples, supporting both directions:

```text
Raw:        7900 -> 7990 ->   20 ->  100
Unwrapped:  7900 -> 7990 -> 8020 -> 8100
```

The initial logical zero waits for the first valid hardware sample. Successful landing, Return, and respawn reset the compression reference without requiring physical knob reversal. With no data, the game continues running and the debug panel reports Waiting / No Data; it does not silently switch sources.

Configure the following under **Settings -> UDP Settings**:

| Setting | Current default | Validation |
|---|---|---|
| Listen Port | `5005` | Integer, 1-65535 |
| Remote IP | `127.0.0.1` | Numeric IP address |
| Output Port | `5006` | Integer, 1-65535 |
| Send Rate Hz | `50` | Finite number, 1-100 |

APPLY validates and saves the configuration in `PlayerPrefs`, closes/reinitializes active sockets, and takes effect without restarting the application. Invalid values preserve the old configuration. RESET DEFAULTS restores the values above. These are defaults, **not fixed deployment ports**. A valid configuration can still fail to bind if its port is unavailable.

Source: [GameSettings.cs](Assets/Scripts/Core/GameSettings.cs), [UdpKnobInputReceiver.cs](Assets/Scripts/Input/UdpKnobInputReceiver.cs), and [EncoderUnwrapper.cs](Assets/Scripts/Input/EncoderUnwrapper.cs).

### Keyboard Simulation

There are two distinct ways to play without hardware:

| Menu selection | Charge / rotate | Release |
|---|---|---|
| **KEYBOARD** | Hold SPACE; `x = Clamp01(holdTime / 2 seconds)` | Release SPACE |
| **KNOB -> SIMULATION** | Hold D / Right Arrow to increase; A / Left Arrow to decrease | Press SPACE |

Keyboard charge reaches full compression after 2 seconds. For example, 0.5 seconds gives `x = 0.25` and 1 second gives `x = 0.5`. The encoder simulator starts at 0 and changes at **2400 counts/second**, wrapping in both directions rather than clamping. These are current configurable defaults.

Both paths feed the same compression/jump system. Airborne input cannot launch a second jump. Keyboard charge is cleared on release and retry; Knob launch velocity is latched at release while the encoder can continue updating for the next zero reference.

## Game Modes

### Experience Mode

The first five successful jumps teach stiffness in the order **K1 -> K5 -> K2 -> K3 -> K4**, with target distance **4** throughout. Tutorial levels have distinct colors. The formal stage then covers all **5 stiffness x 5 distance = 25** combinations once, shuffled with Fisher-Yates. Formal platforms have uniform colors rather than revealing stiffness.

Only Success / Perfect advances a step. Fail retries the same combination while lives remain; Return does not advance. After 25 formal completions, the manager marks `ExperienceCompleted` and disables further jumping.

### Endless Mode

Endless skips the tutorial and selects stiffness and distance randomly from the five configured levels. Success / Perfect generates the next combination; Fail retries the current one, and Return leaves it unchanged. Platforms use uniform colors. Play continues until lives run out.

Sources: [TutorialManager.cs](Assets/Scripts/Modes/TutorialManager.cs), [ExperienceModeManager.cs](Assets/Scripts/Modes/ExperienceModeManager.cs), and [EndlessModeManager.cs](Assets/Scripts/Modes/EndlessModeManager.cs).

## Platform Mechanics

The current [GameScene](Assets/Scenes/GameScene.unity) and component defaults use:

| Platform | Relative physics stiffness `K` | Haptic coefficient `kh` | Gameplay effect at equal compression |
|---|---|---|---|
| K1 | 1.00 | 0.2 | Lowest launch speed / shallowest damping increase |
| K2 | 1.25 | 0.4 | Increased launch speed and damping gradient |
| K3 | 1.50 | 0.6 | Intermediate launch speed and damping gradient |
| K4 | 1.75 | 0.8 | Higher launch speed and damping gradient |
| K5 | 2.00 | 1.0 | Highest launch speed / steepest damping increase |

Target distances are **3, 4, 5, 6, 7 Unity world units**, measured between platform positions, not between their edges. Platform width is **2.5 units**. `PlatformGenerator` reuses two platform slots: the target becomes the current platform on success, and the previous platform becomes the new target to the right.

Knob compression and launch velocity are computed as:

```text
deltaEncoder = UnwrappedEncoder - ZeroEncoder
x = Clamp01(deltaEncoder / 4000)
v = jumpScale * x * sqrt(K)
launchVelocity = (v * cos(45 degrees), v * sin(45 degrees))
```

Maximum effective compression is **4000 counts = 180 degrees**. Reverse motion cannot make compression negative. Current `jumpScale` is **16**, and compression at or below **0.01** does not launch. Flight uses Rigidbody2D; the player's configured gravity scale is **3**. Larger compression or stiffness increases launch speed, affecting both height and distance; the player does not steer the launch angle.

`PlatformCompressor` interpolates Y scale from its rest value to **0.65 times** that value at full compression, correcting position to hold the bottom stable. Its scale calculation reads only compression, never stiffness.

### Landing, retries, and scoring

- Target landings require a top-supporting collision. With `error = abs(playerX - targetCenterX)`, the central 20% is Perfect: `error < 0.1 * platformWidth`. Other in-range positions are Success; positions outside half the width are Fail.
- Falling below the configured **Y = -5** threshold while airborne produces Fail. One life is lost; remaining lives respawn the player on the same launch platform and retain the pair. Three lives are provided initially; zero triggers Game Over.
- Returning to the current platform uses a later non-rising physics step and a top-supporting contact, excluding the initial launch-step contact. It clears airborne/charge, resets the encoder reference, and emits **no scoring/progression landing event**. Lives, score, existing combo, target, and mode progress remain unchanged, including supported edge returns.
- Success awards **100**. Perfect awards **200 x combo**, with consecutive Perfect multipliers **1-5**. Success and Fail reset the Perfect combo. Game Over stops further scoring/jumps; the result panel offers Restart and Main Menu.

Sources: [JumpPhysics.cs](Assets/Scripts/Gameplay/JumpPhysics.cs), [PlatformGenerator.cs](Assets/Scripts/Platform/PlatformGenerator.cs), [PlayerLandingDetector.cs](Assets/Scripts/Player/PlayerLandingDetector.cs), and [ScoreManager.cs](Assets/Scripts/Gameplay/ScoreManager.cs).

## Project Structure

```text
Assets/
  Scenes/              MainMenuScene, CalibrationScene, GameScene
  Scripts/
    Core/              Modes, state definitions, saved settings
    Input/             Keyboard charge, encoder simulation, unwrap, UDP input
    Haptics/           Damping mapping, calibration values, UDP output
    Player/            Jump, landing, respawn, visual feedback
    Platform/          Parameters, generation, compression
    Gameplay/          Physics, lives, score, camera, audio
    Modes/             Tutorial, Experience, Endless
    UI/                Menu, HUD, results, calibration, debug, UDP settings
  Art/                 Reserved asset folder
  Prefabs/             Reserved asset folder
  ScriptableObjects/   Reserved asset folder
  Settings/            Reserved asset folder
  Editor/              Reserved editor-tool folder
Packages/              Dependency manifest and lock file
ProjectSettings/       Unity project configuration
```

Reserved folders currently have folder metadata rather than tracked content. Audio placeholders are generated in code; no `Assets/Audio` folder is required. Unity generates local caches such as `Library` on import; they and local builds are excluded from Git.

## Requirements

- **Unity 2022.3.30f1**, verified in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
- Unity Hub and Git for the setup below. Install Windows Build Support if producing a Windows player.
- Built-In Render Pipeline and the legacy Input Manager configuration; this project does not require URP or the new Input System package.
- Dependencies are declared in [manifest.json](Packages/manifest.json) and resolved in [packages-lock.json](Packages/packages-lock.json). Let Unity restore them rather than upgrading them on first import.

Notable declared packages are **2D feature set 2.0.0**, **Unity UI 1.0.0**, **TextMeshPro 3.0.6**, **Test Framework 1.1.33**, **Timeline 1.7.6**, and **Visual Scripting 1.9.4**, alongside Unity engine modules and editor integrations. Installed packages do not imply that every package is used by gameplay. Current UI components use `UnityEngine.UI`.

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/wangziyanghz-design/MR-Haptic-Jump-Game.git
   cd MR-Haptic-Jump-Game
   ```

2. Open Unity Hub and add the cloned project from disk (the folder containing `Assets`, `Packages`, and `ProjectSettings`).
3. Open it with **Unity 2022.3.30f1**.
4. Wait for package resolution, asset import, and automatic `Library` generation to finish.
5. Open **`Assets/Scenes/MainMenuScene.unity`** and enter Play Mode. Focus the Game view for keyboard input.
6. Select the game mode and input configuration, then PLAY. Start with the hardware-free options below.

Enabled Build Settings scenes are ordered **0 MainMenuScene**, **1 CalibrationScene**, **2 GameScene**. Start at MainMenuScene to choose settings explicitly rather than relying on saved preferences.

## Running Without Hardware

For the simplest setup, select **KEYBOARD** and either game mode. Haptic feedback is automatically OFF. Hold SPACE to compress and release SPACE to jump. A short jump back onto the current platform should allow another attempt without a penalty.

To test the encoder path, select **KNOB -> SIMULATION**. Rotate with D / Right Arrow, reverse with A / Left Arrow, and press SPACE to release. Simulation does not depend on incoming UDP and sends no hardware output. Haptic ON can still calculate damping for inspection in the debug panel.

Press **F1** to show/hide encoder, compression, platform, damping, mode, and connection diagnostics. Development controls include stiffness/distance overrides, Reset Zero, Recalibrate, and Force Fail. Closing the panel or clearing overrides restores mode-controlled parameters. Force Fail requires an active jump.

## Hardware Integration

```text
Unity <-> UDP <-> external control/haptic system <-> MR rotary knob
```

Implement the external adapter against the ASCII datagrams described above: encoder integers into Unity's Listen Port, rounded integer damping commands from Unity's Output Port. Output datagrams contain the number only, without a required newline. Configure the external system to receive at the Remote IP and Output Port; the default loopback address reaches only the local computer.

No specific external controller, firmware, or hardware model is prescribed by the repository. Translating damping command values into device actuation is the external system's responsibility. Use a trusted network and independently verified device limits and shutdown handling. A high damping command is a resistance request, not a software guarantee of mechanical locking or active return motion.

## Development Workflow

```text
main
  feature/*
  fix/*
  docs/*
```

Develop on a focused branch, push it, and request review through a Pull Request. Do not develop directly on `main`. Keep simulation usable when changing hardware code and coordinate scene/prefab edits to reduce merge conflicts.

## Known Limitations

- Calibration currently saves a value but does not transmit its ramp to hardware in CalibrationScene (see above).
- Encoder unwrapping assumes less than half a revolution between consumed samples. Large gaps or fast movement can make direction ambiguous; the receiver keeps the latest accepted raw value.
- UDP has no authentication, acknowledgements, or device-side safety watchdog here. Missing data does not freeze gameplay, but the last encoder value is retained; the receiving timeout is a status indicator, not a guaranteed fail-safe.
- Output scheduling is frame-driven and sends at most one packet per Update. The configured frequency is a target, not a hard real-time guarantee.
- Stiffness and damping are gameplay parameters, not measured material properties. Hardware compatibility and physical safety need separate validation.
- Experience completion sets a flag, logs completion, and disables jumping; there is no dedicated completion screen in that manager. Test Framework is installed, but no automated test suite is tracked in this repository.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This project is licensed under the [MIT License](LICENSE).
