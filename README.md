# AvaRoids

A classic Asteroids arcade game clone built with .NET and Avalonia UI, featuring vector graphics rendering.

## Screenshots

The game features classic vector-style graphics with white lines on a black background, faithful to the original 1979 arcade aesthetic.

| Start Screen | Gameplay | Game Over |
|:------------:|:--------:|:---------:|
| ![Start Screen](IMAGES/AVAROIDS0.png) | ![Gameplay](IMAGES/AVAROIDS1.png) | ![Game Over](IMAGES/AVAROIDS2.png) |

## Requirements

- .NET 9.0 SDK
- Compatible with Windows, macOS, and Linux

SDL2 native libraries are bundled with the application - no additional installation required.

## Building and Running

```bash
# Clone or navigate to the project directory
cd AvaRoids

# Build the project
dotnet build

# Run the game
dotnet run
```

## Controls

### Keyboard

| Key | Action |
|-----|--------|
| **W** / **Up Arrow** | Thrust forward |
| **A** / **Left Arrow** | Rotate left |
| **D** / **Right Arrow** | Rotate right |
| **Space** | Fire bullets |
| **ESC** | Pause game |
| **ESC** (while paused) | Exit game |
| **Any key** (while paused) | Resume game |
| **Space** (on Game Over) | Restart game |

### Mouse

| Input | Action |
|-------|--------|
| **Left Button** | Fire bullets |
| **Right Button** | Thrust forward |
| **Scroll Wheel** | Rotate ship |

### Controller (Xbox/PlayStation/Switch Pro)

| Input | Action |
|-------|--------|
| **Left Stick** / **D-Pad** | Rotate and thrust |
| **A** / **Right Trigger** | Thrust forward |
| **X** / **B** / **Bumpers** | Fire bullets |
| **Start** | Pause / Resume game |
| **Back/Select** (while paused) | Exit game |
| **A** (on Game Over) | Restart game |

Controllers are automatically detected when connected. The game supports Xbox, PlayStation, Switch Pro, and most SDL2-compatible controllers.

### Other

| Key | Action |
|-----|--------|
| **Ctrl+S** / **Cmd+S** | Save screenshot to Pictures folder |

## Gameplay

- The game starts with a **title screen** showing controls and scoring - press **Space** to begin
- Destroy asteroids by shooting them with your ship
- Large asteroids split into 2 medium asteroids when shot
- Medium asteroids split into 2 small asteroids when shot
- Small asteroids are destroyed completely when shot
- **Flying saucers** appear periodically and shoot at you - destroy them for bonus points!
- Saucers will try to dodge asteroids while tracking your position
- Explosions with particle effects appear when asteroids, saucers, or your ship are destroyed
- Saucers can also be destroyed by colliding with asteroids
- Avoid colliding with asteroids, saucers, and saucer bullets - you have 3 lives
- After losing a life, you respawn with brief invincibility (indicated by flashing)
- Clear all asteroids to advance to the next wave (more asteroids spawn each wave)
- Press ESC to pause the game at any time

## Scoring

| Target | Points |
|--------|--------|
| Small Asteroid | 100 |
| Medium Asteroid | 50 |
| Large Asteroid | 20 |
| Flying Saucer | 200 |

## Physics

- **Zero-G Movement**: Your ship maintains its velocity after thrusting. There is no friction in space!
- **Screen Wrapping**: Flying off one edge of the screen wraps you to the opposite side
- **Momentum**: Plan your movements carefully - you'll need to thrust in the opposite direction to slow down

## Sound Effects

- **Weapon**: Fires when shooting bullets
- **Thruster**: Loops while holding the thrust key
- **Explosion**: Plays when asteroids or ship are destroyed

## Project Structure

```
AvaRoids/
├── AvaRoids.sln
├── AvaRoids.csproj
├── Program.cs
├── App.axaml / App.axaml.cs
├── MainWindow.axaml / MainWindow.axaml.cs
├── IMAGES/
│   ├── AVAROIDS0.png       # Title screen screenshot
│   ├── AVAROIDS1.png       # Gameplay screenshot
│   └── AVAROIDS2.png       # Game over screenshot
├── Models/
│   ├── AsteroidSize.cs     # Enum: Large, Medium, Small
│   ├── GameObject.cs       # Base class with position, velocity, collision
│   ├── Ship.cs             # Player ship with thrust and rotation
│   ├── Asteroid.cs         # Irregular polygon asteroids with splitting
│   ├── Bullet.cs           # Projectile with lifetime limit
│   ├── Explosion.cs        # Particle explosion effect
│   └── Saucer.cs           # Enemy UFO with AI and shooting
├── Engine/
│   ├── GameEngine.cs       # Game loop, state management, collisions
│   ├── InputHandler.cs     # Keyboard, mouse, and controller input
│   ├── ControllerManager.cs # SDL2 gamepad support
│   └── SoundManager.cs     # Cross-platform audio playback
├── Views/
│   └── GameCanvas.cs       # Custom Avalonia control for rendering
└── Sounds/
    ├── Weapon.wav          # Shooting sound effect
    ├── Thruster.wav        # Engine thrust sound
    └── Explosion1.wav      # Explosion sound effect
```

## Technical Details

- **Framework**: Avalonia UI 11.x
- **Audio**: NetCoreAudio for cross-platform sound playback
- **Controller**: SDL2 GameController API for cross-platform gamepad support
- **Rendering**: Custom `Control` with `DrawingContext` for vector graphics
- **Game Loop**: 60 FPS using `DispatcherTimer`
- **Collision Detection**: Circle-based bounding radius collision
- **Asteroids**: Procedurally generated irregular polygons with 8-12 vertices
- **Saucer AI**: Autonomous enemy with asteroid avoidance and player targeting
- **Effects**: Particle-based explosion animations with fade-out
- **Screenshots**: Captured using `RenderTargetBitmap` and saved as PNG

## License

MIT License
