using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaRoids.Engine;
using AvaRoids.Models;

namespace AvaRoids.Views;

public class GameCanvas : Control
{
    private readonly GameEngine _engine;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate;
    private bool _isThrusting;

    private static readonly IPen WhitePen = new Pen(Brushes.White, 2);
    private static readonly IPen ThinWhitePen = new Pen(Brushes.White, 1);
    private static readonly IBrush WhiteBrush = Brushes.White;

    public GameCanvas()
    {
        _engine = new GameEngine();
        _lastUpdate = DateTime.Now;

        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gameTimer.Tick += GameLoop;

        Focusable = true;
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _gameTimer.Start();
        Focus();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gameTimer.Stop();
    }

    protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape)
        {
            if (_engine.State == GameState.Playing)
            {
                _engine.Pause();
            }
            else if (_engine.State == GameState.Paused)
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                window?.Close();
            }
            e.Handled = true;
            return;
        }

        if (_engine.State == GameState.Paused)
        {
            _engine.Resume();
            e.Handled = true;
            return;
        }

        _engine.Input.OnKeyDown(e.Key);
        if (e.Key == Avalonia.Input.Key.W)
            _isThrusting = true;
        e.Handled = true;
    }

    protected override void OnKeyUp(Avalonia.Input.KeyEventArgs e)
    {
        _engine.Input.OnKeyUp(e.Key);
        if (e.Key == Avalonia.Input.Key.W)
            _isThrusting = false;
        e.Handled = true;
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        deltaTime = Math.Min(deltaTime, 0.1);

        _engine.ScreenWidth = Bounds.Width;
        _engine.ScreenHeight = Bounds.Height;

        _engine.Update(deltaTime);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));

        foreach (var asteroid in _engine.Asteroids)
        {
            DrawPolygon(context, asteroid.GetPolygonPoints(), WhitePen);
        }

        foreach (var explosion in _engine.Explosions)
        {
            DrawExplosion(context, explosion);
        }

        foreach (var bullet in _engine.Bullets)
        {
            var pos = bullet.Position;
            context.DrawEllipse(WhiteBrush, null, pos, 2, 2);
        }

        if (_engine.State == GameState.Playing || _engine.State == GameState.Paused)
        {
            bool shouldDraw = !_engine.Ship.IsInvincible ||
                              (int)(DateTime.Now.Millisecond / 100) % 2 == 0;

            if (shouldDraw)
            {
                DrawPolygon(context, _engine.Ship.GetPolygonPoints(), WhitePen);

                if (_isThrusting && _engine.State == GameState.Playing &&
                    (int)(DateTime.Now.Millisecond / 50) % 2 == 0)
                {
                    DrawPolygon(context, _engine.Ship.GetThrustFlamePoints(), ThinWhitePen);
                }
            }
        }

        DrawHUD(context);
    }

    private void DrawPolygon(DrawingContext context, Point[] points, IPen pen)
    {
        if (points.Length < 2) return;

        for (int i = 0; i < points.Length; i++)
        {
            int next = (i + 1) % points.Length;
            context.DrawLine(pen, points[i], points[next]);
        }
    }

    private void DrawExplosion(DrawingContext context, Explosion explosion)
    {
        double opacity = explosion.GetOpacity();
        var color = Color.FromArgb((byte)(255 * opacity), 255, 255, 255);
        var pen = new Pen(new SolidColorBrush(color), 2);

        var lines = explosion.GetParticleLines();
        foreach (var (start, end) in lines)
        {
            context.DrawLine(pen, start, end);
        }
    }

    private void DrawHUD(DrawingContext context)
    {
        var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);

        var scoreText = new FormattedText(
            $"SCORE: {_engine.Score}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            24,
            Brushes.White
        );
        context.DrawText(scoreText, new Point(20, 20));

        var livesText = new FormattedText(
            $"LIVES: {_engine.Lives}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            24,
            Brushes.White
        );
        context.DrawText(livesText, new Point(20, 50));

        if (_engine.State == GameState.Paused)
        {
            var pausedText = new FormattedText(
                "PAUSED",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                48,
                Brushes.White
            );
            context.DrawText(pausedText, new Point(
                (Bounds.Width - pausedText.Width) / 2,
                Bounds.Height / 2 - 50
            ));

            var escText = new FormattedText(
                "Press ESC to exit",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                24,
                Brushes.White
            );
            context.DrawText(escText, new Point(
                (Bounds.Width - escText.Width) / 2,
                Bounds.Height / 2 + 10
            ));

            var resumeText = new FormattedText(
                "Press any other key to resume",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                24,
                Brushes.White
            );
            context.DrawText(resumeText, new Point(
                (Bounds.Width - resumeText.Width) / 2,
                Bounds.Height / 2 + 45
            ));
        }

        if (_engine.State == GameState.GameOver)
        {
            var gameOverText = new FormattedText(
                "GAME OVER",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                48,
                Brushes.White
            );
            context.DrawText(gameOverText, new Point(
                (Bounds.Width - gameOverText.Width) / 2,
                Bounds.Height / 2 - 50
            ));

            var restartText = new FormattedText(
                "Press SPACE to restart",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                24,
                Brushes.White
            );
            context.DrawText(restartText, new Point(
                (Bounds.Width - restartText.Width) / 2,
                Bounds.Height / 2 + 20
            ));
        }
    }
}
