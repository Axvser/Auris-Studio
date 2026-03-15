using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    public partial class PianoSlidingDoorView : UserControl
    {
        public PianoSlidingDoorView()
        {
            InitializeComponent();
        }

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(PianoSlidingDoorView), new PropertyMetadata(0d, OnHorizontalOffsetChanged));

        public double HorizontalViewportWidth
        {
            get { return (double)GetValue(HorizontalViewportWidthProperty); }
            set { SetValue(HorizontalViewportWidthProperty, value); }
        }
        public static readonly DependencyProperty HorizontalViewportWidthProperty =
            DependencyProperty.Register(nameof(HorizontalViewportWidth), typeof(double), typeof(PianoSlidingDoorView), new PropertyMetadata(0d));

        private void VerticalScrollBar_OffsetChanged(object? sender, double e)
        {
            NotesScrollViewer.ScrollToVerticalOffset(e);
            PianoKeysScrollViewer.ScrollToVerticalOffset(e);
            BackDrawLineScrollViewer.ScrollToVerticalOffset(e);
        }

        private static void OnHorizontalOffsetChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PianoSlidingDoorView view &&
               e.NewValue is double value)
            {
                view.NotesScrollViewer.ScrollToHorizontalOffset(value);
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HorizontalViewportWidth = NotesScrollViewer.ViewportWidth;
        }
    }
}
