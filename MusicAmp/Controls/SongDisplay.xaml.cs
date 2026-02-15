using PlaylistEditing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicAmp.Controls
{
    /// <summary>
    /// Interaction logic for SongDisplay.xaml
    /// </summary>
    public partial class SongDisplay : UserControl
    {
        public static readonly DependencyProperty CurrentSongProperty = DependencyProperty.Register(nameof(CurrentSong), typeof(PlaylistItem), typeof(SongDisplay), new PropertyMetadata(null, OnSongChangedCallback));

        private static void OnSongChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SongDisplay sd)
            {
                var newSong = e.NewValue as PlaylistItem;
                if (newSong != null)
                {
                    sd.SongTitle = newSong.SongTitle;
                    sd.PositionInSeconds = 0;
                    sd.UpdateTimeDisplay();
                }
                else
                {
                    sd.SongTitle = string.Empty;
                    sd.PositionInSeconds = 0;
                    sd.UpdateTimeDisplay();
                }
            }
        }

        public static readonly DependencyProperty SongTitleProperty = DependencyProperty.Register(nameof(SongTitle), typeof(string), typeof(SongDisplay), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TimePlayingProperty = DependencyProperty.Register(nameof(TimePlaying), typeof(string), typeof(SongDisplay), new PropertyMetadata("00:00"));
        public static readonly DependencyProperty TimeRemainingProperty = DependencyProperty.Register(nameof(TimeRemaining), typeof(string), typeof(SongDisplay), new PropertyMetadata("00:00"));

        public PlaylistItem? CurrentSong
        {
            get { return (PlaylistItem?)GetValue(CurrentSongProperty); }
            set { SetValue(CurrentSongProperty, value); }
        }

        public SongDisplay()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string SongTitle
        {
            get { return (string)GetValue(SongTitleProperty); }
            set { SetValue(SongTitleProperty, value); }
        }

        public string TimePlaying
        {
            get { return (string)GetValue(TimePlayingProperty); }
            set { SetValue(TimePlayingProperty, value); }
        }

        public string TimeRemaining
        {
            get { return (string)GetValue(TimeRemainingProperty); }
            set { SetValue(TimeRemainingProperty, value); }
        }

        public int TotalTimeInSeconds => CurrentSong?.SongDurationSeconds ?? 0;
        public int PositionInSeconds { get; set; } = 0;
        public int RemainingSeconds { get => Math.Max(0, TotalTimeInSeconds - PositionInSeconds); }



        /**************** Private fields, properties, and methods ****************/
        private bool _remainingTimeShowing = false;

        private void UpdateTimeDisplay()
        {
            TimePlayingTextBlock.Text = Format(PositionInSeconds);
            TimeRemainingTextBlock.Text = $"- {Format(RemainingSeconds)}";
        }

        private string Format(int seconds)
        {
            var ts = TimeSpan.FromSeconds((double)seconds);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        private void TimeBox_LeftButtonPressed(object sender, MouseButtonEventArgs e)
        {
            _remainingTimeShowing = !_remainingTimeShowing;
            TimeRemainingTextBlock.Visibility = _remainingTimeShowing ? Visibility.Visible : Visibility.Collapsed;
            TimePlayingTextBlock.Visibility = _remainingTimeShowing ? Visibility.Collapsed : Visibility.Visible;
            UpdateTimeDisplay();
        }
    }
}
