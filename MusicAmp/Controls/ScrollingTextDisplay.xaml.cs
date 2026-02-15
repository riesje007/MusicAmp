using System.ComponentModel;
using System.Diagnostics;
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

        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ScrollingTextDisplay), new PropertyMetadata(string.Empty, DisplayTextUpdated));

        private static void DisplayTextUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollingTextDisplay control)
            {
                // Ensure scrolling is updated on the UI thread after layout has a chance to run
                control.Dispatcher.BeginInvoke((System.Action)control.UpdateScrolling, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

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
            _scrollingStoryboard = null;
        }

        public void Dispose()
        {
            if (_scrollingStoryboard is not null)
                StopScrolling();
        }
    }
}
