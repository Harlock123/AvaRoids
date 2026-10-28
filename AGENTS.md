# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers playing the game, its controls and
scoring; this file covers how the code is put together and what to watch when changing it.

## What this is

An Asteroids-style arcade game (after Atari's 1979 original), in C# on .NET 9 and Avalonia 11, drawn as white
vector lines by a custom Avalonia control. It is one of the author's older AVA games and is simpler than the later
ones: a single project at the repository root, no tests, and no high-score table.

## Commands

```bash
dotnet build AvaRoids.sln       # needs the .NET 9 SDK (there is no global.json)
dotnet run                      # play it; there are no command-line options
```

There are no tests yet. The project sets nullable reference types but not `TreatWarningsAsErrors`.

## Layout

- `Program.cs`, `App.axaml`, `MainWindow.axaml`: the Avalonia start-up and the window that holds the canvas.
- `Engine/`
  - `GameEngine.cs` holds the game states, waves, the ship, bullets, asteroids, saucers, explosions, collisions
    and the score.
  - `InputHandler.cs` turns keys, the mouse and the gamepad into questions such as `IsThrusting`.
  - `ControllerManager.cs` reads SDL2 game controllers.
  - `SoundManager.cs` plays the WAV files in `Sounds/` through NetCoreAudio, with a looping thruster.
- `Models/`: `GameObject` (position, velocity, wrapping, collision) and its kinds: `Ship`, `Asteroid`, `Bullet`,
  `Saucer`; also `Explosion` and `AsteroidSize`.
- `Views/GameCanvas.cs`: draws everything in `Render`, runs the game from a 16 ms `DispatcherTimer`, handles
  pause and saves a screenshot to the Pictures folder on Ctrl+S (Cmd+S).
- `Sounds/`: the WAV files, copied next to the executable by the project file.
- `Images/`: the README's screenshots.

## Things to know before changing it

- **Variable time step.** `GameCanvas` measures the time since the last tick (capped at 0.1 s) and passes it to
  `GameEngine.Update`, so movement scales with frame time. There is no fixed-step loop.
- **Unseeded randomness.** `GameEngine` uses a static `new Random()`, so no two games are the same and nothing can
  be replayed from a seed.
- **Input.** Up on the stick is negative Y (`LeftStickY < -StickThreshold` means thrust). Keys are tracked as
  Avalonia `Key` values.
- **Sound.** NetCoreAudio shells out to a system player; on Linux that is `aplay` from `alsa-utils`, and without
  it the game is silent. `SoundManager.IsMuted` silences it in code.
- **Gamepads.** SDL2 comes from `ppy.SDL2-CS`; ARM Linux needs SDL2 installed (see the README).
- **Line endings.** Some files are CRLF (`Program.cs`, `App.axaml*`, `MainWindow.axaml*`, `AvaRoids.csproj`,
  `Engine/ControllerManager.cs`) and the rest LF. Keep each file's existing line endings so diffs show only real
  changes.

## Making a change

- Build with no new warnings, and play it to check the change, since there are no tests.
- Update `README.md` (controls, gameplay, scoring, project structure) to match.

## CI and releases

`.github/workflows/build.yml` builds every push with the .NET 9 SDK (it runs no tests). It then publishes
self-contained single-file builds for win-x64/x86/arm64, linux-x64/arm64/arm and osx-x64/arm64. Every run keeps
the builds as workflow artifacts. Only pushes to `main` replace the assets of the `latest` pre-release, and a
`v*` tag makes a proper release.
