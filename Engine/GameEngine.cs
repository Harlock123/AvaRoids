using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using AvaRoids.Models;

namespace AvaRoids.Engine;

public enum GameState
{
    StartScreen,
    Playing,
    Paused,
    GameOver
}

public class GameEngine : IDisposable
{
    private static readonly Random Random = new();

    private const double SaucerSpawnInterval = 15.0;
    public const int SaucerScoreValue = 200;

    public Ship Ship { get; private set; }
    public List<Asteroid> Asteroids { get; } = new();
    public List<Bullet> Bullets { get; } = new();
    public List<Explosion> Explosions { get; } = new();
    public List<Saucer> Saucers { get; } = new();
    public List<Bullet> SaucerBullets { get; } = new();
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; }

    private double _screenWidth = 800;
    private double _screenHeight = 600;
    private bool _screenWidthSet;
    private bool _screenHeightSet;
    private bool _initialPositionSet;

    public double ScreenWidth
    {
        get => _screenWidth;
        set
        {
            _screenWidth = value;
            _screenWidthSet = true;
            CheckInitialPosition();
        }
    }

    public double ScreenHeight
    {
        get => _screenHeight;
        set
        {
            _screenHeight = value;
            _screenHeightSet = true;
            CheckInitialPosition();
        }
    }

    private void CheckInitialPosition()
    {
        if (!_initialPositionSet && _screenWidthSet && _screenHeightSet)
        {
            _initialPositionSet = true;
            Ship.Respawn(new Point(_screenWidth / 2, _screenHeight / 2));
        }
    }

    public InputHandler Input { get; } = new();
    public SoundManager Sound { get; } = new();

    private bool _wasShootingLastFrame;
    private bool _wasThrustingLastFrame;
    private int _currentWave;
    private double _saucerSpawnTimer;

    public GameEngine()
    {
        Ship = new Ship(new Point(ScreenWidth / 2, ScreenHeight / 2));
        State = GameState.StartScreen;
        SpawnStartScreenAsteroids();
    }

    private void SpawnStartScreenAsteroids()
    {
        // Spawn some asteroids for the start screen background
        for (int i = 0; i < 5; i++)
        {
            var position = new Point(
                Random.NextDouble() * ScreenWidth,
                Random.NextDouble() * ScreenHeight
            );
            var size = (AsteroidSize)Random.Next(3);
            Asteroids.Add(new Asteroid(position, size));
        }
    }

    public void Dispose()
    {
        Sound.Dispose();
        Input.Dispose();
    }

    public void StartNewGame()
    {
        Score = 0;
        Lives = 3;
        State = GameState.Playing;
        _currentWave = 1;
        _saucerSpawnTimer = SaucerSpawnInterval;

        Ship = new Ship(new Point(ScreenWidth / 2, ScreenHeight / 2));
        Asteroids.Clear();
        Bullets.Clear();
        Explosions.Clear();
        Saucers.Clear();
        SaucerBullets.Clear();

        SpawnWave(_currentWave);
    }

    private void SpawnWave(int wave)
    {
        int asteroidCount = 3 + wave;

        for (int i = 0; i < asteroidCount; i++)
        {
            SpawnAsteroid();
        }
    }

    private void SpawnAsteroid()
    {
        Point position;
        do
        {
            int edge = Random.Next(4);
            position = edge switch
            {
                0 => new Point(Random.NextDouble() * ScreenWidth, -40),
                1 => new Point(ScreenWidth + 40, Random.NextDouble() * ScreenHeight),
                2 => new Point(Random.NextDouble() * ScreenWidth, ScreenHeight + 40),
                _ => new Point(-40, Random.NextDouble() * ScreenHeight)
            };
        } while (DistanceToShip(position) < 150);

        Asteroids.Add(new Asteroid(position, AsteroidSize.Large));
    }

    private double DistanceToShip(Point position)
    {
        double dx = position.X - Ship.Position.X;
        double dy = position.Y - Ship.Position.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void Update(double deltaTime)
    {
        if (State == GameState.StartScreen)
        {
            // Update asteroids so they float around on the start screen
            UpdateAsteroids(deltaTime);

            if (Input.IsShooting && !_wasShootingLastFrame)
            {
                StartNewGame();
            }
            _wasShootingLastFrame = Input.IsShooting;
            return;
        }

        if (State == GameState.GameOver)
        {
            if (Input.IsShooting && !_wasShootingLastFrame)
            {
                StartNewGame();
            }
            _wasShootingLastFrame = Input.IsShooting;
            return;
        }

        if (State == GameState.Paused)
        {
            return;
        }

        UpdateShip(deltaTime);
        UpdateBullets(deltaTime);
        UpdateAsteroids(deltaTime);
        UpdateSaucers(deltaTime);
        UpdateSaucerBullets(deltaTime);
        UpdateExplosions(deltaTime);
        CheckCollisions();

        if (Asteroids.Count == 0)
        {
            _currentWave++;
            SpawnWave(_currentWave);
        }

        _wasShootingLastFrame = Input.IsShooting;
    }

    public void Pause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Sound.StopThruster();
        }
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            State = GameState.Playing;
        }
    }

    private void UpdateShip(double deltaTime)
    {
        if (Input.IsRotatingLeft)
            Ship.RotateLeft(deltaTime);
        if (Input.IsRotatingRight)
            Ship.RotateRight(deltaTime);

        // Apply mouse wheel rotation directly (faster than key rotation)
        if (Input.MouseWheelDelta != 0)
            Ship.Rotation += Input.MouseWheelDelta * 0.3;

        if (Input.IsThrusting)
        {
            Ship.Thrust(deltaTime);
            Sound.PlayThruster();
        }
        else if (_wasThrustingLastFrame)
        {
            Sound.StopThruster();
        }
        _wasThrustingLastFrame = Input.IsThrusting;

        if (Input.IsShooting && !_wasShootingLastFrame)
        {
            var bullet = Ship.Shoot();
            if (bullet != null)
            {
                Bullets.Add(bullet);
                Sound.PlayWeapon();
            }
        }

        Ship.Update(deltaTime, ScreenWidth, ScreenHeight);
    }

    private void UpdateBullets(double deltaTime)
    {
        foreach (var bullet in Bullets)
        {
            bullet.Update(deltaTime, ScreenWidth, ScreenHeight);
        }

        Bullets.RemoveAll(b => !b.IsAlive);
    }

    private void UpdateAsteroids(double deltaTime)
    {
        foreach (var asteroid in Asteroids)
        {
            asteroid.Update(deltaTime, ScreenWidth, ScreenHeight);
        }
    }

    private void UpdateExplosions(double deltaTime)
    {
        foreach (var explosion in Explosions)
        {
            explosion.Update(deltaTime);
        }

        Explosions.RemoveAll(e => e.IsFinished);
    }

    private void UpdateSaucers(double deltaTime)
    {
        // Spawn timer
        _saucerSpawnTimer -= deltaTime;
        if (_saucerSpawnTimer <= 0 && Saucers.Count == 0)
        {
            SpawnSaucer();
            _saucerSpawnTimer = SaucerSpawnInterval;
        }

        // Update each saucer
        foreach (var saucer in Saucers)
        {
            saucer.Update(deltaTime, ScreenWidth, ScreenHeight, Asteroids, Ship.Position);

            // Try to shoot at player
            var bullet = saucer.TryShoot(Ship.Position);
            if (bullet != null)
            {
                SaucerBullets.Add(bullet);
                Sound.PlayWeapon();
            }
        }

        // Remove saucers that left the screen
        Saucers.RemoveAll(s => s.IsOffScreen(ScreenWidth));
    }

    private void SpawnSaucer()
    {
        // Spawn from left or right edge
        bool fromLeft = Random.Next(2) == 0;
        int direction = fromLeft ? 1 : -1;
        double x = fromLeft ? -20 : ScreenWidth + 20;
        double y = 50 + Random.NextDouble() * (ScreenHeight - 100);

        Saucers.Add(new Saucer(new Point(x, y), direction));
    }

    private void UpdateSaucerBullets(double deltaTime)
    {
        foreach (var bullet in SaucerBullets)
        {
            bullet.Update(deltaTime, ScreenWidth, ScreenHeight);
        }

        SaucerBullets.RemoveAll(b => !b.IsAlive);
    }

    private void CheckCollisions()
    {
        var bulletsToRemove = new List<Bullet>();
        var asteroidsToRemove = new List<Asteroid>();
        var asteroidsToAdd = new List<Asteroid>();
        var saucersToRemove = new List<Saucer>();
        var saucerBulletsToRemove = new List<Bullet>();

        // Player bullets vs asteroids
        foreach (var bullet in Bullets)
        {
            foreach (var asteroid in Asteroids)
            {
                if (bullet.CollidesWith(asteroid))
                {
                    bulletsToRemove.Add(bullet);
                    asteroidsToRemove.Add(asteroid);

                    Explosions.Add(new Explosion(asteroid.Position, asteroid.Size));
                    Sound.PlayExplosion();

                    asteroidsToAdd.AddRange(asteroid.Split());
                    Score += asteroid.GetScoreValue();
                    break;
                }
            }
        }

        // Player bullets vs saucers
        foreach (var bullet in Bullets)
        {
            if (bulletsToRemove.Contains(bullet)) continue;

            foreach (var saucer in Saucers)
            {
                if (bullet.CollidesWith(saucer))
                {
                    bulletsToRemove.Add(bullet);
                    saucersToRemove.Add(saucer);

                    Explosions.Add(new Explosion(saucer.Position, AsteroidSize.Medium));
                    Sound.PlayExplosion();

                    Score += SaucerScoreValue;
                    break;
                }
            }
        }

        // Saucers vs asteroids
        foreach (var saucer in Saucers)
        {
            if (saucersToRemove.Contains(saucer)) continue;

            foreach (var asteroid in Asteroids)
            {
                if (saucer.CollidesWith(asteroid))
                {
                    saucersToRemove.Add(saucer);
                    asteroidsToRemove.Add(asteroid);

                    Explosions.Add(new Explosion(saucer.Position, AsteroidSize.Medium));
                    Explosions.Add(new Explosion(asteroid.Position, asteroid.Size));
                    Sound.PlayExplosion();

                    asteroidsToAdd.AddRange(asteroid.Split());
                    break;
                }
            }
        }

        foreach (var bullet in bulletsToRemove)
            Bullets.Remove(bullet);

        foreach (var asteroid in asteroidsToRemove)
            Asteroids.Remove(asteroid);

        foreach (var saucer in saucersToRemove)
            Saucers.Remove(saucer);

        Asteroids.AddRange(asteroidsToAdd);

        if (!Ship.IsInvincible)
        {
            bool shipDestroyed = false;

            // Ship vs asteroids
            foreach (var asteroid in Asteroids)
            {
                if (Ship.CollidesWith(asteroid))
                {
                    shipDestroyed = true;
                    break;
                }
            }

            // Ship vs saucers (physical collision)
            if (!shipDestroyed)
            {
                foreach (var saucer in Saucers)
                {
                    if (Ship.CollidesWith(saucer))
                    {
                        shipDestroyed = true;
                        break;
                    }
                }
            }

            // Saucer bullets vs ship
            if (!shipDestroyed)
            {
                foreach (var bullet in SaucerBullets)
                {
                    if (bullet.CollidesWith(Ship))
                    {
                        saucerBulletsToRemove.Add(bullet);
                        shipDestroyed = true;
                        break;
                    }
                }
            }

            if (shipDestroyed)
            {
                Explosions.Add(Explosion.CreateShipExplosion(Ship.Position));
                Sound.PlayExplosion();
                Sound.StopThruster();

                Lives--;
                if (Lives <= 0)
                {
                    State = GameState.GameOver;
                }
                else
                {
                    Ship.Respawn(new Point(ScreenWidth / 2, ScreenHeight / 2));
                }
            }
        }

        foreach (var bullet in saucerBulletsToRemove)
            SaucerBullets.Remove(bullet);
    }
}
