using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Auris_Studio.Views.Workflow;

public partial class BezierCurveView : UserControl
{
    private DateTime _lastRenderTime;
    private double _dashOffset;

    public BezierCurveView()
    {
        InitializeComponent();
        IsHitTestVisible = false;
        Panel.SetZIndex(this, -100);
        _lastRenderTime = DateTime.Now;

        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    #region 依赖属性

    public static readonly DependencyProperty StartLeftProperty =
        DependencyProperty.Register(
            nameof(StartLeft),
            typeof(double),
            typeof(BezierCurveView),
            new PropertyMetadata(0d, OnRenderPropertyChanged));

    public static readonly DependencyProperty StartTopProperty =
        DependencyProperty.Register(
            nameof(StartTop),
            typeof(double),
            typeof(BezierCurveView),
            new PropertyMetadata(0d, OnRenderPropertyChanged));

    public static readonly DependencyProperty EndLeftProperty =
        DependencyProperty.Register(
            nameof(EndLeft),
            typeof(double),
            typeof(BezierCurveView),
            new PropertyMetadata(0d, OnRenderPropertyChanged));

    public static readonly DependencyProperty EndTopProperty =
        DependencyProperty.Register(
            nameof(EndTop),
            typeof(double),
            typeof(BezierCurveView),
            new PropertyMetadata(0d, OnRenderPropertyChanged));

    public static readonly DependencyProperty CanRenderProperty =
        DependencyProperty.Register(
            nameof(CanRender),
            typeof(bool),
            typeof(BezierCurveView),
            new PropertyMetadata(true, OnRenderPropertyChanged));

    public static readonly DependencyProperty IsVirtualProperty =
        DependencyProperty.Register(
            nameof(IsVirtual),
            typeof(bool),
            typeof(BezierCurveView),
            new PropertyMetadata(false, OnRenderPropertyChanged));

    public double StartLeft
    {
        get => (double)GetValue(StartLeftProperty);
        set => SetValue(StartLeftProperty, value);
    }

    public double StartTop
    {
        get => (double)GetValue(StartTopProperty);
        set => SetValue(StartTopProperty, value);
    }

    public double EndLeft
    {
        get => (double)GetValue(EndLeftProperty);
        set => SetValue(EndLeftProperty, value);
    }

    public double EndTop
    {
        get => (double)GetValue(EndTopProperty);
        set => SetValue(EndTopProperty, value);
    }

    public bool CanRender
    {
        get => (bool)GetValue(CanRenderProperty);
        set => SetValue(CanRenderProperty, value);
    }

    public bool IsVirtual
    {
        get => (bool)GetValue(IsVirtualProperty);
        set => SetValue(IsVirtualProperty, value);
    }

    #endregion

    #region 流光效果依赖

    public static readonly DependencyProperty IsFlowEnabledProperty =
        DependencyProperty.Register(
            nameof(IsFlowEnabled),
            typeof(bool),
            typeof(BezierCurveView),
            new PropertyMetadata(false, OnFlowEnabledChanged));

    public static readonly DependencyProperty FlowColorProperty =
        DependencyProperty.Register(
            nameof(FlowColor),
            typeof(Color),
            typeof(BezierCurveView),
            new PropertyMetadata(Colors.Cyan, OnRenderPropertyChanged));

    public static readonly DependencyProperty FlowSpeedProperty =
        DependencyProperty.Register(
            nameof(FlowSpeed),
            typeof(double),
            typeof(BezierCurveView),
            new PropertyMetadata(2.0, OnRenderPropertyChanged));

    public static readonly DependencyProperty FlowDashArrayProperty =
        DependencyProperty.Register(
            nameof(FlowDashArray),
            typeof(DoubleCollection),
            typeof(BezierCurveView),
            new PropertyMetadata(new DoubleCollection([8, 4]), OnRenderPropertyChanged));

    public bool IsFlowEnabled
    {
        get => (bool)GetValue(IsFlowEnabledProperty);
        set => SetValue(IsFlowEnabledProperty, value);
    }

    public Color FlowColor
    {
        get => (Color)GetValue(FlowColorProperty);
        set => SetValue(FlowColorProperty, value);
    }

    public double FlowSpeed
    {
        get => (double)GetValue(FlowSpeedProperty);
        set => SetValue(FlowSpeedProperty, value);
    }

    public DoubleCollection FlowDashArray
    {
        get => (DoubleCollection)GetValue(FlowDashArrayProperty);
        set => SetValue(FlowDashArrayProperty, value);
    }

    #endregion

    #region 属性变更

    private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (BezierCurveView)d;
        control.InvalidateVisual();
    }

