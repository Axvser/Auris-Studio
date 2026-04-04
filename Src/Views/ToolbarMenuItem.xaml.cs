using Auris_Studio.Views.Transitions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TransitionSystem;

namespace Auris_Studio.Views
{
    public partial class ToolbarMenuItem : UserControl
    {
        private bool _hovered;

        public ToolbarMenuItem()
        {
            InitializeComponent();
            Loaded += ToolbarMenuItem_Loaded;
        }

        public event EventHandler<RoutedEventArgs>? Click;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ToolbarMenuItem), new PropertyMetadata(string.Empty));

        public object? Parameter
        {
            get => GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }
        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.Register(nameof(Parameter), typeof(object), typeof(ToolbarMenuItem), new PropertyMetadata(null));

        private void HoverLayer_MouseEnter(object sender, MouseEventArgs e)
        {
            _hovered = true;
            LoadHoverAnimation();
        }

        private void HoverLayer_MouseLeave(object sender, MouseEventArgs e)
        {
            _hovered = false;
            LoadNoHoverAnimation();
        }

        private void HoverLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Click?.Invoke(this, new RoutedEventArgs());
        }

        private void ToolbarMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            Foreground = ThemeManager.Current == typeof(Dark) ? Brushes.White : Brushes.Black;
            LoadNoHoverAnimation();
        }

        private void LoadHoverAnimation()
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                ToolbarMenuItemTransitions.DarkHover_Background.Execute(HoverLayer);
                ToolbarMenuItemTransitions.DarkHover_Foreground.Execute(this);
                return;
            }

            ToolbarMenuItemTransitions.LightHover_Background.Execute(HoverLayer);
            ToolbarMenuItemTransitions.LightHover_Foreground.Execute(this);
        }

        private void LoadNoHoverAnimation()
        {
            ToolbarMenuItemTransitions.NoHover_Background.Execute(HoverLayer);

            if (ThemeManager.Current == typeof(Dark))
            {
                ToolbarMenuItemTransitions.DarkNoHover_Foreground.Execute(this);
                return;
            }

            ToolbarMenuItemTransitions.LightNoHover_Foreground.Execute(this);
        }
    }
}
