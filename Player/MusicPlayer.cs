using NAudio.Wave;
using PlaylistEditing;
using System.ComponentModel;

namespace Player
{
    public class MusicPlayer
    {
        private CancellationTokenSource? _cts;
        public event EventHandler<TimeSpan?>? PositionChanged;

        public PlaylistItem? CurrentSong
        {
            get => field;
            set
            {
                field = value;
                _ = OnCurrentSongChange(value);
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
            while (!_cts.Token.IsCancellationRequested)
            {
                TimeSpan position = (_reader?.CurrentTime ?? _streamReader?.CurrentTime) ?? TimeSpan.Zero;
                PositionChanged?.Invoke(this, position);
                await Task.Delay(1000, _cts.Token);
            }
        }

        private async Task OnCurrentSongChange(PlaylistItem? newSong)
        {
            await StopMediaPlayback(true);
            if (CurrentSong is not null)
            {
                if (_output is null)
                    _output = new WaveOutEvent();

                if (CurrentSong.IsStream)
                {
                    _streamReader = new MediaFoundationReader(CurrentSong.StreamUri!.OriginalString);
                    _output!.Init(_streamReader);
                }
                else
                {
                    _reader = new AudioFileReader(CurrentSong.FileInformation!.FullName);
                    _output!.Init(_reader);
                }
            }

            await Play();
            await StartTimer();
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

        public async Task Play()
        {
            if (CurrentSong is null || _output is null)
                return;

            _output.Play();
        }

        public async Task Stop()
        {
            await StopMediaPlayback();
            _cts?.Cancel();
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
                    float curVol = _output.Volume;
                    int fadeOut = 250;
                    float curStep = curVol / (float)fadeOut;
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
