using NAudio.Wave;
using PlaylistEditing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Player
{
    public class MusicPlayer
    {
        /****************** public fields, properties and methods ******************/
        public MusicPlayer()
        {
            _output = new WaveOutEvent();
        }

        // Events
        public event EventHandler<TimeSpan?>? PositionChanged;
        public event EventHandler<bool>? ErrorOccurred;
        public event EventHandler<bool>? PlayableSong;
        public event EventHandler<EventArgs>? EndOfSongReached;

        public bool IsPlaying => _output is not null && _output.PlaybackState == PlaybackState.Playing && !_isStopping;
        public PlaybackState? PlaybackStatus => _output?.PlaybackState;

        public PlaylistItem? CurrentSong
        {
            get => _currentsong;
            set
            {
                _currentsong = value;
            }
        }

        public float Volume
        {
            get => _output?.Volume ?? 0.2f;
            set
            {
                if (_output != null)
                    _output.Volume = value;
            }
        }

        public void Seek(TimeSpan position)
        {
            if (_reader != null && _reader.CanSeek && position <= _reader.TotalTime)
            {
                _reader.CurrentTime = position;
                if (_output.PlaybackState == PlaybackState.Paused)
                    _pausedAt = position;
                PositionChanged?.Invoke(this, _reader.CurrentTime);
            }
            else if (_streamReader != null && _streamReader.CanSeek && position <= _streamReader.TotalTime)
            {
                _streamReader.CurrentTime = position;
                PositionChanged?.Invoke(this, _streamReader.CurrentTime);
            }
        }

        public async Task Play(double atVolume = 0.0)
        {
            if (CurrentSong is null || _fetchingStream)
                return;

            if (CurrentSong.FileInformation is null && (CurrentSong.StreamUri is null || string.IsNullOrEmpty(CurrentSong.StreamUri.OriginalString)))
                return;

            if (_reader is null && _streamReader is null)
                await OnCurrentSongChange(CurrentSong);

            if (_output.PlaybackState == PlaybackState.Paused && !CurrentSong.IsStream)
            {
                _reader?.CurrentTime = _pausedAt;
                _pausedAt = TimeSpan.Zero;
            }

            if (atVolume != 0.0f)
                _lastVolume = (float)atVolume;
            else
                _lastVolume = Volume;

            _output.Play();
            _lastVolume = 0.0f;

            if (_cts is null)
                _playingTask = StartTimer();
        }

        public async Task Pause()
        {
            if (!CurrentSong!.IsStream)
            {
                _pausedAt = _reader?.CurrentTime ?? TimeSpan.Zero;
                _lastVolume = Volume;
                int steps = 100;
                int interval = 10;
                float stepSize = (float)_lastVolume / (float)steps;
                for (int i = 0; i < steps; i++)
                {
                    if (_output.PlaybackState != PlaybackState.Playing)
                        break;
                    _output.Volume -= Math.Min(stepSize, _output.Volume);
                    await Task.Delay(interval);
                }
            }

            if (_output.PlaybackState == PlaybackState.Playing)
                _output.Pause();
        }

        public async Task Stop()
        {
            await StopMediaPlayback();
        }

        public Task ChangeSong(PlaylistItem? newSong)
        {
            _currentsong = newSong;
            return OnCurrentSongChange(newSong);
        }

        /****************** private fields, properties and methods ******************/
        private CancellationTokenSource? _cts;
        private float _lastVolume = 0.0f;
        private bool _fetchingStream = false;
        private bool _startOnNewSong = false;
        private WaveOutEvent _output;
        private AudioFileReader? _reader;
        private MediaFoundationReader? _streamReader;
        private Task? _playingTask;
        private PlaylistItem? _currentsong;
        private TimeSpan _pausedAt = TimeSpan.Zero;
        private bool _isStopping = false;

        private async Task StopMediaPlayback(bool noFadeOut = false)
        {
            if (CurrentSong is null)
                return;

            _isStopping = true;
            _cts?.Cancel();

            if (_output.PlaybackState != PlaybackState.Stopped)
            {
                if (!noFadeOut)
                {
                    _lastVolume = _output.Volume;
                    int fadeOut = 250;
                    float curStep = _lastVolume / (float)fadeOut;
                    for (int i = 0; i < fadeOut; i++)
                    {
                        _output.Volume -= Math.Min(curStep, _output.Volume);
                        await Task.Delay(10);
                    }
                }
                _output.Stop();
            }

            if (_playingTask is not null)
                await _playingTask;
            _playingTask = null;

            await Reset();
            _isStopping = false;
        }

        private async Task Reset()
        {
            _reader?.Dispose();
            _reader = null;
            _streamReader?.Dispose();
            _streamReader = null;
        }

        private async Task StartTimer()
        {
            _cts = new CancellationTokenSource();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            while (!_cts.Token.IsCancellationRequested)
            {
                TimeSpan position = (_reader?.CurrentTime ?? _streamReader?.CurrentTime) ?? TimeSpan.Zero;
                TimeSpan timePlaying = DateTimeOffset.UtcNow - now;
                PositionChanged?.Invoke(this, position);
                if (position > timePlaying)
                    now = DateTimeOffset.UtcNow.Add(-position);
                if (_reader is not null && (position >= _reader.TotalTime || timePlaying >= _reader.TotalTime))
                {
                    _cts.Cancel();
                    _startOnNewSong = true;
                    EndOfSongReached?.Invoke(this, EventArgs.Empty);
                }
                try
                {
                    await Task.Delay(400, _cts.Token);
                }
                catch { }
            }
            _cts = null;
        }

        private async Task OnCurrentSongChange(PlaylistItem? newSong)
        {
            if (IsPlaying)
                await StopMediaPlayback(true);
            if (CurrentSong is not null)
            {
                if (CurrentSong.IsStream)
                {
                    bool succeeded = false;
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            CancellationTokenSource mySource = new CancellationTokenSource(2000);
                            _streamReader = await Task.Run(async () => new MediaFoundationReader(CurrentSong.StreamUri!.OriginalString), mySource.Token);
                            succeeded = true;
                            break;
                        }
                        catch { _streamReader = null; }
                    }
                    if (!succeeded)
                        ErrorOccurred?.Invoke(this, true);

                    if (_streamReader is not null)
                    {
                        _output.Init(_streamReader);
                        PlayableSong?.Invoke(this, true);
                    }
                    else
                        ErrorOccurred?.Invoke(this, true);
                }

                else
                {
                    if (_isStopping)
                    {
                        DateTimeOffset switchTime = DateTimeOffset.UtcNow;
                        float lastVolume = _lastVolume;
                        while (DateTimeOffset.UtcNow - switchTime < TimeSpan.FromSeconds(3) && _isStopping)
                        {
                            await Task.Delay(100);
                        }
                        Volume = lastVolume;
                    }

                    _reader = new AudioFileReader(CurrentSong.FileInformation!.FullName);
                    if (_reader is not null)
                    {
                        _output.Init(_reader);
                        PlayableSong?.Invoke(this, true);
                    }
                }

                if (_startOnNewSong)
                {
                    _startOnNewSong = false;
                    if (_reader is not null || _streamReader is not null)
                        await Play(_lastVolume);
                }
            }
        }
    }
}
