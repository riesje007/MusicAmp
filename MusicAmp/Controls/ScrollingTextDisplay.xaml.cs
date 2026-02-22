using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MusicAmp.Controls
{
    /// <summary>
    /// Interaction logic for ScrollingTextDisplay.xaml
    /// </summary>
    public partial class ScrollingTextDisplay : UserControl
    {
        private Storyboard? _scrollingStoryboard = null;
        private DoubleAnimationUsingKeyFrames? animation;
        public ScrollingTextDisplay()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ScrollingTextDisplay), new PropertyMetadata(string.Empty, DisplayTextUpdated));

        private static void DisplayTextUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollingTextDisplay control)
            {
                // Ensure scrolling is updated on the UI thread after layout has a chance to run
                control.Dispatcher.BeginInvoke(() => control.ScrollingTextDisplaySizeChanged(control, new RoutedEventArgs() as SizeChangedEventArgs));
            }
        }

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, value); }
        }

        private void ScrollingTextDisplaySizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipGeometry.Rect = new Rect(0, 0, RootGrid.ActualWidth, RootGrid.ActualHeight);
            UpdateScrolling();
        }

        private void UpdateScrolling()
        {
            StopScrolling();
            if (string.IsNullOrEmpty(DisplayText))
            {
                StopScrolling();
                TitleBoxTransform.X = 0;
                return;
            }


            // Force measure to get actual text width
            var ft = new FormattedText(TitleBox.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(TitleBox.FontFamily, TitleBox.FontStyle, TitleBox.FontWeight, TitleBox.FontStretch),
                TitleBox.FontSize, TitleBox.Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            TitleBox.Width = ft.WidthIncludingTrailingWhitespace + 10;
            TitleBox.Height = ft.Height;
            double textWidth = ft.WidthIncludingTrailingWhitespace + 10;
            double controlWidth = RootGrid.ActualWidth;

            // Center vertically within the control by positioning the TextBlock on the Canvas
            double titleHeight = TitleBox.DesiredSize.Height;
            double hostHeight = RootGrid.ActualHeight;
            double top = Math.Max(0, (hostHeight - titleHeight) / 2.0);
            Canvas.SetTop(TitleBox, top);

            // Make the Canvas wide enough to contain the full text so that when the Canvas is
            // translated the TextBlock isn't clipped by the Canvas bounds. The Grid's ClipGeometry
            // still limits what is visible to the control area.
            TextHost.Width = Math.Max(textWidth, controlWidth);

            if (textWidth <= controlWidth)
            {
                StopScrolling();
                TitleBoxTransform.X = 0;
                // Ensure TextHost fills the control when no scrolling is required
                TextHost.Width = controlWidth;
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

            animation = new DoubleAnimationUsingKeyFrames()
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

            // Target the TextBlock and animate its RenderTransform.X property — this resolves correctly in the namescope
            Storyboard.SetTarget(animation, TextHost);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            _scrollingStoryboard = new Storyboard()
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            _scrollingStoryboard.Children.Add(animation);
            // Begin the storyboard with the TitleBox as the containing object to ensure the property path resolves
            _scrollingStoryboard.Begin(TextHost, true);
        }

        private void StopScrolling()
        {
            _scrollingStoryboard?.Stop();
            if (animation is not null && _scrollingStoryboard is not null)
                _scrollingStoryboard?.Children?.Remove(animation);

            TitleBoxTransform.BeginAnimation(TranslateTransform.XProperty, null);
            TitleBoxTransform.X = 0;
            //TextHost.RenderTransform.Transform(new Point(0, 0));
            _scrollingStoryboard = null;
        }

        public void Dispose()
        {
            if (_scrollingStoryboard is not null)
                StopScrolling();
        }
    }
}
