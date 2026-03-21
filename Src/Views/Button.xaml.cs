using Auris_Studio.Views.Transitions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TransitionSystem;
using VeloxDev.WPF.PlatformAdapters;
using BrushConverter = VeloxDev.WPF.PlatformAdapters.BrushConverter;

namespace Auris_Studio.Views
{
    [ThemeConfig<BrushConverter, Dark, Light>(nameof(Foreground), [nameof(Brushes.White)], [nameof(Brushes.Black)])]
    [ThemeConfig<BrushConverter, Dark, Light>(nameof(BorderBrush), [nameof(Brushes.White)], [nameof(Brushes.Black)])]
    public partial class Button : UserControl
    {
        private Viewbox? _iconViewbox;
        private Border? _hoverBorder;

        public Button()
        {
            InitializeComponent();
            InitializeTheme();
            Loaded += Button_Loaded;
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= Button_Loaded; // 只执行一次
            // 在 Loaded 时，模板肯定已经应用了
            _iconViewbox = FindVisualChild<Viewbox>(this);
            _hoverBorder = FindVisualChild<Border>(this, "Hover");

            // 如果此时 ButtonContent 已经有值，需要重新渲染
            if (_iconViewbox != null && ButtonContent != null)
            {
                OnButtonContextChanged(this, new DependencyPropertyChangedEventArgs(ButtonContentProperty, null, ButtonContent));
            }
        }

        public event EventHandler<RoutedEventArgs>? Click;

        public object ButtonContent
        {
            get { return (object)GetValue(ButtonContentProperty); }
            set { SetValue(ButtonContentProperty, value); }
        }
        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(object), typeof(Button),
                new PropertyMetadata(OnButtonContextChanged));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(Button),
                new PropertyMetadata(OnCommandChanged));

        public object Parameter
        {
            get { return (object)GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }
        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.Register(nameof(Parameter), typeof(object), typeof(Button),
                new PropertyMetadata());

        public Thickness ContentMargin
        {
            get { return (Thickness)GetValue(ContentMarginProperty); }
            set { SetValue(ContentMarginProperty, value); }
        }
        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register(nameof(ContentMargin), typeof(Thickness), typeof(Button),
                new PropertyMetadata());

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(Button),
                new PropertyMetadata());

        public double BlurRadius
        {
            get { return (double)GetValue(BlurRadiusProperty); }
            set { SetValue(BlurRadiusProperty, value); }
        }
        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(Button),
                new PropertyMetadata());

        public bool UseHoverBackground
        {
            get { return (bool)GetValue(UseHoverBackgroundProperty); }
            set { SetValue(UseHoverBackgroundProperty, value); }
        }
        public static readonly DependencyProperty UseHoverBackgroundProperty =
            DependencyProperty.Register(nameof(UseHoverBackground), typeof(bool), typeof(Button), new PropertyMetadata(true));

        public bool UseHoverForeground
        {
            get { return (bool)GetValue(UseHoverForegroundProperty); }
            set { SetValue(UseHoverForegroundProperty, value); }
        }
        public static readonly DependencyProperty UseHoverForegroundProperty =
            DependencyProperty.Register(nameof(UseHoverForeground), typeof(bool), typeof(Button), new PropertyMetadata(true));

        private static void OnButtonContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Button button)
                return;

            // 1. 优先使用缓存的 _iconViewbox (如果在 Loaded 后)
            var viewbox = button._iconViewbox;

            // 2. 如果缓存为空（例如在构造函数早期触发），尝试实时查找
            if (viewbox == null)
            {
                viewbox = FindVisualChild<Viewbox>(button);
                // 如果还是找不到，说明模板还没好，暂不处理，等待 Loaded 事件去重试
                if (viewbox == null) return;

                // 如果实时找到了，缓存起来
                button._iconViewbox = viewbox;
            }

            // 清空原有内容
            viewbox.Child = null;

            var newValue = e.NewValue;
            if (newValue == null) return;

            // 情况1: NewValue为UIElement
            if (newValue is UIElement uiElement)
            {
                viewbox.Child = uiElement;
                return;
            }

            // 情况2: NewValue为字符串
            if (newValue is string strValue)
            {
                Geometry? geometry = null;
                try
                {
                    geometry = Geometry.Parse(strValue);
                }
                catch { }

                if (geometry != null)
                {
                    var path = new Path
                    {
                        Data = geometry,
                    };
                    var fillBinding = new Binding("Foreground") { Source = button, Mode = BindingMode.OneWay };
                    path.SetBinding(Path.FillProperty, fillBinding);
                    viewbox.Child = path;
                }
                else
                {
                    var textBlock = new TextBlock
                    {
                        Text = strValue
                    };
                    var foregroundBinding = new Binding("Foreground") { Source = button, Mode = BindingMode.OneWay };
                    textBlock.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);
                    viewbox.Child = textBlock;
                }
            }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button && e.NewValue is ICommand command)
            {
                var canExecute = command is null || command.CanExecute(button.Parameter);
                button.Visibility = canExecute ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject obj, string? name = null) where T : FrameworkElement
        {
            if (obj == null) return null;

            if (obj is T currentElement)
            {
                if (string.IsNullOrEmpty(name) || currentElement.Name == name)
                {
                    return currentElement;
                }
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }

            return null;
        }

        private int _mouseDownTime = 0;
        private int _mouseUpTime = 0;

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownTime++;
            CheckClick(e);
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseUpTime++;
            CheckClick(e);
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ClearClick();
            LoadHoverAnimation();
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ClearClick();
            LoadNoHoverAnimation();
        }

        private void CheckClick(MouseButtonEventArgs e)
        {
            if (_mouseDownTime > 0 && _mouseUpTime > 0)
            {
                Click?.Invoke(this, e);
                Command?.Execute(Parameter);
                var canExecute = Command is null || Command.CanExecute(Parameter);
                Visibility = canExecute ? Visibility.Visible : Visibility.Collapsed;
                ClearClick();
            }
        }

        private void ClearClick()
        {
            _mouseDownTime = 0;
            _mouseUpTime = 0;
        }

        private void LoadHoverAnimation()
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                if (UseHoverBackground) ButtonTransitions.DarkHover_Background.Execute(_hoverBorder!);
                if (UseHoverForeground) ButtonTransitions.DarkHover_Foreground.Execute(this);
                return;
            }
            if (UseHoverBackground) ButtonTransitions.LightHover_Background.Execute(_hoverBorder!);
            if (UseHoverForeground) ButtonTransitions.LightHover_Foreground.Execute(this);
        }

        private void LoadNoHoverAnimation()
        {
            if (UseHoverBackground)
                ButtonTransitions.NoHover_Background.Execute(_hoverBorder!);
            if (UseHoverForeground)
                Transition<Button>.Create()
                .Effect(TransitionEffects.Hover)
                .Property(x => x.Foreground, ThemeManager.Current == typeof(Dark) ? Brushes.White : Brushes.Black)
                .Execute(this);
        }
    }
}
