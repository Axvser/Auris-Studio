using Auris_Studio.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using Converter = VeloxDev.WPF.PlatformAdapters.BrushConverter;

namespace Auris_Studio.Views.Decorators
{
    [ThemeConfig<Converter, Dark, Light>(nameof(MeasureLineBrush), [nameof(Brushes.DarkGray)],[nameof(Brushes.Black)])]
    [ThemeConfig<Converter, Dark, Light>(nameof(HeaderTextBrush), [nameof(Brushes.DarkGray)], [nameof(Brushes.Black)])]
    [ThemeConfig<Converter, Dark, Light>(nameof(BeatLineBrush), [nameof(Brushes.LightGray)], [nameof(Brushes.Black)])]
    public partial class MeasureBeatDecorator : ContentControl
    {
        private MidiEditorViewModel? _viewModel;

        private const double GoldenRatio = 0.618;

        public MeasureBeatDecorator()
        {
            InitializeTheme();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnDecoratorUnloaded;
        }

        #region 依赖属性
        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register(nameof(ShowHeader), typeof(bool), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        public static readonly DependencyProperty ShowContentProperty =
            DependencyProperty.Register(nameof(ShowContent), typeof(bool), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowContent
        {
            get { return (bool)GetValue(ShowContentProperty); }
            set { SetValue(ShowContentProperty, value); }
        }

        public static readonly DependencyProperty MeasureLineBrushProperty =
            DependencyProperty.Register(nameof(MeasureLineBrush), typeof(Brush), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush MeasureLineBrush
        {
            get { return (Brush)GetValue(MeasureLineBrushProperty); }
            set { SetValue(MeasureLineBrushProperty, value); }
        }

        public static readonly DependencyProperty BeatLineBrushProperty =
            DependencyProperty.Register(nameof(BeatLineBrush), typeof(Brush), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BeatLineBrush
        {
            get { return (Brush)GetValue(BeatLineBrushProperty); }
            set { SetValue(BeatLineBrushProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register(nameof(HeaderHeight), typeof(double), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register(nameof(HeaderBackground), typeof(Brush), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HeaderBackground
        {
            get { return (Brush)GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextFontSizeProperty =
            DependencyProperty.Register(nameof(HeaderTextFontSize), typeof(double), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double HeaderTextFontSize
        {
            get { return (double)GetValue(HeaderTextFontSizeProperty); }
            set { SetValue(HeaderTextFontSizeProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextBrushProperty =
            DependencyProperty.Register(nameof(HeaderTextBrush), typeof(Brush), typeof(MeasureBeatDecorator),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HeaderTextBrush
        {
            get { return (Brush)GetValue(HeaderTextBrushProperty); }
            set { SetValue(HeaderTextBrushProperty, value); }
        }
        #endregion

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MidiEditorViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = e.NewValue as MidiEditorViewModel;
            _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
            InvalidateVisual();
        }

        private void OnDecoratorUnloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MidiEditorViewModel.ViewportStartTime) ||
                e.PropertyName == nameof(MidiEditorViewModel.ViewportEndTime) ||
                e.PropertyName == nameof(MidiEditorViewModel.WidthPerTick) ||
                e.PropertyName == nameof(MidiEditorViewModel.Numerator) ||
                e.PropertyName == nameof(MidiEditorViewModel.Denominator) ||
                e.PropertyName == nameof(MidiEditorViewModel.PPQN))
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (!ShowHeader && !ShowContent)
                return;

            if (_viewModel == null ||
                _viewModel.WidthPerTick <= 0 ||
                _viewModel.Numerator <= 0 ||
                _viewModel.Denominator <= 0)
            {
                return;
            }

            DrawMeasureAndBeatLines(dc, _viewModel);
        }

        private void DrawMeasureAndBeatLines(DrawingContext dc, MidiEditorViewModel vm)
        {
            double ticksPerBeat = vm.PPQN * 4.0 / vm.Denominator;
            double ticksPerMeasure = ticksPerBeat * vm.Numerator;

            if (ticksPerMeasure <= 0) return;

            long startMeasureIndex = (long)(vm.ViewportStartTime / ticksPerMeasure) - 1;
            long endMeasureIndex = (long)(vm.ViewportEndTime / ticksPerMeasure) + 1;

            if (startMeasureIndex < 0) startMeasureIndex = 0;

            double renderWidth = ActualWidth;
            double renderHeight = ActualHeight;

            // *** 修正：身体区域起始Y的计算逻辑 ***
            double headerHeight = HeaderHeight;
            double headerStartY = 0;
            double headerEndY = ShowHeader ? headerHeight : 0; // 头部显示时的结束Y坐标
            // 关键修正：当ShowHeader为false时，bodyStartY = 0
            double bodyStartY = ShowHeader ? headerEndY : 0;
            double bodyEndY = renderHeight;

            // 头部区域内部的分割线顶部计算
            double headerBottom = headerEndY;
            double headerMeasureLineTop = ShowHeader ? headerBottom - (headerHeight * GoldenRatio) : 0;
            double headerBeatLineTop = ShowHeader ? headerBottom - (headerHeight * (1.0 - GoldenRatio)) : 0;

            // 创建画笔
            Pen? headerMeasurePen = ShowHeader ? new Pen(MeasureLineBrush, 1.0) : null;
            Pen? headerBeatPen = ShowHeader ? new Pen(BeatLineBrush, 1.0) : null;
            Pen? bodyMeasurePen = ShowContent ? new Pen(MeasureLineBrush, 1.0) : null;
            Pen? bodyBeatPen = ShowContent ? new Pen(BeatLineBrush, 0.3) : null; // 拍线宽度改为0.3

            // 绘制头部背景
            if (ShowHeader && HeaderBackground != null && !HeaderBackground.Equals(Brushes.Transparent))
            {
                dc.DrawRectangle(HeaderBackground, null,
                    new Rect(0, headerStartY, renderWidth, headerHeight));
            }

            for (long measureIndex = startMeasureIndex; measureIndex <= endMeasureIndex; measureIndex++)
            {
                double measureTick = measureIndex * ticksPerMeasure;
                double nextMeasureTick = measureTick + ticksPerMeasure;

                bool isMeasureVisible = measureTick <= vm.ViewportEndTime &&
                                       nextMeasureTick >= vm.ViewportStartTime;

                if (isMeasureVisible)
                {
                    double measureX = (measureTick - vm.ViewportStartTime) * vm.WidthPerTick;

                    if (measureX >= 0 && measureX <= renderWidth)
                    {
                        // 绘制小节线
                        // 1. 身体区域的小节线
                        if (bodyMeasurePen != null)
                        {
                            dc.DrawLine(bodyMeasurePen,
                                       new Point(measureX, bodyStartY),
                                       new Point(measureX, bodyEndY));
                        }

                        // 2. 头部区域的小节线
                        if (headerMeasurePen != null)
                        {
                            dc.DrawLine(headerMeasurePen,
                                       new Point(measureX, headerMeasureLineTop),
                                       new Point(measureX, headerBottom));
                        }

                        // 3. 绘制小节序号文本
                        if (ShowHeader && measureIndex >= 0 && measureX + 5 < renderWidth)
                        {
                            DrawMeasureHeaderText(dc, measureIndex, measureX, headerStartY, headerHeight);
                        }
                    }
                }

                // 绘制拍线
                for (int beat = 1; beat < vm.Numerator; beat++)
                {
                    double beatTick = measureTick + beat * ticksPerBeat;

                    bool isBeatVisible = beatTick >= vm.ViewportStartTime &&
                                       beatTick <= vm.ViewportEndTime;

                    if (isBeatVisible)
                    {
                        double beatX = (beatTick - vm.ViewportStartTime) * vm.WidthPerTick;

                        if (beatX >= 0 && beatX <= renderWidth)
                        {
                            // 1. 身体区域的拍线
                            if (bodyBeatPen != null)
                            {
                                dc.DrawLine(bodyBeatPen,
                                           new Point(beatX, bodyStartY),
                                           new Point(beatX, bodyEndY));
                            }

                            // 2. 头部区域的拍线
                            if (headerBeatPen != null)
                            {
                                dc.DrawLine(headerBeatPen,
                                           new Point(beatX, headerBeatLineTop),
                                           new Point(beatX, headerBottom));
                            }
                        }
                    }
                }
            }
        }

        private void DrawMeasureHeaderText(DrawingContext dc, long measureIndex, double measureX, double headerStartY, double headerHeight)
        {
            try
            {
                FormattedText measureText = new(
                    (measureIndex + 1).ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    HeaderTextFontSize,
                    HeaderTextBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double textX = measureX + 2;
                double textY = headerStartY + 2;

                if (textY + measureText.Height <= headerStartY + headerHeight)
                {
                    dc.DrawText(measureText, new Point(textX, textY));
                }
            }
            catch
            {
                // 忽略绘制错误
            }
        }
    }
}