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
        private const string MuteOffIcon = "M96 352H32c-17.7 0-32 14.3-32 32v256c0 17.7 14.3 32 32 32h64l160 128V224L96 352zm264.6 150.6c12.5-12.5 12.5-32.8 0-45.3s-32.8-12.5-45.3 0L256 516.7v-9.4c0-35.7 20.2-68.3 52.1-84.3 15.5-7.8 21.8-26.6 14-42.1s-26.6-21.8-42.1-14C226.8 393.5 192 447.4 192 507.3V576l-59.3-59.3c-12.5-12.5-32.8-12.5-45.3 0s-12.5 32.8 0 45.3l128 128c12.5 12.5 32.8 12.5 45.3 0l128-128zm257.9-246.1c-14.2-10.6-34.3-7.7-44.8 6.5s-7.7 34.3 6.5 44.8C631 345.4 672 417.8 672 496s-41 150.6-91.8 194.2c-14.2 10.6-17.1 30.7-6.5 44.8 10.6 14.2 30.7 17.1 44.8 6.5C683.4 687.6 736 594.5 736 496s-52.6-191.6-117.5-239.5z";
        private const string MuteOnIcon = "M96 352H32c-17.7 0-32 14.3-32 32v256c0 17.7 14.3 32 32 32h64l160 128V224L96 352zm626.7-53.3c-12.5-12.5-32.8-12.5-45.3 0L544 432 410.7 298.7c-12.5-12.5-32.8-12.5-45.3 0s-12.5 32.8 0 45.3L498.7 477.3 365.4 610.7c-12.5 12.5-12.5 32.8 0 45.3s32.8 12.5 45.3 0L544 522.7 677.3 656c12.5 12.5 32.8 12.5 45.3 0s12.5-32.8 0-45.3L589.3 477.3 722.7 344c12.5-12.5 12.5-32.8 0-45.3z";
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

            if (e.PropertyName is nameof(MidiTrackViewModel.Selected) or nameof(MidiTrackViewModel.Muted))
            {
                UpdateVisualState(track);
            }

            if (e.PropertyName is nameof(MidiTrackViewModel.Channel) or nameof(MidiTrackViewModel.Patch))
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
                muteButton.ButtonContent = track.Muted ? MuteOnIcon : MuteOffIcon;
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

            Opacity = track.Muted ? 0.68 : 1.0;
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
                UpdateVisualState(vm);
            }
        }

        private Border? FindCardBorder() => FindName("CardBorder") as Border;
        private TextBox? FindNameTextBox() => FindName("NameTextBox") as TextBox;
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
