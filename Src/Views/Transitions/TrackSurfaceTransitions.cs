using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.TransitionSystem;
using VeloxDev.WPF.PlatformAdapters;
using BrushConverter = VeloxDev.WPF.PlatformAdapters.BrushConverter;

namespace Auris_Studio.Views.Transitions;

public static class TrackSurfaceTransitions
{
    private static readonly BrushConverter brushConverter = new();

    public static readonly Transition<Border>.StateSnapshot DarkCard_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#16181D"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightCard_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#FAFAFC"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot DarkHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#1C2027"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightHover_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#F2F4F8"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot DarkSelected_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#2200FFFF"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightSelected_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#16000000"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot DarkPanel_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#111318"]) as Brush);

    public static readonly Transition<Border>.StateSnapshot LightPanel_Background = Transition<Border>.Create()
        .Effect(TransitionEffects.Hover)
        .Property(x => x.Background, brushConverter.Convert(typeof(Brush), nameof(Border.Background), ["#F7F8FB"]) as Brush);
}
