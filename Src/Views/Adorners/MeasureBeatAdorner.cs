using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Auris_Studio.ViewModels;
using System.ComponentModel;

namespace Auris_Studio.Views.Adorners
{
    public class MeasureBeatAdorner : Adorner
    {
        private MidiEditorViewModel? _viewModel;

        public MeasureBeatAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            Loaded += OnAdornerLoaded;
            Unloaded += OnAdornerUnloaded;
        }

        #region 依赖属性

        public static readonly DependencyProperty ShowMeasureLinesProperty =
            DependencyProperty.Register(nameof(ShowMeasureLines), typeof(bool), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowMeasureLines
        {
            get { return (bool)GetValue(ShowMeasureLinesProperty); }
            set { SetValue(ShowMeasureLinesProperty, value); }
        }

        public static readonly DependencyProperty ShowBeatLinesProperty =
            DependencyProperty.Register(nameof(ShowBeatLines), typeof(bool), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowBeatLines
        {
            get { return (bool)GetValue(ShowBeatLinesProperty); }
            set { SetValue(ShowBeatLinesProperty, value); }
        }

        public static readonly DependencyProperty MeasureLineBrushProperty =
            DependencyProperty.Register(nameof(MeasureLineBrush), typeof(Brush), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush MeasureLineBrush
        {
            get { return (Brush)GetValue(MeasureLineBrushProperty); }
            set { SetValue(MeasureLineBrushProperty, value); }
        }

        public static readonly DependencyProperty BeatLineBrushProperty =
            DependencyProperty.Register(nameof(BeatLineBrush), typeof(Brush), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BeatLineBrush
        {
            get { return (Brush)GetValue(BeatLineBrushProperty); }
            set { SetValue(BeatLineBrushProperty, value); }
        }

        public static readonly DependencyProperty MeasureTextFontSizeProperty =
            DependencyProperty.Register(nameof(MeasureTextFontSize), typeof(double), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(9.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double MeasureTextFontSize
        {
            get { return (double)GetValue(MeasureTextFontSizeProperty); }
            set { SetValue(MeasureTextFontSizeProperty, value); }
        }

        public static readonly DependencyProperty LineScaleProperty =
            DependencyProperty.Register(nameof(LineScale), typeof(double), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double LineScale
        {
            get { return (double)GetValue(LineScaleProperty); }
            set { SetValue(LineScaleProperty, value); }
        }

        public static readonly DependencyProperty DataContextBridgeProperty =
            DependencyProperty.Register(nameof(DataContextBridge), typeof(object), typeof(MeasureBeatAdorner),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnDataContextBridgeChanged));

        public object DataContextBridge
        {
            get { return GetValue(DataContextBridgeProperty); }
            set { SetValue(DataContextBridgeProperty, value); }
        }

        #endregion

        private void OnAdornerLoaded(object sender, RoutedEventArgs e)
        {
            // 尝试从被装饰元素获取DataContext
            if (AdornedElement is FrameworkElement element)
            {
                DataContextBridge = element.DataContext;
            }

            // 监听DataContext变化
            if (AdornedElement is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContextChanged += OnAdornedElementDataContextChanged;
            }
        }

        private void OnAdornerUnloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;

            if (AdornedElement is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContextChanged -= OnAdornedElementDataContextChanged;
            }
        }

        private void OnAdornedElementDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                DataContextBridge = element.DataContext;
            }
        }

        private static void OnDataContextBridgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var adorner = (MeasureBeatAdorner)d;
            adorner.OnDataContextChanged(e.NewValue);
        }

        private void OnDataContextChanged(object newValue)
        {
            _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = newValue as MidiEditorViewModel;

            _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MidiEditorViewModel.ViewportStartTime) ||
                e.PropertyName == nameof(MidiEditorViewModel.ViewportEndTime) ||
                e.PropertyName == nameof(MidiEditorViewModel.WidthPerTick) ||
                e.PropertyName == nameof(MidiEditorViewModel.Numerator) ||
                e.PropertyName == nameof(MidiEditorViewModel.Denominator) ||
                e.PropertyName == nameof(MidiEditorViewModel.PPQN) ||
                e.PropertyName == nameof(MidiEditorViewModel.CanvasWidth))
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!ShowMeasureLines && !ShowBeatLines)
                return;

