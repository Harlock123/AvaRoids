using System;
using Avalonia;

namespace AvaRoids.Models;

public class Explosion
{
    private static readonly Random Random = new();

    public Point Position { get; }
    public double Duration { get; }
    public double TimeElapsed { get; private set; }
    public bool IsFinished => TimeElapsed >= Duration;
    public double Progress => Math.Min(TimeElapsed / Duration, 1.0);

    private readonly ExplosionParticle[] _particles;
    public int ParticleCount => _particles.Length;

    public Explosion(Point position, AsteroidSize size)
        : this(position, GetParticleCount(size), GetBaseSpeed(size), 0.4)
    {
    }

    private Explosion(Point position, int particleCount, double baseSpeed, double duration)
    {
        Position = position;
        Duration = duration;
        TimeElapsed = 0;

        _particles = new ExplosionParticle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            double angle = Random.NextDouble() * Math.PI * 2;
            double speed = baseSpeed * (0.5 + Random.NextDouble());
            double length = 5 + Random.NextDouble() * 10;

            _particles[i] = new ExplosionParticle
            {
                Angle = angle,
                Speed = speed,
                Length = length,
                Offset = Random.NextDouble() * 5
            };
        }
    }

    public static Explosion CreateShipExplosion(Point position)
    {
        return new Explosion(position, 24, 200, 0.6);
    }

    private static int GetParticleCount(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => 16,
        AsteroidSize.Medium => 12,
        AsteroidSize.Small => 8,
        _ => 10
    };

    private static double GetBaseSpeed(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => 150,
        AsteroidSize.Medium => 100,
        AsteroidSize.Small => 60,
        _ => 100
    };

    public void Update(double deltaTime)
    {
        TimeElapsed += deltaTime;
    }

    public (Point Start, Point End)[] GetParticleLines()
    {
        var lines = new (Point Start, Point End)[_particles.Length];
        double progress = Progress;

        for (int i = 0; i < _particles.Length; i++)
        {
            var p = _particles[i];
            double distance = (p.Speed * TimeElapsed) + p.Offset;
            double fadeLength = p.Length * (1 - progress);

            double startDist = distance;
            double endDist = distance + fadeLength;

            var start = new Point(
                Position.X + Math.Cos(p.Angle) * startDist,
                Position.Y + Math.Sin(p.Angle) * startDist
            );

            var end = new Point(
                Position.X + Math.Cos(p.Angle) * endDist,
                Position.Y + Math.Sin(p.Angle) * endDist
            );

            lines[i] = (start, end);
        }

        return lines;
    }

    public double GetOpacity()
    {
        return 1.0 - Progress;
    }

    private struct ExplosionParticle
    {
        public double Angle;
        public double Speed;
        public double Length;
        public double Offset;
    }
}
