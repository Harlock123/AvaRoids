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

    // Must keep a reference to prevent garbage collection of the callback delegate
    private SDL.SDL_AudioCallback? _audioCallback;

    private byte[]? _weaponData;
    private byte[]? _thrusterData;
    private byte[]? _explosionData;

    private readonly object _lock = new();
    private readonly List<SoundInstance> _playingSounds = new();
    private bool _thrusterPlaying;

    private int _callbackCount;
    private const int TARGET_FREQ = 44100;
    private const int TARGET_CHANNELS = 2;

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
                Console.WriteLine($"SDL_Init failed: {SDL.SDL_GetError()}");
                return;
            }

            // List available audio devices for debugging
            int numDevices = SDL.SDL_GetNumAudioDevices(0);
            Console.WriteLine($"Available audio devices: {numDevices}");
            for (int i = 0; i < numDevices; i++)
            {
                Console.WriteLine($"  Device {i}: {SDL.SDL_GetAudioDeviceName(i, 0)}");
            }

            _audioCallback = AudioCallback;

            _audioSpec = new SDL.SDL_AudioSpec
            {
                freq = 44100,
                format = SDL.AUDIO_S16LSB,
                channels = 2,
                samples = 4096,
                callback = _audioCallback
            };

            // Try to get the device name, use null if not available
            string? deviceName = null;
            if (numDevices > 0)
            {
                deviceName = SDL.SDL_GetAudioDeviceName(0, 0);
                Console.WriteLine($"Using audio device: {deviceName ?? "default"}");
            }

            // Use SDL_OpenAudioDevice for better device control
            SDL.SDL_AudioSpec obtained;
            _audioDevice = SDL.SDL_OpenAudioDevice(
                deviceName,
                0,     // Not a capture device
                ref _audioSpec,
                out obtained,
                0      // No allowed changes
            );

            if (_audioDevice == 0)
            {
                Console.WriteLine($"SDL_OpenAudioDevice failed: {SDL.SDL_GetError()}");
                // Fall back to SDL_OpenAudio
                Console.WriteLine("Trying SDL_OpenAudio fallback...");
                if (SDL.SDL_OpenAudio(ref _audioSpec, out obtained) < 0)
                {
                    Console.WriteLine($"SDL_OpenAudio also failed: {SDL.SDL_GetError()}");
                    return;
                }
                _audioDevice = 1; // Legacy API uses device ID 1
            }

            Console.WriteLine($"Opened audio device ID: {_audioDevice}");
            Console.WriteLine($"Audio spec - freq: {obtained.freq}, format: {obtained.format}, channels: {obtained.channels}, samples: {obtained.samples}");

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string soundsPath = Path.Combine(basePath, "Sounds");
            Console.WriteLine($"Loading sounds from: {soundsPath}");

            _weaponData = LoadWavFile(Path.Combine(soundsPath, "Weapon.wav"));
            _thrusterData = LoadWavFile(Path.Combine(soundsPath, "Thruster.wav"));
            _explosionData = LoadWavFile(Path.Combine(soundsPath, "Explosion1.wav"));

            // Unpause the audio device to start playback
            if (_audioDevice > 1)
                SDL.SDL_PauseAudioDevice(_audioDevice, 0);
            else
                SDL.SDL_PauseAudio(0);
            _initialized = true;
            Console.WriteLine("Audio initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio initialization error: {ex.Message}");
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

            Console.WriteLine($"Loaded {Path.GetFileName(path)}: {length} bytes, freq={spec.freq}, format={spec.format}, channels={spec.channels}");

            byte[] rawData = new byte[length];
            Marshal.Copy(buffer, rawData, 0, (int)length);
            SDL.SDL_FreeWAV(buffer);

            // Convert to target format (44100 Hz, stereo, 16-bit)
            byte[] converted = ConvertAudio(rawData, spec.freq, spec.channels, TARGET_FREQ, TARGET_CHANNELS);
            Console.WriteLine($"  Converted to: {converted.Length} bytes, freq={TARGET_FREQ}, channels={TARGET_CHANNELS}");

            return converted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {path}: {ex.Message}");
            return null;
        }
    }

    private byte[] ConvertAudio(byte[] input, int srcFreq, int srcChannels, int dstFreq, int dstChannels)
    {
        // Input is 16-bit samples
        int srcSampleCount = input.Length / 2 / srcChannels;

        // Calculate output sample count based on frequency ratio
        int dstSampleCount = (int)((long)srcSampleCount * dstFreq / srcFreq);

        byte[] output = new byte[dstSampleCount * 2 * dstChannels];

        for (int i = 0; i < dstSampleCount; i++)
        {
            // Calculate source position with linear interpolation
            double srcPos = (double)i * srcFreq / dstFreq;
            int srcIndex = (int)srcPos;
            double frac = srcPos - srcIndex;

            if (srcIndex >= srcSampleCount - 1)
                srcIndex = srcSampleCount - 2;
            if (srcIndex < 0)
                srcIndex = 0;

            // Get samples for interpolation
            short sample1, sample2;
            if (srcChannels == 1)
            {
                // Mono source
                int bytePos1 = srcIndex * 2;
                int bytePos2 = (srcIndex + 1) * 2;

                sample1 = (short)(input[bytePos1] | (input[bytePos1 + 1] << 8));
                sample2 = (short)(input[bytePos2] | (input[bytePos2 + 1] << 8));
            }
            else
            {
                // Stereo source - just take left channel for simplicity
                int bytePos1 = srcIndex * 4;
                int bytePos2 = (srcIndex + 1) * 4;

                sample1 = (short)(input[bytePos1] | (input[bytePos1 + 1] << 8));
                sample2 = (short)(input[bytePos2] | (input[bytePos2 + 1] << 8));
            }

            // Interpolate
            short sample = (short)(sample1 + (sample2 - sample1) * frac);

            // Write to output (stereo - same sample to both channels)
            int outPos = i * 4; // 2 bytes per sample * 2 channels
            output[outPos] = (byte)(sample & 0xFF);
            output[outPos + 1] = (byte)((sample >> 8) & 0xFF);
            output[outPos + 2] = (byte)(sample & 0xFF);
            output[outPos + 3] = (byte)((sample >> 8) & 0xFF);
        }

        return output;
    }

    private void AudioCallback(IntPtr userdata, IntPtr stream, int len)
    {
        _callbackCount++;
        if (_callbackCount <= 5 || _callbackCount % 100 == 0)
        {
            Console.WriteLine($"AudioCallback called #{_callbackCount}, len={len}, playing={_playingSounds.Count}");
        }

        // Fill with silence first
        byte[] silence = new byte[len];
        Marshal.Copy(silence, 0, stream, len);

        lock (_lock)
        {
            if (_playingSounds.Count == 0)
                return;

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
            Console.WriteLine("PlayWeapon called");
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
            Console.WriteLine("PlayExplosion called");
            lock (_lock)
            {
                _playingSounds.Add(new SoundInstance(_explosionData, 100));
            }
        }
    }

    public void Dispose()
    {
        if (_audioDevice > 1)
        {
            SDL.SDL_CloseAudioDevice(_audioDevice);
        }
        else if (_audioDevice == 1)
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
