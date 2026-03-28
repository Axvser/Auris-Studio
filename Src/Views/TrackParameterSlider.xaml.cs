using Auris_Studio.Views.Transitions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TransitionSystem;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views;
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(Foreground), [nameof(Brushes.White)], [nameof(Brushes.Black)])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(CardBackground), ["#16181D"], ["#FAFAFC"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(TrackBrush), ["#30FFFFFF"], ["#16000000"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(SecondaryForeground), ["#AAFFFFFF"], ["#99000000"])]
public partial class TrackParameterSlider : UserControl
{
    private bool _isHovered;

    public TrackParameterSlider()
    {
        InitializeComponent();
        InitializeTheme();
        Loaded += TrackParameterSlider_Loaded;
    }

    private void TrackParameterSlider_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVisualState();
    }

    partial void OnThemeChanged(Type? oldValue, Type? newValue)
    {
        if (newValue is not null)
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (FindCardBorder() is not Border cardBorder)
        {
            return;
        }

        if (_isHovered)
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                TrackSurfaceTransitions.DarkHover_Background.Execute(cardBorder);
            }
            else
            {
                TrackSurfaceTransitions.LightHover_Background.Execute(cardBorder);
            }
            return;
        }

        if (ThemeManager.Current == typeof(Dark))
        {
            TrackSurfaceTransitions.DarkCard_Background.Execute(cardBorder);
        }
        else
        {
            TrackSurfaceTransitions.LightCard_Background.Execute(cardBorder);
        }
    }

    private void CardBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = true;
        UpdateVisualState();
    }

    private void CardBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = false;
        UpdateVisualState();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(TrackParameterSlider), new PropertyMetadata(string.Empty));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(TrackParameterSlider), new PropertyMetadata(0d));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(TrackParameterSlider), new PropertyMetadata(0d));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(TrackParameterSlider), new PropertyMetadata(127d));

    public Brush CardBackground
    {
        get => (Brush)GetValue(CardBackgroundProperty);
        set => SetValue(CardBackgroundProperty, value);
    }
    public static readonly DependencyProperty CardBackgroundProperty =
        DependencyProperty.Register(nameof(CardBackground), typeof(Brush), typeof(TrackParameterSlider), new PropertyMetadata(Brushes.Transparent));

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }
    public static readonly DependencyProperty TrackBrushProperty =
        DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(TrackParameterSlider), new PropertyMetadata(Brushes.Transparent));

    public Brush SecondaryForeground
    {
        get => (Brush)GetValue(SecondaryForegroundProperty);
        set => SetValue(SecondaryForegroundProperty, value);
    }
    public static readonly DependencyProperty SecondaryForegroundProperty =
        DependencyProperty.Register(nameof(SecondaryForeground), typeof(Brush), typeof(TrackParameterSlider), new PropertyMetadata(Brushes.Gray));

    private Border? FindCardBorder() => FindName("CardBorder") as Border;
}
