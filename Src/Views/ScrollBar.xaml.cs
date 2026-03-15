using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Auris_Studio.Views
{
    public partial class ScrollBar : UserControl
    {
        public ScrollBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public event EventHandler<double>? ValueChanged;

        public event EventHandler<double>? OffsetChanged;

        public event EventHandler<double>? ViewportSizeChanged;

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(ScrollBar),
                new PropertyMetadata(Brushes.Gray, OnVisualPropertyChanged));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ScrollBar),
                new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(0d, OnLogicValueRangeChanged));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(100d, OnLogicValueRangeChanged));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(0d, OnValueChanged));

        public double ViewportSize
        {
            get { return (double)GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }
        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register(nameof(ViewportSize), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(20d, OnViewportSizeChanged));

        public double SmallChange
        {
            get { return (double)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(1d, OnLogicValueRangeChanged));

        public double LargeChange
        {
            get { return (double)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }
        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(10d, OnLogicValueRangeChanged));

        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(nameof(Offset), typeof(double), typeof(ScrollBar),
                new PropertyMetadata(0d, OnOffsetChanged));

        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _dragStartValue;
        private Size _trackSize = new(0, 0);
        private Size _dragBarSize = new(0, 0);
        private double _dragBarLength = 0;
        private const double MIN_DRAGBAR_SIZE = 20; // 最小拖动条尺寸
        private const double BUTTON_SIZE = 16; // 按钮尺寸

        #region 布局计算相关方法

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateContentLayout();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateContentLayout();
        }

        private void UpdateContentLayout()
        {
            if (!IsLoaded) return;

            var availableSize = new Size(ActualWidth, ActualHeight);

            if (Orientation == Orientation.Vertical)
            {
                CalculateVerticalLayout(availableSize);
            }
            else
            {
                CalculateHorizontalLayout(availableSize);
            }
        }

        private void CalculateVerticalLayout(Size availableSize)
        {
            // 更新 Canvas 尺寸
            MainCanvas.Width = availableSize.Width;
            MainCanvas.Height = availableSize.Height;

            double width = availableSize.Width;
            double height = availableSize.Height;

            if (width <= 0 || height <= 0) return;

            // 计算可拖动轨道尺寸
            _trackSize = new Size(width, height - 2 * BUTTON_SIZE);

            // 计算拖动条长度
            double totalRange = Math.Max(1, Maximum - Minimum);
            double visibleRatio = ViewportSize / totalRange;
            visibleRatio = Math.Min(1, Math.Max(0.1, visibleRatio));

            _dragBarLength = Math.Max(MIN_DRAGBAR_SIZE, _trackSize.Height * visibleRatio);

            // 计算拖动条位置
            double valueRatio = (Value - Minimum) / totalRange;
            valueRatio = Math.Max(0, Math.Min(1, valueRatio));
            double dragBarTop = BUTTON_SIZE + (_trackSize.Height - _dragBarLength) * valueRatio;

            // 更新元素位置和尺寸
            // 轨道背景
            Canvas.SetLeft(TrackBackground, 0);
            Canvas.SetTop(TrackBackground, BUTTON_SIZE);
            TrackBackground.Width = width;
            TrackBackground.Height = _trackSize.Height;

            // 按钮A（顶部） - 向上三角形
            Canvas.SetLeft(ButtonA, 0);
            Canvas.SetTop(ButtonA, 0);
            ButtonA.Width = width;
            ButtonA.Height = BUTTON_SIZE;

            // 设置三角形几何图形（向上箭头）
            SetTriangleGeometry(ButtonA, ArrowDirection.Up, width, BUTTON_SIZE);

            // 拖动条
            Canvas.SetLeft(DragBar, 0);
            Canvas.SetTop(DragBar, dragBarTop);
            DragBar.Width = width;
            DragBar.Height = _dragBarLength;

            // 按钮B（底部） - 向下三角形
            Canvas.SetLeft(ButtonB, 0);
            Canvas.SetTop(ButtonB, height - BUTTON_SIZE);
            ButtonB.Width = width;
            ButtonB.Height = BUTTON_SIZE;

            // 设置三角形几何图形（向下箭头）
            SetTriangleGeometry(ButtonB, ArrowDirection.Down, width, BUTTON_SIZE);

            _dragBarSize = new Size(width, _dragBarLength);
        }

        private void CalculateHorizontalLayout(Size availableSize)
        {
            // 更新 Canvas 尺寸
            MainCanvas.Width = availableSize.Width;
            MainCanvas.Height = availableSize.Height;

            double width = availableSize.Width;
            double height = availableSize.Height;

            if (width <= 0 || height <= 0) return;

            // 计算可拖动轨道尺寸
            _trackSize = new Size(width - 2 * BUTTON_SIZE, height);

            // 计算拖动条长度
            double totalRange = Math.Max(1, Maximum - Minimum);
            double visibleRatio = ViewportSize / totalRange;
            visibleRatio = Math.Min(1, Math.Max(0.1, visibleRatio));

            _dragBarLength = Math.Max(MIN_DRAGBAR_SIZE, _trackSize.Width * visibleRatio);

            // 计算拖动条位置
            double valueRatio = (Value - Minimum) / totalRange;
            valueRatio = Math.Max(0, Math.Min(1, valueRatio));
            double dragBarLeft = BUTTON_SIZE + (_trackSize.Width - _dragBarLength) * valueRatio;

            // 更新元素位置和尺寸
            // 轨道背景
            Canvas.SetLeft(TrackBackground, BUTTON_SIZE);
            Canvas.SetTop(TrackBackground, 0);
            TrackBackground.Width = _trackSize.Width;
            TrackBackground.Height = height;

            // 按钮A（左侧） - 向左三角形
            Canvas.SetLeft(ButtonA, 0);
            Canvas.SetTop(ButtonA, 0);
            ButtonA.Width = BUTTON_SIZE;
            ButtonA.Height = height;

            // 设置三角形几何图形（向左箭头）
            SetTriangleGeometry(ButtonA, ArrowDirection.Left, BUTTON_SIZE, height);

            // 拖动条
            Canvas.SetLeft(DragBar, dragBarLeft);
            Canvas.SetTop(DragBar, 0);
            DragBar.Width = _dragBarLength;
            DragBar.Height = height;

            // 按钮B（右侧） - 向右三角形
            Canvas.SetLeft(ButtonB, width - BUTTON_SIZE);
            Canvas.SetTop(ButtonB, 0);
            ButtonB.Width = BUTTON_SIZE;
            ButtonB.Height = height;

            // 设置三角形几何图形（向右箭头）
            SetTriangleGeometry(ButtonB, ArrowDirection.Right, BUTTON_SIZE, height);

            _dragBarSize = new Size(_dragBarLength, height);
        }

        private enum ArrowDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        private static void SetTriangleGeometry(Path path, ArrowDirection direction, double width, double height)
        {
            double padding = 3; // 内边距，避免三角形贴边
            double halfWidth = width / 2;
            double halfHeight = height / 2;

            PathGeometry geometry = new();
            PathFigure figure = new()
            {
                IsClosed = true
            };

            switch (direction)
            {
                case ArrowDirection.Up:
                    // 向上箭头：顶点在上，底边在下
                    figure.StartPoint = new Point(padding, height - padding);
                    figure.Segments.Add(new LineSegment(new Point(halfWidth, padding), true));
                    figure.Segments.Add(new LineSegment(new Point(width - padding, height - padding), true));
                    break;

                case ArrowDirection.Down:
                    // 向下箭头：顶点在下，底边在上
                    figure.StartPoint = new Point(padding, padding);
                    figure.Segments.Add(new LineSegment(new Point(halfWidth, height - padding), true));
                    figure.Segments.Add(new LineSegment(new Point(width - padding, padding), true));
                    break;

                case ArrowDirection.Left:
                    // 向左箭头：顶点在左，底边在右
                    figure.StartPoint = new Point(width - padding, padding);
                    figure.Segments.Add(new LineSegment(new Point(padding, halfHeight), true));
                    figure.Segments.Add(new LineSegment(new Point(width - padding, height - padding), true));
                    break;

                case ArrowDirection.Right:
                    // 向右箭头：顶点在右，底边在左
                    figure.StartPoint = new Point(padding, padding);
                    figure.Segments.Add(new LineSegment(new Point(width - padding, halfHeight), true));
                    figure.Segments.Add(new LineSegment(new Point(padding, height - padding), true));
                    break;
            }

            geometry.Figures.Add(figure);
            path.Data = geometry;
        }

        #endregion

        #region 依赖属性变更处理

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar)
            {
                scrollBar.UpdateVisuals();
            }
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar)
            {
                scrollBar.UpdateContentLayout();
                scrollBar.UpdateCursors();
            }
        }

        private static void OnLogicValueRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar)
            {
                scrollBar.ClampValue();
                scrollBar.UpdateContentLayout();
            }
        }

        private static void OnViewportSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar)
            {
                scrollBar.UpdateContentLayout();
                scrollBar.ViewportSizeChanged?.Invoke(scrollBar, (double)e.NewValue);
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar)
            {
                scrollBar.ClampValue();
                scrollBar.UpdateContentLayout();

                // 当 Value 变化时，同步更新 Offset
                if (!scrollBar._isUpdatingOffsetFromValue)
                {
                    scrollBar._isUpdatingValueFromOffset = false;
                    scrollBar.Offset = (double)e.NewValue;
                }

                scrollBar.ValueChanged?.Invoke(scrollBar, (double)e.NewValue);
            }
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollBar scrollBar && e.NewValue is double newValue)
            {
                // 当 Offset 变化时，同步更新 Value
                if (!scrollBar._isUpdatingValueFromOffset)
                {
                    scrollBar._isUpdatingOffsetFromValue = false;
                    scrollBar.Value = newValue;
                }
                scrollBar.OffsetChanged?.Invoke(scrollBar, newValue);
            }
        }

        private bool _isUpdatingValueFromOffset = false;
        private bool _isUpdatingOffsetFromValue = false;

        private void ClampValue()
        {
            double clamped = Math.Max(Minimum, Math.Min(Maximum, Value));
            if (Math.Abs(Value - clamped) > 0.001)
            {
                _isUpdatingValueFromOffset = true;
                _isUpdatingOffsetFromValue = false;
                Value = clamped;
            }
        }

        #endregion

        #region 鼠标事件处理

        private void ButtonA_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 点击按钮A（上/左）
            double newValue = Value - SmallChange;
            _isUpdatingValueFromOffset = false;
            _isUpdatingOffsetFromValue = false;
            Value = Math.Max(Minimum, newValue);
        }

        private void ButtonB_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 点击按钮B（下/右）
            double newValue = Value + SmallChange;
            _isUpdatingValueFromOffset = false;
            _isUpdatingOffsetFromValue = false;
            Value = Math.Min(Maximum, newValue);
        }

        private void OnTrackMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(TrackBackground);
            double newValue = CalculateValueFromPosition(position);

            double currentValueRatio = (Value - Minimum) / (Maximum - Minimum);
            double clickValueRatio = (newValue - Minimum) / (Maximum - Minimum);

            if (clickValueRatio < currentValueRatio)
            {
                // 点击了拖动条前面的轨道
                _isUpdatingValueFromOffset = false;
                _isUpdatingOffsetFromValue = false;
                Value = Math.Max(Minimum, Value - LargeChange);
            }
            else
            {
                // 点击了拖动条后面的轨道
                _isUpdatingValueFromOffset = false;
                _isUpdatingOffsetFromValue = false;
                Value = Math.Min(Maximum, Value + LargeChange);
            }
        }

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                _dragStartValue = Value;

                DragBar.CaptureMouse();
                UpdateCursors();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                var currentPoint = e.GetPosition(this);
                double delta = Orientation == Orientation.Vertical
                    ? currentPoint.Y - _dragStartPoint.Y
                    : currentPoint.X - _dragStartPoint.X;

                double pixelRange = Orientation == Orientation.Vertical
                    ? _trackSize.Height - _dragBarLength
                    : _trackSize.Width - _dragBarLength;

                if (pixelRange > 0)
                {
                    double valueRange = Maximum - Minimum;
                    double valueDelta = (delta / pixelRange) * valueRange;

                    double newValue = _dragStartValue + valueDelta;
                    _isUpdatingValueFromOffset = false;
                    _isUpdatingOffsetFromValue = false;
                    Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDragging && e.ChangedButton == MouseButton.Left)
            {
                _isDragging = false;
                DragBar.ReleaseMouseCapture();
                UpdateCursors();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isDragging)
            {
                _isDragging = false;
                DragBar.ReleaseMouseCapture();
                UpdateCursors();
            }
        }

        private double CalculateValueFromPosition(Point position)
        {
            if (Orientation == Orientation.Vertical)
            {
                double pixelRange = _trackSize.Height - _dragBarLength;
                if (pixelRange <= 0) return Minimum;

                double pixelPosition = Math.Max(0, Math.Min(pixelRange, position.Y));
                double ratio = pixelPosition / pixelRange;
                return Minimum + (Maximum - Minimum) * ratio;
            }
            else
            {
                double pixelRange = _trackSize.Width - _dragBarLength;
                if (pixelRange <= 0) return Minimum;

                double pixelPosition = Math.Max(0, Math.Min(pixelRange, position.X));
                double ratio = pixelPosition / pixelRange;
                return Minimum + (Maximum - Minimum) * ratio;
            }
        }

        private void UpdateCursors()
        {
            if (Orientation == Orientation.Vertical)
            {
                Cursor = _isDragging ? Cursors.ScrollAll : Cursors.Arrow;
                DragBar.Cursor = _isDragging ? Cursors.ScrollAll : Cursors.SizeNS;
                ButtonA.Cursor = Cursors.Hand;
                ButtonB.Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = _isDragging ? Cursors.ScrollAll : Cursors.Arrow;
                DragBar.Cursor = _isDragging ? Cursors.ScrollAll : Cursors.SizeWE;
                ButtonA.Cursor = Cursors.Hand;
                ButtonB.Cursor = Cursors.Hand;
            }
        }

        #endregion

        #region 视觉更新

        private void UpdateVisuals()
        {
            // 更新按钮填充色
            ButtonA.Fill = Fill;
            ButtonB.Fill = Fill;

            // 更新拖动条背景
            DragBar.Background = Fill;
        }

        #endregion
    }
}