            if (_viewModel == null ||
                _viewModel.WidthPerTick <= 0 ||
                _viewModel.Numerator <= 0 ||
                _viewModel.Denominator <= 0)
            {
                return;
            }

            DrawMeasureAndBeatLines(drawingContext, _viewModel);
        }

        private void DrawMeasureAndBeatLines(DrawingContext dc, MidiEditorViewModel vm)
        {
            double ticksPerBeat = vm.PPQN * 4.0 / vm.Denominator;
            double ticksPerMeasure = ticksPerBeat * vm.Numerator;

            if (ticksPerMeasure <= 0) return;

            // 计算可见区域的小节范围
            long startMeasureIndex = (long)(vm.ViewportStartTime / ticksPerMeasure) - 1;
            long endMeasureIndex = (long)(vm.ViewportEndTime / ticksPerMeasure) + 1;

            if (startMeasureIndex < 0) startMeasureIndex = 0;

            Pen? measurePen = ShowMeasureLines ?
                new Pen(MeasureLineBrush, 1.0 * LineScale) : null;
            Pen? beatPen = ShowBeatLines ?
                new Pen(BeatLineBrush, 0.3 * LineScale) : null;

            Typeface typeface = new("Segoe UI");
            Brush textBrush = MeasureLineBrush;

            double renderWidth = ActualWidth;
            double renderHeight = ActualHeight;

            for (long measureIndex = startMeasureIndex; measureIndex <= endMeasureIndex; measureIndex++)
            {
                double measureTick = measureIndex * ticksPerMeasure;
                double nextMeasureTick = measureTick + ticksPerMeasure;

                // 修正小节线检查逻辑
                // 小节线可见的条件：measureTick在可见时间范围内
                bool isMeasureVisible = measureTick <= vm.ViewportEndTime &&
                                       nextMeasureTick >= vm.ViewportStartTime;

                if (isMeasureVisible)
                {
                    double measureX = (measureTick - vm.ViewportStartTime) * vm.WidthPerTick;

                    // 只绘制在可见区域内的线
                    if (measureX >= 0 && measureX <= renderWidth && ShowMeasureLines && measurePen != null)
                    {
                        dc.DrawLine(measurePen,
                                   new Point(measureX, 0),
                                   new Point(measureX, renderHeight));

                        // 绘制小节序号
                        if (measureIndex >= 0)
                        {
                            try
                            {
                                FormattedText measureText = new(
                                    (measureIndex + 1).ToString(),
                                    System.Globalization.CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    typeface,
                                    MeasureTextFontSize,
                                    textBrush,
                                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                                dc.DrawText(measureText, new Point(measureX + 2, 2));
                            }
                            catch
                            {
                                // 忽略绘制错误
                            }
                        }
                    }
                }

                // 绘制拍线
                if (ShowBeatLines && beatPen != null)
                {
                    for (int beat = 1; beat < vm.Numerator; beat++)
                    {
                        double beatTick = measureTick + beat * ticksPerBeat;

                        // 修正拍线可见性检查
                        // 拍线可见的条件：beatTick在可见时间范围内
                        bool isBeatVisible = beatTick >= vm.ViewportStartTime &&
                                           beatTick <= vm.ViewportEndTime;

                        if (isBeatVisible)
                        {
                            double beatX = (beatTick - vm.ViewportStartTime) * vm.WidthPerTick;

                            // 只绘制在可见区域内的线
                            if (beatX >= 0 && beatX <= renderWidth)
                            {
                                dc.DrawLine(beatPen,
                                           new Point(beatX, 0),
                                           new Point(beatX, renderHeight));
                            }
                        }
                    }
                }
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (VisualParent == null)
            {
                Cleanup();
            }
        }
    }
}