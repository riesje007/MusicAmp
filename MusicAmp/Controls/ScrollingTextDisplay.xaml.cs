using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MusicAmp.Controls
{
    /// <summary>
    /// Interaction logic for ScrollingTextDisplay.xaml
    /// </summary>
    public partial class ScrollingTextDisplay : UserControl
    {
        private Storyboard? _scrollingStoryboard = null;
        public ScrollingTextDisplay()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ScrollingTextDisplay), new PropertyMetadata(string.Empty));

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, value); }
        }

        private void ScrollingTetDisplaySizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipGeometry.Rect = new Rect(0, 0, RootGrid.ActualWidth, RootGrid.ActualHeight);
            UpdateScrolling();
        }

        private void UpdateScrolling()
        {
            if (string.IsNullOrEmpty(DisplayText))
            {
                StopScrolling();
                TitleBoxTransform.X = 0;
                return;
            }


            // Force measure to get actual text width
            TitleBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            TitleBox.Width = TitleBox.DesiredSize.Width;
            double textWidth = TitleBox.DesiredSize.Width;
            double controlWidth = RootGrid.ActualWidth;

            if (textWidth <= controlWidth)
            {
                StopScrolling();
                TitleBoxTransform.X = 0;
                return;
            }

            StartScrolling(textWidth, controlWidth);
        }

        private void StartScrolling(double textWidth, double controlWidth)
        {
            StopScrolling();

            double offset = controlWidth - textWidth;
            double targetSpeed = 30.0;      // pixels per second
            double duration = Math.Abs(offset / targetSpeed);
            var ease = new SineEase() { EasingMode = EasingMode.EaseInOut };

            var animation = new DoubleAnimationUsingKeyFrames()
            {
                KeyFrames =
                {
                    new LinearDoubleKeyFrame() { Value = 0, KeyTime = TimeSpan.FromSeconds(0) },
                    new EasingDoubleKeyFrame() { Value = offset, KeyTime = TimeSpan.FromSeconds(duration), EasingFunction = ease },
                    new LinearDoubleKeyFrame() { Value = offset, KeyTime = TimeSpan.FromSeconds(duration + 1) },
                    new EasingDoubleKeyFrame() { Value = 0, KeyTime = TimeSpan.FromSeconds(2 * duration + 1), EasingFunction = ease },
                    new LinearDoubleKeyFrame() { Value = 0, KeyTime = TimeSpan.FromSeconds(2 * duration + 2) }
                }
            };

            Storyboard.SetTarget(animation, TitleBoxTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath("X"));

            _scrollingStoryboard = new Storyboard()
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            _scrollingStoryboard.Children.Add(animation);
            _scrollingStoryboard.Begin();
        }

        private void StopScrolling()
        {
            _scrollingStoryboard?.Stop();
            _scrollingStoryboard = null;
        }

        public void Dispose()
        {
            if (_scrollingStoryboard is not null)
                StopScrolling();
        }
    }
}
