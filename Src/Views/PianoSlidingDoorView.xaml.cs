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
        }

        private void VerticalScrollBar_OffsetChanged(object? sender, double e)
        {
            NotesScrollViewer.ScrollToVerticalOffset(e);
            PianoKeysScrollViewer.ScrollToVerticalOffset(e);
            BackDrawLineScrollViewer.ScrollToVerticalOffset(e);
            TickScrollViewer.ScrollToVerticalOffset(e);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportTop = e;
            }
        }

        private void HorizontalScrollBar_OffsetChanged(object sender, double e)
        {
            NotesScrollViewer.ScrollToHorizontalOffset(e);
            PianoKeysScrollViewer.ScrollToHorizontalOffset(e);
            BackDrawLineScrollViewer.ScrollToHorizontalOffset(e);
            TickScrollViewer.ScrollToHorizontalOffset(e);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportLeft = e;
            }
        }

        private void NotesScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportLeft = HorizontalScrollBar.Offset;
                vm.ViewportTop = VerticalScrollBar.Offset;
                vm.ViewportWidth = e.NewSize.Width;
                vm.ViewportHeight = e.NewSize.Height;
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportLeft = HorizontalScrollBar.Offset;
                vm.ViewportTop = VerticalScrollBar.Offset;
                vm.ViewportWidth = HorizontalScrollBar.ActualWidth;
                vm.ViewportHeight = HorizontalScrollBar.ActualHeight;
            }
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(sender as Canvas);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.PointerLeft = point.X;
                vm.PointerTop = point.Y;
            }
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.HitTestCommand.Execute(null);
            }
        }

        private void Canvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.CapturedNote = null;
            }
        }

        private void TickChanged(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = e.GetPosition(sender as Canvas);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.MoveTickCommand.Execute(point.X);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}