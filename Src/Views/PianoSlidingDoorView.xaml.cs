using Auris_Studio.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views
{
    public partial class PianoSlidingDoorView : UserControl
    {
        public PianoSlidingDoorView()
        {
            InitializeComponent();

            // 这儿暂时找不出问题，只知道非得窗口尺寸变一下，获取到的Viewport相关信息才对
            DataContextChanged += PianoSlidingDoorView_DataContextChanged;
        }

        private void PianoSlidingDoorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Application.Current.MainWindow.Width += 1;
            Application.Current.MainWindow.Width -= 1;
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
               e.NewValue is double value &&
               view.DataContext is MidiEditorViewModel vm)
            {
                view.NotesScrollViewer.ScrollToHorizontalOffset(value);
                view.TopCuttingLinesScrollViewer.ScrollToHorizontalOffset(value);
                view.CenterCuttingLinesScrollViewer.ScrollToHorizontalOffset(value);
                view.HorizontalViewportWidth = view.NotesScrollViewer.ViewportWidth;
                vm.ViewportLeft = value;
            }
        }

        private void NotesScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                HorizontalViewportWidth = NotesScrollViewer.ViewportWidth;
                vm.ViewportWidth = e.NewSize.Width;
            }
        }
    }
}
