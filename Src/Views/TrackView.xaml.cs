using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.Views.Transitions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TransitionSystem;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Foreground), [nameof(Brushes.White)], [nameof(Brushes.Black)])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(FieldBackground), ["#22FFFFFF"], ["#14000000"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(SecondaryForeground), ["#AAFFFFFF"], ["#99000000"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(PopupBackground), ["#111318"], ["#F7F8FB"])]
    public partial class TrackView : UserControl
    {
        private const string MuteOffIcon = "M128 420.576v200.864h149.12l175.456 140.064V284.288l-169.792 136.288H128z m132.256-64l204.288-163.968a32 32 0 0 1 52.032 24.96v610.432a32 32 0 0 1-51.968 24.992l-209.92-167.552H96a32 32 0 0 1-32-32v-264.864a32 32 0 0 1 32-32h164.256zM752 458.656L870.4 300.8a32 32 0 1 1 51.2 38.4L792 512l129.6 172.8a32 32 0 0 1-51.2 38.4l-118.4-157.856-118.4 157.856a32 32 0 0 1-51.2-38.4l129.6-172.8-129.6-172.8a32 32 0 0 1 51.2-38.4l118.4 157.856z";
        private const string MuteOnIcon = "M257.493333 322.4l215.573334-133.056c24.981333-15.413333 57.877333-7.914667 73.493333 16.746667 5.301333 8.373333 8.106667 18.048 8.106667 27.914666v555.989334C554.666667 819.093333 530.784 842.666667 501.333333 842.666667c-9.994667 0-19.786667-2.773333-28.266666-8L257.493333 701.6H160c-41.237333 0-74.666667-33.013333-74.666667-73.738667V396.138667c0-40.725333 33.429333-73.738667 74.666667-73.738667h97.493333z m26.133334 58.4a32.298667 32.298667 0 0 1-16.96 4.8H160c-5.888 0-10.666667 4.714667-10.666667 10.538667v231.733333c0 5.813333 4.778667 10.538667 10.666667 10.538667h106.666667c5.994667 0 11.872 1.664 16.96 4.8L490.666667 770.986667V253.013333L283.626667 380.8zM800.906667 829.653333a32.288 32.288 0 0 1-45.248-0.757333 31.317333 31.317333 0 0 1 0.768-44.693333c157.653333-150.464 157.653333-393.962667 0-544.426667a31.317333 31.317333 0 0 1-0.768-44.682667 32.288 32.288 0 0 1 45.248-0.757333c183.68 175.306667 183.68 460.010667 0 635.317333z m-106.901334-126.186666a32.288 32.288 0 0 1-45.248-1.216 31.328 31.328 0 0 1 1.237334-44.672c86.229333-80.608 86.229333-210.56 0-291.178667a31.328 31.328 0 0 1-1.237334-44.672 32.288 32.288 0 0 1 45.248-1.216c112.885333 105.546667 112.885333 277.418667 0 382.965333z";
        private static readonly Thickness SoloInactiveBorderThickness = new(0);
        private static readonly Thickness SoloActiveBorderThickness = new(1.5);

        private bool _isHovered;

        public TrackView()
        {
            InitializeComponent();
            InitializeTheme();
            DataContextChanged += TrackView_DataContextChanged;
            Loaded += TrackView_Loaded;
        }

        private void TrackView_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateChannelOptions();
            PopulatePatchOptions();
            if (DataContext is MidiTrackViewModel track)
            {
                RefreshButtonContent(track);
                UpdateVisualState(track);
            }
        }

        public Brush FieldBackground
        {
            get => (Brush)GetValue(FieldBackgroundProperty);
            set => SetValue(FieldBackgroundProperty, value);
        }
        public static readonly DependencyProperty FieldBackgroundProperty =
            DependencyProperty.Register(nameof(FieldBackground), typeof(Brush), typeof(TrackView), new PropertyMetadata(Brushes.Transparent));

        public Brush SecondaryForeground
        {
            get => (Brush)GetValue(SecondaryForegroundProperty);
            set => SetValue(SecondaryForegroundProperty, value);
        }
        public static readonly DependencyProperty SecondaryForegroundProperty =
            DependencyProperty.Register(nameof(SecondaryForeground), typeof(Brush), typeof(TrackView), new PropertyMetadata(Brushes.Gray));

        public Brush PopupBackground
        {
            get => (Brush)GetValue(PopupBackgroundProperty);
            set => SetValue(PopupBackgroundProperty, value);
        }
        public static readonly DependencyProperty PopupBackgroundProperty =
            DependencyProperty.Register(nameof(PopupBackground), typeof(Brush), typeof(TrackView), new PropertyMetadata(Brushes.Transparent));

        partial void OnThemeChanged(Type? oldValue, Type? newValue)
        {
            if (newValue is not null && DataContext is MidiTrackViewModel track)
            {
                RefreshButtonContent(track);
                UpdateVisualState(track);
            }
        }

        private void TrackView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MidiTrackViewModel oldTrack)
            {
                oldTrack.PropertyChanged -= Track_PropertyChanged;
            }
            if (e.NewValue is MidiTrackViewModel newTrack)
            {
                newTrack.PropertyChanged += Track_PropertyChanged;
                RefreshButtonContent(newTrack);
                UpdateVisualState(newTrack);
            }
        }

        private void Track_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not MidiTrackViewModel track)
            {
                return;
            }

            if (e.PropertyName is nameof(MidiTrackViewModel.Selected)
                or nameof(MidiTrackViewModel.Muted)
                or nameof(MidiTrackViewModel.Solo)
                or nameof(MidiTrackViewModel.IsAudible))
            {
                UpdateVisualState(track);
            }

            if (e.PropertyName is nameof(MidiTrackViewModel.Channel)
                or nameof(MidiTrackViewModel.Patch)
                or nameof(MidiTrackViewModel.Muted)
                or nameof(MidiTrackViewModel.Solo))
            {
                if (e.PropertyName == nameof(MidiTrackViewModel.Channel))
                {
                    PopulatePatchOptions();
                }
                RefreshButtonContent(track);
            }
        }

        private void RefreshButtonContent(MidiTrackViewModel track)
        {
            if (FindChannelButton() is Button channelButton)
            {
                channelButton.ButtonContent = $"Channel · {track.Channel} ▾";
            }
            if (FindPatchButton() is Button patchButton)
            {
                string label = track.IsPercussionChannel ? "Drum" : "Patch";
                patchButton.ButtonContent = $"{label} · {track.PatchDisplayName} ▾";
                if (FindPatchLabel() is TextBlock patchLabel)
                {
                    patchLabel.Text = label;
                }
            }
            if (FindMuteButton() is Button muteButton)
            {
                muteButton.ButtonContent = track.Muted ? MuteOffIcon : MuteOnIcon;
            }
            if (FindSoloButton() is Button soloButton)
            {
                soloButton.ButtonContent = "S";
                soloButton.BorderThickness = track.Solo ? SoloActiveBorderThickness : SoloInactiveBorderThickness;
                soloButton.BorderBrush = track.Solo ? Foreground : Brushes.Transparent;
            }
        }

        private void UpdateVisualState(MidiTrackViewModel track)
        {
            if (FindCardBorder() is not Border cardBorder)
            {
                return;
            }

            if (track.Selected)
            {
                ExecuteSelectedTransition(cardBorder);
            }
            else if (_isHovered)
            {
                ExecuteHoverTransition(cardBorder);
            }
            else
            {
                ExecuteNormalTransition(cardBorder);
            }

            Opacity = track.IsAudible ? 1.0 : 0.68;
        }

        private static void ExecuteSelectedTransition(Border border)
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                TrackSurfaceTransitions.DarkSelected_Background.Execute(border);
                return;
            }
            TrackSurfaceTransitions.LightSelected_Background.Execute(border);
        }

        private static void ExecuteHoverTransition(Border border)
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                TrackSurfaceTransitions.DarkHover_Background.Execute(border);
                return;
            }
            TrackSurfaceTransitions.LightHover_Background.Execute(border);
        }

        private static void ExecuteNormalTransition(Border border)
        {
            if (ThemeManager.Current == typeof(Dark))
            {
                TrackSurfaceTransitions.DarkCard_Background.Execute(border);
                return;
            }
            TrackSurfaceTransitions.LightCard_Background.Execute(border);
        }

        private void PopulateChannelOptions()
        {
            if (FindChannelOptionsPanel() is not StackPanel panel)
            {
                return;
            }

            panel.Children.Clear();
            for (int i = 1; i <= 16; i++)
            {
                panel.Children.Add(CreateMenuItem($"Channel {i}", i, ChannelOption_Click, panel.Children.Count > 0));
            }
        }

        private void PopulatePatchOptions()
        {
            if (FindPatchOptionsPanel() is not StackPanel panel)
            {
                return;
            }

            panel.Children.Clear();
            var patches = (DataContext as MidiTrackViewModel)?.AvailablePatches ?? Enum.GetValues<Patch>().Cast<Patch>().Where(patch => !patch.IsDrum(out _));
            foreach (Patch patch in patches)
            {
                panel.Children.Add(CreateMenuItem(MidiTrackViewModel.GetPatchDisplayName(patch), patch, PatchOption_Click, panel.Children.Count > 0));
            }
        }

        private static ToolbarMenuItem CreateMenuItem(string text, object parameter, RoutedEventHandler clickHandler, bool withTopMargin)
        {
            var item = new ToolbarMenuItem
            {
                Text = text,
                Parameter = parameter,
                Margin = withTopMargin ? new Thickness(0, 6, 0, 0) : new Thickness(0),
            };
            item.Click += (_, e) => clickHandler(item, e);
            return item;
        }

        private void CardBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ShouldIgnoreTrackSelection(e.OriginalSource as DependencyObject))
            {
                return;
            }

            if (DataContext is MidiTrackViewModel vm)
            {
                vm.TrackSelectCommand.Execute(null);
            }
        }

        private void CardBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHovered = true;
            if (DataContext is MidiTrackViewModel vm)
            {
                UpdateVisualState(vm);
            }
        }

        private void CardBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;
            if (DataContext is MidiTrackViewModel vm)
            {
                UpdateVisualState(vm);
            }
        }

        private void ChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindChannelPopup() is Popup popup)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void PatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindPatchPopup() is Popup popup)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        private void ChannelOption_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MidiTrackViewModel vm && sender is ToolbarMenuItem item && item.Parameter is int channel)
            {
                vm.Channel = channel;
                RefreshButtonContent(vm);
                if (FindChannelPopup() is Popup popup)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private void PatchOption_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MidiTrackViewModel vm && sender is ToolbarMenuItem item && item.Parameter is Patch patch)
            {
                vm.Patch = patch;
                RefreshButtonContent(vm);
                if (FindPatchPopup() is Popup popup)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiTrackViewModel vm)
            {
                vm.TrackMutedCommand.Execute(null);
                RefreshButtonContent(vm);
                e.Handled = true;
                UpdateVisualState(vm);
            }
        }

        private void Solo_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiTrackViewModel vm)
            {
                vm.TrackSoloCommand.Execute(null);
                RefreshButtonContent(vm);
                e.Handled = true;
                UpdateVisualState(vm);
            }
        }

        private static bool ShouldIgnoreTrackSelection(DependencyObject? originalSource)
            => FindAncestor<Button>(originalSource) is not null
            || FindAncestor<TextBox>(originalSource) is not null
            || FindAncestor<ToolbarMenuItem>(originalSource) is not null;

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current is not null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = current switch
                {
                    Visual visual => VisualTreeHelper.GetParent(visual),
                    FrameworkContentElement contentElement => contentElement.Parent,
                    _ => null,
                };
            }

            return null;
        }

        private Border? FindCardBorder() => FindName("CardBorder") as Border;
        private TextBox? FindNameTextBox() => FindName("NameTextBox") as TextBox;
        private Button? FindSoloButton() => FindName("SoloButton") as Button;
        private Button? FindChannelButton() => FindName("ChannelButton") as Button;
        private Button? FindPatchButton() => FindName("PatchButton") as Button;
        private Button? FindMuteButton() => FindName("MuteButton") as Button;
        private TextBlock? FindChannelLabel() => FindName("ChannelLabel") as TextBlock;
        private TextBlock? FindPatchLabel() => FindName("PatchLabel") as TextBlock;
        private Popup? FindChannelPopup() => FindName("ChannelPopup") as Popup;
        private Popup? FindPatchPopup() => FindName("PatchPopup") as Popup;
        private Border? FindChannelPopupBorder() => FindName("ChannelPopupBorder") as Border;
        private Border? FindPatchPopupBorder() => FindName("PatchPopupBorder") as Border;
        private StackPanel? FindChannelOptionsPanel() => FindName("ChannelOptionsPanel") as StackPanel;
        private StackPanel? FindPatchOptionsPanel() => FindName("PatchOptionsPanel") as StackPanel;
    }
}
