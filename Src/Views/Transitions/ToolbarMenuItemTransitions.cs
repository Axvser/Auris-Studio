using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.WPF.PlatformAdapters;
using BrushConverter = VeloxDev.WPF.PlatformAdapters.BrushConverter;

namespace Auris_Studio.Views.Transitions;

public static class ToolbarMenuItemTransitions
{
    private static readonly BrushConverter brushConverter = new();

    public static readonly Transition<Border>.StateSnapshot DarkHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#2200FFFF"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#14000000"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot NoHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#00000000"]) as Brush);

    public static readonly Transition<ToolbarMenuItem>.StateSnapshot DarkHover_Foreground = Transition<ToolbarMenuItem>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(ToolbarMenuItem.Foreground), ["#00FFFF"]) as Brush);

    public static readonly Transition<ToolbarMenuItem>.StateSnapshot LightHover_Foreground = Transition<ToolbarMenuItem>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(ToolbarMenuItem.Foreground), ["#FF0000"]) as Brush);

    public static readonly Transition<ToolbarMenuItem>.StateSnapshot DarkNoHover_Foreground = Transition<ToolbarMenuItem>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(ToolbarMenuItem.Foreground), [nameof(Brushes.White)]) as Brush);

    public static readonly Transition<ToolbarMenuItem>.StateSnapshot LightNoHover_Foreground = Transition<ToolbarMenuItem>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(ToolbarMenuItem.Foreground), [nameof(Brushes.Black)]) as Brush);
}
