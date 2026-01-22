using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using AvaRoids.Models;

namespace AvaRoids.Engine;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameEngine : IDisposable
{
    private static readonly Random Random = new();

    public Ship Ship { get; private set; }
    public List<Asteroid> Asteroids { get; } = new();
    public List<Bullet> Bullets { get; } = new();
    public List<Explosion> Explosions { get; } = new();
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; }

    public double ScreenWidth { get; set; } = 800;
    public double ScreenHeight { get; set; } = 600;

    public InputHandler Input { get; } = new();
    public SoundManager Sound { get; } = new();

    private bool _wasShootingLastFrame;
    private bool _wasThrustingLastFrame;
    private int _currentWave;

    public GameEngine()
    {
        Ship = new Ship(new Point(ScreenWidth / 2, ScreenHeight / 2));
        StartNewGame();
    }

    public void Dispose()
    {
        Sound.Dispose();
    }

    public void StartNewGame()
    {
        Score = 0;
        Lives = 3;
        State = GameState.Playing;
        _currentWave = 1;

        Ship = new Ship(new Point(ScreenWidth / 2, ScreenHeight / 2));
        Asteroids.Clear();
        Bullets.Clear();
        Explosions.Clear();

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

    private void CheckCollisions()
    {
        var bulletsToRemove = new List<Bullet>();
        var asteroidsToRemove = new List<Asteroid>();
        var asteroidsToAdd = new List<Asteroid>();

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

        foreach (var bullet in bulletsToRemove)
            Bullets.Remove(bullet);

        foreach (var asteroid in asteroidsToRemove)
            Asteroids.Remove(asteroid);

        Asteroids.AddRange(asteroidsToAdd);

        if (!Ship.IsInvincible)
        {
            foreach (var asteroid in Asteroids)
            {
                if (Ship.CollidesWith(asteroid))
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
                    break;
                }
            }
        }
    }
}
