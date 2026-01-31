using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    private bool _wasPausePressed;
    private bool _wasBackPressed;

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
        // Handle Ctrl+S (or Cmd+S on macOS) for screenshot
        if (e.Key == Avalonia.Input.Key.S &&
            (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
             e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta)))
        {
            TakeScreenshot();
            e.Handled = true;
            return;
        }

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

    protected override void OnPointerPressed(Avalonia.Input.PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed)
            _engine.Input.OnMouseDown(Avalonia.Input.MouseButton.Left);
        if (props.IsRightButtonPressed)
            _engine.Input.OnMouseDown(Avalonia.Input.MouseButton.Right);
        e.Handled = true;
    }

    protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed)
            _engine.Input.OnMouseUp(Avalonia.Input.MouseButton.Left);
        if (!props.IsRightButtonPressed)
            _engine.Input.OnMouseUp(Avalonia.Input.MouseButton.Right);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(Avalonia.Input.PointerWheelEventArgs e)
    {
        _engine.Input.OnMouseWheel(e.Delta.Y);
        e.Handled = true;
    }

    private void TakeScreenshot()
    {
        var pixelSize = new PixelSize((int)Bounds.Width, (int)Bounds.Height);
        if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            return;

        using var bitmap = new RenderTargetBitmap(pixelSize);
        bitmap.Render(this);

        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = Path.Combine(picturesPath, $"AvaRoids_{timestamp}.png");

        bitmap.Save(filename);
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        deltaTime = Math.Min(deltaTime, 0.1);

        _engine.ScreenWidth = Bounds.Width;
        _engine.ScreenHeight = Bounds.Height;

        // Poll controller state
        _engine.Input.Update();

        // Handle controller pause/resume (with edge detection)
        HandleControllerPause();

        // Update thrust visual state from controller
        if (_engine.Input.IsThrusting)
            _isThrusting = true;
        else if (!_engine.Input.IsThrusting && _engine.State == GameState.Playing)
            _isThrusting = _engine.Input.IsThrusting;

        _engine.Update(deltaTime);
        _engine.Input.PostUpdate();
        InvalidateVisual();
    }

    private void HandleControllerPause()
    {
        // Start button toggles pause (edge detection)
        bool pausePressed = _engine.Input.IsPausePressed;
        if (pausePressed && !_wasPausePressed)
        {
            if (_engine.State == GameState.Playing)
                _engine.Pause();
            else if (_engine.State == GameState.Paused)
                _engine.Resume();
        }
        _wasPausePressed = pausePressed;

        // Back button exits when paused (edge detection)
        bool backPressed = _engine.Input.IsBackPressed;
        if (backPressed && !_wasBackPressed && _engine.State == GameState.Paused)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window?.Close();
        }
        _wasBackPressed = backPressed;
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

        foreach (var saucer in _engine.Saucers)
        {
            DrawPolygon(context, saucer.GetPolygonPoints(), WhitePen);
        }

        foreach (var bullet in _engine.SaucerBullets)
        {
            var pos = bullet.Position;
            context.DrawEllipse(WhiteBrush, null, pos, 3, 3);
        }

        if (_engine.State == GameState.Playing || _engine.State == GameState.Paused || _engine.State == GameState.StartScreen)
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

    private void DrawStartScreen(DrawingContext context, Typeface typeface)
    {
        var titleText = new FormattedText(
            "AVAROIDS",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            64,
            Brushes.White
        );
        context.DrawText(titleText, new Point(
            (Bounds.Width - titleText.Width) / 2,
            80
        ));

        var instructions = new[]
        {
            "KEYBOARD",
            "W / UP         Thrust",
            "A / LEFT       Rotate Left",
            "D / RIGHT      Rotate Right",
            "SPACE          Fire",
            "",
            "MOUSE",
            "Right Button   Thrust",
            "Left Button    Fire",
            "Scroll Wheel   Rotate",
            "",
            "OTHER",
            "ESC            Pause",
            "CTRL/CMD+S     Screenshot",
            "",
            "SCORING",
            "Large Asteroid    20",
            "Medium Asteroid   50",
            "Small Asteroid   100",
            "Flying Saucer    200"
        };

        double y = 160;
        foreach (var line in instructions)
        {
            int fontSize = line is "KEYBOARD" or "MOUSE" or "OTHER" or "SCORING" ? 18 : 14;
            var text = new FormattedText(
                line,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White
            );
            context.DrawText(text, new Point(
                (Bounds.Width - text.Width) / 2,
                y
            ));
            y += fontSize + 6;
        }

        var startText = new FormattedText(
            "Press SPACE to start",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            28,
            Brushes.White
        );
        context.DrawText(startText, new Point(
            (Bounds.Width - startText.Width) / 2,
            Bounds.Height - 80
        ));
    }

    private void DrawHUD(DrawingContext context)
    {
        var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);

        if (_engine.State == GameState.StartScreen)
        {
            DrawStartScreen(context, typeface);
            return;
        }

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