    private static void OnFlowEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (BezierCurveView)d;
        control.OnFlowEnabledChanged();
    }

    private void OnFlowEnabledChanged()
    {
        if (IsFlowEnabled)
        {
            _lastRenderTime = DateTime.Now;
            _dashOffset = 0;
            StartAnimationLoop();
        }
        else
        {
            _dashOffset = 0;
            StopAnimationLoop();
            InvalidateVisual();
        }
    }

    private void StartAnimationLoop()
    {
        if (!IsFlowEnabled) return;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopAnimationLoop()
    {
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsFlowEnabled)
        {
            StopAnimationLoop();
            return;
        }

        var currentTime = DateTime.Now;
        var deltaTime = (currentTime - _lastRenderTime).TotalSeconds;
        _lastRenderTime = currentTime;

        _dashOffset -= FlowSpeed * deltaTime * 10;

        // 计算虚线总长度
        double dashLength = 0;
        foreach (double segment in FlowDashArray)
        {
            dashLength += segment;
        }

        // 循环偏移量
        if (dashLength > 0)
        {
            _dashOffset = (_dashOffset % dashLength + dashLength) % dashLength;
        }

        InvalidateVisual();
    }

    #endregion

    #region 渲染

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);

        if (!CanRender)
            return;

        var pathGeometry = CreateBezierGeometry();
        if (pathGeometry == null) return;

        if (IsFlowEnabled)
        {
            DrawFlowEffect(context, pathGeometry);
        }
        else if (IsVirtual)
        {
            DrawVirtualLine(context, pathGeometry);
        }
        else
        {
            DrawNormalLine(context, pathGeometry);
        }
    }

    private void DrawFlowEffect(DrawingContext context, PathGeometry pathGeometry)
    {
        // 计算总虚线长度
        double totalDashLength = 0;
        foreach (double segment in FlowDashArray)
        {
            totalDashLength += segment;
        }

        var glowBrush = new SolidColorBrush(Color.FromArgb(80, FlowColor.R, FlowColor.G, FlowColor.B));
        var glowPen = new Pen(glowBrush, 5);
        double glowOffset = _dashOffset + 3; // 滞后效果
        if (glowOffset >= totalDashLength) glowOffset -= totalDashLength;
        glowPen.DashStyle = new DashStyle(FlowDashArray, glowOffset);
        context.DrawGeometry(null, glowPen, pathGeometry);

        // 中层：主流光
        var mainBrush = new SolidColorBrush(FlowColor);
        var mainPen = new Pen(mainBrush, 3)
        {
            DashStyle = new DashStyle(FlowDashArray, _dashOffset)
        };
        context.DrawGeometry(null, mainPen, pathGeometry);

        // 上层：高光效果（稍微超前）
        var highlightBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
        var highlightPen = new Pen(highlightBrush, 1);
        double highlightOffset = _dashOffset - 2; // 超前效果
        if (highlightOffset < 0) highlightOffset += totalDashLength;
        highlightPen.DashStyle = new DashStyle(FlowDashArray, highlightOffset);
        context.DrawGeometry(null, highlightPen, pathGeometry);

        // 绘制流动指示箭头
        DrawFlowArrow(context, pathGeometry);
    }

    private void DrawFlowArrow(DrawingContext context, PathGeometry pathGeometry)
    {
        if (pathGeometry.IsEmpty() || pathGeometry.Figures.Count == 0)
            return;

        var pathFigure = pathGeometry.Figures[0];
        if (pathFigure.Segments.Count == 0 || pathFigure.Segments[0] is not BezierSegment bezierSegment)
            return;

        // 在曲线的终点绘制箭头（表示从起点流向终点）
        Point arrowTip = bezierSegment.Point3;

        // 计算曲线在终点的切线方向
        Vector tangentDirection = new(
            bezierSegment.Point3.X - bezierSegment.Point2.X,
            bezierSegment.Point3.Y - bezierSegment.Point2.Y);

        if (tangentDirection.LengthSquared < double.Epsilon)
        {
            tangentDirection = new Vector(1, 0);
        }
        else
        {
            tangentDirection.Normalize();
        }

        // 绘制箭头
        double arrowLength = 10;
        double arrowWidth = 6;

        Point arrowBase = new(
            arrowTip.X - tangentDirection.X * arrowLength,
            arrowTip.Y - tangentDirection.Y * arrowLength);

        Vector perpendicular = new(-tangentDirection.Y, tangentDirection.X);
        Point arrowWing1 = new(
            arrowBase.X + perpendicular.X * arrowWidth / 2,
            arrowBase.Y + perpendicular.Y * arrowWidth / 2);
        Point arrowWing2 = new(
            arrowBase.X - perpendicular.X * arrowWidth / 2,
            arrowBase.Y - perpendicular.Y * arrowWidth / 2);

        StreamGeometry arrowGeometry = new();
        using (var geometryContext = arrowGeometry.Open())
        {
            geometryContext.BeginFigure(arrowTip, true, true);
            geometryContext.LineTo(arrowWing1, true, false);
            geometryContext.LineTo(arrowWing2, true, false);
        }
        arrowGeometry.Freeze();

        var arrowBrush = new SolidColorBrush(FlowColor);
        var arrowPen = new Pen(arrowBrush, 1);
        context.DrawGeometry(arrowBrush, arrowPen, arrowGeometry);
    }

    private void DrawVirtualLine(DrawingContext context, PathGeometry pathGeometry)
    {
        var brush = new SolidColorBrush(FlowColor);
        var pen = new Pen(brush, 2)
        {
            DashStyle = DashStyles.Dash
        };
        context.DrawGeometry(null, pen, pathGeometry);
    }

    private void DrawNormalLine(DrawingContext context, PathGeometry pathGeometry)
    {
        var brush = new SolidColorBrush(FlowColor);
        var pen = new Pen(brush, 2);
        context.DrawGeometry(null, pen, pathGeometry);

        DrawArrowhead(context, pathGeometry, new Pen(Brushes.Red, 2));
    }

    private PathGeometry CreateBezierGeometry()
    {
        var diffx = EndLeft - StartLeft;
        var diffy = EndTop - StartTop;

        var cp1 = new Point(StartLeft + diffx * 0.618, StartTop + diffy * 0.1);
        var cp2 = new Point(EndLeft - diffx * 0.618, EndTop - diffy * 0.1);

        var pathFigure = new PathFigure
        {
            StartPoint = new Point(StartLeft, StartTop),
            IsClosed = false
        };

        var bezierSegment = new BezierSegment
        {
            Point1 = cp1,
            Point2 = cp2,
            Point3 = new Point(EndLeft, EndTop)
        };

        pathFigure.Segments.Add(bezierSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    #endregion

    #region 箭头绘制

    private static void DrawArrowhead(DrawingContext context, PathGeometry pathGeometry, Pen pen)
    {
        if (pathGeometry == null || pathGeometry.IsEmpty() || pathGeometry.Figures.Count == 0)
            return;

        var pathFigure = pathGeometry.Figures[0];
        if (pathFigure.Segments.Count == 0 || pathFigure.Segments[0] is not BezierSegment bezierSegment)
            return;

        Point arrowTip = bezierSegment.Point3;

        Vector tangentDirection = new(
            bezierSegment.Point3.X - bezierSegment.Point2.X,
            bezierSegment.Point3.Y - bezierSegment.Point2.Y);

        if (tangentDirection.LengthSquared < double.Epsilon)
        {
            tangentDirection = new Vector(1, 0);
        }
        else
        {
            tangentDirection.Normalize();
        }

        double arrowLength = 12 + pen.Thickness * 2;
        double arrowWidth = 8 + pen.Thickness * 1.5;

        Vector perpendicular = new(-tangentDirection.Y, tangentDirection.X);

        Point arrowHeadBase = arrowTip - tangentDirection * arrowLength;
        Point arrowWing1 = arrowHeadBase + perpendicular * arrowWidth / 2;
        Point arrowWing2 = arrowHeadBase - perpendicular * arrowWidth / 2;

        StreamGeometry arrowGeometry = new();
        using (var geometryContext = arrowGeometry.Open())
        {
            geometryContext.BeginFigure(arrowTip, true, true);
            geometryContext.LineTo(arrowWing1, true, false);
            geometryContext.LineTo(arrowWing2, true, false);
        }
        arrowGeometry.Freeze();

        context.DrawGeometry(pen.Brush, null, arrowGeometry);
        context.DrawGeometry(null, pen, arrowGeometry);
    }

    #endregion

    #region 清理资源

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);

        if (VisualParent == null)
        {
            StopAnimationLoop();
        }
    }

    #endregion
}
