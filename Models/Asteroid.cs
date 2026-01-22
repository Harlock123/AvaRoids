using System;
using System.Collections.Generic;
using Avalonia;

namespace AvaRoids.Models;

public class Asteroid : GameObject
{
    private static readonly Random Random = new();

    public AsteroidSize Size { get; }
    private readonly double[] _vertexOffsets;
    private readonly int _vertexCount;
    private readonly double _rotationSpeed;

    public Asteroid(Point position, AsteroidSize size) : this(position, size, GetRandomVelocity())
    {
    }

    public Asteroid(Point position, AsteroidSize size, Vector velocity) : base(position, GetRadiusForSize(size))
    {
        Size = size;
        Velocity = velocity;
        _vertexCount = Random.Next(8, 12);
        _vertexOffsets = new double[_vertexCount];

        for (int i = 0; i < _vertexCount; i++)
        {
            _vertexOffsets[i] = 0.7 + Random.NextDouble() * 0.6;
        }

        _rotationSpeed = (Random.NextDouble() - 0.5) * 2;
    }

    private static double GetRadiusForSize(AsteroidSize size)
    {
        return size switch
        {
            AsteroidSize.Large => 40,
            AsteroidSize.Medium => 20,
            AsteroidSize.Small => 10,
            _ => 20
        };
    }

    private static Vector GetRandomVelocity()
    {
        double angle = Random.NextDouble() * Math.PI * 2;
        double speed = 50 + Random.NextDouble() * 100;
        return new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        Rotation += _rotationSpeed * deltaTime;
        base.Update(deltaTime, screenWidth, screenHeight);
    }

    public List<Asteroid> Split()
    {
        var newAsteroids = new List<Asteroid>();

        AsteroidSize? newSize = Size switch
        {
            AsteroidSize.Large => AsteroidSize.Medium,
            AsteroidSize.Medium => AsteroidSize.Small,
            AsteroidSize.Small => null,
            _ => null
        };

        if (newSize.HasValue)
        {
            for (int i = 0; i < 2; i++)
            {
                double angle = Random.NextDouble() * Math.PI * 2;
                double speed = 80 + Random.NextDouble() * 80;
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

                var offset = new Point(
                    (Random.NextDouble() - 0.5) * 10,
                    (Random.NextDouble() - 0.5) * 10
                );

                newAsteroids.Add(new Asteroid(
                    new Point(Position.X + offset.X, Position.Y + offset.Y),
                    newSize.Value,
                    velocity
                ));
            }
        }

        return newAsteroids;
    }

    public override Point[] GetPolygonPoints()
    {
        var points = new Point[_vertexCount];

        for (int i = 0; i < _vertexCount; i++)
        {
            double angle = Rotation + (Math.PI * 2 * i / _vertexCount);
            double r = Radius * _vertexOffsets[i];
            points[i] = new Point(
                Position.X + Math.Cos(angle) * r,
                Position.Y + Math.Sin(angle) * r
            );
        }

        return points;
    }

    public int GetScoreValue()
    {
        return Size switch
        {
            AsteroidSize.Large => 20,
            AsteroidSize.Medium => 50,
            AsteroidSize.Small => 100,
            _ => 0
        };
    }
}
