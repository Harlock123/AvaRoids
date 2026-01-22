using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SDL2;

namespace AvaRoids.Engine;

public class SoundManager : IDisposable
{
    private SDL.SDL_AudioSpec _audioSpec;
    private uint _audioDevice;
    private bool _initialized;

    private byte[]? _weaponData;
    private byte[]? _thrusterData;
    private byte[]? _explosionData;

    private readonly object _lock = new();
    private readonly List<SoundInstance> _playingSounds = new();
    private bool _thrusterPlaying;

    public SoundManager()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
            {
                return;
            }

            _audioSpec = new SDL.SDL_AudioSpec
            {
                freq = 44100,
                format = SDL.AUDIO_S16LSB,
                channels = 2,
                samples = 2048,
                callback = AudioCallback
            };

            if (SDL.SDL_OpenAudio(ref _audioSpec, out _) < 0)
            {
                return;
            }
            _audioDevice = 1;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string soundsPath = Path.Combine(basePath, "Sounds");

            _weaponData = LoadWavFile(Path.Combine(soundsPath, "Weapon.wav"));
            _thrusterData = LoadWavFile(Path.Combine(soundsPath, "Thruster.wav"));
            _explosionData = LoadWavFile(Path.Combine(soundsPath, "Explosion1.wav"));

            SDL.SDL_PauseAudio(0);
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    private byte[]? LoadWavFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Sound file not found: {path}");
                return null;
            }

            if (SDL.SDL_LoadWAV(path, out var spec, out var buffer, out var length) == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to load {path}: {SDL.SDL_GetError()}");
                return null;
            }

            byte[] data = new byte[length];
            Marshal.Copy(buffer, data, 0, (int)length);
            SDL.SDL_FreeWAV(buffer);

            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {path}: {ex.Message}");
            return null;
        }
    }

    private void AudioCallback(IntPtr userdata, IntPtr stream, int len)
    {
        byte[] silence = new byte[len];
        Marshal.Copy(silence, 0, stream, len);

        lock (_lock)
        {
            short[] mixBuffer = new short[len / 2];

            for (int i = _playingSounds.Count - 1; i >= 0; i--)
            {
                var sound = _playingSounds[i];
                int remaining = sound.Data.Length - sound.Position;
                int toCopy = Math.Min(remaining, len);

                for (int j = 0; j < toCopy / 2; j++)
                {
                    int srcIndex = sound.Position + j * 2;
                    if (srcIndex + 1 < sound.Data.Length)
                    {
                        short sample = (short)(sound.Data[srcIndex] | (sound.Data[srcIndex + 1] << 8));
                        int mixed = mixBuffer[j] + (sample * sound.Volume / 100);
                        mixBuffer[j] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
                    }
                }

                sound.Position += toCopy;

                if (sound.Position >= sound.Data.Length)
                {
                    if (sound.Loop)
                    {
                        sound.Position = 0;
                    }
                    else
                    {
                        _playingSounds.RemoveAt(i);
                    }
                }
            }

            byte[] output = new byte[len];
            Buffer.BlockCopy(mixBuffer, 0, output, 0, len);
            Marshal.Copy(output, 0, stream, len);
        }
    }

    public void PlayWeapon()
    {
        if (_initialized && _weaponData != null)
        {
            lock (_lock)
            {
                _playingSounds.Add(new SoundInstance(_weaponData, 80));
            }
        }
    }

    public void PlayThruster()
    {
        if (_initialized && _thrusterData != null && !_thrusterPlaying)
        {
            lock (_lock)
            {
                _thrusterPlaying = true;
                _playingSounds.Add(new SoundInstance(_thrusterData, 50) { Loop = true, Tag = "thruster" });
            }
        }
    }

    public void StopThruster()
    {
        lock (_lock)
        {
            _thrusterPlaying = false;
            _playingSounds.RemoveAll(s => s.Tag == "thruster");
        }
    }

    public void PlayExplosion()
    {
        if (_initialized && _explosionData != null)
        {
            lock (_lock)
            {
                _playingSounds.Add(new SoundInstance(_explosionData, 100));
            }
        }
    }

    public void Dispose()
    {
        if (_audioDevice != 0)
        {
            SDL.SDL_CloseAudio();
        }
        SDL.SDL_Quit();
    }

    private class SoundInstance
    {
        public byte[] Data { get; }
        public int Position { get; set; }
        public int Volume { get; }
        public bool Loop { get; set; }
        public string? Tag { get; set; }

        public SoundInstance(byte[] data, int volume = 100)
        {
            Data = data;
            Volume = volume;
            Position = 0;
        }
    }
}
