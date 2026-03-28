using Auris_Studio.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views
{
    public partial class TrackListView : UserControl
    {
        private MidiEditorViewModel? _currentVm;

        public TrackListView()
        {
            InitializeComponent();
            DataContextChanged += TrackListView_DataContextChanged;
            Loaded += TrackListView_Loaded;
        }

        private void TrackListView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshScrollMetrics();
        }

        private void TrackListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentVm is not null)
            {
                _currentVm.Tracks.CollectionChanged -= Tracks_CollectionChanged;
            }

            _currentVm = e.NewValue as MidiEditorViewModel;
            if (_currentVm is not null)
            {
                _currentVm.Tracks.CollectionChanged += Tracks_CollectionChanged;
                RefreshScrollMetrics();
            }

            VerticalScrollBar.SetValueSafely(value: 0);
        }

        private void Tracks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshScrollMetrics();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.Tracks.Add(new MidiTrackViewModel()
                {
                    Name = "Acoustic Grand Piano",
                    Channel = 1,
                });
                RefreshScrollMetrics();
            }
        }

        private void VerticalScrollBar_OffsetChanged(object sender, double e)
        {
            TracksViewer.ScrollToVerticalOffset(e);
        }

        private void TracksViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.TrackListViewportHeight = e.NewSize.Height;
            }
            RefreshScrollMetrics();
        }

        private void RefreshScrollMetrics()
        {
            if (DataContext is not MidiEditorViewModel vm)
            {
                return;
            }

            VerticalScrollBar.Maximum = vm.TrackListCanvasHeight;
            VerticalScrollBar.ViewportSize = vm.TrackListViewportHeight <= 0 ? TracksViewer.ViewportHeight : vm.TrackListViewportHeight;
        }

        private void TracksViewer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
