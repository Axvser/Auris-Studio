using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Auris_Studio.Views
{
    public partial class ControlCurveEditorView : UserControl
    {
        private const double PointDiameter = 10d;
        private const double PointRadius = PointDiameter / 2d;
        private const double VerticalPadding = 8d;

        private MidiEditorViewModel? _viewModel;
        private TimeOrderableCollection<ControlChangeEventViewModel>? _currentCollection;
        private readonly Dictionary<ControlChangeEventViewModel, Ellipse> _pointElements = [];
        private ControlChangeEventViewModel? _draggingPoint;

        public ControlCurveEditorView()
        {
            InitializeComponent();
            DataContextChanged += ControlCurveEditorView_DataContextChanged;
            Loaded += ControlCurveEditorView_Loaded;
            Unloaded += ControlCurveEditorView_Unloaded;
        }

        public void SetHorizontalOffset(double offset)
        {
            EditorScrollViewer.ScrollToHorizontalOffset(offset);
            UpdateVirtualization();
        }

        public void RefreshViewport()
        {
            UpdateVirtualization();
            UpdateVisiblePointPositions();
        }

        private void ControlCurveEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshViewport();
            UpdateLaneButtonState();
        }

        private void ControlCurveEditorView_Unloaded(object sender, RoutedEventArgs e)
        {
            ReleaseDrag();
            UnsubscribeFromCollection();
            UnsubscribeFromViewModel(_viewModel);
        }

        private void ControlCurveEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromViewModel(e.OldValue as MidiEditorViewModel);
            _viewModel = e.NewValue as MidiEditorViewModel;
            SubscribeToViewModel(_viewModel);
            RefreshSelectedCollection();
            UpdateLaneButtonState();
            RefreshViewport();
        }

        private void SubscribeToViewModel(MidiEditorViewModel? viewModel)
        {
            if (viewModel is not null)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void UnsubscribeFromViewModel(MidiEditorViewModel? viewModel)
        {
            if (viewModel is not null)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MidiEditorViewModel.CurrentSelectedTrack)
                or nameof(MidiEditorViewModel.SelectedControlLane))
            {
                RefreshSelectedCollection();
                UpdateLaneButtonState();
                RefreshViewport();
                return;
            }

            if (e.PropertyName is nameof(MidiEditorViewModel.WidthPerTick)
                or nameof(MidiEditorViewModel.CanvasWidth))
            {
                RefreshViewport();
            }
        }

        private void RefreshSelectedCollection()
        {
            var nextCollection = _viewModel?.SelectedControlCollection;
            if (ReferenceEquals(_currentCollection, nextCollection))
            {
                CurveDecorator.ItemsSource = _currentCollection?.VisibleItems;
                return;
            }

            UnsubscribeFromCollection();
            _currentCollection = nextCollection;
            CurveDecorator.ItemsSource = _currentCollection?.VisibleItems;

            if (_currentCollection is not null)
            {
                _currentCollection.VisibleItems.CollectionChanged += OnCollectionChanged;
                foreach (var item in _currentCollection.VisibleItems)
                {
                    item.PropertyChanged += OnPointPropertyChanged;
                }
            }

            RebuildVisiblePoints();
        }

        private void UnsubscribeFromCollection()
        {
            if (_currentCollection is null)
            {
                PointsCanvas.Children.Clear();
                _pointElements.Clear();
                return;
            }

            _currentCollection.VisibleItems.CollectionChanged -= OnCollectionChanged;
            foreach (var item in _currentCollection.VisibleItems)
            {
                item.PropertyChanged -= OnPointPropertyChanged;
            }

            PointsCanvas.Children.Clear();
            _pointElements.Clear();
            _currentCollection = null;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildVisiblePoints();
                return;
            }

            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems.OfType<ControlChangeEventViewModel>())
                {
                    item.PropertyChanged -= OnPointPropertyChanged;
                    RemovePoint(item);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<ControlChangeEventViewModel>())
                {
                    item.PropertyChanged += OnPointPropertyChanged;
                    AddPoint(item);
                }
            }

            UpdateVisiblePointPositions();
        }

        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ControlChangeEventViewModel point)
            {
                return;
            }

            if (e.PropertyName is nameof(ControlChangeEventViewModel.AbsoluteTime)
                or nameof(ControlChangeEventViewModel.Value))
            {
                UpdatePointPosition(point);
            }
        }

        private void RebuildVisiblePoints()
        {
            foreach (var item in _pointElements.Keys)
            {
                item.PropertyChanged -= OnPointPropertyChanged;
            }

            PointsCanvas.Children.Clear();
            _pointElements.Clear();

            if (_currentCollection is null)
            {
                return;
            }

            foreach (var item in _currentCollection.VisibleItems)
            {
                AddPoint(item);
            }

            UpdateVisiblePointPositions();
        }

        private void AddPoint(ControlChangeEventViewModel point)
        {
            if (_pointElements.ContainsKey(point))
            {
                UpdatePointPosition(point);
                return;
            }

            var ellipse = new Ellipse
            {
                Width = PointDiameter,
                Height = PointDiameter,
                StrokeThickness = 1,
                DataContext = point,
                Cursor = Cursors.Hand,
            };
            ellipse.SetBinding(Shape.FillProperty, new Binding(nameof(Foreground)) { Source = this });
            ellipse.SetBinding(Shape.StrokeProperty, new Binding(nameof(Background)) { Source = this });

            _pointElements[point] = ellipse;
            PointsCanvas.Children.Add(ellipse);
            UpdatePointPosition(point);
        }

        private void RemovePoint(ControlChangeEventViewModel point)
        {
            if (_pointElements.Remove(point, out var ellipse))
            {
                PointsCanvas.Children.Remove(ellipse);
            }
        }

        private void UpdateVisiblePointPositions()
        {
            foreach (var point in _pointElements.Keys.ToArray())
            {
                UpdatePointPosition(point);
            }
        }

        private void UpdatePointPosition(ControlChangeEventViewModel point)
        {
            if (!_pointElements.TryGetValue(point, out var ellipse) || _viewModel is null || _viewModel.WidthPerTick <= 0)
            {
                return;
            }

            double x = (point.AbsoluteTime * _viewModel.WidthPerTick) - PointRadius;
            double y = ConvertValueToY(point.Value) - PointRadius;
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
        }

        private void LaneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel is null || sender is not Auris_Studio.Views.Button button || button.Tag is not string laneText)
            {
                return;
            }

            if (Enum.TryParse<TrackControlLane>(laneText, out var lane))
            {
                _viewModel.SelectedControlLane = lane;
            }
        }

        private void UpdateLaneButtonState()
        {
            if (_viewModel is null)
            {
                return;
            }

            UpdateLaneButton(VolumeButton, TrackControlLane.Volume);
            UpdateLaneButton(PanButton, TrackControlLane.Pan);
            UpdateLaneButton(ExpressionButton, TrackControlLane.Expression);
            UpdateLaneButton(ModulationButton, TrackControlLane.Modulation);
            UpdateLaneButton(SustainButton, TrackControlLane.Sustain);
        }

        private void UpdateLaneButton(Auris_Studio.Views.Button button, TrackControlLane lane)
        {
            bool isActive = _viewModel?.SelectedControlLane == lane;
            button.Opacity = isActive ? 1d : 0.72d;
            button.BorderThickness = isActive ? new Thickness(1.2) : new Thickness(0);
            button.BorderBrush = isActive ? Foreground : Brushes.Transparent;
        }

        private void EditorScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshViewport();
        }

        private void EditorSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.CurrentSelectedTrack is null)
            {
                return;
            }

            if (TryGetPointFromSource(e.OriginalSource as DependencyObject, out var existingPoint))
            {
                _draggingPoint = existingPoint;
                EditorSurface.CaptureMouse();
                e.Handled = true;
                return;
            }

            if (_currentCollection is null)
            {
                return;
            }

            Point position = e.GetPosition(EditorSurface);
            var createdPoint = new ControlChangeEventViewModel
            {
                AbsoluteTime = ConvertXToTick(position.X),
                MidiController = MidiTrackViewModel.GetMidiController(_viewModel.SelectedControlLane),
                Value = ConvertYToValue(position.Y),
            };

            _viewModel.CurrentSelectedTrack.Ctrls.Add(createdPoint);
            _draggingPoint = createdPoint;
            EditorSurface.CaptureMouse();
            e.Handled = true;
        }

        private void EditorSurface_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingPoint is null || _viewModel is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point position = e.GetPosition(EditorSurface);
            _draggingPoint.AbsoluteTime = ConvertXToTick(position.X);
            _draggingPoint.Value = ConvertYToValue(position.Y);
            e.Handled = true;
        }

        private void EditorSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseDrag();
        }

        private void EditorSurface_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                ReleaseDrag();
            }
        }

        private void EditorSurface_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.CurrentSelectedTrack is null)
            {
                return;
            }

            if (!TryGetPointFromSource(e.OriginalSource as DependencyObject, out var point))
            {
                return;
            }

            _viewModel.CurrentSelectedTrack.Ctrls.Remove(point);
            if (ReferenceEquals(_draggingPoint, point))
            {
                ReleaseDrag();
            }

            e.Handled = true;
        }

        private bool TryGetPointFromSource(DependencyObject? source, out ControlChangeEventViewModel point)
        {
            while (source is not null)
            {
                if (source is FrameworkElement element && element.DataContext is ControlChangeEventViewModel controlPoint)
                {
                    point = controlPoint;
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            point = null!;
            return false;
        }

        private void ReleaseDrag()
        {
            _draggingPoint = null;
            if (EditorSurface.IsMouseCaptured)
            {
                EditorSurface.ReleaseMouseCapture();
            }
        }

        private void UpdateVirtualization()
        {
            if (_currentCollection is null || _viewModel is null || _viewModel.WidthPerTick <= 0)
            {
                return;
            }

            double viewportWidth = EditorScrollViewer.ViewportWidth > 0 ? EditorScrollViewer.ViewportWidth : EditorScrollViewer.ActualWidth;
            double horizontalOffset = EditorScrollViewer.HorizontalOffset;

            long startTick = Math.Max(0L, (long)Math.Floor(horizontalOffset / _viewModel.WidthPerTick));
            long endTick = Math.Max(startTick + 1, (long)Math.Ceiling((horizontalOffset + viewportWidth) / _viewModel.WidthPerTick) + 1);

            var previousPoint = _currentCollection.FindFirstAtOrBefore(startTick);
            if (previousPoint is not null)
            {
                startTick = Math.Min(startTick, previousPoint.AbsoluteTime);
            }

            var nextPoint = _currentCollection.FindFirstAtOrAfter(endTick);
            if (nextPoint is not null)
            {
                endTick = Math.Max(endTick, nextPoint.AbsoluteTime + Math.Max(1, nextPoint.DeltaTime));
            }

            _currentCollection.Virtualize(startTick, endTick);

            CurveDecorator.ItemsSource = _currentCollection.VisibleItems;
            UpdateVisiblePointPositions();
        }

        private long ConvertXToTick(double x)
        {
            if (_viewModel is null || _viewModel.WidthPerTick <= 0)
            {
                return 0;
            }

            double rawTick = Math.Max(0d, x / _viewModel.WidthPerTick);
            return _viewModel.AlignTimeNearest((long)Math.Round(rawTick));
        }

        private int ConvertYToValue(double y)
        {
            double availableHeight = Math.Max(1d, EditorSurface.ActualHeight - (VerticalPadding * 2));
            double clampedY = Math.Clamp(y, VerticalPadding, EditorSurface.ActualHeight - VerticalPadding);
            double normalized = 1d - ((clampedY - VerticalPadding) / availableHeight);
            return Math.Clamp((int)Math.Round(normalized * 127d), 0, 127);
        }

        private double ConvertValueToY(int value)
        {
            double availableHeight = Math.Max(1d, EditorSurface.ActualHeight - (VerticalPadding * 2));
            double normalized = Math.Clamp(value, 0, 127) / 127d;
            return VerticalPadding + ((1d - normalized) * availableHeight);
        }
    }
}
