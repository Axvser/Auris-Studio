using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views
{
    public partial class ControllerView : UserControl
    {
        public ControllerView()
        {
            InitializeComponent();
        }

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(ControllerView), new PropertyMetadata(0d));

        public double HorizontalViewportWidth
        {
            get { return (double)GetValue(HorizontalViewportWidthProperty); }
            set { SetValue(HorizontalViewportWidthProperty, value); }
        }
        public static readonly DependencyProperty HorizontalViewportWidthProperty =
            DependencyProperty.Register(nameof(HorizontalViewportWidth), typeof(double), typeof(ControllerView), new PropertyMetadata(0d));

        private void ScrollBar_OffsetChanged(object? sender, double e)
        {
            HorizontalOffset = e;
        }
    }
}
