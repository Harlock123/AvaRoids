using System;
using Avalonia;

namespace AvaRoids.Models;

public class Bullet : GameObject
{
    public const double Speed = 500.0;
    public const double MaxLifetime = 2.0;

    private double _lifetime;

    public Bullet(Point position, double direction) : base(position, 3)
    {
        Rotation = direction;
        Velocity = new Vector(
            Math.Cos(direction) * Speed,
            Math.Sin(direction) * Speed
        );
        _lifetime = MaxLifetime;
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        base.Update(deltaTime, screenWidth, screenHeight);

        _lifetime -= deltaTime;
        if (_lifetime <= 0)
        {
            IsAlive = false;
        }
    }

    public override Point[] GetPolygonPoints()
    {
        return new[]
        {
            new Point(Position.X - 2, Position.Y - 2),
            new Point(Position.X + 2, Position.Y - 2),
            new Point(Position.X + 2, Position.Y + 2),
            new Point(Position.X - 2, Position.Y + 2)
        };
    }
}
