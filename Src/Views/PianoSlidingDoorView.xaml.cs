using Auris_Studio.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VeloxDev.Core.TimeLine;

namespace Auris_Studio.Views
{
    [MonoBehaviour]
    public partial class PianoSlidingDoorView : UserControl
    {
        private MidiEditorViewModel? _currentVm;

        public PianoSlidingDoorView()
        {
            InitializeComponent();
            MonoBehaviourManager.RegisterBehaviour(this);
            DataContextChanged += PianoSlidingDoorView_DataContextChanged;
            Loaded += PianoSlidingDoorView_Loaded;
            Unloaded += PianoSlidingDoorView_Unloaded;
        }

        ~PianoSlidingDoorView()
        {
            if (Application.Current?.MainWindow is Window mainWindow)
            {
                mainWindow.StateChanged -= MainWindow_StateChanged;
            }
        }

        private void PianoSlidingDoorView_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow is Window mainWindow)
            {
                mainWindow.StateChanged -= MainWindow_StateChanged;
                mainWindow.StateChanged += MainWindow_StateChanged;
            }

            ScheduleViewportSync();
        }

        private void PianoSlidingDoorView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow is Window mainWindow)
            {
                mainWindow.StateChanged -= MainWindow_StateChanged;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            SyncViewportMetrics();
        }

        private void PianoSlidingDoorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentVm is not null)
            {
                _currentVm.Tracks.CollectionChanged -= Tracks_CollectionChanged;
            }

            _currentVm = e.NewValue as MidiEditorViewModel;
            if (_currentVm is not null)
            {
                _currentVm.Tracks.CollectionChanged += Tracks_CollectionChanged;
                HorizontalScrollBar.SetValueSafely(value: 0);
                VerticalScrollBar.SetValueSafely(value: 0);
            }

            ScheduleViewportSync();
        }

        partial void Update(FrameEventArgs e)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (DataContext is MidiEditorViewModel vm && vm.ProgressFollow && vm.IsPlaying)
                {
                    double playheadPixelPosition = vm.NowTime * vm.WidthPerTick;
                    double targetOffset = playheadPixelPosition - (vm.ViewportWidth * 0.382);
                    double maxScrollableOffset = Math.Max(0d, vm.CanvasWidth - vm.ViewportWidth);
                    double clampedOffset = Math.Max(0, Math.Min(targetOffset, maxScrollableOffset));
                    HorizontalScrollBar.SetValueSafely(offset: clampedOffset, updateViewport: true);
                }
            });
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
            SyncViewportMetrics(e.NewSize.Width, e.NewSize.Height);
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncViewportMetrics();
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

        private void Canvas_MouseLeftDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.HitTestCommand.Execute(null);
            }
        }

        private void Canvas_MouseLeftUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private bool IsDragging { get; set; }

        private void Canvas_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsDragging = true;
        }

        private void Canvas_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsDragging = false;
        }

        private void Canvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IsDragging = false;
        }

        private void HorizontalScaleUp_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.WidthPerQuarterNote = Math.Clamp(vm.WidthPerQuarterNote + 10, 20, double.MaxValue);
            }
        }

        private void HorizontalScaleDown_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.WidthPerQuarterNote = Math.Clamp(vm.WidthPerQuarterNote - 10, 20, double.MaxValue);
            }
        }

        private void HorizontalScrollBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                if (vm.ProgressFollow && vm.IsPlaying) vm.StopCommand.Execute(null);
            }
        }

        private void NoteView_LayerChanged(object sender, EventArgs e)
        {
            NotesCanvas.InvalidateVisual();
        }

        private void Tracks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ScheduleViewportSync();
        }

        private void ScheduleViewportSync()
        {
            Dispatcher.BeginInvoke(SyncViewportMetrics, DispatcherPriority.Loaded);
        }

        private void SyncViewportMetrics()
        {
            double viewportWidth = NotesScrollViewer.ViewportWidth > 0 ? NotesScrollViewer.ViewportWidth : NotesScrollViewer.ActualWidth;
            double viewportHeight = NotesScrollViewer.ViewportHeight > 0 ? NotesScrollViewer.ViewportHeight : NotesScrollViewer.ActualHeight;
            SyncViewportMetrics(viewportWidth, viewportHeight);
        }

        private void SyncViewportMetrics(double viewportWidth, double viewportHeight)
        {
            if (DataContext is not MidiEditorViewModel vm)
            {
                return;
            }

            vm.ViewportLeft = HorizontalScrollBar.Offset;
            vm.ViewportTop = VerticalScrollBar.Offset;
            vm.ViewportWidth = viewportWidth > 0 ? viewportWidth : HorizontalScrollBar.ActualWidth;
            vm.ViewportHeight = viewportHeight > 0 ? viewportHeight : NotesScrollViewer.ActualHeight;
        }
    }
}