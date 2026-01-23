using System;
using System.Collections.Generic;
using Avalonia;

namespace AvaRoids.Models;

public class Saucer : GameObject
{
    private static readonly Random Random = new();

    private const double Speed = 120.0;
    private const double VerticalSpeed = 80.0;
    private const double ShootCooldownTime = 1.5;
    private const double AvoidanceDistance = 100.0;

    public int Direction { get; } // +1 = right, -1 = left
    private double _shootCooldown;
    private double _targetVerticalVelocity;

    public Saucer(Point position, int direction) : base(position, 20)
    {
        Direction = direction;
        Velocity = new Vector(Speed * direction, 0);
        _shootCooldown = ShootCooldownTime * 0.5; // Initial delay before first shot
    }

    public void Update(double deltaTime, double screenWidth, double screenHeight,
        IEnumerable<Asteroid> asteroids, Point shipPosition)
    {
        _shootCooldown -= deltaTime;

        // Asteroid avoidance AI
        UpdateAvoidance(asteroids);

        // Smoothly adjust vertical velocity toward target
        double currentVy = Velocity.Y;
        double diff = _targetVerticalVelocity - currentVy;
        double adjustment = Math.Sign(diff) * Math.Min(Math.Abs(diff), 200 * deltaTime);
        double newVy = currentVy + adjustment;

        Velocity = new Vector(Speed * Direction, newVy);

        // Update position (don't use base.Update as we don't want wrapping)
        Position = new Point(
            Position.X + Velocity.X * deltaTime,
            Position.Y + Velocity.Y * deltaTime
        );

        // Keep within vertical bounds
        double y = Position.Y;
        if (y < Radius)
        {
            y = Radius;
            _targetVerticalVelocity = Math.Max(0, _targetVerticalVelocity);
        }
        else if (y > screenHeight - Radius)
        {
            y = screenHeight - Radius;
            _targetVerticalVelocity = Math.Min(0, _targetVerticalVelocity);
        }
        Position = new Point(Position.X, y);
    }

    private void UpdateAvoidance(IEnumerable<Asteroid> asteroids)
    {
        _targetVerticalVelocity = 0;
        double closestThreatDistance = double.MaxValue;
        double threatY = 0;

        foreach (var asteroid in asteroids)
        {
            double dx = asteroid.Position.X - Position.X;
            double dy = asteroid.Position.Y - Position.Y;

            // Only consider asteroids ahead of us (in our direction of travel)
            if (Direction > 0 && dx < 0) continue;
            if (Direction < 0 && dx > 0) continue;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            double combinedRadius = Radius + asteroid.Radius + 20; // Extra buffer

            // Check if asteroid is within avoidance range and on collision course
            if (Math.Abs(dx) < AvoidanceDistance + asteroid.Radius &&
                Math.Abs(dy) < combinedRadius &&
                distance < closestThreatDistance)
            {
                closestThreatDistance = distance;
                threatY = dy;
            }
        }

        // If there's a threat, move away from it
        if (closestThreatDistance < double.MaxValue)
        {
            // Move opposite to the asteroid's relative Y position
            _targetVerticalVelocity = threatY < 0 ? VerticalSpeed : -VerticalSpeed;
        }
    }

    public Bullet? TryShoot(Point shipPosition)
    {
        if (_shootCooldown > 0)
            return null;

        _shootCooldown = ShootCooldownTime;

        // Calculate angle to ship
        double dx = shipPosition.X - Position.X;
        double dy = shipPosition.Y - Position.Y;
        double angle = Math.Atan2(dy, dx);

        // Add slight inaccuracy for fairness
        angle += (Random.NextDouble() - 0.5) * 0.2;

        return new Bullet(Position, angle);
    }

    public bool IsOffScreen(double screenWidth)
    {
        if (Direction > 0)
            return Position.X > screenWidth + Radius;
        else
            return Position.X < -Radius;
    }

    public override Point[] GetPolygonPoints()
    {
        // Classic UFO shape - elliptical body with dome
        var points = new List<Point>();

        // Bottom half of ellipse (body)
        int segments = 12;
        for (int i = 0; i <= segments; i++)
        {
            double angle = Math.PI * i / segments; // 0 to PI (bottom half)
            double x = Position.X + Math.Cos(angle) * Radius;
            double y = Position.Y + Math.Sin(angle) * Radius * 0.4;
            points.Add(new Point(x, y));
        }

        // Dome on top
        double domeWidth = Radius * 0.5;
        double domeHeight = Radius * 0.5;
        points.Add(new Point(Position.X + domeWidth, Position.Y - Radius * 0.1));
        points.Add(new Point(Position.X + domeWidth * 0.7, Position.Y - domeHeight));
        points.Add(new Point(Position.X - domeWidth * 0.7, Position.Y - domeHeight));
        points.Add(new Point(Position.X - domeWidth, Position.Y - Radius * 0.1));

        return points.ToArray();
    }
}
