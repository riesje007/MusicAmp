using NAudio.Wave;
using PlaylistEditing;

namespace Player
{
    public class MusicPlayer
    {
        private CancellationTokenSource? _cts;
        public event EventHandler<TimeSpan?>? PositionChanged;
        private float _lastVolume = 0.0f;
        public event EventHandler<bool>? ErrorOccurred;
        public event EventHandler<bool>? PlayableSong;
        private bool fetchingStream = false;

        public PlaylistItem? CurrentSong
        {
            get => field;
            set
            {
                field = value;
                _ = OnCurrentSongChange(value, new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
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

        private async Task StartTimer()
        {
            _cts = new CancellationTokenSource();
            while (_cts is not null && !_cts.Token.IsCancellationRequested)
            {
                TimeSpan position = (_reader?.CurrentTime ?? _streamReader?.CurrentTime) ?? TimeSpan.Zero;
                PositionChanged?.Invoke(this, position);
                try
                {
                    await Task.Delay(1000, _cts.Token);
                }
                catch { }
            }
        }

        private async Task OnCurrentSongChange(PlaylistItem? newSong, CancellationToken token)
        {
            await StopMediaPlayback(true);
            if (CurrentSong is not null && !token.IsCancellationRequested)
            {
                if (_output is null)
                    _output = new WaveOutEvent();

                if (CurrentSong.IsStream && !token.IsCancellationRequested)
                {
                    try
                    {
                        _streamReader = await Task.Run(() => new MediaFoundationReader(CurrentSong.StreamUri!.OriginalString), token);
                    }
                    catch 
                    { 
                        _streamReader = null; 
                        ErrorOccurred?.Invoke(this, true); 
                    }

                    if (_streamReader is not null)
                    {
                        _output!.Init(_streamReader);
                        PlayableSong?.Invoke(this, true);
                    }
                    else
                        ErrorOccurred?.Invoke(this, true);
                }
                else if (!token.IsCancellationRequested)
                {
                    _reader = new AudioFileReader(CurrentSong.FileInformation!.FullName);
                    if (_reader is not null)
                    {
                        _output!.Init(_reader);
                        PlayableSong?.Invoke(this, true);
                    }
                }
            }
        }

        private async Task Reset()
        {
            _reader?.Dispose();
            _reader = null;
            _streamReader?.Dispose();
            _streamReader = null;
        }

        private WaveOutEvent? _output;
        private AudioFileReader? _reader;
        private MediaFoundationReader? _streamReader;

        public MusicPlayer()
        {
            _output = new WaveOutEvent();
        }

        public async Task Play(double volume = 0.0)
        {
            if (CurrentSong is null || _output is null || fetchingStream)
                return;

            if (_reader is null && _streamReader is null)
                await OnCurrentSongChange(CurrentSong, new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            if (volume != 0.0)
                _lastVolume = (float)volume;
            else
                _lastVolume = Volume;
            _output.Volume = 0.0f;
            _output.Play();
            await Task.Delay(500);
            if (_cts is null)
                _ = StartTimer();
            if (_lastVolume > 0)
            {
                int fadeIn = 50;
                float curStep = _lastVolume / (float)fadeIn;
                for (int i = 0; i < fadeIn; i++)
                {
                    _output.Volume += Math.Min(curStep, _lastVolume - _output.Volume);
                    await Task.Delay(10);
                }
            }
            _lastVolume = 0.0f;
        }

        public async Task Stop()
        {
            _cts?.Cancel();
            await StopMediaPlayback();
            _cts = null;
        }

        private async Task StopMediaPlayback(bool noFadeOut = false)
        {
            if (CurrentSong is null)
                return;

            if (_output is not null && (_output.PlaybackState == PlaybackState.Playing || _output.PlaybackState == PlaybackState.Paused))
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
            await Reset();
        }

        public async Task Pause()
        {
            if (_output is null)
                return;

            if (_output.PlaybackState == PlaybackState.Playing)
                _output.Pause();
        }

        ~MusicPlayer()
        {
            _output?.Dispose();
            _reader?.Dispose();
            _streamReader?.Dispose();
        }
    }
}
