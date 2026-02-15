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
        }

        public static readonly DependencyProperty NowPlayingProperty = DependencyProperty.Register(nameof(NowPlaying), typeof(PlaylistItem), typeof(MainWindow), new PropertyMetadata(new PlaylistItem(0, "Please open an audio file for playback", 0, new Uri("", UriKind.Relative))));

        public PlaylistItem NowPlaying
        {
            get { return (PlaylistItem)GetValue(NowPlayingProperty); }
            set { SetValue(NowPlayingProperty, value); }

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

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnRadioSwitch(object sender, RoutedEventArgs e)
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

                    // ToDo: trigger play function of player
                }
                else if (rb == PauseBtn)
                {
                    StopBtn.IsEnabled = true;
                    PauseBtn.IsEnabled = true;
                    PlayBtn.IsChecked = false;
                    PauseBtn.IsChecked = true;
                    StopBtn.IsChecked = false;
                    // ToDo: trigger pause function of player
                }
                else if (rb == StopBtn)
                {
                    StopBtn.IsEnabled = false;
                    PauseBtn.IsEnabled = false;
                    PlayBtn.IsChecked = false;
                    PauseBtn.IsChecked = false;
                    StopBtn.IsChecked = true;
                    // ToDo: trigger stop function of player
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
    }
}