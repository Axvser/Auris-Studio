using Auris_Studio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(PianoKeyBrush), ["#00FFFF"], ["#FFA500"])]
    public partial class PianoSlidingDoorView : UserControl
    {
        public PianoSlidingDoorView()
        {
            InitializeComponent();
            InitializeTheme();
        }

        public Brush PianoKeyBrush
        {
            get { return (Brush)GetValue(PianoKeyBrushProperty); }
            set { SetValue(PianoKeyBrushProperty, value); }
        }
        public static readonly DependencyProperty PianoKeyBrushProperty =
            DependencyProperty.Register(nameof(PianoKeyBrush), typeof(Brush), typeof(PianoSlidingDoorView),
                new PropertyMetadata(Brushes.Cyan));

        private void VerticalScrollBar_OffsetChanged(object? sender, double e)
        {
            NotesScrollViewer.ScrollToVerticalOffset(e);
            PianoKeysScrollViewer.ScrollToVerticalOffset(e);
            BackDrawLineScrollViewer.ScrollToVerticalOffset(e);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportTop = e;
            }
        }

        private void HorizontalScrollBar_OffsetChanged(object sender, double e)
        {
            NotesScrollViewer.ScrollToHorizontalOffset(e);
            PianoKeysScrollViewer.ScrollToHorizontalOffset(e);
            BackDrawLineScrollViewer.ScrollToHorizontalOffset(e);
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportLeft = e;
            }
        }

        private void NotesScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ViewportLeft = HorizontalScrollBar.Offset;
                vm.ViewportTop = VerticalScrollBar.Offset;
                vm.ViewportWidth = e.NewSize.Width;
                vm.ViewportHeight = e.NewSize.Height;
            }
        }
    }
}
