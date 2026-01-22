using System.Collections.Generic;
using Avalonia.Input;

namespace AvaRoids.Engine;

public class InputHandler
{
    private readonly HashSet<Key> _pressedKeys = new();

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

    public bool IsRotatingLeft => IsKeyPressed(Key.A);
    public bool IsRotatingRight => IsKeyPressed(Key.D);
    public bool IsThrusting => IsKeyPressed(Key.W);
    public bool IsShooting => IsKeyPressed(Key.Space);
}
