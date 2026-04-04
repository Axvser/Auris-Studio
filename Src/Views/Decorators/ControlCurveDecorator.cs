using Auris_Studio.ViewModels.MidiEvents;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views.Decorators
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(CurveBrush), ["#FF00FFFF"], ["#FF2F6BFF"])]
    public partial class ControlCurveDecorator : Control
    {
        private INotifyCollectionChanged? _observableItemsSource;
        private readonly HashSet<ControlChangeEventViewModel> _subscribedItems = [];

        public ControlCurveDecorator()
        {
            InitializeTheme();
            Unloaded += (_, _) => DetachFromItemsSource();
        }

        public IEnumerable<ControlChangeEventViewModel>? ItemsSource
        {
            get => (IEnumerable<ControlChangeEventViewModel>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<ControlChangeEventViewModel>), typeof(ControlCurveDecorator),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        public double WidthPerTick
        {
            get => (double)GetValue(WidthPerTickProperty);
            set => SetValue(WidthPerTickProperty, value);
        }
        public static readonly DependencyProperty WidthPerTickProperty =
            DependencyProperty.Register(nameof(WidthPerTick), typeof(double), typeof(ControlCurveDecorator),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush CurveBrush
        {
            get => (Brush)GetValue(CurveBrushProperty);
            set => SetValue(CurveBrushProperty, value);
        }
        public static readonly DependencyProperty CurveBrushProperty =
            DependencyProperty.Register(nameof(CurveBrush), typeof(Brush), typeof(ControlCurveDecorator),
                new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        public double CurveThickness
        {
            get => (double)GetValue(CurveThicknessProperty);
            set => SetValue(CurveThicknessProperty, value);
        }
        public static readonly DependencyProperty CurveThicknessProperty =
            DependencyProperty.Register(nameof(CurveThickness), typeof(double), typeof(ControlCurveDecorator),
                new FrameworkPropertyMetadata(1.5d, FrameworkPropertyMetadataOptions.AffectsRender));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ControlCurveDecorator decorator)
            {
                decorator.AttachToItemsSource(e.OldValue as IEnumerable<ControlChangeEventViewModel>, e.NewValue as IEnumerable<ControlChangeEventViewModel>);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ItemsSource is null || WidthPerTick <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            var points = ItemsSource
                .OrderBy(item => item.AbsoluteTime)
                .Select(item => new Point(item.AbsoluteTime * WidthPerTick, ConvertValueToY(item.Value)))
                .ToList();

            if (points.Count == 0)
            {
                return;
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], false, false);

                for (int i = 1; i < points.Count; i++)
                {
                    var previous = points[i - 1];
                    var current = points[i];
                    context.LineTo(new Point(current.X, previous.Y), true, false);
                    context.LineTo(current, true, false);
                }

                if (points[^1].X < ActualWidth)
                {
                    context.LineTo(new Point(ActualWidth, points[^1].Y), true, false);
                }
            }
            geometry.Freeze();

            var pen = new Pen(CurveBrush, CurveThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };
            pen.Freeze();

            drawingContext.DrawGeometry(null, pen, geometry);
        }

        private void AttachToItemsSource(IEnumerable<ControlChangeEventViewModel>? oldSource, IEnumerable<ControlChangeEventViewModel>? newSource)
        {
            if (ReferenceEquals(oldSource, newSource))
            {
                return;
            }

            DetachFromItemsSource();

            if (newSource is null)
            {
                InvalidateVisual();
                return;
            }

            _observableItemsSource = newSource as INotifyCollectionChanged;
            if (_observableItemsSource is not null)
            {
                _observableItemsSource.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            foreach (var item in newSource)
            {
                SubscribeItem(item);
            }

            InvalidateVisual();
        }

        private void DetachFromItemsSource()
        {
            if (_observableItemsSource is not null)
            {
                _observableItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;
                _observableItemsSource = null;
            }

            foreach (var item in _subscribedItems.ToArray())
            {
                UnsubscribeItem(item);
            }
        }

        private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems.OfType<ControlChangeEventViewModel>())
                {
                    UnsubscribeItem(item);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<ControlChangeEventViewModel>())
                {
                    SubscribeItem(item);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in _subscribedItems.ToArray())
                {
                    UnsubscribeItem(item);
                }

                if (ItemsSource is not null)
                {
                    foreach (var item in ItemsSource)
                    {
                        SubscribeItem(item);
                    }
                }
            }

            InvalidateVisual();
        }

        private void SubscribeItem(ControlChangeEventViewModel item)
        {
            if (_subscribedItems.Add(item))
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        private void UnsubscribeItem(ControlChangeEventViewModel item)
        {
            if (_subscribedItems.Remove(item))
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ControlChangeEventViewModel.AbsoluteTime)
                or nameof(ControlChangeEventViewModel.Value))
            {
                InvalidateVisual();
            }
        }

        private double ConvertValueToY(int value)
        {
            const double padding = 8d;
            double availableHeight = Math.Max(1d, ActualHeight - (padding * 2));
            double normalized = Math.Clamp(value, 0, 127) / 127d;
            return padding + ((1d - normalized) * availableHeight);
        }
    }
}
