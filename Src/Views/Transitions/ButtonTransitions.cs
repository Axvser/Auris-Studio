using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.WPF.PlatformAdapters;
using BrushConverter = VeloxDev.WPF.PlatformAdapters.BrushConverter;

namespace Auris_Studio.Views.Transitions;

public static class ButtonTransitions
{
    private static readonly BrushConverter brushConverter = new();

    public static readonly Transition<Border>.StateSnapshot DarkHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#44FFFFFF"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#44000000"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot NoHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#00000000"]) as Brush);

    public static readonly Transition<Button>.StateSnapshot DarkHover_Foreground = Transition<Button>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(Button.Foreground), ["#00FFFF"]) as Brush);

    public static readonly Transition<Button>.StateSnapshot LightHover_Foreground = Transition<Button>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Foreground, brushConverter.Convert(typeof(Brush), nameof(Button.Foreground), ["#FF0000"]) as Brush);
}
