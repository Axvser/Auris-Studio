using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TimeLine;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views.Decorators
{
    [MonoBehaviour]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(OBrush), [nameof(Brushes.Violet)], [nameof(Brushes.Red)])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(IBrush), [nameof(Brushes.White)], [nameof(Brushes.Violet)])]
    public partial class WaitingView : UserControl
    {
        public static WaitingView? Instance { get; private set; }

        public WaitingView()
        {
            Instance = this;
            Visibility = Visibility.Hidden;
            InitializeComponent();
            InitializeTheme();
            SizeChanged += (s, e) =>
            {
                rotateO.CenterX = ActualHeight / 2;
                rotateO.CenterY = ActualHeight / 2;
                rotateI.CenterX = ActualHeight / 2;
                rotateI.CenterY = ActualHeight / 2;
                FontSize = ActualHeight / 10;
            };
            Loaded += (s, e) =>
            {
                rotateO.CenterX = ActualHeight / 2;
                rotateO.CenterY = ActualHeight / 2;
                rotateI.CenterX = ActualHeight / 2;
                rotateI.CenterY = ActualHeight / 2;
                FontSize = ActualHeight / 10;
                OChildTransform = rotateO;
                IChildTransform = rotateI;
            };
        }

        public Transform OChildTransform
        {
            get { return (Transform)GetValue(OChildTransformProperty); }
            set { SetValue(OChildTransformProperty, value); }
        }
        public static readonly DependencyProperty OChildTransformProperty =
            DependencyProperty.Register("OChildTransform", typeof(Transform), typeof(WaitingView), new PropertyMetadata(Transform.Identity));
        public Transform IChildTransform
        {
            get { return (Transform)GetValue(IChildTransformProperty); }
            set { SetValue(IChildTransformProperty, value); }
        }
        public static readonly DependencyProperty IChildTransformProperty =
            DependencyProperty.Register("IChildTransform", typeof(Transform), typeof(WaitingView), new PropertyMetadata(Transform.Identity));
        public Brush OBrush
        {
            get { return (Brush)GetValue(OBrushProperty); }
            set { SetValue(OBrushProperty, value); }
        }
        public static readonly DependencyProperty OBrushProperty =
            DependencyProperty.Register("OBrush", typeof(Brush), typeof(WaitingView), new PropertyMetadata(Brushes.Transparent));
        public Brush IBrush
        {
            get { return (Brush)GetValue(IBrushProperty); }
            set { SetValue(IBrushProperty, value); }
        }
        public static readonly DependencyProperty IBrushProperty =
            DependencyProperty.Register("IBrush", typeof(Brush), typeof(WaitingView), new PropertyMetadata(Brushes.Transparent));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(WaitingView), new PropertyMetadata(string.Empty));

        public bool IsWaiting
        {
            get { return (bool)GetValue(IsWaitingProperty); }
            set { SetValue(IsWaitingProperty, value); }
        }
        public static readonly DependencyProperty IsWaitingProperty =
            DependencyProperty.Register(nameof(IsWaiting), typeof(bool), typeof(WaitingView), new PropertyMetadata(false, OnIsWaitingChanged));

        private static void OnIsWaitingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaitingView view && e.NewValue is bool value)
            {
                if (value)
                {
                    MonoBehaviourManager.RegisterBehaviour(view);
                    view.Visibility = Visibility.Visible;
                }
                else
                {
                    MonoBehaviourManager.UnregisterBehaviour(view);
                    view.Visibility = Visibility.Hidden;
                }
            }
        }

        private readonly RotateTransform rotateO = new(0, 0, 0);
        private readonly RotateTransform rotateI = new(0, 0, 0);

        private readonly double opacitydelta = 0.01;
        private double textopacitydirection = 1;

        partial void Update(FrameEventArgs e)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                rotateO.Angle += 1;
                rotateI.Angle -= 4;
                TextView.Opacity += textopacitydirection * opacitydelta;
            });
        }

        partial void LateUpdate(FrameEventArgs e)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                textopacitydirection = textopacitydirection > 0 ?
                    (TextView.Opacity >= 1 ? -1 : 1)
                    :
                    (TextView.Opacity <= 0 ? 1 : -1);
            });
        }
    }
}