using PlaylistEditing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Player;
using System.Windows.Media.Animation;
using NAudio.Wave;

namespace MusicAmp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicPlayer musicPlayer = new MusicPlayer();
        private double _nonFocusOpacity = 0.2;
        private double _focusOpacity = 1.0;
        private double _onHoverOpacity = 0.8;
        private DoubleAnimation? _windowAnimation;
        private Storyboard? _storyboard;
        private bool _isActive = false;
        private bool _isUserSeeking = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            NowPlaying = new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative));
            PlaylistControl.PlaylistItemDoubleClicked += PlaylistControl_ItemDoubleClicked;
            PlaylistControl.NoContent += OnNoPlayableContent;
            PlaylistControl.ContentLoaded += OnPlayableContent;
            musicPlayer.PositionChanged += OnPositionChanged;
            musicPlayer.ErrorOccurred += OnErrorOccurred;
            musicPlayer.PlayableSong += OnPlayableSongSelected;
            PlaylistControl.NewSelection += OnNewSelection;
            PresetAnimations();
            KeyboardMediaHookService.NextButtonPress += OnNextPress;
            KeyboardMediaHookService.PrevButtonPress += OnPreviousPress;
            KeyboardMediaHookService.PlayButtonPress += OnPlayPausePress;
            KeyboardMediaHookService.StopButtonPress += OnStopPress;
            KeyboardMediaHookService.StartHook();
            musicPlayer.EndOfSongReached += OnEndfOfSongReached;
        }

        public static readonly DependencyProperty NowPlayingProperty = DependencyProperty.Register(nameof(NowPlaying), typeof(PlaylistItem), typeof(MainWindow), new PropertyMetadata(new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative))));
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(nameof(Volume), typeof(double), typeof(MainWindow), new PropertyMetadata(0.2));

        public PlaylistItem NowPlaying
        {
            get { return (PlaylistItem)GetValue(NowPlayingProperty); }
            set
            {
                SetValue(NowPlayingProperty, value);
                VolumeControl.IsEnabled = true;
                TrackSlider.Value = 0;
                if (value.IsStream)
                {
                    TrackSlider.IsEnabled = false;
                }
                else
                {
                    TrackSlider.IsEnabled = true;
                    TrackSlider.Minimum = 0;
                    TrackSlider.Maximum = value.SongDurationSeconds;
                }
            }

        }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        /******************* Private fields, properties, and methods *******************/
        private void OnPositionChanged(object? sender, TimeSpan? position)
        {
            if (!position.HasValue)
                return;

            // Ensure UI updates happen on the UI thread
            Dispatcher.BeginInvoke(() =>
            {
                SongDisplayControl.PositionInSeconds = (int)position.Value.TotalSeconds;
                SongDisplayControl.UpdateTimeDisplay();
                // Only update the slider when the user is not actively dragging it
                if (!_isUserSeeking)
                {
                    // Ensure value is within bounds
                    double secs = position.Value.TotalSeconds;
                    if (secs < TrackSlider.Minimum) secs = TrackSlider.Minimum;
                    if (secs > TrackSlider.Maximum) secs = TrackSlider.Maximum;
                    TrackSlider.Value = secs;
                }
            });
        }


        private void TrackSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isUserSeeking = true;
        }

        private void TrackSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isUserSeeking = false;
            // Seek to the requested position
            musicPlayer.Seek(TimeSpan.FromSeconds(TrackSlider.Value));
        }

        private void TrackSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Handle clicks on the slider track (not drags)
            if (!_isUserSeeking)
            {
                musicPlayer.Seek(TimeSpan.FromSeconds(TrackSlider.Value));
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void CloseWindow(object sender, RoutedEventArgs e)
        {
            KeyboardMediaHookService.NextButtonPress -= OnNextPress;
            KeyboardMediaHookService.PrevButtonPress -= OnPreviousPress;
            KeyboardMediaHookService.PlayButtonPress -= OnPlayPausePress;
            KeyboardMediaHookService.StopButtonPress -= OnStopPress;
            KeyboardMediaHookService.StopHook();
            var fadeTask = FadeWindow();
            await musicPlayer.Stop();
            musicPlayer.EndOfSongReached -= OnEndfOfSongReached;
            await fadeTask;
            Application.Current.Shutdown();
        }

        private async Task FadeWindow()
        {
            _storyboard?.Stop();
            _windowAnimation = null;
            _storyboard = null;
            Activated -= OnFocus;
            Deactivated -= OnFocusLost;
            MouseEnter -= OnHover;
            MouseLeave -= OnMouseLeave;
            for (int i = 100; i > 0; i--)
            {
                double opacity = (double)i / 100.0;
                Opacity = opacity;
                await Task.Delay(20);
            }
        }

        private async Task StartMusicPlayer()
        {
            if ((double)musicPlayer.Volume != VolumeControl.Value)
                musicPlayer.Volume = (float)VolumeControl.Value;
            if (NowPlaying != musicPlayer.CurrentSong)
                await musicPlayer.ChangeSong(NowPlaying);
            await musicPlayer.Play(VolumeControl.Value);
        }

        private async void OnRadioSwitch(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                if (rb == PlayBtn)
                {
                    if (musicPlayer.PlaybackStatus == PlaybackState.Playing)
                    {
                        PauseBtn.IsChecked = true;
                        await musicPlayer.Pause();
                    }
                    else
                    {
                        PlayBtn.IsChecked = true;
                        await StartMusicPlayer();
                    }
                }

                if (rb == PauseBtn)
                {
                    if (musicPlayer.PlaybackStatus == PlaybackState.Paused)
                    {
                        PlayBtn.IsChecked = true;
                        await StartMusicPlayer();
                    }
                    else if (musicPlayer.PlaybackStatus == PlaybackState.Playing)
                    {
                        PauseBtn.IsChecked = true;
                        await musicPlayer.Pause();
                    }
                }

                if (rb == StopBtn)
                {
                    if (musicPlayer.PlaybackStatus != PlaybackState.Stopped)
                    {
                        StopBtn.IsChecked = true;
                        await musicPlayer.Stop();
                    }
                }
            }
        }

        private async void PlaylistControl_ItemDoubleClicked(object? sender, PlaylistItem item)
        {
            NowPlaying = item;
            await musicPlayer.ChangeSong(NowPlaying);
            if (!musicPlayer.IsPlaying)
                OnRadioSwitch(PlayBtn, new RoutedEventArgs());
        }

        private void VolumeChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            musicPlayer.Volume = (float)e.NewValue;
            SongDisplayControl.Volume = e.NewValue;
        }


        private async void OnNextSong(object? sender, RoutedEventArgs e)
        {
            await NextSong();
        }

        private async void OnPreviousSong(object? sender, RoutedEventArgs e)
        {
            await PreviousSong();
        }

        private async Task NextSong()
        {
            var item = PlaylistControl.SelectNext();
            if (item is not null && item != NowPlaying)
            {
                NowPlaying = item;
                await musicPlayer.ChangeSong(NowPlaying);
                if (PlayBtn.IsChecked == true)
                    await StartMusicPlayer();
            }
        }

        private async Task PreviousSong()
        {
            if (SongDisplayControl.PositionInSeconds >= 5 && !NowPlaying.IsStream)
            {
                musicPlayer.Seek(TimeSpan.Zero);
                return;
            }
            var item = PlaylistControl.SelectPrevious();
            if (item is not null && item != NowPlaying)
            {
                NowPlaying = item;
                await musicPlayer.ChangeSong(NowPlaying);
                if (PlayBtn.IsChecked == true)
                    await StartMusicPlayer();
            }
        }

        private void OnErrorOccurred(object? sender, bool StopRequired)
        {
            if (StopRequired)
            {
                StopBtn.IsChecked = true;
                PlayBtn.IsEnabled = false;
                PauseBtn.IsEnabled = false;
            }
        }

        private void OnPlayableSongSelected(object? sender, bool isPlayable)
        {
            if (isPlayable)
            {
                PlayBtn.IsEnabled = true;
                PauseBtn.IsEnabled = true;
            }
        }

        private void OnNewSelection(object? sender, PlaylistItem? item)
        {
            if (item == NowPlaying)
                return;

            if (item is null)
            {
                NowPlaying = new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative));
                PlayBtn.IsEnabled = false;
                PauseBtn?.IsEnabled = false;
            }
            else if (!musicPlayer.IsPlaying)
            {
                PlayBtn.IsEnabled = true;
                PauseBtn?.IsEnabled = true;
                NowPlaying = item;
            }
        }

        private void PresetAnimations()
        {
            _windowAnimation = new DoubleAnimation();
            Storyboard.SetTarget(_windowAnimation, this);
            Storyboard.SetTargetProperty(_windowAnimation, new PropertyPath("Opacity"));
            _storyboard = new Storyboard();
            _storyboard.Children.Add(_windowAnimation);
        }

        private void OnFocus(object? sender, EventArgs e)
        {
            _storyboard!.Stop();
            _isActive = true;
            _windowAnimation!.To = _focusOpacity;
            _windowAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
            _storyboard!.Begin();
        }

        private async void OnFocusLost(object? sender, EventArgs e)
        {
            _storyboard!.Stop();
            _isActive = false;
            await Task.Delay(IsMouseOver ? 0 : 2500);
            _windowAnimation!.To = IsMouseOver ? _onHoverOpacity : _nonFocusOpacity;
            _windowAnimation.Duration = new Duration(TimeSpan.FromSeconds(IsMouseOver ? 0.3 : 2.5));
            _storyboard!.Begin();
        }

        private void OnHover(object? sender, RoutedEventArgs e)
        {
            _storyboard!.Stop();
            if (_isActive)
                return;
            _windowAnimation!.To = _onHoverOpacity;
            _windowAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            _storyboard!.Begin();
        }

        private void OnMouseLeave(object? sender, RoutedEventArgs e)
        {
            _storyboard!.Stop();
            _windowAnimation!.To = IsActive ? _focusOpacity : _nonFocusOpacity;
            _windowAnimation.Duration = new Duration(TimeSpan.FromSeconds(_isActive ? 0.2 : 1.5));
            _storyboard!.Begin();
        }

        private async void OnPlayPausePress(object? sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    OnRadioSwitch(StopBtn, new RoutedEventArgs());
                else
                    OnRadioSwitch(PlayBtn, new RoutedEventArgs());
            });
        }

        private async void OnNextPress(object? sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => OnNextSong(NextBtn, new RoutedEventArgs()));
        }

        private async void OnPreviousPress(object? sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => OnPreviousSong(PrevBtn, new RoutedEventArgs()));
        }

        private async void OnStopPress(object? sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => OnRadioSwitch(StopBtn, new RoutedEventArgs()));
        }

        private async void OnEndfOfSongReached(object? sender, EventArgs e)
        {
            var lastPlaying = Application.Current.Dispatcher.Invoke(() => NowPlaying);
            NowPlaying = PlaylistControl.SelectNext() ?? new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative));
            await musicPlayer.ChangeSong(NowPlaying);
        }

        private void OnNoPlayableContent(object? sender, EventArgs e)
        {
            PlayBtn.IsEnabled = false;
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;

            if (musicPlayer.IsPlaying || musicPlayer.CurrentSong?.FileInformation is not null || !string.IsNullOrEmpty(musicPlayer.CurrentSong?.StreamUri?.OriginalString))
            {
                PauseBtn.IsEnabled = true;
                StopBtn.IsEnabled = true;
            }
        }

        private void OnPlayableContent(object? sender, EventArgs e)
        {
            PlayBtn.IsEnabled = true;
            PauseBtn.IsEnabled = true;
            StopBtn.IsEnabled = true;
        }
    }
}