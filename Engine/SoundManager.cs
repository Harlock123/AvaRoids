using System;
using System.IO;
using NetCoreAudio;

namespace AvaRoids.Engine;

public class SoundManager : IDisposable
{
    private readonly string _soundsPath;
    private Player? _thrusterPlayer;
    private bool _thrusterPlaying;

    public bool IsMuted { get; set; } = false;

    public SoundManager()
    {
        _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
    }

    public void PlayWeapon()
    {
        if (IsMuted) return;
        PlaySound("Weapon.wav");
    }

    public void PlayThruster()
    {
        if (IsMuted || _thrusterPlaying) return;

        _thrusterPlaying = true;
        PlayThrusterLoop();
    }

    private async void PlayThrusterLoop()
    {
        var path = Path.Combine(_soundsPath, "Thruster.wav");
        if (!File.Exists(path)) return;

        try
        {
            _thrusterPlayer = new Player();

            // Loop the thruster sound while _thrusterPlaying is true
            while (_thrusterPlaying)
            {
                await _thrusterPlayer.Play(path);

                // Wait for playback to complete before looping
                while (_thrusterPlayer.Playing && _thrusterPlaying)
                {
                    await System.Threading.Tasks.Task.Delay(50);
                }
            }
        }
        catch
        {
            // Silently ignore playback errors
        }
    }

    public void StopThruster()
    {
        _thrusterPlaying = false;
        try
        {
            _thrusterPlayer?.Stop();
        }
        catch
        {
            // Silently ignore stop errors
        }
    }

    public void PlayExplosion()
    {
        if (IsMuted) return;
        PlaySound("Explosion1.wav");
    }

    public void PlaySaucerAlert()
    {
        if (IsMuted) return;
        PlaySound("SaucerAlert.wav");
    }

    private async void PlaySound(string filename)
    {
        var path = Path.Combine(_soundsPath, filename);
        if (!File.Exists(path)) return;

        try
        {
            // Create new player for each sound to allow overlapping
            var player = new Player();
            await player.Play(path);
        }
        catch
        {
            // Silently ignore playback errors
        }
    }

    public void Dispose()
    {
        StopThruster();
    }
}
