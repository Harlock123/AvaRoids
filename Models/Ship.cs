using System;
using Avalonia;

namespace AvaRoids.Models;

public class Ship : GameObject
{
    public const double RotationSpeed = 5.0;
    public const double ThrustPower = 300.0;
    public const double MaxSpeed = 400.0;
    public const double ShootCooldown = 0.15;

    private double _shootTimer;
    public bool IsInvincible { get; set; }
    public double InvincibilityTimer { get; set; }
    public const double InvincibilityDuration = 2.0;

    public Ship(Point position) : base(position, 15)
    {
        Rotation = -Math.PI / 2;
    }

    public void RotateLeft(double deltaTime)
    {
        Rotation -= RotationSpeed * deltaTime;
    }

    public void RotateRight(double deltaTime)
    {
        Rotation += RotationSpeed * deltaTime;
    }

    public void Thrust(double deltaTime)
    {
        var thrustVector = new Vector(
            Math.Cos(Rotation) * ThrustPower * deltaTime,
            Math.Sin(Rotation) * ThrustPower * deltaTime
        );

        Velocity += thrustVector;

        double speed = Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
        if (speed > MaxSpeed)
        {
            Velocity = new Vector(
                Velocity.X / speed * MaxSpeed,
                Velocity.Y / speed * MaxSpeed
            );
        }
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        base.Update(deltaTime, screenWidth, screenHeight);

        if (_shootTimer > 0)
            _shootTimer -= deltaTime;

        if (IsInvincible)
        {
            InvincibilityTimer -= deltaTime;
            if (InvincibilityTimer <= 0)
            {
                IsInvincible = false;
            }
        }
    }

    public bool CanShoot()
    {
        return _shootTimer <= 0;
    }

    public Bullet? Shoot()
    {
        if (!CanShoot()) return null;

        _shootTimer = ShootCooldown;

        var bulletPos = new Point(
            Position.X + Math.Cos(Rotation) * 20,
            Position.Y + Math.Sin(Rotation) * 20
        );

        return new Bullet(bulletPos, Rotation);
    }

    public void Respawn(Point position)
    {
        Position = position;
        Velocity = new Vector(0, 0);
        Rotation = -Math.PI / 2;
        IsInvincible = true;
        InvincibilityTimer = InvincibilityDuration;
    }

    public override Point[] GetPolygonPoints()
    {
        double size = 20;
        var points = new Point[3];

        points[0] = new Point(
            Position.X + Math.Cos(Rotation) * size,
            Position.Y + Math.Sin(Rotation) * size
        );

        points[1] = new Point(
            Position.X + Math.Cos(Rotation + 2.5) * size * 0.7,
            Position.Y + Math.Sin(Rotation + 2.5) * size * 0.7
        );

        points[2] = new Point(
            Position.X + Math.Cos(Rotation - 2.5) * size * 0.7,
            Position.Y + Math.Sin(Rotation - 2.5) * size * 0.7
        );

        return points;
    }

    public Point[] GetThrustFlamePoints()
    {
        double size = 20;
        var points = new Point[3];

        double backAngle = Rotation + Math.PI;

        points[0] = new Point(
            Position.X + Math.Cos(Rotation + 2.5) * size * 0.5,
            Position.Y + Math.Sin(Rotation + 2.5) * size * 0.5
        );

        points[1] = new Point(
            Position.X + Math.Cos(backAngle) * size * 1.2,
            Position.Y + Math.Sin(backAngle) * size * 1.2
        );

        points[2] = new Point(
            Position.X + Math.Cos(Rotation - 2.5) * size * 0.5,
            Position.Y + Math.Sin(Rotation - 2.5) * size * 0.5
        );

        return points;
    }
}
