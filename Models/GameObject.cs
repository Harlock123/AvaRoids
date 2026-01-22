using System;
using Avalonia;

namespace AvaRoids.Models;

public abstract class GameObject
{
    public Point Position { get; set; }
    public Vector Velocity { get; set; }
    public double Rotation { get; set; }
    public double Radius { get; protected set; }
    public bool IsAlive { get; set; } = true;

    protected GameObject(Point position, double radius)
    {
        Position = position;
        Radius = radius;
        Velocity = new Vector(0, 0);
        Rotation = 0;
    }

    public virtual void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        Position = new Point(
            Position.X + Velocity.X * deltaTime,
            Position.Y + Velocity.Y * deltaTime
        );

        WrapPosition(screenWidth, screenHeight);
    }

    protected void WrapPosition(double screenWidth, double screenHeight)
    {
        double x = Position.X;
        double y = Position.Y;

        if (x < -Radius) x += screenWidth + Radius * 2;
        if (x > screenWidth + Radius) x -= screenWidth + Radius * 2;
        if (y < -Radius) y += screenHeight + Radius * 2;
        if (y > screenHeight + Radius) y -= screenHeight + Radius * 2;

        Position = new Point(x, y);
    }

    public bool CollidesWith(GameObject other)
    {
        double dx = Position.X - other.Position.X;
        double dy = Position.Y - other.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        return distance < Radius + other.Radius;
    }

    public abstract Point[] GetPolygonPoints();
}
