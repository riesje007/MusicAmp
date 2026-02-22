using PlaylistEditing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Player;

namespace MusicAmp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicPlayer musicPlayer = new MusicPlayer();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            NowPlaying = new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative));
            PlaylistControl.PlaylistItemDoubleClicked += PlaylistControl_ItemDoubleClicked;
            musicPlayer.PositionChanged += OnPositionChanged;
            musicPlayer.ErrorOccurred += OnErrorOccurred;
            musicPlayer.PlayableSong += OnPlayableSongSelected;
            PlaylistControl.NewSelection += OnNewSelection;
        }

        public static readonly DependencyProperty NowPlayingProperty = DependencyProperty.Register(nameof(NowPlaying), typeof(PlaylistItem), typeof(MainWindow), new PropertyMetadata(new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative))));
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(nameof(Volume), typeof(double), typeof(MainWindow), new PropertyMetadata(0.2));

        private void OnPositionChanged(object? sender, TimeSpan? position)
        {
            if (position.HasValue)
            {
                SongDisplayControl.PositionInSeconds = (int)position.Value.TotalSeconds;
                SongDisplayControl.UpdateTimeDisplay();
            }
        }   

        public PlaylistItem NowPlaying
        {
            get { return (PlaylistItem)GetValue(NowPlayingProperty); }
            set 
            { 
                SetValue(NowPlayingProperty, value); 
                VolumeControl.IsEnabled = true;
            }

        }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        /******************* Private fields, properties, and methods *******************/

        private bool suppressClick = false;

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
            _ = FadeWindow();
            await musicPlayer.Stop();
            Application.Current.Shutdown();
        }

        private async Task FadeWindow()
        {
            for (int i = 100; i > 0; i--)
            {
                double opacity = (double)i / 100.0;
                this.Opacity = opacity;
                await Task.Delay(20);
            }
        }

        private async Task StartMusicPlayer()
        {
            if ((double)musicPlayer.Volume != VolumeControl.Value)
                musicPlayer.Volume = (float)VolumeControl.Value;
            await musicPlayer.Play(VolumeControl.Value);
        }

        private async void OnRadioSwitch(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                suppressClick = true;
                if (rb == PlayBtn)
                {
                    StopBtn.IsEnabled = true;
                    PauseBtn.IsEnabled = true;
                    PlayBtn.IsChecked = true;
                    PauseBtn.IsChecked = false;
                    StopBtn.IsChecked = false;

                    await StartMusicPlayer();
                }
                else if (rb == PauseBtn)
                {
                    StopBtn.IsEnabled = true;
                    PauseBtn.IsEnabled = true;
                    PlayBtn.IsChecked = false;
                    PauseBtn.IsChecked = true;
                    StopBtn.IsChecked = false;
                    // ToDo: trigger pause function of player
                    await musicPlayer.Pause();
                }
                else if (rb == StopBtn)
                {
                    StopBtn.IsEnabled = false;
                    PauseBtn.IsEnabled = false;
                    PlayBtn.IsChecked = false;
                    PauseBtn.IsChecked = false;
                    StopBtn.IsChecked = true;
                    // ToDo: trigger stop function of player
                    await musicPlayer.Stop();
                }
            }
        }

        private void OffRadioSwitch(object sender, RoutedEventArgs e)
        {
            if (suppressClick)
            {
                suppressClick = false;
                return;
            }

            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                if (rb == PlayBtn)
                {
                    PauseBtn.IsChecked = true;
                    PlayBtn.IsChecked = false;
                    StopBtn.IsChecked = false;
                }
                else if (rb == PauseBtn)
                {
                    PlayBtn.IsChecked = true;
                    PauseBtn.IsChecked = false;
                    StopBtn.IsChecked = false;
                }
            }
        }

        private async void PlaylistControl_ItemDoubleClicked(object? sender, PlaylistItem item)
        {
            NowPlaying = item;
            musicPlayer.CurrentSong = NowPlaying;
            bool? prevState = PlayBtn.IsChecked;
            PlayBtn.IsChecked = true;
            if (prevState == true)
                await musicPlayer.Play();
            //OnRadioSwitch(PlayBtn, new RoutedEventArgs());
        }

        private void VolumeChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            musicPlayer.Volume = (float)e.NewValue;
            SongDisplayControl.Volume = e.NewValue;
        }

        private async void NextSong(object sender, RoutedEventArgs e)
        {
            var item = PlaylistControl.SelectNext();
            if (item is not null && item != NowPlaying)
            {
                NowPlaying = item;
                musicPlayer.CurrentSong = NowPlaying;
                if (PlayBtn.IsChecked == true)
                    await StartMusicPlayer();
            }
        }

        private async void PreviousSong(object sender, RoutedEventArgs e)
        {
            var item = PlaylistControl.SelectPrevious();
            if (item is not null && item != NowPlaying)
            {
                NowPlaying = item;
                musicPlayer.CurrentSong = NowPlaying;
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
            else
            {
                PlayBtn.IsEnabled = true;
                PauseBtn?.IsEnabled = true;
                NowPlaying = item;
            }
        }
    }
}