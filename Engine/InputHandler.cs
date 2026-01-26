using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace AvaRoids.Engine;

public class InputHandler : IDisposable
{
    private readonly HashSet<Key> _pressedKeys = new();
    private readonly ControllerManager _controller = new();

    // Threshold for analog stick to register as direction
    private const float StickThreshold = 0.3f;
    // Threshold for trigger to register as pressed
    private const float TriggerThreshold = 0.3f;

    public void OnKeyDown(Key key)
    {
        _pressedKeys.Add(key);
    }

    public void OnKeyUp(Key key)
    {
        _pressedKeys.Remove(key);
    }

    public bool IsKeyPressed(Key key)
    {
        return _pressedKeys.Contains(key);
    }

    public void Update()
    {
        _controller.Update();
    }

    public bool IsControllerConnected => _controller.IsConnected;

    public bool IsRotatingLeft =>
        IsKeyPressed(Key.A) ||
        IsKeyPressed(Key.Left) ||
        _controller.DPadLeft ||
        _controller.LeftStickX < -StickThreshold;

    public bool IsRotatingRight =>
        IsKeyPressed(Key.D) ||
        IsKeyPressed(Key.Right) ||
        _controller.DPadRight ||
        _controller.LeftStickX > StickThreshold;

    public bool IsThrusting =>
        IsKeyPressed(Key.W) ||
        IsKeyPressed(Key.Up) ||
        _controller.DPadUp ||
        _controller.LeftStickY < -StickThreshold ||  // Up is negative Y
        _controller.RightTrigger > TriggerThreshold ||
        _controller.ButtonA;

    public bool IsShooting =>
        IsKeyPressed(Key.Space) ||
        _controller.ButtonX ||
        _controller.ButtonB ||
        _controller.RightBumper ||
        _controller.LeftBumper;

    public bool IsPausePressed =>
        _controller.ButtonStart;

    public bool IsBackPressed =>
        _controller.ButtonBack;

    public void Dispose()
    {
        _controller.Dispose();
    }
}
